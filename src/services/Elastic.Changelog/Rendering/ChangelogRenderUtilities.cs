// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Changelog.Configuration;
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
	/// Determines if an entry should be hidden based on feature IDs or block configuration
	/// </summary>
	public static bool ShouldHideEntry(
		ChangelogEntry entry,
		HashSet<string> featureIdsToHide,
		ChangelogRenderContext? context = null)
	{
		// Check feature IDs first
		if (!string.IsNullOrWhiteSpace(entry.FeatureId) && featureIdsToHide.Contains(entry.FeatureId))
			return true;

		// Check block configuration if context and configuration are available
		if (context?.Configuration?.Block == null)
			return false;

		// Get product IDs for this entry
		var productIds = context.EntryToBundleProducts.GetValueOrDefault(entry, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
		if (productIds.Count == 0)
			return false;

		// Check each product's block configuration
		foreach (var productId in productIds)
		{
			var blocker = GetPublishBlockerForProduct(context.Configuration.Block, productId);
			if (blocker != null && blocker.ShouldBlock(entry))
				return true;
		}

		return false;
	}

	/// <summary>
	/// Gets the publish blocker configuration for a specific product, checking product-specific overrides first
	/// </summary>
	private static PublishBlocker? GetPublishBlockerForProduct(BlockConfiguration blockConfig, string productId)
	{
		// Check product-specific override first
		if (blockConfig.ByProduct?.TryGetValue(productId, out var productBlockers) == true)
			return productBlockers.Publish;

		// Fall back to global publish blocker
		return blockConfig.Publish;
	}
}
