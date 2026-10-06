// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using Elastic.Documentation.Diagnostics;
using Generator.Equals;

namespace Elastic.Documentation.ReleaseNotes;

/// <summary>What is known about a pull request when an entry is synthesized from it (a GitHub release, a git range).</summary>
/// <param name="Url">The pull request's URL, recorded as the entry's only <c>prs</c> item.</param>
/// <param name="Title">The title, already stripped of its prefix when the configuration says so.</param>
/// <param name="Type">The type's text (from a label), or null to take <c>other</c>.</param>
/// <param name="Description">The release note extracted from the PR body, if any.</param>
/// <param name="Areas">Areas derived from labels; an empty list is the same as none.</param>
/// <param name="FeatureId">The feature id derived from labels.</param>
/// <param name="Highlight">Whether a highlight label was present.</param>
/// <param name="Issues">The issues the PR links; an empty list is the same as none.</param>
[Equatable]
public sealed partial record PullRequestFacts(
	string Url,
	string Title,
	string? Type = null,
	string? Description = null,
	[property: OrderedEquality] IReadOnlyList<string>? Areas = null,
	string? FeatureId = null,
	bool? Highlight = null,
	[property: OrderedEquality] IReadOnlyList<string>? Issues = null
);

/// <summary>What the CLI's <c>changelog add</c> and <c>changelog note</c> collect, before it is an entry.</summary>
[Equatable]
public sealed partial record CreateEntryFacts(
	string? Title,
	string? Type,
	string? Subtype,
	string? Description,
	string? Impact,
	string? Action,
	string? FeatureId,
	bool? Highlight,
	[property: OrderedEquality] IReadOnlyList<string>? Prs,
	[property: OrderedEquality] IReadOnlyList<ProductReference> Products,
	[property: OrderedEquality] IReadOnlyList<string>? Areas,
	[property: OrderedEquality] IReadOnlyList<string>? Issues
);

/// <summary>Where an entry belongs in the pool: the file named after its lowest PR, and a marker for each other PR.</summary>
/// <param name="CanonicalFileName">The entry's file name in the pool (<c>{lowest pr}.yaml</c>).</param>
/// <param name="IsCanonical">Whether the file the entry came from already has that name.</param>
/// <param name="Markers">A marker entry per other PR, with the file name it is stored under.</param>
[Equatable]
public sealed partial record CanonicalPlacement(
	string CanonicalFileName,
	bool IsCanonical,
	[property: OrderedEquality] IReadOnlyList<(string FileName, ChangelogEntry Marker)> Markers
);

/// <summary>
/// The operations that make a <see cref="ChangelogEntry"/> from something that was not a file, or change one that was: pure functions that return the
/// entry checked by <see cref="ChangelogEntryReadConstraints"/> (the rules that hold for every entry, an incomplete one a CLI is still writing included).
/// </summary>
public partial record ChangelogEntry
{
	/// <summary>The type a label or CLI text names, or <see cref="ChangelogEntryType.Other"/> when it names none.</summary>
	public static ChangelogEntryType TypeOrOther(string? text) =>
		ChangelogEntryTypeExtensions.TryParse(text ?? "other", out var parsed, ignoreCase: true, allowMatchingMetadataAttribute: true)
			? parsed
			: ChangelogEntryType.Other;

	/// <summary>
	/// An entry synthesized from a pull request's metadata: the one definition behind a GitHub release's entry and a git range's. The type is
	/// <see cref="TypeOrOther"/> of <see cref="PullRequestFacts.Type"/>; an empty list of areas or issues is none.
	/// </summary>
	public static Result<ChangelogEntry> FromPullRequest(PullRequestFacts facts, IReadOnlyList<ProductReference> products) =>
		ReleaseNotesOperation.Built(
			ChangelogEntryReadConstraints.All,
			new ChangelogEntry
			{
				Title = facts.Title,
				Type = TypeOrOther(facts.Type),
				Description = facts.Description,
				Products = products,
				Areas = facts.Areas is { Count: > 0 } ? facts.Areas : null,
				FeatureId = facts.FeatureId,
				Highlight = facts.Highlight,
				Prs = [facts.Url],
				Issues = facts.Issues is { Count: > 0 } ? facts.Issues : null
			}
		);

