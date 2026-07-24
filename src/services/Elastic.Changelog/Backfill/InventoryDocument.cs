// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace Elastic.Changelog.Backfill;

/// <summary>
/// What the census concluded about one release-note source: does it have history worth
/// backfilling, and if not, why not. Only the first two classifications produce backfill
/// work; the rest stay visible in the census so nothing silently disappears.
/// </summary>
public enum SourceClassification
{
	/// <summary>Published release-note pages exist for this source, so there is history to backfill.</summary>
	[JsonStringEnumMemberName("published-history-found")]
	PublishedHistoryFound,

	/// <summary>Native changelog entries or bundles already exist for this source (the strongest input for backfilling).</summary>
	[JsonStringEnumMemberName("native-artifacts-found")]
	NativeArtifactsFound,

	/// <summary>The published page mixes hand-written history with live <c>{changelog}</c> output, so only the hand-written part needs backfilling.</summary>
	[JsonStringEnumMemberName("hybrid-page")]
	HybridPage,

	/// <summary>The product declares release notes but no published history could be found.</summary>
	[JsonStringEnumMemberName("declared-no-history")]
	DeclaredNoHistory,

	/// <summary>We could not work out where this product's release notes live; needs a human.</summary>
	[JsonStringEnumMemberName("source-unresolved")]
	SourceUnresolved,

	/// <summary>The source's history predates the product's migration cutoff, so it is out of scope.</summary>
	[JsonStringEnumMemberName("outside-cutoff")]
	OutsideCutoff,

	/// <summary>Live artifacts already fully cover this source; nothing to backfill.</summary>
	[JsonStringEnumMemberName("already-live")]
	AlreadyLive
}

/// <summary>How far a repository has adopted the live changelog workflows.</summary>
public enum AdoptionState
{
	/// <summary>The repository does not use the changelog workflows at all yet.</summary>
	[JsonStringEnumMemberName("not-adopted")]
	NotAdopted,

	/// <summary>The repository uses some of the changelog workflows, so live and historical data may overlap mid-release.</summary>
	[JsonStringEnumMemberName("partially-adopted")]
	PartiallyAdopted,

	/// <summary>The repository fully uses the changelog workflows.</summary>
	[JsonStringEnumMemberName("fully-adopted")]
	FullyAdopted
}

/// <summary>How a product names its releases, which decides how targets are parsed and compared.</summary>
public enum TargetScheme
{
	/// <summary>Semantic versions like <c>9.0.0</c>.</summary>
	[JsonStringEnumMemberName("semver")]
	Semver,

	/// <summary>Calendar dates like <c>2025-11-04</c>.</summary>
	[JsonStringEnumMemberName("date")]
	Date,

	/// <summary>Months like <c>2025-11</c>, used by products that ship monthly.</summary>
	[JsonStringEnumMemberName("monthly")]
	Monthly
}

/// <summary>Whether a cutoff boundary is expressed as a version or as a date.</summary>
public enum CutoffKind
{
	/// <summary>The boundary is a version, e.g. everything from stack <c>9.0.0</c> onwards.</summary>
	[JsonStringEnumMemberName("version")]
	Version,

	/// <summary>The boundary is a date, e.g. everything published after <c>2025-01-01</c>.</summary>
	[JsonStringEnumMemberName("date")]
	Date
}

/// <summary>
/// The line between "backfill this" and "leave it alone" for one source: releases on or
/// after the boundary are in scope, anything older is not. Products without stack
/// versioning get a date boundary instead of a version.
/// </summary>
public sealed record BackfillCutoff
{
	/// <summary>Whether <see cref="Value"/> is a version or a date.</summary>
	public required CutoffKind Kind { get; init; }

	/// <summary>The boundary itself, e.g. <c>9.0.0</c> or <c>2025-01-01</c>.</summary>
	public required string Value { get; init; }

	/// <summary>Optional free-text explanation of why this boundary was chosen.</summary>
	public string? Notes { get; init; }

	/// <summary>Adds a plain-English description of every problem in this cutoff to <paramref name="problems"/>.</summary>
	public void Validate(IList<string> problems)
	{
		if (string.IsNullOrWhiteSpace(Value))
			problems.Add("A cutoff needs a non-empty value (a version like 9.0.0 or a date like 2025-01-01).");
	}
}

/// <summary>
/// A repository that entries from this source attribute their changes to, together with
/// whether the deployed scrubber's link allowlist knows it. Links to repositories that are
/// not on the allowlist get silently stripped on publication, so planning must check this
/// flag before upload instead of finding out afterwards.
/// </summary>
public sealed record AttributedRepository
{
	/// <summary>The repository entries attribute changes to.</summary>
	public required GitRepository Repository { get; init; }

