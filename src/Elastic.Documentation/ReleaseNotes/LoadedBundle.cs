// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.ReleaseNotes;

/// <summary>
/// Represents a loaded and parsed changelog bundle with its metadata.
/// </summary>
/// <param name="Version">The semantic version or date extracted from the bundle.</param>
/// <param name="Repo">The repository/product name.</param>
/// <param name="Data">The full parsed bundle data.</param>
/// <param name="FilePath">The absolute path to the bundle file.</param>
/// <param name="Entries">Resolved changelog entries (from inline data or file references).</param>
public record LoadedBundle(
	string Version,
	string Repo,
	Bundle Data,
	string FilePath,
	IReadOnlyList<ChangelogEntry> Entries)
{
	/// <summary>
	/// Entries grouped by their changelog entry type.
	/// </summary>
	public IReadOnlyDictionary<ChangelogEntryType, IReadOnlyCollection<ChangelogEntry>> EntriesByType =>
		Entries
			.GroupBy(e => e.Type)
			.ToDictionary(g => g.Key, g => (IReadOnlyCollection<ChangelogEntry>)g.ToList().AsReadOnly());
}
