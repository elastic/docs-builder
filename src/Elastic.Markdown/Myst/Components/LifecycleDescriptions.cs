// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.AppliesTo;

namespace Elastic.Markdown.Myst.Components;

/// <summary>
/// Contains static lifecycle descriptions for use in applicability popovers.
/// </summary>
public static class LifecycleDescriptions
{
	/// <summary>
	/// Gets the lifecycle description for a given lifecycle and release state.
	/// The returned text may contain a {product} placeholder that should be replaced with the product name.
	/// </summary>
	/// <param name="lifecycle">The product lifecycle state.</param>
	/// <param name="isReleased">Whether the version is released.</param>
	/// <returns>The description text, or null if not applicable.</returns>
	private static string? GetDescription(ProductLifecycle lifecycle, bool isReleased) =>
		Descriptions.GetValueOrDefault((lifecycle, isReleased));

	/// <summary>
	/// Gets the lifecycle description with the product name substituted.
	/// </summary>
	/// <param name="lifecycle">The product lifecycle state.</param>
	/// <param name="isReleased">Whether the version is released.</param>
	/// <param name="productName">The product name to substitute for {product}.</param>
	/// <returns>The description text with product name substituted, or null if not applicable.</returns>
	public static string? GetDescriptionWithProduct(ProductLifecycle lifecycle, bool isReleased, string productName)
	{
		var description = GetDescription(lifecycle, isReleased);
		return description?.Replace("{product}", productName);
	}

	private static readonly Dictionary<(ProductLifecycle Lifecycle, bool IsReleased), string> Descriptions = new()
	{
		// Preview
		[(ProductLifecycle.TechnicalPreview, true)] =
			"This functionality is in technical preview and is not ready for production usage. Technical preview features may change or be removed at any time. Elastic will work to fix any issues, but features in technical preview are not subject to the support SLA of official GA features. Specific Support terms apply.",
		[(ProductLifecycle.TechnicalPreview, false)] =
			"We plan to add this functionality in a future {product} update. Subject to changes.",

		// Beta
		[(ProductLifecycle.Beta, true)] =
			"This functionality is in beta and is not ready for production usage. For beta features, the design and code is less mature than official GA features and is being provided as-is with no warranties. Beta features are not subject to the support SLA of official GA features. Specific Support terms apply.",
		[(ProductLifecycle.Beta, false)] =
			"We plan to add this functionality in a future {product} update. Subject to changes.",

		// GA
		[(ProductLifecycle.GenerallyAvailable, true)] =
			"This functionality is generally available and ready for production usage.",
		[(ProductLifecycle.GenerallyAvailable, false)] =
			"We plan to add this functionality in a future {product} update. Subject to changes.",

		// Deprecated
		[(ProductLifecycle.Deprecated, true)] =
			"This functionality is deprecated. You can still use it, but it'll be removed in a future {product} update.",
		[(ProductLifecycle.Deprecated, false)] =
			"This functionality is planned to be deprecated in a future {product} update. Subject to changes.",

		// Removed
		[(ProductLifecycle.Removed, true)] =
			"This functionality was removed. You can no longer use it if you're running on this version or a later one.",
		[(ProductLifecycle.Removed, false)] =
			"This functionality is planned to be removed in an upcoming {product} update. Subject to changes.",

		// Unavailable
		[(ProductLifecycle.Unavailable, true)] =
			"This functionality is not available in {product}."
	};
}

