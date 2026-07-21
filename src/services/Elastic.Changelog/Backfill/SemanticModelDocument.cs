// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;

namespace Elastic.Changelog.Backfill;

/// <summary>
/// The section of a release-notes page an entry belongs to. Published pages often merge
/// related types into one section (features with enhancements, fixes with security
/// fixes), so this is deliberately coarser than the exact changelog type — the family is
/// always known from the page structure, while <see cref="ReleaseEntry.PreciseType"/> is
/// only filled in when the evidence actually distinguishes it.
/// </summary>
public enum EntryCategoryFamily
{
	/// <summary>New features and improvements — pages often merge these into one section.</summary>
	[JsonStringEnumMemberName("features-and-enhancements")]
	FeaturesAndEnhancements,

	/// <summary>Bug fixes and security fixes — pages often merge these into one section.</summary>
	[JsonStringEnumMemberName("fixes-and-security")]
	FixesAndSecurity,

	/// <summary>Changes that break existing behavior and need user action.</summary>
	[JsonStringEnumMemberName("breaking-changes")]
	BreakingChanges,

	/// <summary>Functionality that still works but is on its way out.</summary>
	[JsonStringEnumMemberName("deprecations")]
	Deprecations,

	/// <summary>Problems known to exist in the release.</summary>
	[JsonStringEnumMemberName("known-issues")]
	KnownIssues,

	/// <summary>Major documentation changes or reorganizations.</summary>
	[JsonStringEnumMemberName("docs")]
	Docs,

	/// <summary>Functionality that stopped working or now behaves incorrectly.</summary>
	[JsonStringEnumMemberName("regressions")]
	Regressions,

	/// <summary>Anything that does not fit the other families.</summary>
	[JsonStringEnumMemberName("other")]
	Other
}

/// <summary>
/// The exact changelog type of an entry, using the same vocabulary as live changelog
/// entries. Only recorded when the source or enrichment evidence actually distinguishes
/// it — a merged "Fixes" section, for example, gives every entry the
/// <see cref="EntryCategoryFamily.FixesAndSecurity"/> family but no precise type.
/// </summary>
public enum PreciseEntryType
{
	/// <summary>A new feature.</summary>
	[JsonStringEnumMemberName("feature")]
	Feature,

	/// <summary>An improvement to an existing feature.</summary>
	[JsonStringEnumMemberName("enhancement")]
	Enhancement,

	/// <summary>A fix or advisory for a security vulnerability.</summary>
	[JsonStringEnumMemberName("security")]
	Security,

	/// <summary>A bug fix.</summary>
	[JsonStringEnumMemberName("bug-fix")]
	BugFix,

	/// <summary>A change that breaks documented behavior.</summary>
	[JsonStringEnumMemberName("breaking-change")]
	BreakingChange,

	/// <summary>Functionality that is deprecated and will be removed later.</summary>
	[JsonStringEnumMemberName("deprecation")]
	Deprecation,

	/// <summary>A problem known to exist in the product.</summary>
	[JsonStringEnumMemberName("known-issue")]
	KnownIssue,

	/// <summary>A major documentation change.</summary>
	[JsonStringEnumMemberName("docs")]
	Docs,

	/// <summary>Functionality that stopped working or behaves incorrectly.</summary>
	[JsonStringEnumMemberName("regression")]
	Regression,

	/// <summary>Anything that does not fit the other types.</summary>
	[JsonStringEnumMemberName("other")]
	Other
}

/// <summary>What part of a breaking change breaks, using the same vocabulary as live changelog entries.</summary>
public enum BreakingChangeSubtype
{
	/// <summary>Breaks an API.</summary>
	[JsonStringEnumMemberName("api")]
	Api,

	/// <summary>Breaks the way something works.</summary>
	[JsonStringEnumMemberName("behavioral")]
	Behavioral,

	/// <summary>Breaks configuration.</summary>
	[JsonStringEnumMemberName("configuration")]
	Configuration,

	/// <summary>Breaks a dependency, such as a third-party product.</summary>
	[JsonStringEnumMemberName("dependency")]
	Dependency,

	/// <summary>Breaks licensing or subscription behavior.</summary>
	[JsonStringEnumMemberName("subscription")]
	Subscription,

	/// <summary>Breaks a plugin.</summary>
	[JsonStringEnumMemberName("plugin")]
	Plugin,

	/// <summary>Breaks authentication, authorization, or permissions.</summary>
	[JsonStringEnumMemberName("security")]
	Security,

	/// <summary>A breaking change that does not fit the other subtypes.</summary>
	[JsonStringEnumMemberName("other")]
	Other
}

/// <summary>How mature a feature is for a given product, using the same vocabulary as live changelog entries.</summary>
public enum ReleaseLifecycle
{
	/// <summary>Technical preview.</summary>
	[JsonStringEnumMemberName("preview")]
	Preview,

