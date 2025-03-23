// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using Elastic.Markdown.IO.Configuration;
using Elastic.Markdown.IO.Navigation;
using Elastic.Markdown.Slices;

namespace Documentation.Assembler.Navigation;

public class GlobalNavigationHtmlWriter(GlobalNavigation navigation) : INavigationHtmlWriter
{
	private readonly GlobalNavigation _navigation = navigation;
	private readonly ConcurrentDictionary<Uri, string> _renderedNavigationCache = [];

	private (DocumentationGroup, Uri) GetRealNavigationRoot(INavigation navigation)
	{
		if (navigation is not DocumentationGroup group)
			throw new InvalidOperationException($"Expected a {nameof(DocumentationGroup)}");

		Uri? source = null;
		if (group.NavigationRoot is TableOfContentsTree tree)
			source = _navigation.TopLevelLookup.TryGetValue(tree.Source, out var s) ? s : tree.Source;
		else if (group.NavigationRoot is DocumentationGroup groupParent)
		{
			source = group.FolderName == "reference/index.md"
				? new Uri("docs-content://reference/")
				: throw new InvalidOperationException($"Expected a {nameof(TableOfContentsTree)}");
		}

		if (source == new Uri("docs-content:///") || source == new Uri("docs-content://reference/"))
			return (group, source);

		if (!_navigation.TreeLookup.TryGetValue(source!, out var topLevel))
		{
		}


		return (topLevel!, source!);
	}

	public async Task<string> RenderNavigation(INavigation currentRootNavigation, Cancel ctx = default)
	{
		var (navigation, source) = GetRealNavigationRoot(currentRootNavigation);
		if (_renderedNavigationCache.TryGetValue(source, out var value))
			return value;

		if (source == new Uri("docs-content:///") || source == new Uri("docs-content://reference/"))
		{
			_renderedNavigationCache[source] = string.Empty;
			return string.Empty;
		}

		Console.WriteLine($"Rendering navigation for {source}");

		var model = CreateNavigationModel(navigation);
		value = await ((INavigationHtmlWriter)this).Render(model, ctx);
		_renderedNavigationCache[source] = value;
		if (source == new Uri("docs-content://extend"))
		{
		}


		return value;
	}

	private NavigationViewModel CreateNavigationModel(DocumentationGroup group)
	{
		var topLevelItems = _navigation.TopLevelItems;
		return new NavigationViewModel
		{
			Title = group.Index?.NavigationTitle ?? "Docs",
			TitleUrl = group.Index?.Url ?? "/",
			Tree = group,
			IsPrimaryNavEnabled = true,
			TopLevelItems = topLevelItems
		};
	}
}
