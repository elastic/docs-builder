// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Changelog;

namespace Elastic.Changelog.Rendering;

/// <summary>
/// Result of resolving entries from validated bundles
/// </summary>
public record ResolvedEntriesResult
{
	/// <summary>
	/// Whether resolution was successful
	/// </summary>
	public required bool IsValid { get; init; }

	/// <summary>
	/// List of resolved changelog entries with their metadata
	/// </summary>
	public required IReadOnlyList<ResolvedEntry> Entries { get; init; }

	/// <summary>
	/// All products found across all bundles
	/// </summary>
	public required IReadOnlySet<(string product, string target)> AllProducts { get; init; }
}

/// <summary>
/// A resolved changelog entry with its associated metadata
/// </summary>
public record ResolvedEntry
{
	public required ChangelogData Entry { get; init; }
	public required string Repo { get; init; }
	public required HashSet<string> BundleProductIds { get; init; }
	public required bool HideLinks { get; init; }
}
