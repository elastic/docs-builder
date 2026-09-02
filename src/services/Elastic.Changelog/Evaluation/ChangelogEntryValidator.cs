// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation;
using Elastic.Documentation.Configuration.Changelog;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Elastic.Documentation.ReleaseNotes;

namespace Elastic.Changelog.Evaluation;

/// <summary>Finding severity level.</summary>
public enum FindingSeverity
{
	Error,
	Warning
}

/// <summary>A validation finding for a changelog entry file.</summary>
public record EntryFileFinding(string File, FindingSeverity Severity, string Message);

/// <summary>
/// Pure, stateless rule engine for changelog entry files.
/// A <see cref="YamlDotNet.Core.YamlException"/> during parse should be converted to a single
/// <see cref="EntryFileFinding"/> error by the caller before calling <see cref="Validate"/>.
/// </summary>
public static class ChangelogEntryValidator
{
	/// <summary>
	/// Validates an already-parsed <see cref="ChangelogEntryDto"/> against <paramref name="config"/>
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
	public static IReadOnlyList<EntryFileFinding> Validate(
		string filePath,
		ChangelogEntryDto entry,
		ChangelogConfiguration config,
		ChangelogEntryType? labelDerivedType,
		IReadOnlySet<string>? knownProducts
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
					Error(filePath, "marker entries (link:) must not contain other fields such as title, type, products, prs, or issues")
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
			findings.Add(Error(filePath, "title is required"));
		else if (entry.Title.Length > 80)
			findings.Add(Warning(filePath, $"title exceeds 80 characters (current: {entry.Title.Length})"));

		// ── Description ──────────────────────────────────────────────────────────────────────────
		if (entry.Description is not null && entry.Description.Length > 600)
			findings.Add(Warning(filePath, $"description exceeds 600 characters (current: {entry.Description.Length})"));

		// ── Products ─────────────────────────────────────────────────────────────────────────────
		if (entry.Products is null || entry.Products.Count == 0)
			findings.Add(Error(filePath, "products is required"));
		else
		{
			for (var i = 0; i < entry.Products.Count; i++)
			{
				var product = entry.Products[i];
				if (string.IsNullOrWhiteSpace(product.Product))
					findings.Add(Error(filePath, $"products[{i}].product is required"));
				else if (knownProducts is not null)
				{
					var normalized = product.Product.Replace('_', '-');
					if (!knownProducts.Contains(normalized))
					{
						var available = string.Join(", ", knownProducts.OrderBy(p => p));
						findings.Add(
							Error(
								filePath,
								$"product '{product.Product}' is not in the list of available products from config/products.yml. Available products: {available}"
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
					findings.Add(Warning(filePath, "products[].target is obsolete; use products[].versions in a note file"));
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
								$"lifecycle '{product.Lifecycle}' is not valid; expected one of: {string.Join(", ", availableLifecycles)}"
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
					findings.Add(Error(filePath, $"type is omitted; add `type: {labelTypeName}` to match this PR's label"));
				}
				else
					findings.Add(Error(filePath, "type is required"));
			}
			else
			{
				// Present but unrecognised
				findings.Add(Error(filePath, $"type '{entry.Type}' is not recognised; valid values: {string.Join(", ", config.Types)}"));
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
					Error(filePath, $"subtype '{entry.Subtype}' is not valid; expected one of: {string.Join(", ", config.SubTypes)}")
				);
			else if (parsedEntryType is not (ChangelogEntryType.BreakingChange or ChangelogEntryType.Invalid))
				findings.Add(Warning(filePath, "subtype is only expected on breaking-change entries"));
		}

		// ── Areas ────────────────────────────────────────────────────────────────────────────────
		if (config.Areas is { Count: > 0 } && entry.Areas is { Count: > 0 })
		{
			foreach (var area in entry.Areas)
			{
				if (!config.Areas.Contains(area, StringComparer.OrdinalIgnoreCase))
					findings.Add(Error(filePath, $"area '{area}' is not valid; expected one of: {string.Join(", ", config.Areas)}"));
			}
		}

		return findings;
	}

	/// <summary>
	/// Validates PR references in <paramref name="entry"/>, checking existence for own-repo refs.
	/// </summary>
	/// <param name="filePath">Repo-relative path used in finding messages.</param>
	/// <param name="entry">The deserialized DTO.</param>
	/// <param name="defaultOwner">Owner used for normalization (the configured bundle owner).</param>
	/// <param name="defaultRepo">Repo used to identify own-repo refs.</param>
	/// <param name="existenceResults">Number → exists, for own-repo PRs. Missing keys mean "unknown".</param>
	/// <param name="linkAllowRepos">Repos allowed for foreign-repo refs (from bundle.link_allow_repos).</param>
	public static IReadOnlyList<EntryFileFinding> ValidatePrReferences(
		string filePath,
		ChangelogEntryDto entry,
		string defaultOwner,
		string defaultRepo,
		IReadOnlyDictionary<int, bool> existenceResults,
		IReadOnlyList<string>? linkAllowRepos = null
	)
	{
		var findings = new List<EntryFileFinding>();
		var refs = CollectPrRefs(entry);

		foreach (var prRef in refs)
		{
			var (parsedOwner, parsedRepo, parsedNumber) = ParsePrRef(prRef, defaultOwner, defaultRepo);
			if (parsedNumber is null)
			{
				findings.Add(Error(filePath, $"PR reference '{prRef}' could not be parsed as a valid PR URL or number"));
				continue;
			}

			var isOwnRepo = string.Equals(parsedRepo, defaultRepo, StringComparison.OrdinalIgnoreCase);
			if (isOwnRepo)
			{
				if (existenceResults.TryGetValue(parsedNumber.Value, out var exists) && !exists)
					findings.Add(Error(filePath, $"PR #{parsedNumber.Value} does not exist in {parsedOwner}/{parsedRepo}"));
				// If not in existenceResults, it's "unknown" — warn-and-omit, not an error
			}
			else
			{
				// Foreign repo: warn if not on the allowlist
				var foreignRepo = $"{parsedOwner}/{parsedRepo}";
				if (linkAllowRepos is { Count: > 0 })
				{
					var allowed = linkAllowRepos.Any(
						r => r.Contains('/')
							? r.Equals(foreignRepo, StringComparison.OrdinalIgnoreCase)
							: r.Equals(parsedRepo, StringComparison.OrdinalIgnoreCase)
					);
					if (!allowed)
						findings.Add(
							Warning(
								filePath,
								$"PR reference to '{foreignRepo}' is outside bundle.link_allow_repos; it will be scrubbed when published"
							)
						);
				}
			}
		}

		return findings;
	}

	/// <summary>Collect all PR reference strings from <c>pr:</c> and <c>prs:</c>.</summary>
	public static IReadOnlyList<string> CollectPrRefs(ChangelogEntryDto entry)
	{
		var refs = new List<string>();
		if (!string.IsNullOrWhiteSpace(entry.Pr))
			refs.Add(entry.Pr.Trim());
		if (entry.Prs is { Count: > 0 })
			refs.AddRange(entry.Prs.Select(p => p.Trim()).Where(p => !string.IsNullOrWhiteSpace(p)));
		return refs;
	}

	/// <summary>Parses a PR ref into (owner, repo, number). Returns nulls on failure.</summary>
	private static (string? owner, string? repo, int? number) ParsePrRef(string prRef, string defaultOwner, string defaultRepo)
	{
		// Full URL: https://github.com/owner/repo/pull/123
		if (
			prRef.StartsWith("https://github.com/", StringComparison.OrdinalIgnoreCase)
			|| prRef.StartsWith("http://github.com/", StringComparison.OrdinalIgnoreCase)
		)
		{
			var uri = new Uri(prRef);
			var segments = uri.Segments;
			if (segments.Length >= 5 && segments[3].Equals("pull/", StringComparison.OrdinalIgnoreCase))
			{
				var owner = segments[1].TrimEnd('/');
				var repo = segments[2].TrimEnd('/');
				if (int.TryParse(segments[4].TrimEnd('/'), out var num))
					return (owner, repo, num);
			}
			return (null, null, null);
		}

		// Short: owner/repo#123
		var hashIdx = prRef.LastIndexOf('#');
		if (hashIdx > 0 && hashIdx < prRef.Length - 1)
		{
			var repoPart = prRef[..hashIdx];
			if (int.TryParse(prRef[(hashIdx + 1)..], out var num))
			{
				var parts = repoPart.Split('/');
				if (parts.Length == 2)
					return (parts[0], parts[1], num);
			}
		}

		// Bare number
		if (int.TryParse(prRef, out var bareNum))
			return (defaultOwner, defaultRepo, bareNum);

		return (null, null, null);
	}

	private static EntryFileFinding Error(string file, string message) => new(file, FindingSeverity.Error, message);
	private static EntryFileFinding Warning(string file, string message) => new(file, FindingSeverity.Warning, message);
}
