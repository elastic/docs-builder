// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Navigation.Isolated;

namespace Elastic.Documentation.Navigation;

public static class NavigationItemExtensions
{
	public static ILeafNavigationItem<TModel> QueryIndex<TModel>(
		this IReadOnlyCollection<INavigationItem> items, INodeNavigationItem<INavigationModel, INavigationItem> node, string fallbackPath, out IReadOnlyCollection<INavigationItem> children
	)
		where TModel : class, IDocumentationFile
	{
		var index = LookupIndex();

		children = items.Except([index]).ToArray();

		return index;

		ILeafNavigationItem<TModel> LookupIndex()
		{
			foreach (var item in items)
			{
				// Check for the exact type match
				if (item is ILeafNavigationItem<TModel> leaf)
					return leaf;

				// Check if this is a node navigation item and return its index
				if (item is INodeNavigationItem<TModel, INavigationItem> nodeItem)
					return nodeItem.Index;
			}

			// If no index is found, throw an exception
			throw new InvalidOperationException($"No index found for navigation node '{node.GetType().Name}' at path '{fallbackPath}'");
		}
	}

	public static int UpdateNavigationIndex<TModel>(this IRootNavigationItem<TModel, INavigationItem> node, IDocumentationContext context)
		where TModel : IDocumentationFile
	{
		var navigationIndex = -1;
		ProcessNavigationItem(context, ref navigationIndex, node);
		return navigationIndex;

	}

	private static void UpdateNavigationIndex(IReadOnlyCollection<INavigationItem> navigationItems, IDocumentationContext context, ref int navigationIndex)
	{
		foreach (var item in navigationItems)
			ProcessNavigationItem(context, ref navigationIndex, item);
	}

	private static void ProcessNavigationItem(IDocumentationContext context, ref int navigationIndex, INavigationItem item)
	{
		switch (item)
		{
			case ILeafNavigationItem<INavigationModel> leaf:
				var fileIndex = Interlocked.Increment(ref navigationIndex);
				leaf.NavigationIndex = fileIndex;
				break;
			case INodeNavigationItem<INavigationModel, INavigationItem> node:
				var groupIndex = Interlocked.Increment(ref navigationIndex);
				node.NavigationIndex = groupIndex;
				node.Index.NavigationIndex = groupIndex;
				UpdateNavigationIndex(node.NavigationItems, context, ref navigationIndex);
				break;
			default:
				context.EmitError(context.ConfigurationPath, $"{nameof(UpdateNavigationIndex)}: Unhandled navigation item type: {item.GetType()}");
				break;
		}
	}
}
