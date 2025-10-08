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

		var nodes = navigationItems.OfType<INodeNavigationItem<TModel, INavigationItem>>().ToList();
		if (nodes.Count == 0)
			return null;

		return nodes.First().Index;

	}
}
