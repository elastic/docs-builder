// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using Elastic.Documentation.Configuration.TableOfContents;
using Elastic.Documentation.Site.Navigation;
using Elastic.Markdown.IO;
using Elastic.Markdown.IO.Navigation;

namespace Documentation.Assembler.Navigation;

public record GlobalNavigation : IPositionalNavigation
{
	private readonly AssembleSources _assembleSources;
	private readonly GlobalNavigationFile _navigationFile;

	public IReadOnlyCollection<INavigationItem> NavigationItems { get; }

	public IReadOnlyCollection<TableOfContentsTree> TopLevelItems { get; }

	public IReadOnlyDictionary<Uri, TableOfContentsTree> NavigationLookup { get; }

	public FrozenDictionary<string, INavigationItem> MarkdownNavigationLookup { get; }

	public FrozenDictionary<int, INavigationItem> NavigationIndexedByOrder { get; }

	public GlobalNavigation(AssembleSources assembleSources, GlobalNavigationFile navigationFile)
	{
		_assembleSources = assembleSources;
		_navigationFile = navigationFile;
		NavigationItems = BuildNavigation(navigationFile.TableOfContents.Concat(navigationFile.Phantoms).ToArray(), 0);
		var navigationIndex = 0;
		var allNavigationItems = new HashSet<INavigationItem>();
		UpdateNavigationIndex(allNavigationItems, NavigationItems, null, ref navigationIndex);
		TopLevelItems = NavigationItems.OfType<TableOfContentsTree>().Where(t => !t.Hidden).ToList();
		NavigationLookup = TopLevelItems.ToDictionary(kv => kv.Source, kv => kv);

		NavigationIndexedByOrder = allNavigationItems.ToDictionary(i => i.NavigationIndex, i => i).ToFrozenDictionary();

		MarkdownNavigationLookup = NavigationItems
			.SelectMany(DocumentationSet.Pairs)
			.ToDictionary(kv => kv.Item1, kv => kv.Item2)
			.ToFrozenDictionary();
	}

	private void UpdateNavigationIndex(
		HashSet<INavigationItem> allNavigationItems,
		IReadOnlyCollection<INavigationItem> navigationItems,
		INodeNavigationItem<INavigationModel, INavigationItem>? parent,
		ref int navigationIndex
	)
	{
		foreach (var item in navigationItems)
		{
			switch (item)
			{
				case FileNavigationItem fileNavigationItem:
					var fileIndex = Interlocked.Increment(ref navigationIndex);
					fileNavigationItem.NavigationIndex = fileIndex;
					if (parent is not null)
						fileNavigationItem.Parent = parent;
					_ = allNavigationItems.Add(fileNavigationItem);
					break;
				case DocumentationGroup documentationGroup:
					var groupIndex = Interlocked.Increment(ref navigationIndex);
					documentationGroup.NavigationIndex = groupIndex;
					if (parent is not null)
						documentationGroup.Parent = parent;
					_ = allNavigationItems.Add(documentationGroup);
					UpdateNavigationIndex(allNavigationItems, documentationGroup.NavigationItems, documentationGroup, ref navigationIndex);
					break;
				default:
					_navigationFile.EmitError($"Unhandled navigation item type: {item.GetType()}");
					break;
			}
		}
	}

	private IReadOnlyCollection<INavigationItem> BuildNavigation(IReadOnlyCollection<TocReference> node, int depth)
	{
		var list = new List<INavigationItem>();
		foreach (var toc in node)
		{
			if (!_assembleSources.TreeCollector.TryGetTableOfContentsTree(toc.Source, out var tree))
			{
				_navigationFile.EmitError($"{toc.Source} does not define a toc.yml or docset.yml file");
				continue;
			}

			var navigationItem = tree;
			var tocChildren = toc.Children.OfType<TocReference>().ToArray();
			var tocNavigationItems = BuildNavigation(tocChildren, depth + 1);

			var allNavigationItems =
				depth == 0
					? tocNavigationItems.Concat(tree.NavigationItems)
					: tree.NavigationItems.Concat(tocNavigationItems);

			var cleanNavigationItems = new List<INavigationItem>();
			var seenSources = new HashSet<Uri>();
			foreach (var item in allNavigationItems)
			{
				if (item is not TableOfContentsTree tocNav)
				{
					cleanNavigationItems.Add(item);
					continue;
				}

				if (seenSources.Contains(tocNav.Source))
					continue;

				if (!_assembleSources.TocTopLevelMappings.TryGetValue(tocNav.Source, out var mapping))
					continue;

				if (mapping.ParentSource != tree.Source)
					continue;

				_ = seenSources.Add(tocNav.Source);
				cleanNavigationItems.Add(item);
				item.Parent = navigationItem;
			}

			tree.NavigationItems = cleanNavigationItems.ToArray();
			list.Add(navigationItem);

			if (toc.IsPhantom)
				navigationItem.Hidden = true;
		}

		return list.ToArray().AsReadOnly();
	}
}
