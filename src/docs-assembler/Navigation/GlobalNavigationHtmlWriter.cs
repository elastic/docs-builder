// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using Elastic.Markdown.IO.Configuration;
using Elastic.Markdown.IO.Navigation;
using Elastic.Markdown.Slices;

namespace Documentation.Assembler.Navigation;

public class GlobalNavigationHtmlWriter(AssembleContext assembleContext, GlobalNavigation navigation, AssembleSources assembleSources) : INavigationHtmlWriter
{
	private readonly AssembleContext _assembleContext = assembleContext;
	private readonly ConcurrentDictionary<Uri, string> _renderedNavigationCache = [];

	private (DocumentationGroup, Uri) GetRealNavigationRoot(INavigation navigation)
	{
		if (navigation is not DocumentationGroup group)
			throw new InvalidOperationException($"Expected a {nameof(DocumentationGroup)}");


		if (group.NavigationRoot is TableOfContentsTree tree)
		{
			if (!assembleSources.TocTopLevelMappings.TryGetValue(tree.Source, out var topLevelUri))
			{
				_assembleContext.Collector.EmitWarning(_assembleContext.NavigationPath.FullName, $"Could not find a top level mapping for {tree.Source}");
				return (tree, tree.Source);
			}

			if (!assembleSources.TreeCollector.TryGetTableOfContentsTree(topLevelUri.TopLevelSource, out var topLevel))
			{
				_assembleContext.Collector.EmitWarning(_assembleContext.NavigationPath.FullName, $"Could not find a toc tree for {topLevelUri.TopLevelSource}");
				return (tree, tree.Source);

			}
			return (topLevel, topLevelUri.TopLevelSource);

		}
		else if (group.NavigationRoot is DocumentationGroup)
		{
			var source = group.FolderName == "reference/index.md"
				? new Uri("docs-content://reference/")
				: throw new InvalidOperationException($"{group.FolderName} is not a valid navigation root");
			return (group, source);
		}
		throw new InvalidOperationException($"Unknown navigation root {group.NavigationRoot}");
	}

	public async Task<string> RenderNavigation(INavigation currentRootNavigation, Cancel ctx = default)
	{
		var (navigation, source) = GetRealNavigationRoot(currentRootNavigation);
		if (_renderedNavigationCache.TryGetValue(source, out var value))
			return value;

		if (source == new Uri("docs-content:///"))
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
		var topLevelItems = navigation.TopLevelItems;
		return new NavigationViewModel
		{
			Title = group.Index?.NavigationTitle ?? "Docs",
			TitleUrl = group.Index?.Url ?? "/",
			Tree = group,
			IsPrimaryNavEnabled = true,
			IsGlobalAssemblyBuild = true,
			TopLevelItems = topLevelItems
		};
	}
}
