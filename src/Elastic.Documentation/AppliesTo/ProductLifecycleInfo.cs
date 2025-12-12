// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.AppliesTo;

/// <summary>
/// Provides consolidated metadata for product lifecycle states.
/// </summary>
public static class ProductLifecycleInfo
{
	/// <summary>
	/// Contains all display and ordering information for a lifecycle state.
	/// </summary>
	/// <param name="ShortName">Short name for badges (e.g., "Preview", "Beta", "GA").</param>
	/// <param name="DisplayText">Full display text for popovers (e.g., "Generally available", "Preview").</param>
	/// <param name="Order">Priority order for sorting (lower = higher priority, GA=0).</param>
	public sealed record LifecycleMetadata(string ShortName, string DisplayText, int Order);

	/// <summary>
	/// Gets the metadata for a given lifecycle state.
	/// </summary>
	public static LifecycleMetadata GetMetadata(ProductLifecycle lifecycle) =>
		Metadata.GetValueOrDefault(lifecycle, FallbackMetadata);

	/// <summary>
	/// Gets the short name for a lifecycle (e.g., "Preview", "Beta", "GA").
	/// Used for badge CSS classes and compact display.
	/// </summary>
	public static string GetShortName(ProductLifecycle lifecycle) =>
		GetMetadata(lifecycle).ShortName;

	/// <summary>
	/// Gets the full display text for a lifecycle (e.g., "Generally available", "Preview").
	/// Used in popover availability text.
	/// </summary>
	public static string GetDisplayText(ProductLifecycle lifecycle) =>
		GetMetadata(lifecycle).DisplayText;

	/// <summary>
	/// Gets the sort order for a lifecycle (lower = higher priority).
	/// GA=0, Beta=1, Preview=2, etc.
	/// </summary>
	public static int GetOrder(ProductLifecycle lifecycle) =>
		GetMetadata(lifecycle).Order;

	private static readonly LifecycleMetadata FallbackMetadata = new("", "", 999);

	private static readonly Dictionary<ProductLifecycle, LifecycleMetadata> Metadata = new()
	{
		[ProductLifecycle.GenerallyAvailable] = new("GA", "Generally available", 0),
		[ProductLifecycle.Beta] = new("Beta", "Beta", 1),
		[ProductLifecycle.TechnicalPreview] = new("Preview", "Preview", 2),
		[ProductLifecycle.Planned] = new("Planned", "Planned", 3),
		[ProductLifecycle.Deprecated] = new("Deprecated", "Deprecated", 4),
		[ProductLifecycle.Removed] = new("Removed", "Removed", 5),
		[ProductLifecycle.Unavailable] = new("Unavailable", "Unavailable", 6),
		[ProductLifecycle.Development] = new("Development", "Development", 7),
		[ProductLifecycle.Discontinued] = new("Discontinued", "Discontinued", 8),
	};
}

