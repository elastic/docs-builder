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

	/// <summary>The first accordion in an Explore stack is expanded by default.</summary>
	public static bool IsFirstCardGroup(ExploreBlock explore, CardGroupBlock card)
	{
		foreach (var child in explore)
		{
			if (child is CardGroupBlock candidate)
				return ReferenceEquals(candidate, card);
		}
		return false;
	}
}
