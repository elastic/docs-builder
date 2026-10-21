// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using Generator.Equals;
using Nullean.Entities;

namespace Elastic.Documentation.ReleaseNotes;

/// <summary>
/// Domain type representing bundled changelog data.
/// Contains products and entries for a changelog bundle.
/// </summary>
/// <remarks>
/// Constructed with <c>BundleLoader</c> or <see cref="Bundle.Assemble"/>, <see cref="Bundle.ForRelease"/> and <see cref="Bundle.AmendOf"/>.
/// Optional members are <c>internal init</c>: only tests (<c>InternalsVisibleTo</c>) can set them.
/// </remarks>
[Equatable]
public partial record Bundle
{
	/// <summary>Products included in this bundle.</summary>
	[OrderedEquality]
	public IReadOnlyList<BundledProduct> Products { get; internal init; } = [];

	/// <summary>
	/// Optional introductory description text for this bundle.
	/// Rendered as introductory content after the release heading.
	/// </summary>
	public string? Description { get; internal init; }

	/// <summary>
	/// Optional release date for this bundle.
	/// Purely informative for end-users; rendered after the release heading.
	/// Useful for components released outside the usual stack lifecycle (e.g., APM/EDOT agents).
	/// Parsed from YYYY-MM-DD format in YAML; serialized back as YYYY-MM-DD.
	/// </summary>
	[IgnoreEquality]
	public DateOnly? ReleaseDate
	{
		get => ReleaseDateAuthored.Value;
		internal init => ReleaseDateAuthored = Authored.Of(value?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), value);
	}

	/// <summary>
	/// The one stored release date: <see cref="Authored{T}.Raw"/> is the <c>release-date</c> as written (null when absent or when the bundle was not read from YAML),
	/// <see cref="Authored{T}.Value"/> the date it parsed to (null for a malformed one). <see cref="ReleaseDate"/> is its value.
	/// </summary>
	public Authored<DateOnly?> ReleaseDateAuthored { get; internal init; } = Authored.Defaulted<DateOnly?>(null);

	/// <summary>
	/// Optional git ref of the published endpoint this bundle was cut for.
	/// Set by commit-range bundling (<c>--end-git-ref</c>); not rendered to end-users.
	/// </summary>
	public string? GitRef { get; internal init; }

	/// <summary>
	/// Feature IDs that should be hidden when rendering this bundle.
	/// Entries with matching feature-id values will be commented out in the output.
	/// </summary>
	[OrderedEquality]
	public IReadOnlyList<string> HideFeatures { get; internal init; } = [];

	/// <summary>Changelog entries in this bundle.</summary>
	[OrderedEquality]
	public IReadOnlyList<BundledEntry> Entries { get; internal init; } = [];

	/// <summary>
	/// Entries to exclude when this amend file is merged with its parent bundle.
	/// Matched by file name and optional checksum.
	/// </summary>
	[OrderedEquality]
	public IReadOnlyList<BundledEntry> ExcludeEntries { get; internal init; } = [];
}
