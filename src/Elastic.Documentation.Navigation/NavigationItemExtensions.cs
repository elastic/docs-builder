// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Navigation;

public static class NavigationItemExtensions
{
	public static ILeafNavigationItem<TModel>? FindIndex<TModel>(this IReadOnlyCollection<INavigationItem> navigationItems)
		where TModel : INavigationModel
	{
		var leaf = navigationItems.OfType<ILeafNavigationItem<TModel>>().FirstOrDefault();
		if (leaf is not null)
			return leaf;

		var nodes = navigationItems.OfType<INodeNavigationItem<INavigationModel, INavigationItem>>().ToList();
		if (nodes.Count == 0)
			return null;

		var topLevelLeafs = nodes.First().NavigationItems.OfType<ILeafNavigationItem<TModel>>().ToList();
		if (topLevelLeafs.Count == 0)
			return null;

		foreach (var node in nodes)
		{
			var index = node.NavigationItems.FindIndex<TModel>();
			if (index is not null)
				return index;
		}

		return null;

	}
}
