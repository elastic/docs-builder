// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Elastic.Documentation.Extensions;
using Elastic.Documentation.Site.Navigation;
using Elastic.Markdown.IO.Navigation;

namespace Documentation.Assembler.Navigation;

public class GlobalNavigationHtmlWriter(
	GlobalNavigationFile navigationFile,
	AssembleContext assembleContext,
	GlobalNavigation globalNavigation,
	AssembleSources assembleSources
) : INavigationHtmlWriter
{
	private readonly ConcurrentDictionary<(Uri, int), string> _renderedNavigationCache = [];

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
			if (assembleSources.TreeCollector.TryGetTableOfContentsTree(navigationSource, out navigationRoot))
			{
				if (navigationRoot.Parent is DocumentationGroup parent &&
					assembleSources.TocTopLevelMappings.TryGetValue(parent.NavigationSource, out var topLevelMapping2))
				{
					navigationRootSource = topLevelMapping2.TopLevelSource;
					return true;
				}

				assembleContext.Collector.EmitError(assembleContext.NavigationPath.FullName, $"The following toc: {navigationSource} is not declared in navigation.yml and it's parent failed to yield a navigation root.");
				return false;
			}
			assembleContext.Collector.EmitError(assembleContext.NavigationPath.FullName, $"The following toc: {navigationSource} is not declared in navigation.yml");
			return false;
		}

		if (!assembleSources.TreeCollector.TryGetTableOfContentsTree(topLevelMapping.TopLevelSource, out navigationRoot))
		{
			assembleContext.Collector.EmitError(assembleContext.NavigationPath.FullName, $"None of the assemble sources define a toc for: {topLevelMapping.TopLevelSource}");
			return false;
		}
		navigationRootSource = topLevelMapping.TopLevelSource;
		return true;
	}

	public async Task<NavigationRenderResult> RenderNavigation(IRootNavigationItem<INavigationModel, INavigationItem> currentRootNavigation,
		Uri navigationSource, int maxLevel, Cancel ctx = default)
	{
		if (Phantoms.Contains(navigationSource)
			|| !TryGetNavigationRoot(navigationSource, out var navigationRoot, out var navigationRootSource)
			|| Phantoms.Contains(navigationRootSource)
		   )
			return NavigationRenderResult.Empty;

		var navigationId = ShortId.Create($"{(navigationRootSource, maxLevel).GetHashCode()}");

		if (_renderedNavigationCache.TryGetValue((navigationRootSource, maxLevel), out var value))
		{
			return new NavigationRenderResult
			{
				Html = value,
				Id = navigationId
			};
		}

		if (navigationRootSource == new Uri("docs-content:///"))
		{
			_renderedNavigationCache[(navigationRootSource, maxLevel)] = string.Empty;
			return NavigationRenderResult.Empty;
		}

		Console.WriteLine($"Rendering navigation for {navigationRootSource}");

		var model = CreateNavigationModel(navigationRoot, maxLevel);
		value = await ((INavigationHtmlWriter)this).Render(model, ctx);
		_renderedNavigationCache[(navigationRootSource, maxLevel)] = value;
		return new NavigationRenderResult
		{
			Html = value,
			Id = navigationId
		};
	}

	private NavigationViewModel CreateNavigationModel(DocumentationGroup group, int maxLevel)
	{
		var topLevelItems = globalNavigation.TopLevelItems;
		return new NavigationViewModel
		{
			Title = group.Index.NavigationTitle,
			TitleUrl = group.Index.Url,
			Tree = group,
			IsPrimaryNavEnabled = true,
			IsUsingNavigationDropdown = true,
			IsGlobalAssemblyBuild = true,
			TopLevelItems = topLevelItems,
			MaxLevel = maxLevel
		};
	}
}
