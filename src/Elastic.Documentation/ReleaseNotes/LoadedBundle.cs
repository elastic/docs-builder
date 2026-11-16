// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Generator.Equals;

namespace Elastic.Documentation.ReleaseNotes;

/// <summary>
/// Represents a loaded and parsed changelog bundle with its metadata.
/// </summary>
/// <remarks>
/// Constructed with <c>BundleLoader.LoadBundles</c> or <see cref="LoadedBundle.MergeByTarget"/>.
/// Optional members are <c>internal init</c>: only tests (<c>InternalsVisibleTo</c>) can set them.
/// </remarks>
/// <param name="Version">The semantic version or date extracted from the bundle.</param>
/// <param name="Repo">The repository/product name.</param>
/// <param name="Owner">The GitHub owner (organization or user) for PR/issue link generation.</param>
/// <param name="Data">The full parsed bundle data.</param>
/// <param name="FilePath">The absolute path to the bundle file.</param>
/// <param name="Entries">Resolved changelog entries (from inline data or file references).</param>
[Equatable]
public partial record LoadedBundle
{
	internal LoadedBundle(string version, string repo, string owner, Bundle data, string filePath, IReadOnlyList<ChangelogEntry> entries)
	{
		Version = version;
		Repo = repo;
		Owner = owner;
		Data = data;
		FilePath = filePath;
		Entries = entries;
	}

	public string Version { get; internal init; }

	public string Repo { get; internal init; }

	public string Owner { get; internal init; }

	[DefaultEquality]
	public Bundle Data { get; internal init; }

	public string FilePath { get; internal init; }

	[OrderedEquality]
	public IReadOnlyList<ChangelogEntry> Entries { get; internal init; }

	public void Deconstruct(
		out string version,
		out string repo,
		out string owner,
		out Bundle data,
		out string filePath,
		out IReadOnlyList<ChangelogEntry> entries
	)
	{
		version = Version;
		repo = Repo;
		owner = Owner;
		data = Data;
		filePath = FilePath;
		entries = Entries;
	}

	/// <summary>
	/// Entries grouped by their changelog entry type.
	/// </summary>
	[IgnoreEquality]
	public IReadOnlyDictionary<ChangelogEntryType, IReadOnlyCollection<ChangelogEntry>> EntriesByType =>
		Entries.GroupBy(e => e.Type).ToDictionary(g => g.Key, g => (IReadOnlyCollection<ChangelogEntry>)g.ToList().AsReadOnly());

	/// <summary>
	/// Feature IDs that should be hidden when rendering this bundle.
	/// Entries with matching feature-id values will be commented out in the output.
	/// </summary>
	[IgnoreEquality]
	public IReadOnlyList<string> HideFeatures => Data.HideFeatures;
}