	/// <summary>True when the deployed scrubber's link allowlist includes this repository, so its links survive publication.</summary>
	public required bool OnScrubberAllowlist { get; init; }

	/// <summary>Adds a plain-English description of every problem in this attribution to <paramref name="problems"/>.</summary>
	public void Validate(IList<string> problems) => Repository.Validate(problems);
}

/// <summary>
/// Everything the census recorded about one product/repository source: where its release
/// notes live, which products they feed, where the backfill boundary sits, and what we
/// concluded about it. One inventory document holds one of these per source.
/// </summary>
public sealed record InventorySource
{
	/// <summary>The repository the release-note content lives in.</summary>
	public required GitRepository SourceRepository { get; init; }

	/// <summary>The git ref (branch, tag, or commit) the census read the content at.</summary>
	public required string GitRef { get; init; }

	/// <summary>The docset the content belongs to, when the repository hosts more than one.</summary>
	public string? Docset { get; init; }

	/// <summary>Paths inside the repository where the release-note content lives.</summary>
	public IReadOnlyList<string> Paths { get; init; } = [];

	/// <summary>The product IDs (as known to <c>products.yml</c>) this source produces release notes for.</summary>
	public required IReadOnlyList<string> ProductIds { get; init; }

	/// <summary>How this product names its releases (versions, dates, or months).</summary>
	public required TargetScheme TargetScheme { get; init; }

	/// <summary>Where hand-written history ends and live workflow data begins. Null when the census could not determine one yet.</summary>
	public BackfillCutoff? Cutoff { get; init; }

	/// <summary>Docset variable substitutions (like <c>{{es}}</c> → <c>Elasticsearch</c>) needed to expand the source text.</summary>
	public IReadOnlyDictionary<string, string> Substitutions { get; init; } = ReadOnlyDictionary<string, string>.Empty;

	/// <summary>Known mappings from links as written in the source to the destinations they should resolve to.</summary>
	public IReadOnlyDictionary<string, string> LinkMappings { get; init; } = ReadOnlyDictionary<string, string>.Empty;

	/// <summary>The repositories entries attribute changes to, each with its scrubber-allowlist status.</summary>
	public IReadOnlyList<AttributedRepository> AttributedRepositories { get; init; } = [];

	/// <summary>The repository to attribute content to when an entry does not say where it came from. Null when every entry is attributed.</summary>
	public GitRepository? DefaultRepository { get; init; }

	/// <summary>The file-name pattern bundles for this source are expected to use, e.g. <c>{repo}-{target}.yaml</c>.</summary>
	public string? BundleFilenameConvention { get; init; }

	/// <summary>How far this repository has adopted the live changelog workflows.</summary>
	public required AdoptionState AdoptionState { get; init; }

	/// <summary>What the census concluded about this source.</summary>
	public required SourceClassification Classification { get; init; }

	/// <summary>IDs of overrides (from the overrides document) that apply to this source.</summary>
	public IReadOnlyList<string> AppliedOverrideIds { get; init; } = [];

	/// <summary>Open questions about this source that a human still needs to answer, in plain text.</summary>
	public IReadOnlyList<string> UnresolvedItems { get; init; } = [];

	/// <summary>Adds a plain-English description of every problem in this source to <paramref name="problems"/>.</summary>
	public void Validate(IList<string> problems)
	{
		SourceRepository.Validate(problems);
		if (string.IsNullOrWhiteSpace(GitRef))
			problems.Add("An inventory source needs a non-empty git ref.");
		if (ProductIds.Count == 0)
			problems.Add("An inventory source needs at least one product ID.");
		if (ProductIds.Any(string.IsNullOrWhiteSpace))
			problems.Add("An inventory source's product IDs must all be non-empty.");
		Cutoff?.Validate(problems);
		foreach (var attributed in AttributedRepositories)
			attributed.Validate(problems);
		DefaultRepository?.Validate(problems);
	}
}

/// <summary>
/// The census: which products and release-note sources exist, where they came from, and
/// what we decided about each. Produced by the inventory stage before any planning;
/// read by planning and by humans reviewing what a backfill run will cover. Sources that
/// produce no backfill work stay listed with their classification, so "we looked and
/// decided no" is always distinguishable from "we never looked".
/// </summary>
public sealed record InventoryDocument : IBackfillDocument
{
	/// <inheritdoc />
	public static BackfillArtifactKind Kind => BackfillArtifactKind.Inventory;

	/// <summary>One record per product/repository source the census examined.</summary>
	public required IReadOnlyList<InventorySource> Sources { get; init; }

	/// <inheritdoc />
	public void Validate(IList<string> problems)
	{
		for (var i = 0; i < Sources.Count; i++)
		{
			var before = problems.Count;
			Sources[i].Validate(problems);
			ValidationProblems.PrefixNew(problems, before, $"sources[{i}]");
		}
	}
}
