// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Catalog;
using Elastic.Documentation.ReleaseNotes;
using Nullean.Entities;
using YamlDotNet.Core;

namespace Elastic.Documentation.Configuration.ReleaseNotes;

/// <summary>Finding severity level.</summary>
public enum FindingSeverity
{
	Error,
	Warning
}

/// <summary>A validation finding for a changelog entry file.</summary>
/// <param name="File">The entry file the finding is about.</param>
/// <param name="Severity">Error or warning.</param>
/// <param name="Message">The finding, worded as the validate stage pins it.</param>
/// <param name="RuleId">The id of the constraint that covers the same problem (<c>ChangelogEntryConstraints</c>, <c>ChangelogEntryInConfigurationConstraints</c>), when one does: it is reported under it.</param>
public record EntryFileFinding(string File, FindingSeverity Severity, string Message, string? RuleId = null);

/// <summary>An entry file's validation: every finding, and the product ids the file names (for the PR existence check, which needs the repository each belongs to).</summary>
public sealed record EntryFileValidation(IReadOnlyList<EntryFileFinding> Findings, IReadOnlyList<string> ProductIds);

/// <summary>
/// The authoring-stage rule engine for changelog entry files (<c>validate-entries</c>). It reads the file's own shape, which a
/// <see cref="ChangelogEntry"/> no longer carries (an unrecognized type, a <c>versions</c> list on an entry, a source-redirect on an authored file,
/// the raw lifecycle text), so it lives here beside the loader, the only code that touches the DTO. The rules a record can answer for are
/// <see cref="ChangelogEntryConstraints"/>; their messages differ from the ones this pins, so this stays the validate stage's own report.
/// </summary>
public static class ChangelogEntryValidator
{
	/// <summary>
	/// Parses <paramref name="yaml"/> and validates it. A parse failure is a single error finding (<c>YAML parse error: ...</c>) and no product ids;
	/// an empty file has no findings.
	/// </summary>
	public static EntryFileValidation Check(
		string filePath,
		string yaml,
		ChangelogConfiguration config,
		ChangelogEntryType? labelDerivedType,
		IReadOnlySet<string>? knownProducts,
		int? filenamePrNumber = null,
		ProductCatalog? products = null
	)
	{
		try
		{
			var dto = ReleaseNotesSerialization.ParseEntryDto(yaml);
			if (dto is null)
				return new EntryFileValidation([], []);
			var findings = Validate(filePath, dto, config, labelDerivedType, knownProducts, filenamePrNumber);
			if (products is not null)
				findings = [.. findings, .. InConfiguration(filePath, yaml, config, products)];
			var productIds = dto.Products?.Select(p => p.Product).OfType<string>().Where(id => !string.IsNullOrWhiteSpace(id)).ToList()
				?? [];
			return new EntryFileValidation(findings, productIds);
		}
		catch (YamlException ex)
		{
			return new EntryFileValidation([new EntryFileFinding(filePath, FindingSeverity.Error, $"YAML parse error: {ex.Message}")], []);
		}
	}

	/// <summary>
	/// The rules of <see cref="ChangelogEntryValidateConstraints.InConfiguration"/>: the ones the file's own text cannot answer (a recognized type the
	/// configuration does not list, a product outside <c>products.available</c>), asked of the entry the file reads as. The rest of the
	/// configuration rules are reported above, from the text, under their rule ids with the validate stage's pinned wording.
	/// </summary>
	private static IEnumerable<EntryFileFinding> InConfiguration(
		string filePath,
		string yaml,
		ChangelogConfiguration config,
		ProductCatalog products
	)
	{
		var entry = ReleaseNotesLoader.LoadEntry(yaml, Path.GetFileName(filePath), filePath).Value;
		if (entry is null)
			return [];
		var report = ChangelogEntryValidateConstraints.InConfiguration.Report(
			new ChangelogEntryInConfiguration(entry, new ChangelogConfigurationInCatalog(config, products))
		);
		return report.Violations.Select(
			v => new EntryFileFinding(
				filePath,
				v.Severity == Nullean.Entities.Severity.Error ? FindingSeverity.Error : FindingSeverity.Warning,
				v.Message,
				v.ConstraintId
			)
		);
	}