	/// <summary>Beta.</summary>
	[JsonStringEnumMemberName("beta")]
	Beta,

	/// <summary>Generally available.</summary>
	[JsonStringEnumMemberName("ga")]
	Ga,

	/// <summary>Experimental.</summary>
	[JsonStringEnumMemberName("experimental")]
	Experimental
}

/// <summary>How serious a triage finding is.</summary>
public enum TriageSeverity
{
	/// <summary>The scope cannot be published until a human resolves this.</summary>
	[JsonStringEnumMemberName("blocker")]
	Blocker,

	/// <summary>Worth a look, but does not block publication (e.g. missing optional metadata).</summary>
	[JsonStringEnumMemberName("warning")]
	Warning
}

/// <summary>A product touched by an entry, with the release it lands in and, when known, the feature's maturity.</summary>
public sealed record ReleaseProductReference
{
	/// <summary>The product ID, as known to <c>products.yml</c>.</summary>
	public required string Product { get; init; }

	/// <summary>The release target for that product, e.g. <c>9.0.0</c> or <c>2025-11</c>.</summary>
	public required string Target { get; init; }

	/// <summary>The feature's maturity for this product, when the source or enrichment says so.</summary>
	public ReleaseLifecycle? Lifecycle { get; init; }

	/// <summary>Adds a plain-English description of every problem in this reference to <paramref name="problems"/>.</summary>
	public void Validate(IList<string> problems)
	{
		if (string.IsNullOrWhiteSpace(Product))
			problems.Add("A product reference needs a non-empty product.");
		if (string.IsNullOrWhiteSpace(Target))
			problems.Add("A product reference needs a non-empty target.");
	}
}

/// <summary>
/// One release-note entry reduced to its meaning: what changed, how it is categorized,
/// and what it links to. Everything about presentation — anchors, heading text, dropdown
/// versus list rendering, ordering — is deliberately absent, so a renderer redesign can
/// never make two equal entries look different to the pipeline.
/// </summary>
public sealed record ReleaseEntry
{
	/// <summary>The section family the entry belongs to. Always known, because it comes from the page structure.</summary>
	public required EntryCategoryFamily CategoryFamily { get; init; }

	/// <summary>The exact changelog type, when the evidence distinguishes it. Null when only the family is known.</summary>
	public PreciseEntryType? PreciseType { get; init; }

	/// <summary>What part of a breaking change breaks. Only meaningful for breaking changes; filled in by enrichment when known.</summary>
	public BreakingChangeSubtype? Subtype { get; init; }

	/// <summary>The entry's one-line summary of what changed.</summary>
	public required string Title { get; init; }

	/// <summary>Longer explanation of the change, when the source has one. Markdown structure, not rendered HTML.</summary>
	public string? Description { get; init; }

	/// <summary>For breaking changes: what the change does to existing users.</summary>
	public string? Impact { get; init; }

	/// <summary>For breaking changes: what users must do about it.</summary>
	public string? Action { get; init; }

	/// <summary>
	/// True when the release presents this entry as a highlight. A highlighted entry
	/// appears in both the regular sections and the highlights output; it is never a
	/// separate entry, because that would make it show up twice.
	/// </summary>
	public bool? Highlight { get; init; }

	/// <summary>The products this entry applies to, each with its release target.</summary>
	public IReadOnlyList<ReleaseProductReference> ProductReferences { get; init; } = [];

	/// <summary>Links the entry carries besides PRs and issues, as absolute URLs after substitutions are applied.</summary>
	public IReadOnlyList<string> Links { get; init; } = [];

	/// <summary>Pull requests behind the change, as full canonical URLs — never bare numbers.</summary>
	public IReadOnlyList<string> Prs { get; init; } = [];

	/// <summary>Issues related to the change, as full canonical URLs — never bare numbers.</summary>
	public IReadOnlyList<string> Issues { get; init; } = [];

	/// <summary>Product areas the entry touches, e.g. <c>Search</c>, when the source or enrichment says so.</summary>
	public IReadOnlyList<string> Areas { get; init; } = [];

	/// <summary>The entry's stable identity, once identity resolution has run. Null straight out of the parser.</summary>
	public EntryIdentity? Identity { get; init; }

	/// <summary>Where in the source this entry was parsed from, so a human can check the original text.</summary>
	public required SourceLocation SourceLocation { get; init; }

