// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Nullean.Entities;

namespace Elastic.Documentation.ReleaseNotes;

/// <summary>
/// Every rule a <see cref="ChangelogEntry"/> must satisfy on its own. A marker entry (<c>link:</c>) carries nothing else,
/// so the content rules leave it alone and <c>marker-carries-only-link</c> holds it to that. The rules that need the
/// configuration are <see cref="ChangelogEntryInConfigurationConstraints"/>.
/// </summary>
[Constraints]
public static partial class ChangelogEntryConstraints
{
	public static Constraint<ChangelogEntry> EntryTitleRequired { get; } = Constraints<ChangelogEntry>.Always(
		e => e.IsMarker || !string.IsNullOrWhiteSpace(e.Title),
		"title is required"
	).Describe(
		"R34: A changelog entry has a title (stage dependent: error at create, validate, bundle and render, a warning in the directive)."
	);

	public static Constraint<ChangelogEntry> EntryTitleMax80 { get; } = Constraints<ChangelogEntry>
		.Always(e => e.Title.Length <= 80, "title exceeds 80 characters")
		.WithSeverity(Severity.Warning)
		.Describe("R35: A changelog entry title is at most 80 characters.");

	public static Constraint<ChangelogEntry> EntryDescriptionMax600 { get; } = Constraints<ChangelogEntry>
		.Always(e => e.Description is null || e.Description.Length <= 600, "description exceeds 600 characters")
		.WithSeverity(Severity.Warning)
		.Describe("R35: A changelog entry description is at most 600 characters.");

	public static Constraint<ChangelogEntry> EntryTypeRequired { get; } = Constraints<ChangelogEntry>.Always(
		e => e.IsMarker || e.Type != ChangelogEntryType.Invalid,
		"type is required and must be a recognized type"
	).Describe("R36: A changelog entry has a recognized type; an unrecognized type is read as invalid.");

	public static Constraint<ChangelogEntry> EntrySubtypeOnlyBreakingChange { get; } = Constraints<ChangelogEntry>
		.Always(
			e => e.Subtype is null || e.Type is ChangelogEntryType.BreakingChange or ChangelogEntryType.Invalid,
			"subtype is only expected on breaking-change entries"
		)
		.WithSeverity(Severity.Warning)
		.Describe(
			"R40: A subtype is only set on a breaking-change entry (stage dependent: a warning at validate, an error in the configuration, unchecked at create)."
		);

	public static Constraint<ChangelogEntry> EntryProductsRequired { get; } = Constraints<ChangelogEntry>
		.When(e => !e.IsMarker)
		.Requires(e => e.Products is { Count: > 0 }, "products is required: an entry names at least one product.")
		.Describe("R41: A changelog entry names at least one product (stage dependent: not checked at bundle or render).");

	public static Constraint<ChangelogEntry> EntryProductIdRequired { get; } = Constraints<ChangelogEntry>.Each(
		e => e.IsMarker ? [] : e.Products ?? [],
		p => !string.IsNullOrWhiteSpace(p.ProductId),
		_ => "A product of the entry has no id."
	).Describe("R41: Each product of a changelog entry has an id (stage dependent: not checked at bundle or render).");

	public static Constraint<ChangelogEntry> EntryProductTargetObsolete { get; } = Constraints<ChangelogEntry>
		.Each(
			e => e.IsMarker ? [] : e.Products ?? [],
			p => string.IsNullOrWhiteSpace(p.TargetRaw),
			p => $"products[].target ('{p.TargetRaw}' on '{p.ProductId}') is obsolete; use products[].versions in a note file."
		)
		.WithSeverity(Severity.Warning)
		.Describe("R42: A product of an entry has no 'target'; it is obsolete and is read as 'versions'.");

	public static Constraint<ChangelogEntry> EntryFileNameStartsWithPr { get; } = Constraints<ChangelogEntry>.Always(
		e => e.FileName,
		name => name is null || name.StartsWith("note-", StringComparison.OrdinalIgnoreCase) || FileNamePr(name) is > 0,
		describe: name => $"filename '{name}' must start with a PR number (e.g. 14-my-feature.yaml or 14.yaml)."
	).Describe("R63: An entry file is named <digits>[-<name>].yaml or .yml with a PR number above zero (notes are named note-<slug>).");