	/// <summary>Validates the file's text: <see cref="Check"/> without the product ids, an empty file validated as an entry with nothing in it.</summary>
	public static IReadOnlyList<EntryFileFinding> Validate(
		string filePath,
		string yaml,
		ChangelogConfiguration config,
		ChangelogEntryType? labelDerivedType,
		IReadOnlySet<string>? knownProducts,
		int? filenamePrNumber = null
	) =>
		Validate(
			filePath,
			ReleaseNotesSerialization.ParseEntryDto(yaml) ?? new ChangelogEntryDto(),
			config,
			labelDerivedType,
			knownProducts,
			filenamePrNumber
		);

	/// <summary>
	/// Validates a parsed entry against <paramref name="config"/>
	/// and optionally against a <paramref name="labelDerivedType"/> from the PR labels.
	/// </summary>
	/// <param name="filePath">Repo-relative path used in finding messages.</param>
	/// <param name="entry">The deserialized DTO.</param>
	/// <param name="config">Changelog configuration.</param>
	/// <param name="labelDerivedType">
	/// Type derived from PR labels — null when no label context is available (local runs).
	/// When non-null a missing or mismatching <c>type:</c> produces a label-aware message.
	/// </param>
	/// <param name="knownProducts">
	/// Valid product IDs (already normalised to lower-kebab-case) from products.yml.
	/// Null means skip product membership check.
	/// </param>
	private static IReadOnlyList<EntryFileFinding> Validate(
		string filePath,
		ChangelogEntryDto entry,
		ChangelogConfiguration config,
		ChangelogEntryType? labelDerivedType,
		IReadOnlySet<string>? knownProducts,
		int? filenamePrNumber = null
	)
	{
		var findings = new List<EntryFileFinding>();

		// ── Marker hygiene ───────────────────────────────────────────────────────────────────────
		var isMarker = entry.Link is not null;
		if (isMarker)
		{
			// A link: marker must carry nothing else.
			var hasOtherFields = !string.IsNullOrWhiteSpace(entry.Title)
				|| entry.Type is not null
				|| entry.Products is { Count: > 0 }
				|| entry.Prs is { Count: > 0 }
				|| entry.Pr is not null
				|| entry.Issues is { Count: > 0 };
			if (hasOtherFields)
				findings.Add(
					Error(
						filePath,
						"marker entries (link:) must not contain other fields such as title, type, products, prs, or issues",
						ChangelogEntryConstraints.MarkerCarriesOnlyLink
					)
				);

			// source-redirect check still applies
			if (entry.SourceRedirect is true)
				findings.Add(Error(filePath, "source-redirect is written by the scrubber and must not appear in authored files"));

			// For markers we skip all other field-level rules.
			return findings;
		}

		// ── source-redirect ──────────────────────────────────────────────────────────────────────
		if (entry.SourceRedirect is true)
			findings.Add(Error(filePath, "source-redirect is written by the scrubber and must not appear in authored files"));

		// ── Title ────────────────────────────────────────────────────────────────────────────────
		if (string.IsNullOrWhiteSpace(entry.Title))
			findings.Add(Error(filePath, "title is required", ChangelogEntryConstraints.EntryTitleRequired));
		else if (entry.Title.Length > 80)
			findings.Add(
				Warning(filePath, $"title exceeds 80 characters (current: {entry.Title.Length})", ChangelogEntryConstraints.EntryTitleMax80)
			);

		// ── Description ──────────────────────────────────────────────────────────────────────────
		if (entry.Description is not null && entry.Description.Length > 600)
			findings.Add(
				Warning(
					filePath,
					$"description exceeds 600 characters (current: {entry.Description.Length})",
					ChangelogEntryConstraints.EntryDescriptionMax600
				)
			);

		// ── Products ─────────────────────────────────────────────────────────────────────────────
		if (entry.Products is null || entry.Products.Count == 0)
			findings.Add(Error(filePath, "products is required", ChangelogEntryConstraints.EntryProductsRequired));
		else
		{
			for (var i = 0; i < entry.Products.Count; i++)
			{
				var product = entry.Products[i];
				if (string.IsNullOrWhiteSpace(product.Product))
					findings.Add(Error(filePath, $"products[{i}].product is required", ChangelogEntryConstraints.EntryProductIdRequired));
				else if (knownProducts is not null)
				{
					var normalized = ProductNames.Normalize(product.Product);
					if (!knownProducts.Contains(normalized))
					{
						var available = string.Join(", ", knownProducts.OrderBy(p => p));
						findings.Add(
							Error(
								filePath,
								$"product '{product.Product}' is not in the list of available products from config/products.yml. Available products: {available}",
								ChangelogEntryInConfigurationConstraints.EntryProductsKnown
							)
						);
					}
				}

				// versions: not allowed on entries
#pragma warning disable CS0618
				if (product.Versions is { Count: > 0 })
					findings.Add(Error(filePath, "products[].versions is only valid in changelog note files (note-*.yaml)"));

				// obsolete target:
				if (product.Target is not null)
					findings.Add(
						Warning(
							filePath,
							"products[].target is obsolete; use products[].versions in a note file",
							ChangelogEntryConstraints.EntryProductTargetObsolete
						)
					);
#pragma warning restore CS0618

				// lifecycle
				if (!string.IsNullOrWhiteSpace(product.Lifecycle))
				{
					var availableLifecycles = config.Lifecycles.Select(l => l.ToStringFast(true)).ToList();
					if (
						!LifecycleExtensions.TryParse(product.Lifecycle, out _, ignoreCase: true, allowMatchingMetadataAttribute: true)
						|| !availableLifecycles.Contains(product.Lifecycle, StringComparer.OrdinalIgnoreCase)
					)
					{
						findings.Add(
							Error(
								filePath,
								$"lifecycle '{product.Lifecycle}' is not valid; expected one of: {string.Join(", ", availableLifecycles)}",
								ChangelogEntryInConfigurationConstraints.EntryLifecycleAllowed
							)
						);
					}
				}
			}
		}

		// ── Type ─────────────────────────────────────────────────────────────────────────────────
		var parsedEntryType = string.IsNullOrEmpty(entry.Type)
			? ChangelogEntryType.Invalid
			: ChangelogEntryTypeExtensions.TryParse(entry.Type, out var t, ignoreCase: true, allowMatchingMetadataAttribute: true)
				? t
				: ChangelogEntryType.Invalid;
		if (string.IsNullOrWhiteSpace(entry.Type) || parsedEntryType == ChangelogEntryType.Invalid)
		{
			if (string.IsNullOrWhiteSpace(entry.Type))
			{
				// Missing
				if (labelDerivedType.HasValue)
				{
					var labelTypeName = labelDerivedType.Value.ToStringFast(true);
					findings.Add(
						Error(
							filePath,
							$"type is omitted; add `type: {labelTypeName}` to match this PR's label",
							ChangelogEntryConstraints.EntryTypeRequired
						)
					);
				}
				else
					findings.Add(Error(filePath, "type is required", ChangelogEntryConstraints.EntryTypeRequired));
			}
			else
			{
				// Present but unrecognised
				findings.Add(
					Error(
						filePath,
						$"type '{entry.Type}' is not recognised; valid values: {string.Join(", ", config.Types)}",
						ChangelogEntryConstraints.EntryTypeRequired
					)
				);
			}
		}
		else
		{
			// Present and valid — check against label-derived type
			if (labelDerivedType.HasValue && parsedEntryType != labelDerivedType.Value)
			{
				var entryTypeName = parsedEntryType.ToStringFast(true);
				var labelTypeName = labelDerivedType.Value.ToStringFast(true);
				findings.Add(Error(filePath, $"type '{entryTypeName}' does not match label-derived type '{labelTypeName}'"));
			}
		}

		// ── Subtype ──────────────────────────────────────────────────────────────────────────────
		if (!string.IsNullOrWhiteSpace(entry.Subtype))
		{
			if (!config.SubTypes.Contains(entry.Subtype, StringComparer.OrdinalIgnoreCase))
				findings.Add(
					Error(
						filePath,
						$"subtype '{entry.Subtype}' is not valid; expected one of: {string.Join(", ", config.SubTypes)}",
						ChangelogEntryInConfigurationConstraints.EntrySubtypeAllowed
					)
				);
			else if (parsedEntryType is not (ChangelogEntryType.BreakingChange or ChangelogEntryType.Invalid))
				findings.Add(
					Warning(
						filePath,
						"subtype is only expected on breaking-change entries",
						ChangelogEntryConstraints.EntrySubtypeOnlyBreakingChange
					)
				);
		}

		// ── pr: field (optional) ─────────────────────────────────────────────────────────────────
		// If pr: is supplied it must match the PR number encoded in the filename.
		if (!string.IsNullOrWhiteSpace(entry.Pr) && filenamePrNumber.HasValue)
		{
			var rawPr = entry.Pr.Trim().TrimStart('#');
			if (int.TryParse(rawPr, out var declaredPr))
			{
				if (declaredPr != filenamePrNumber.Value)
					findings.Add(
						Error(
							filePath,
							$"pr: {declaredPr} does not match the PR number in the filename ({filenamePrNumber.Value})",
							ChangelogEntryConstraints.EntryPrMatchesFileName
						)
					);
			}
			else
			{
				findings.Add(
					Error(
						filePath,
						$"pr: '{entry.Pr}' could not be parsed as a PR number",
						ChangelogEntryConstraints.EntryPrMatchesFileName
					)
				);
			}
		}

		// ── Areas ────────────────────────────────────────────────────────────────────────────────
		if (config.Areas is { Count: > 0 } && entry.Areas is { Count: > 0 })
		{
			foreach (var area in entry.Areas)
			{
				if (!config.Areas.Contains(area, StringComparer.OrdinalIgnoreCase))
					findings.Add(
						Error(
							filePath,
							$"area '{area}' is not valid; expected one of: {string.Join(", ", config.Areas)}",
							ChangelogEntryInConfigurationConstraints.EntryAreasAllowed
						)
					);
			}
		}

		return findings;
	}

