// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Changelog.Configuration;
using Elastic.Documentation.Changelog;

namespace Elastic.Changelog.Rendering;

/// <summary>
/// Shared context for all changelog render operations
/// </summary>
public record ChangelogRenderContext
{
	public required string OutputDir { get; init; }
	public required string Title { get; init; }
	public required string TitleSlug { get; init; }
	public required string Repo { get; init; }
	public required IReadOnlyDictionary<ChangelogEntryType, IReadOnlyCollection<ChangelogData>> EntriesByType { get; init; }
	public required bool Subsections { get; init; }
	public required HashSet<string> FeatureIdsToHide { get; init; }
	public required IReadOnlyDictionary<string, RenderBlockersEntry>? RenderBlockers { get; init; }
	public required Dictionary<ChangelogData, HashSet<string>> EntryToBundleProducts { get; init; }
	public required Dictionary<ChangelogData, string> EntryToRepo { get; init; }
	public required Dictionary<ChangelogData, bool> EntryToHideLinks { get; init; }
}
