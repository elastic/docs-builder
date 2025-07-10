// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.Collections.Immutable;
using Elastic.Documentation.Configuration.Assembler;
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

#pragma warning disable IDE0052
	private ImmutableHashSet<Uri> Phantoms { get; }
#pragma warning restore IDE0052

	private TableOfContentsTree RootContentTree { get; }

	public GlobalNavigation(AssembleSources assembleSources, GlobalNavigationFile navigationFile)
	{
		_assembleSources = assembleSources;
		_navigationFile = navigationFile;

		// the root files of `docs-content://` are special they contain several special pages such as 404, archive, versions etc.
		// we inject them forcefully here
		var source = new Uri($"{NarrativeRepository.RepositoryName}://");
		RootContentTree = assembleSources.TreeCollector.TryGetTableOfContentsTree(source, out var docsContentTree)
			? docsContentTree
			: throw new Exception($"Could not locate: {source} as root of global navigation.");
		Phantoms = [.. navigationFile.Phantoms.Select(p => p.Source)];
		NavigationItems = BuildNavigation(navigationFile.TableOfContents, 0);

		var navigationIndex = 0;
		var allNavigationItems = new HashSet<INavigationItem>();
		UpdateParent(allNavigationItems, NavigationItems, null);
		UpdateNavigationIndex(NavigationItems, ref navigationIndex);
		TopLevelItems = NavigationItems.OfType<TableOfContentsTree>().Where(t => !t.Hidden).ToList();
		NavigationLookup = TopLevelItems.ToDictionary(kv => kv.Source, kv => kv);

		NavigationIndexedByOrder = allNavigationItems.ToDictionary(i => i.NavigationIndex, i => i).ToFrozenDictionary();

		MarkdownNavigationLookup = NavigationItems
			.SelectMany(DocumentationSet.Pairs)
			.ToDictionary(kv => kv.Item1, kv => kv.Item2)
			.ToFrozenDictionary();

	}

	private void UpdateParent(
		HashSet<INavigationItem> allNavigationItems,
		IReadOnlyCollection<INavigationItem> navigationItems,
		INodeNavigationItem<INavigationModel, INavigationItem>? parent
	)
	{
		foreach (var item in navigationItems)
		{
			switch (item)
			{
				case FileNavigationItem fileNavigationItem:
					if (parent is not null)
						fileNavigationItem.Parent = parent;
					_ = allNavigationItems.Add(fileNavigationItem);
					break;
				case DocumentationGroup documentationGroup:
					if (parent is not null)
						documentationGroup.Parent = parent;
					_ = allNavigationItems.Add(documentationGroup);
					UpdateParent(allNavigationItems, documentationGroup.NavigationItems, documentationGroup);
					break;
				default:
					_navigationFile.EmitError($"Unhandled navigation item type: {item.GetType()}");
					break;
			}
		}
	}


	private void UpdateNavigationIndex(IReadOnlyCollection<INavigationItem> navigationItems, ref int navigationIndex)
	{
		foreach (var item in navigationItems)
		{
			switch (item)
			{
				case FileNavigationItem fileNavigationItem:
					var fileIndex = Interlocked.Increment(ref navigationIndex);
					fileNavigationItem.NavigationIndex = fileIndex;
					break;
				case DocumentationGroup documentationGroup:
					var groupIndex = Interlocked.Increment(ref navigationIndex);
					documentationGroup.NavigationIndex = groupIndex;
					UpdateNavigationIndex(documentationGroup.NavigationItems, ref navigationIndex);
					break;
				default:
					_navigationFile.EmitError($"Unhandled navigation item type: {item.GetType()}");
					break;
			}
		}
	}

	private IReadOnlyCollection<INavigationItem> BuildNavigation(IReadOnlyCollection<TocReference> references, int depth)
	{
		var list = new List<INavigationItem>();
		foreach (var tocReference in references)
		{
			if (!_assembleSources.TreeCollector.TryGetTableOfContentsTree(tocReference.Source, out var tree))
			{
				_navigationFile.EmitError($"{tocReference.Source} does not define a toc.yml or docset.yml file");
				continue;
			}

			var tocChildren = tocReference.Children.OfType<TocReference>().ToArray();
			var tocNavigationItems = BuildNavigation(tocChildren, depth + 1);

			if (depth == 0 && tree.Parent != RootContentTree)
			{
				tree.Parent = RootContentTree;
				tree.Index.NavigationRoot = tree;
			}

			var configuredNavigationItems =
				depth == 0
					? tocNavigationItems.Concat(tree.NavigationItems)
					: tree.NavigationItems.Concat(tocNavigationItems);

			var cleanNavigationItems = new List<INavigationItem>();
			var seenSources = new HashSet<Uri>();
			foreach (var item in configuredNavigationItems)
			{
				if (item is not TableOfContentsTree tocNav)
				{
					cleanNavigationItems.Add(item);
					continue;
				}

				if (seenSources.Contains(tocNav.Source))
					continue;

				if (Phantoms.Contains(tree.NavigationSource))
					continue;

				// toc is not part of `navigation.yml`
				if (!_assembleSources.NavigationTocMappings.TryGetValue(tocNav.Source, out var mapping))
					continue;

				// this TOC was moved in navigation.yml to a new parent and should not be part of the current navigation items
				if (mapping.ParentSource != tree.Source)
					continue;

				_ = seenSources.Add(tocNav.Source);
				cleanNavigationItems.Add(item);
				item.Parent = tree;
			}

			tree.NavigationItems = cleanNavigationItems.ToArray();
			list.Add(tree);

			if (tocReference.IsPhantom)
				tree.Hidden = true;
		}

		if (depth != 0)
			return list.ToArray().AsReadOnly();

		// the root files of `docs-content://` are special they contain several special pages such as 404, archive, versions etc.
		// we inject them forcefully here
		if (!RootContentTree.NavigationItems.OfType<FileNavigationItem>().Any())
			_navigationFile.EmitError($"Could not inject root file navigation items from: {RootContentTree.Source}.");
		else
		{
			var filesAtRoot = RootContentTree.NavigationItems.OfType<FileNavigationItem>().ToArray();
			list.AddRange(filesAtRoot);
			// ensure index exist as a single item rather than injecting the whole tree (which already exists in the returned list)
			var index = new FileNavigationItem(RootContentTree.Index, RootContentTree, RootContentTree.Hidden);
			list.Add(index);
		}

		return list.ToArray().AsReadOnly();
	}
}