	/// <summary>
	/// Validates the filename convention: must be <c>&lt;digits&gt;[-&lt;name&gt;].yaml|yml</c>.
	/// </summary>
	public static IReadOnlyList<EntryFileFinding> ValidateFilename(string filePath)
	{
		var filename = Path.GetFileName(filePath);
		if (!TryParseFilenameAsPrNumber(filePath, out _))
			return [
				Error(
					filePath,
					$"filename '{filename}' must start with a PR number (e.g. 14-my-feature.yaml or 14.yaml)",
					ChangelogEntryConstraints.EntryFileNameStartsWithPr
				)
			];
		return [];
	}

	/// <summary>
	/// Tries to parse the leading PR number from a changelog entry filename.
	/// Valid formats: <c>&lt;digits&gt;.yaml</c>, <c>&lt;digits&gt;-&lt;name&gt;.yaml</c>.
	/// </summary>
	public static bool TryParseFilenameAsPrNumber(string filePath, out int prNumber)
	{
		prNumber = 0;
		var stem = Path.GetFileNameWithoutExtension(filePath);
		if (string.IsNullOrEmpty(stem))
			return false;
		var i = 0;
		while (i < stem.Length && char.IsDigit(stem[i]))
			i++;
		// Must start with digits, followed by end-of-stem or a dash separator
		if (i == 0 || (i < stem.Length && stem[i] != '-'))
			return false;
		return int.TryParse(stem[..i], out prNumber) && prNumber > 0;
	}

	private static EntryFileFinding Error(string file, string message) => new(file, FindingSeverity.Error, message);

	private static EntryFileFinding Error<T>(string file, string message, Constraint<T> rule) =>
		new(file, FindingSeverity.Error, message, rule.Name);

	private static EntryFileFinding Warning<T>(string file, string message, Constraint<T> rule) =>
		new(file, FindingSeverity.Warning, message, rule.Name);
}
