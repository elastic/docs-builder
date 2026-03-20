// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration.Changelog;
using Elastic.Documentation.ReleaseNotes;

namespace Elastic.Changelog.Rendering;

/// <summary>
/// Shared utility methods for changelog rendering
/// </summary>
public static class ChangelogRenderUtilities
{
	/// <summary>
	/// Gets the component (area) for an entry for subsection grouping.
	/// Always uses the first area from the entry; rules.publish is no longer supported.
	/// </summary>
#pragma warning disable IDE0060 // Remove unused parameter
	public static string GetComponent(ChangelogEntry entry, ChangelogRenderContext? context = null)
#pragma warning restore IDE0060 // Remove unused parameter
		=> entry.Areas is { Count: > 0 } ? entry.Areas[0] : string.Empty;

	/// <summary>
	/// Gets the entry context (repo, owner, hideLinks, shouldHide) for a specific entry.
	/// </summary>
	public static (string EntryRepo, string EntryOwner, bool HideLinks, bool ShouldHide) GetEntryContext(
		ChangelogEntry entry,
		ChangelogRenderContext context)
	{
		var entryRepo = context.EntryToRepo.GetValueOrDefault(entry, context.Repo);
		var entryOwner = context.EntryToOwner.GetValueOrDefault(entry, context.Owner);
		var hideLinks = context.EntryToHideLinks.GetValueOrDefault(entry, false);
		var shouldHide = ShouldHideEntry(entry, context.FeatureIdsToHide, context);
		return (entryRepo, entryOwner, hideLinks, shouldHide);
	}

	/// <summary>
	/// Determines if an entry should be hidden based on feature IDs only.
	/// rules.publish is no longer supported; filtering must be done at bundle time via rules.bundle.
	/// </summary>
#pragma warning disable IDE0060 // Remove unused parameter
	public static bool ShouldHideEntry(
		ChangelogEntry entry,
		HashSet<string> featureIdsToHide,
		ChangelogRenderContext? context = null)
#pragma warning restore IDE0060 // Remove unused parameter
	{
		// Check feature IDs only
		if (!string.IsNullOrWhiteSpace(entry.FeatureId) && featureIdsToHide.Contains(entry.FeatureId))
			return true;

		return false;
	}
}
