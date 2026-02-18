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
	/// Determines if an entry should be hidden based on feature IDs or rules configuration
	/// </summary>
	public static bool ShouldHideEntry(
		ChangelogEntry entry,
		HashSet<string> featureIdsToHide,
		ChangelogRenderContext? context = null)
	{
		// Check feature IDs first
		if (!string.IsNullOrWhiteSpace(entry.FeatureId) && featureIdsToHide.Contains(entry.FeatureId))
			return true;

		// Check rules configuration if context and configuration are available
		if (context?.Configuration?.Rules?.Publish == null)
			return false;

		// Get product IDs for this entry
		var productIds = context.EntryToBundleProducts.GetValueOrDefault(entry, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
		if (productIds.Count == 0)
			return false;

		// Check each product's publish configuration
		foreach (var productId in productIds)
		{
			var blocker = GetPublishBlockerForProduct(context.Configuration.Rules.Publish, productId);
			if (blocker != null && blocker.ShouldBlock(entry))
				return true;
		}

		return false;
	}

	/// <summary>
	/// Gets the publish blocker configuration for a specific product, checking product-specific overrides first
	/// </summary>
	private static PublishBlocker? GetPublishBlockerForProduct(PublishRules publishRules, string productId)
	{
		// Check product-specific override first
		if (publishRules.ByProduct?.TryGetValue(productId, out var productBlocker) == true)
			return productBlocker;

		// Fall back to global publish blocker
		return publishRules.Blocker;
	}
}
