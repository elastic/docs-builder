// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using System.Collections.Immutable;
using Elastic.Markdown.IO.Navigation;
using Elastic.Markdown.Slices;

namespace Documentation.Assembler.Navigation;

public class GlobalNavigationHtmlWriter(
	GlobalNavigationFile navigationFile,
	AssembleContext assembleContext,
	GlobalNavigation globalNavigation,
	AssembleSources assembleSources) : INavigationHtmlWriter
{
	private readonly ConcurrentDictionary<Uri, string> _renderedNavigationCache = [];

	private ImmutableHashSet<Uri> Phantoms { get; } = [.. navigationFile.Phantoms.Select(p => p.Source)];

	private (DocumentationGroup, Uri) GetRealNavigationRoot(TableOfContentsTree tree)
	{
		if (!assembleSources.TocTopLevelMappings.TryGetValue(tree.Source, out var topLevelUri))
		{
			assembleContext.Collector.EmitWarning(assembleContext.NavigationPath.FullName, $"Could not find a top level mapping for {tree.Source}");
			return (tree, tree.Source);
		}

		if (!assembleSources.TreeCollector.TryGetTableOfContentsTree(topLevelUri.TopLevelSource, out var topLevel))
		{
			assembleContext.Collector.EmitWarning(assembleContext.NavigationPath.FullName, $"Could not find a toc tree for {topLevelUri.TopLevelSource}");
			return (tree, tree.Source);
		}

		return (topLevel, topLevelUri.TopLevelSource);
	}

	public static TableOfContentsTree? GetCurrentTree(INavigation navigation)
	{
		if (navigation is TableOfContentsTree tree)
			return tree;
		if (navigation is not DocumentationGroup group)
			return null;
		if (group.NavigationRoot is TableOfContentsTree root)
			return root;
		var i = 0;
		while (group.Parent != null)
		{
			group = group.Parent;
			if (group.Parent is TableOfContentsTree groupTree)
				return groupTree;
			if (group.NavigationRoot is TableOfContentsTree treeRoot)
				return treeRoot;
			i++;
			if (i > 100)
				throw new InvalidOperationException("Could not find parent");
		}

		return null;
	}

	public async Task<string> RenderNavigation(INavigation currentRootNavigation, Cancel ctx = default)
	{
		var tree = GetCurrentTree(currentRootNavigation)
			?? throw new InvalidOperationException(
				$"Expected a {nameof(TableOfContentsTree)} but got {currentRootNavigation.GetType().Name} for {nameof(currentRootNavigation)}"
			);

		if (Phantoms.Contains(tree.Source))
			return string.Empty;

		var (navigation, source) = GetRealNavigationRoot(tree);
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

		return value;
	}

	private NavigationViewModel CreateNavigationModel(DocumentationGroup group)
	{
		var topLevelItems = globalNavigation.TopLevelItems;
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
