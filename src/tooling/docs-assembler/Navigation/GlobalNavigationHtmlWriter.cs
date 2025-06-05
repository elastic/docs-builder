// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Elastic.Documentation.Site;
using Elastic.Documentation.Site.Navigation;
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

	private bool TryGetNavigationRoot(
		Uri navigationSource,
		[NotNullWhen(true)] out TableOfContentsTree? navigationRoot,
		[NotNullWhen(true)] out Uri? navigationRootSource
	)
	{
		navigationRoot = null;
		navigationRootSource = null;
		if (!assembleSources.TocTopLevelMappings.TryGetValue(navigationSource, out var topLevelMapping))
		{
			assembleContext.Collector.EmitWarning(assembleContext.NavigationPath.FullName, $"Could not find a top level mapping for {navigationSource}");
			return false;
		}

		if (!assembleSources.TreeCollector.TryGetTableOfContentsTree(topLevelMapping.TopLevelSource, out navigationRoot))
		{
			assembleContext.Collector.EmitWarning(assembleContext.NavigationPath.FullName, $"Could not find a toc tree for {topLevelMapping.TopLevelSource}");
			return false;
		}
		navigationRootSource = topLevelMapping.TopLevelSource;
		return true;
	}

	public async Task<string> RenderNavigation(IGroupNavigationItem currentRootNavigation, Uri navigationSource, Cancel ctx = default)
	{
		if (!TryGetNavigationRoot(navigationSource, out var navigationRoot, out var navigationRootSource))
			return string.Empty;

		if (Phantoms.Contains(navigationRootSource))
			return string.Empty;

		if (_renderedNavigationCache.TryGetValue(navigationRootSource, out var value))
			return value;

		if (navigationRootSource == new Uri("docs-content:///"))
		{
			_renderedNavigationCache[navigationRootSource] = string.Empty;
			return string.Empty;
		}

		Console.WriteLine($"Rendering navigation for {navigationRootSource}");

		var model = CreateNavigationModel(navigationRoot);
		value = await ((INavigationHtmlWriter)this).Render(model, ctx);
		_renderedNavigationCache[navigationRootSource] = value;

		return value;
	}

	private NavigationViewModel CreateNavigationModel(DocumentationGroup group)
	{
		var topLevelItems = globalNavigation.TopLevelItems;
		return new NavigationViewModel
		{
			Title = group.Index?.NavigationTitle ?? "Docs",
			TitleUrl = group.Index?.Url ?? "/",
			Tree = group.GroupNavigationItem,
			IsPrimaryNavEnabled = true,
			IsGlobalAssemblyBuild = true,
			TopLevelItems = topLevelItems
		};
	}
}
