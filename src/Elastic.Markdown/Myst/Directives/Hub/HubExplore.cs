// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Markdig.Syntax;

namespace Elastic.Markdown.Myst.Directives.Hub;

/// <summary>
/// Helpers for the "Explore {product}" section. Nested card-groups and link-cards
/// switch to their accordion/column rendering when they sit inside an
/// <see cref="ExploreBlock"/>, detected by walking the Markdig parent chain.
/// </summary>
internal static class HubExplore
{
	public static ExploreBlock? FindAncestor(Block? block)
	{
		for (var parent = block?.Parent; parent is not null; parent = parent.Parent)
		{
			if (parent is ExploreBlock explore)
				return explore;
		}
		return null;
	}

	/// <summary>
	/// Whether an accordion renders open on load. The <c>:mode:</c> option of the Explore stack decides:
	/// <c>collapsed</c> (default) opens none, <c>first</c> opens the first, <c>expanded</c> opens all.
	/// </summary>
	public static bool IsOpenByDefault(ExploreBlock explore, CardGroupBlock card)
	{
		switch (explore.Mode)
		{
			case ExploreMode.Expanded:
				return true;
			case ExploreMode.First:
				foreach (var child in explore)
				{
					if (child is CardGroupBlock candidate)
						return ReferenceEquals(candidate, card);
				}
				return false;
			default:
				return false;
		}
	}
}
