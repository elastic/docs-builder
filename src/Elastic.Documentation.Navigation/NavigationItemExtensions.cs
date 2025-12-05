// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.Runtime.CompilerServices;
using Elastic.Documentation.Navigation.Isolated;
using Elastic.Documentation.Navigation.Isolated.Leaf;

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

	/// <summary>
	/// Builds navigation lookups by traversing the navigation tree and populating both the
	/// NavigationDocumentationFileLookup and NavigationIndexedByOrder collections.
	/// </summary>
	/// <param name="rootItem">The root navigation item to start traversing from</param>
	/// <param name="navigationDocumentationFileLookup">The ConditionalWeakTable to populate with file-to-navigation mappings</param>
	/// <returns>A frozen dictionary mapping navigation indices to navigation items</returns>
	public static FrozenDictionary<int, INavigationItem> BuildNavigationLookups(
		this INavigationItem rootItem, ConditionalWeakTable<IDocumentationFile, INavigationItem> navigationDocumentationFileLookup
	)
	{
		var navigationByOrder = new Dictionary<int, INavigationItem>();
		BuildNavigationLookupsRecursive(rootItem, navigationDocumentationFileLookup, navigationByOrder);
		return navigationByOrder.ToFrozenDictionary();
	}

	/// <summary>
	/// Recursively builds both NavigationDocumentationFileLookup and NavigationIndexedByOrder in a single traversal
	/// </summary>
	private static void BuildNavigationLookupsRecursive(
		INavigationItem item,
		ConditionalWeakTable<IDocumentationFile, INavigationItem> navigationDocumentationFileLookup,
		Dictionary<int, INavigationItem> navigationByOrder)
	{
		switch (item)
		{
			// CrossLinkNavigationLeaf is not added to NavigationDocumentationFileLookup or NavigationIndexedByOrder
			case CrossLinkNavigationLeaf:
				break;
			case ILeafNavigationItem<IDocumentationFile> documentationFileLeaf:
				_ = navigationDocumentationFileLookup.TryAdd(documentationFileLeaf.Model, documentationFileLeaf);
				_ = navigationByOrder.TryAdd(documentationFileLeaf.NavigationIndex, documentationFileLeaf);
				break;
			case ILeafNavigationItem<INavigationModel> leaf:
				_ = navigationByOrder.TryAdd(leaf.NavigationIndex, leaf);
				break;
			case INodeNavigationItem<IDocumentationFile, INavigationItem> documentationFileNode:
				_ = navigationDocumentationFileLookup.TryAdd(documentationFileNode.Index.Model, documentationFileNode);
				_ = navigationByOrder.TryAdd(documentationFileNode.NavigationIndex, documentationFileNode);
				_ = navigationByOrder.TryAdd(documentationFileNode.Index.NavigationIndex, documentationFileNode.Index);
				foreach (var child in documentationFileNode.NavigationItems)
					BuildNavigationLookupsRecursive(child, navigationDocumentationFileLookup, navigationByOrder);
				break;
			case INodeNavigationItem<INavigationModel, INavigationItem> node:
				_ = navigationByOrder.TryAdd(node.NavigationIndex, node);
				foreach (var child in node.NavigationItems)
					BuildNavigationLookupsRecursive(child, navigationDocumentationFileLookup, navigationByOrder);
				break;
		}
	}
}
