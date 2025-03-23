// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.IO.Configuration;
using Elastic.Markdown.IO.Navigation;

namespace Documentation.Assembler.Navigation;

public record GlobalNavigation
{
	private readonly AssembleSources _assembleSources;

	public IReadOnlyCollection<INavigationItem> NavigationItems { get; }
	public IReadOnlyCollection<TocNavigationItem> TopLevelItems { get; }


	public GlobalNavigation(AssembleSources assembleSources, GlobalNavigationFile navigationFile)
	{
		_assembleSources = assembleSources;
		NavigationItems = BuildNavigation(navigationFile.TableOfContents, 0);
		TopLevelItems = NavigationItems.OfType<TocNavigationItem>().ToList();
	}

	private IReadOnlyCollection<INavigationItem> BuildNavigation(IReadOnlyCollection<TocReference> node, int depth)
	{
		var list = new List<INavigationItem>();
		var i = 0;
		foreach (var toc in node)
		{
			if (!_assembleSources.TreeCollector.TryGetTableOfContentsTree(toc.Source, out var tree))
			{
				// TODO emit error
				continue;
			}

			list.Add(new TocNavigationItem(i, depth, tree, toc.Source));
			var tocChildren = toc.Children.OfType<TocReference>().ToArray();

			var tocNavigationItems = BuildNavigation(tocChildren, depth + 1);
			list.AddRange(tocNavigationItems);
			i++;
		}

		return list.ToArray().AsReadOnly();
	}
}
