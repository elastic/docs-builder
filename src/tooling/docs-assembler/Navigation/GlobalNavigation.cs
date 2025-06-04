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

	public IReadOnlyCollection<TocNavigationItem> TopLevelItems { get; }

	public IReadOnlyDictionary<Uri, TocNavigationItem> NavigationLookup { get; }

	public FrozenDictionary<string, INavigationItem> MarkdownNavigationLookup { get; }

	public FrozenDictionary<int, MarkdownFile> MarkdownFiles { get; set; }


	public GlobalNavigation(AssembleSources assembleSources, GlobalNavigationFile navigationFile)
	{
		_assembleSources = assembleSources;
		_navigationFile = navigationFile;
		NavigationItems = BuildNavigation(navigationFile.TableOfContents, 0);
		var navigationIndex = 0;
		var markdownFiles = new HashSet<MarkdownFile>();
		UpdateNavigationIndex(markdownFiles, NavigationItems, null, ref navigationIndex);
		TopLevelItems = NavigationItems.OfType<TocNavigationItem>().ToList();
		NavigationLookup = TopLevelItems.ToDictionary(kv => kv.Source, kv => kv);
		var grouped = markdownFiles.GroupBy(f => f.NavigationIndex).ToList();
		var files = grouped
			.Select(g => g.First())
			.ToList();

		MarkdownFiles = files.Where(f => f.NavigationIndex > -1).ToDictionary(i => i.NavigationIndex, i => i).ToFrozenDictionary();

		MarkdownNavigationLookup = NavigationItems
			.SelectMany(DocumentationSet.Pairs)
			.ToDictionary(kv => kv.Item1, kv => kv.Item2)
			.ToFrozenDictionary();
	}

	private static void UpdateNavigationIndex(
		HashSet<MarkdownFile> markdownFiles,
		IReadOnlyCollection<INavigationItem> navigationItems,
		INavigationItem? parent,
		ref int navigationIndex
	)
	{
		foreach (var item in navigationItems)
		{
			switch (item)
			{
				case FileNavigationItem fileNavigationItem:
					var fileIndex = Interlocked.Increment(ref navigationIndex);
					fileNavigationItem.File.NavigationIndex = fileIndex;
					fileNavigationItem.Parent = parent;
					_ = markdownFiles.Add(fileNavigationItem.File);
					break;
				case GroupNavigationItem { DocumentationGroup.Index: not null } groupNavigationItem:
					var index = Interlocked.Increment(ref navigationIndex);
					groupNavigationItem.DocumentationGroup.Index.NavigationIndex = index;
					groupNavigationItem.Parent = parent;
					_ = markdownFiles.Add(groupNavigationItem.DocumentationGroup.Index);
					UpdateNavigationIndex(markdownFiles, groupNavigationItem.Group.NavigationItems, groupNavigationItem, ref navigationIndex);
					break;
				case DocumentationGroup { Index: not null } documentationGroup:
					var groupIndex = Interlocked.Increment(ref navigationIndex);
					documentationGroup.Index.NavigationIndex = groupIndex;
					documentationGroup.Parent = parent;
					_ = markdownFiles.Add(documentationGroup.Index);
					UpdateNavigationIndex(markdownFiles, documentationGroup.NavigationItems, documentationGroup, ref navigationIndex);
					break;

			}
		}
	}

	private IReadOnlyCollection<INavigationItem> BuildNavigation(IReadOnlyCollection<TocReference> node, int depth, INavigationItem? parent = null)
	{
		var list = new List<INavigationItem>();
		foreach (var toc in node)
		{
			if (!_assembleSources.TreeCollector.TryGetTableOfContentsTree(toc.Source, out var tree))
			{
				_navigationFile.EmitWarning($"No {nameof(TableOfContentsTree)} found for {toc.Source}");
				if (!_assembleSources.TocTopLevelMappings.TryGetValue(toc.Source, out var topLevel))
				{
					_navigationFile.EmitError(
						$"Can not create temporary {nameof(TableOfContentsTree)} for {toc.Source} since no top level source could be located for it"
					);
					continue;
				}

				// TODO passing DocumentationSet to TableOfContentsTree constructor is temporary
				// We only build this fallback in order to aid with bootstrapping the navigation
				if (!_assembleSources.TreeCollector.TryGetTableOfContentsTree(topLevel.TopLevelSource, out tree))
				{
					_navigationFile.EmitError(
						$"Can not create temporary {nameof(TableOfContentsTree)} for {topLevel.TopLevelSource} since no top level source could be located for it"
					);
					continue;
				}

				var documentationSet = tree.DocumentationSet ?? (tree.Parent as TableOfContentsTree)?.DocumentationSet
					?? throw new InvalidOperationException($"Can not fall back for {toc.Source} because no documentation set is available");

				var lookups = new NavigationLookups
				{
					FlatMappedFiles = new Dictionary<string, DocumentationFile>().ToFrozenDictionary(),
					TableOfContents = [],
					EnabledExtensions = documentationSet.EnabledExtensions,
					FilesGroupedByFolder = new Dictionary<string, DocumentationFile[]>().ToFrozenDictionary(),
				};

				var fileIndex = 0;
				tree = new TableOfContentsTree(
					documentationSet,
					toc.Source,
					documentationSet.Context,
					lookups,
					_assembleSources.TreeCollector, ref fileIndex);
			}

			var navigationItem = new TocNavigationItem(depth, tree, toc.Source, parent);
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
				if (item is not TocNavigationItem tocNav)
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
		}

		return list.ToArray().AsReadOnly();
	}

	public MarkdownFile? GetPrevious(MarkdownFile current)
	{
		var index = current.NavigationIndex;
		do
		{
			var previous = MarkdownFiles.GetValueOrDefault(index - 1);
			if (previous is null)
				return null;
			if (!previous.Hidden)
				return previous;
			index--;
		} while (index >= 0);

		return null;
	}

	public MarkdownFile? GetNext(MarkdownFile current)
	{
		var index = current.NavigationIndex;
		do
		{
			var previous = MarkdownFiles.GetValueOrDefault(index + 1);
			if (previous is null)
				return null;
			if (!previous.Hidden)
				return previous;
			index++;
		} while (index <= MarkdownFiles.Count);

		return null;
	}
}
