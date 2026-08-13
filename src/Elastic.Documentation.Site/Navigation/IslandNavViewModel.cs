// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.Navigation;

/// <summary>A single back-link in the island sidebar's breadcrumb trail.</summary>
public sealed record IslandBackLink(string Title, string Url);

/// <summary>
/// Everything <c>_IslandNav.cshtml</c> renders.
/// The tree is the same <see cref="NavigationRenderNode"/> projection the main sidebar consumes,
/// rooted at the island instead of at the navigation root.
/// </summary>
public sealed record IslandNavViewModel
{
	/// <summary>
	/// Root-first trail out of the island: the top navigation root, each enclosing island,
	/// and the island's immediate parent. Deduped so the root/immediate-parent never appear twice.
	/// </summary>
	public required IReadOnlyList<IslandBackLink> BackLinks { get; init; }

	/// <summary>Display title of the island root node.</summary>
	public required string NavigationTitle { get; init; }

	/// <summary>URL of the island root node (its index page).</summary>
	public required string Url { get; init; }

	/// <summary>Navigation tree rooted at the island, using the same node types as the main sidebar.</summary>
	public required IReadOnlyList<NavigationRenderNode> Tree { get; init; }

	/// <summary>
	/// SHA-256 fragment of the island tree content.
	/// Drives the <c>nav-tree-*</c> <c>hx-preserve</c> id so expand/collapse state survives
	/// htmx navigations within the island.
	/// </summary>
	public required string ContentHash { get; init; }
}
