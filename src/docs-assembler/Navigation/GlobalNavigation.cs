// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using Elastic.Markdown.IO;
using Elastic.Markdown.IO.Configuration;
using Elastic.Markdown.IO.Navigation;

namespace Documentation.Assembler.Navigation;

public record GlobalNavigation
{
	private readonly AssembleSources _assembleSources;
	private readonly GlobalNavigationFile _navigationFile;

	public IReadOnlyCollection<INavigationItem> NavigationItems { get; }

	public IReadOnlyCollection<TocNavigationItem> TopLevelItems { get; }

	public IReadOnlyDictionary<Uri, TocNavigationItem> NavigationLookup { get; }

	public GlobalNavigation(AssembleSources assembleSources, GlobalNavigationFile navigationFile)
	{
		_assembleSources = assembleSources;
		_navigationFile = navigationFile;
		NavigationItems = BuildNavigation(navigationFile.TableOfContents, 0);
		TopLevelItems = NavigationItems.OfType<TocNavigationItem>().ToList();
		NavigationLookup = TopLevelItems.ToDictionary(kv => kv.Source, kv => kv);
	}

	private IReadOnlyCollection<INavigationItem> BuildNavigation(IReadOnlyCollection<TocReference> node, int depth)
	{
		var list = new List<INavigationItem>();
		var i = 0;
		foreach (var toc in node)
		{
			if (toc.Source == new Uri("docs-content://reference/apm/"))
			{
			}

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

				// TODO passing DocumentationSet to TableOfContentsTree constructr is temporary
				// We only build this fallback in order to aid with bootstrapping the navigaton
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
					EnabledExtensions = documentationSet.Configuration.EnabledExtensions,
					FilesGroupedByFolder = new Dictionary<string, DocumentationFile[]>().ToFrozenDictionary(),
				};

				var fileIndex = 0;
				tree = new TableOfContentsTree(
					documentationSet,
					toc.Source,
					documentationSet.Build,
					lookups,
					_assembleSources.TreeCollector, ref fileIndex);
			}

			var tocChildren = toc.Children.OfType<TocReference>().ToArray();
			var tocNavigationItems = BuildNavigation(tocChildren, depth + 1);

			var allNavigationItems = tree.NavigationItems.Concat(tocNavigationItems);
			var cleanNavigationItems = new List<INavigationItem>();
			var seenSources = new HashSet<Uri>();
			foreach (var allNavigationItem in allNavigationItems)
			{
				if (allNavigationItem is not TocNavigationItem tocNav)
				{
					cleanNavigationItems.Add(allNavigationItem);
					continue;
				}
				if (seenSources.Contains(tocNav.Source))
					continue;

				if (!_assembleSources.TocTopLevelMappings.TryGetValue(tocNav.Source, out var mapping))
					continue;

				if (mapping.ParentSource != tree.Source)
					continue;

				_ = seenSources.Add(tocNav.Source);
				cleanNavigationItems.Add(allNavigationItem);
			}

			tree.NavigationItems = cleanNavigationItems.OrderBy(n => n.Order).ToArray();
			var navigationItem = new TocNavigationItem(i, depth, tree, toc.Source);

			list.Add(navigationItem);
			i++;
		}

		return list.ToArray().AsReadOnly();
	}
}
