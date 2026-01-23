// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation;

namespace Elastic.Changelog.Rendering;

/// <summary>
/// Shared utility methods for changelog rendering
/// </summary>
public static class ChangelogRenderUtilities
{
	/// <summary>
	/// Gets the component (area) for an entry. Uses first area or empty string.
	/// </summary>
	public static string GetComponent(ChangelogEntry entry)
	{
		// Map areas (list) to component (string) - use first area or empty string
		if (entry.Areas is { Count: > 0 })
			return entry.Areas[0];
		return string.Empty;
	}

	/// <summary>
	/// Determines if an entry should be hidden based on feature IDs
	/// </summary>
	public static bool ShouldHideEntry(
		ChangelogEntry entry,
		HashSet<string> featureIdsToHide) =>
		!string.IsNullOrWhiteSpace(entry.FeatureId) && featureIdsToHide.Contains(entry.FeatureId);
}