	public static Constraint<ChangelogEntry> EntryPrMatchesFileName { get; } = Constraints<ChangelogEntry>.Check(
		PrAgainstFileName
	).Describe("R45: A singular 'pr:' is the PR number of the entry's file name.");

	public static Constraint<ChangelogEntry> MarkerLinkIsPrNumber { get; } = Constraints<ChangelogEntry>
		.When(e => e.IsMarker && !e.SourceRedirect)
		.Requires(e => int.TryParse(e.Link, out var n) && n > 0, "A marker's 'link' must be a positive PR number.")
		.Describe(
			"R60: A marker's link is a positive integer (a source pointer written by the scrubber links to a file stem, and is exempt)."
		);

	// A marker carries `link:` and nothing else
	public static Constraint<ChangelogEntry> MarkerCarriesOnlyLink { get; } = Constraints<ChangelogEntry>.NoneOf(
		("title", e => e.IsMarker && !string.IsNullOrWhiteSpace(e.Title), "A marker entry (link:) must not carry a 'title'."),
		("type", e => e.IsMarker && e.Type != ChangelogEntryType.Invalid, "A marker entry (link:) must not carry a 'type'."),
		("subtype", e => e.IsMarker && e.Subtype is not null, "A marker entry (link:) must not carry a 'subtype'."),
		("products", e => e.IsMarker && e.Products is { Count: > 0 }, "A marker entry (link:) must not carry 'products'."),
		("areas", e => e.IsMarker && e.Areas is { Count: > 0 }, "A marker entry (link:) must not carry 'areas'."),
		("prs", e => e.IsMarker && e.Prs is { Count: > 0 }, "A marker entry (link:) must not carry 'prs'."),
		("issues", e => e.IsMarker && e.Issues is { Count: > 0 }, "A marker entry (link:) must not carry 'issues'."),
		("description", e => e.IsMarker &&
			!string.IsNullOrWhiteSpace(e.Description), "A marker entry (link:) must not carry a 'description'."),
		("impact", e => e.IsMarker && !string.IsNullOrWhiteSpace(e.Impact), "A marker entry (link:) must not carry an 'impact'."),
		("action", e => e.IsMarker && !string.IsNullOrWhiteSpace(e.Action), "A marker entry (link:) must not carry an 'action'."),
		("feature-id", e => e.IsMarker && !string.IsNullOrWhiteSpace(e.FeatureId), "A marker entry (link:) must not carry a 'feature-id'."),
		("highlight", e => e.IsMarker && e.Highlight is not null, "A marker entry (link:) must not carry a 'highlight'.")
	).Describe("R59: A marker entry (link:) carries nothing but the link; validate also checks issues, the scrubber does not.");

	/// <summary>The PR number a file name starts with (<c>14-x.yaml</c>, <c>14.yml</c>), or null when it does not follow the convention.</summary>
	internal static int? FileNamePr(string fileName)
	{
		var dot = fileName.LastIndexOf('.');
		var extension = dot < 0 ? "" : fileName[dot..];
		if (!extension.Equals(".yaml", StringComparison.OrdinalIgnoreCase) && !extension.Equals(".yml", StringComparison.OrdinalIgnoreCase))
			return null;
		var stem = fileName[..dot];
		var i = 0;
		while (i < stem.Length && char.IsAsciiDigit(stem[i]))
			i++;
		if (i == 0 || (i < stem.Length && stem[i] != '-'))
			return null;
		return int.TryParse(stem[..i], out var pr) ? pr : null;
	}

	private static IEnumerable<Violation> PrAgainstFileName(ChangelogEntry e)
	{
		if (string.IsNullOrWhiteSpace(e.Pr) || e.FileName is null || FileNamePr(e.FileName) is not { } filePr)
			yield break;
		if (!int.TryParse(e.Pr.Trim().TrimStart('#'), out var declared))
			yield return Violation.Error($"pr: '{e.Pr}' could not be parsed as a PR number");
		else if (declared != filePr)
			yield return Violation.Error($"pr: {declared} does not match the PR number in the filename ({filePr})");
	}
}