	/// <summary>Adds a plain-English description of every problem in this entry to <paramref name="problems"/>.</summary>
	public void Validate(IList<string> problems)
	{
		if (string.IsNullOrWhiteSpace(Title))
			problems.Add("An entry needs a non-empty title.");
		foreach (var reference in ProductReferences)
			reference.Validate(problems);
		foreach (var pr in Prs)
		{
			if (!CanonicalGitHubUrls.IsPullRequestUrl(pr))
				problems.Add($"PR references must be full canonical URLs like https://github.com/{{owner}}/{{repo}}/pull/{{number}}, but found '{pr}'.");
		}
		foreach (var issue in Issues)
		{
			if (!CanonicalGitHubUrls.IsIssueUrl(issue))
				problems.Add($"Issue references must be full canonical URLs like https://github.com/{{owner}}/{{repo}}/issues/{{number}}, but found '{issue}'.");
		}
		Identity?.Validate(problems);
		SourceLocation.Validate(problems);
	}
}

/// <summary>
/// One release of one product with everything we learned about it: when it shipped (if
/// known), its introductory text, and its entries. This is the <c>ProductRelease</c>
/// shape from the backfill epic.
/// </summary>
public sealed record ProductRelease
{
	/// <summary>The product ID, as known to <c>products.yml</c>.</summary>
	public required string Product { get; init; }

	/// <summary>The release target, e.g. <c>9.0.0</c>, <c>2025-11</c>, or <c>2025-11-04</c>, matching the product's target scheme.</summary>
	public required string Target { get; init; }

	/// <summary>The day the release shipped, when the source or enrichment recovered it.</summary>
	public DateOnly? ReleaseDate { get; init; }

	/// <summary>Introductory text for the release as a whole, when the source has one.</summary>
	public string? Description { get; init; }

	/// <summary>The release's entries. Order carries no meaning; the fidelity gate compares releases as unordered collections.</summary>
	public required IReadOnlyList<ReleaseEntry> Entries { get; init; }

	/// <summary>Adds a plain-English description of every problem in this release to <paramref name="problems"/>.</summary>
	public void Validate(IList<string> problems)
	{
		if (string.IsNullOrWhiteSpace(Product))
			problems.Add("A release needs a non-empty product.");
		if (string.IsNullOrWhiteSpace(Target))
			problems.Add("A release needs a non-empty target.");
		for (var i = 0; i < Entries.Count; i++)
		{
			var before = problems.Count;
			Entries[i].Validate(problems);
			ValidationProblems.PrefixNew(problems, before, $"entries[{i}]");
		}
	}
}

/// <summary>
/// Something the parser could not fully handle, with where it happened. Blockers stop a
/// scope from being published; warnings are quality notes a human may act on. Structured
/// (rather than log lines) so review tooling can group and count them.
/// </summary>
public sealed record TriageDiagnostic
{
	/// <summary>Whether this finding blocks publication or is only worth a look.</summary>
	public required TriageSeverity Severity { get; init; }

	/// <summary>Plain-English description of what the parser could not handle and why.</summary>
	public required string Message { get; init; }

	/// <summary>The product the finding belongs to, when known.</summary>
	public string? Product { get; init; }

	/// <summary>The release target the finding belongs to, when known.</summary>
	public string? Target { get; init; }

	/// <summary>Where in the source the problem sits, so a human can jump straight to it.</summary>
	public SourceLocation? Location { get; init; }

	/// <summary>Adds a plain-English description of every problem in this diagnostic to <paramref name="problems"/>.</summary>
	public void Validate(IList<string> problems)
	{
		if (string.IsNullOrWhiteSpace(Message))
			problems.Add("A triage diagnostic needs a non-empty message.");
		Location?.Validate(problems);
	}
}

/// <summary>
/// The release notes reduced to their meaning, with formatting stripped away. Written by
/// the parser from expanded source Markdown (or native artifacts); read by planning and
/// by the semantic fidelity gate, which compares two of these instead of diffing rendered
/// output. Carries triage diagnostics for everything the parser could not confidently handle.
/// </summary>
public sealed record SemanticModelDocument : IBackfillDocument
{
	/// <inheritdoc />
	public static BackfillArtifactKind Kind => BackfillArtifactKind.SemanticModel;

	/// <summary>The parsed releases.</summary>
	public required IReadOnlyList<ProductRelease> Releases { get; init; }

	/// <summary>Everything the parser could not fully handle. Any blocker here stops the scope from being published.</summary>
	public IReadOnlyList<TriageDiagnostic> Diagnostics { get; init; } = [];

	/// <inheritdoc />
	public void Validate(IList<string> problems)
	{
		for (var i = 0; i < Releases.Count; i++)
		{
			var before = problems.Count;
			Releases[i].Validate(problems);
			ValidationProblems.PrefixNew(problems, before, $"releases[{i}]");
		}
		for (var i = 0; i < Diagnostics.Count; i++)
		{
			var before = problems.Count;
			Diagnostics[i].Validate(problems);
			ValidationProblems.PrefixNew(problems, before, $"diagnostics[{i}]");
		}
	}
}
