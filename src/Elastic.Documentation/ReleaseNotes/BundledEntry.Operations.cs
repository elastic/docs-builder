// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Diagnostics;

namespace Elastic.Documentation.ReleaseNotes;

/// <summary>
/// The operations on a <see cref="BundledEntry"/>. An entry of a bundle has no rule set of its own (its rules, <c>bundle-entries-inlined</c> among them,
/// are the <see cref="Bundle"/>'s), so the operations that only reshape one return the entry and the bundle that holds it is what is checked.
/// </summary>
public partial record BundledEntry
{
	/// <summary>An inline entry as a release notes page lists it: a type, a title, a product, the PRs it names (none when empty) and an area.</summary>
	public static BundledEntry Inline(ChangelogEntryType type, string title, string productId, IReadOnlyList<string> prs, string? area) =>
		new()
		{
			Type = type,
			Title = title,
			Products = [ProductReference.Of(productId)],
			Prs = prs.Count > 0 ? prs : null,
			Areas = area is not null ? [area] : null
		};

	/// <summary>An exclusion record of an amend: the file name and the checksum it must have (an empty checksum matches by name only).</summary>
	public static BundledEntry Exclusion(string fileName, string checksum) => new() { File = BundledFile.Of(fileName, checksum) };

	/// <summary>
	/// This entry with a wrapped line of its bullet appended: the title is the merged text's, and the PRs it names join the entry's (each once, in order).
	/// </summary>
	public BundledEntry AppendContinuation(string title, IReadOnlyList<string> prs)
	{
		var mergedPrs = (Prs ?? []).Concat(prs).Distinct(StringComparer.Ordinal).ToList();
		return this with { Title = title, Prs = mergedPrs.Count > 0 ? mergedPrs : null };
	}

	/// <summary>
	/// This inline entry as the note of <paramref name="productId"/> for the release <paramref name="version"/>: the product's <c>versions</c> carries the
	/// release (an entry's own products are not a note's), a missing type is <c>other</c> and a missing title blank.
	/// </summary>
	public Result<ChangelogEntry> ToNote(string productId, string version) =>
		ReleaseNotesOperation.Built(
			ChangelogEntryReadConstraints.All,
			new ChangelogEntry
			{
				Type = Type ?? ChangelogEntryType.Other,
				Title = Title ?? string.Empty,
				Areas = Areas,
				Prs = Prs,
				Issues = Issues,
				Products = [ProductReference.Of(productId, [version])]
			}
		);

	/// <summary>
	/// This entry without the references to repositories outside <paramref name="allowRepos"/>
	/// (see <see cref="LinkAllowlist.WithoutPrivateLinks(BundledEntry, IReadOnlyList{string}, string, string?)"/>).
	/// </summary>
	public Result<PrivateLinkScrub<BundledEntry>> WithoutPrivateLinks(
		IReadOnlyList<string> allowRepos,
		string defaultOwner = "elastic",
		string? defaultRepo = null
	) => LinkAllowlist.WithoutPrivateLinks(this, allowRepos, defaultOwner, defaultRepo);
}