	/// <summary>
	/// An entry from what the CLI collected. An unrecognized type is <c>other</c>, an unrecognized subtype none, an empty list of areas none: a CLI that
	/// is still asking for a title or a type writes the entry incomplete, so the stage rules for completeness are not run.
	/// </summary>
	public static Result<ChangelogEntry> Create(CreateEntryFacts facts)
	{
		var subtype = !string.IsNullOrWhiteSpace(facts.Subtype)
			? ChangelogEntrySubtypeExtensions.TryParse(
				facts.Subtype,
				out var subtypeParsed,
				ignoreCase: true,
				allowMatchingMetadataAttribute: true
			) ? subtypeParsed : (ChangelogEntrySubtype?)null
			: null;

		return ReleaseNotesOperation.Built(
			ChangelogEntryReadConstraints.All,
			new ChangelogEntry
			{
				Title = facts.Title ?? string.Empty,
				Type = TypeOrOther(facts.Type),
				Subtype = subtype,
				Description = facts.Description,
				Impact = facts.Impact,
				Action = facts.Action,
				FeatureId = facts.FeatureId,
				Highlight = facts.Highlight,
				Prs = facts.Prs,
				Products = facts.Products,
				Areas = facts.Areas is { Count: > 0 } ? facts.Areas : null,
				Issues = facts.Issues
			}
		);
	}

	/// <summary>
	/// The marker the pipeline writes for a PR whose entry lives under another PR's file: <c>link:</c> and nothing else.
	/// <paramref name="pr"/> is the number of the PR the marker points at.
	/// </summary>
	public static Result<ChangelogEntry> Marker(int pr) => Marker(pr.ToString(CultureInfo.InvariantCulture));

	/// <summary>The marker for a link as written (a file read from the pool may carry anything): a link that is not a PR number is a violation of <c>marker-link-is-pr-number</c> in the result.</summary>
	public static Result<ChangelogEntry> Marker(string link) =>
		ReleaseNotesOperation.Built(ChangelogEntryReadConstraints.All, new ChangelogEntry { Link = link });

	/// <summary>A source pointer the scrubber writes in the public bucket, tracing a source key to a canonical file <paramref name="stem"/> (<c>link:</c> and <c>source-redirect:</c>).</summary>
	public static Result<ChangelogEntry> SourcePointer(string stem) =>
		ReleaseNotesOperation.Built(ChangelogEntryReadConstraints.All, new ChangelogEntry { Link = stem, SourceRedirect = true });

	/// <summary>
	/// Where this entry belongs in the pool, derived from its PR references (a reference to another repository is read against the default owner and repository):
	/// the file named after the lowest PR number, and a marker for each higher one. Null when the entry names no PR. The caller decides what
	/// that means (a note, a marker or an entry without PRs keeps its own file name).
	/// </summary>
	/// <param name="fileName">The file the entry is stored in now; <see cref="CanonicalPlacement.IsCanonical"/> says whether it already is the canonical one.</param>
	public CanonicalPlacement? CanonicalKeyAndMarkers(string fileName)
	{
		var prNumbers = Prs?.Select(pr => ChangelogTextUtilities.ExtractPrNumber(pr))
			.Where(n => n.HasValue)
			.Select(n => n!.Value)
			.Distinct()
			.OrderBy(n => n)
			.ToList();

		if (prNumbers is null or { Count: 0 })
			return null;

		var primaryPr = prNumbers[0];
		var canonicalFileName = $"{primaryPr.ToString(CultureInfo.InvariantCulture)}.yaml";
		var markers = prNumbers
			.Skip(1)
			.Select(pr => ($"{pr.ToString(CultureInfo.InvariantCulture)}.yaml", Marker(primaryPr).Value!))
			.ToList();

		return new CanonicalPlacement(
			canonicalFileName,
			string.Equals(fileName, canonicalFileName, StringComparison.OrdinalIgnoreCase),
			markers
		);
	}

	/// <summary>This entry as the entry of a bundle, read from the file <paramref name="fileName"/> whose content has <paramref name="checksum"/>.</summary>
	public BundledEntry ToBundledEntry(string fileName, string checksum) =>
		ToBundledEntry() with { File = BundledFile.Of(fileName, checksum) };

	/// <summary>
	/// This entry without the references to repositories outside <paramref name="allowRepos"/>
	/// (see <see cref="LinkAllowlist.WithoutPrivateLinks(ChangelogEntry, IReadOnlyList{string}, string, string?)"/>).
	/// </summary>
	public Result<PrivateLinkScrub<ChangelogEntry>> WithoutPrivateLinks(
		IReadOnlyList<string> allowRepos,
		string defaultOwner = "elastic",
		string? defaultRepo = null
	) => LinkAllowlist.WithoutPrivateLinks(this, allowRepos, defaultOwner, defaultRepo);
}
