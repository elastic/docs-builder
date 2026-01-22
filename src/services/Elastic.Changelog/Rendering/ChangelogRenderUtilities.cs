// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Changelog.Configuration;
using Elastic.Documentation.Changelog;

namespace Elastic.Changelog.Rendering;

/// <summary>
/// Shared utility methods for changelog rendering
/// </summary>
public static class ChangelogRenderUtilities
{
	/// <summary>
	/// Gets the component (area) for an entry. Uses first area or empty string.
	/// </summary>
	public static string GetComponent(ChangelogData entry)
	{
		// Map areas (list) to component (string) - use first area or empty string
		if (entry.Areas is { Count: > 0 })
			return entry.Areas[0];
		return string.Empty;
	}

	/// <summary>
	/// Determines if an entry should be hidden based on feature IDs and render blockers
	/// </summary>
	public static bool ShouldHideEntry(
		ChangelogData entry,
		HashSet<string> featureIdsToHide,
		HashSet<string> bundleProductIds,
		IReadOnlyDictionary<string, RenderBlockersEntry>? renderBlockers) =>
		(!string.IsNullOrWhiteSpace(entry.FeatureId) && featureIdsToHide.Contains(entry.FeatureId))
			|| ShouldBlockEntry(entry, bundleProductIds, renderBlockers, out _);

	/// <summary>
	/// Determines if an entry should be blocked from rendering based on render blockers configuration
	/// </summary>
	public static bool ShouldBlockEntry(
		ChangelogData entry,
		HashSet<string> bundleProductIds,
		IReadOnlyDictionary<string, RenderBlockersEntry>? renderBlockers,
		out List<string> reasons)
	{
		reasons = [];
		if (renderBlockers == null || renderBlockers.Count == 0)
			return false;

		// Bundle must have products to be blocked
		if (bundleProductIds.Count == 0)
			return false;

		// Extract area values from entry (case-insensitive comparison)
		var entryAreas = entry.Areas is { Count: > 0 }
			? entry.Areas
				.Where(a => !string.IsNullOrWhiteSpace(a))
				.Select(a => a)
				.ToHashSet(StringComparer.OrdinalIgnoreCase)
			: new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		// Extract type from entry (convert enum to string for comparison)
		var entryType = entry.Type.ToStringFast(true);

		// Check each render_blockers entry
		foreach (var (productKey, blockersEntry) in renderBlockers)
		{
			// Parse product key - can be comma-separated (e.g., "elasticsearch, cloud-serverless")
			var productKeys = productKey
				.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
				.Where(p => !string.IsNullOrWhiteSpace(p))
				.ToHashSet(StringComparer.OrdinalIgnoreCase);

			// Check if any product in the bundle matches any product in the key
			var matchingProducts = bundleProductIds.Intersect(productKeys, StringComparer.OrdinalIgnoreCase).ToList();
			if (matchingProducts.Count == 0)
				continue;

			var isBlocked = false;
			var blockReasons = new List<string>();

			// Check areas if specified
			if (blockersEntry.Areas is { Count: > 0 } && entryAreas.Count > 0)
			{
				var matchingAreas = entryAreas.Intersect(blockersEntry.Areas, StringComparer.OrdinalIgnoreCase).ToList();
				if (matchingAreas.Count > 0)
				{
					isBlocked = true;
					var reasonsForProductsAndAreas = matchingProducts
						.SelectMany(product => matchingAreas
							.Select(area => $"product '{product}' with area '{area}'"))
						.Distinct();

					foreach (var reason in reasonsForProductsAndAreas.Where(reason => !blockReasons.Contains(reason)))
						blockReasons.Add(reason);
				}
			}

			// Check types if specified
			if (blockersEntry.Types is { Count: > 0 } && !string.IsNullOrWhiteSpace(entryType))
			{
				var matchingTypes = blockersEntry.Types
					.Where(t => string.Equals(t, entryType, StringComparison.OrdinalIgnoreCase))
					.ToList();
				if (matchingTypes.Count > 0)
				{
					isBlocked = true;
					var reasonsForProducts = matchingProducts
						.SelectMany(product => matchingTypes
							.Select(type => $"product '{product}' with type '{type}'"))
						.Distinct();

					foreach (var reason in reasonsForProducts.Where(reason => !blockReasons.Contains(reason)))
						blockReasons.Add(reason);
				}
			}

			if (isBlocked)
			{
				reasons.AddRange(blockReasons);
				return true;
			}
		}

		return false;
	}
}
