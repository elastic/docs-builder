// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Navigation;
using Elastic.Documentation.Navigation.Assembler;
using Elastic.Documentation.Site.Navigation;
using Microsoft.Extensions.Logging;
using RazorSlices;

namespace Elastic.Documentation.Assembler.Navigation;

public class GlobalNavigationHtmlWriter(ILoggerFactory logFactory, SiteNavigation globalNavigation, IDiagnosticsCollector collector) : INavigationHtmlWriter
{
	private readonly ILogger _logger = logFactory.CreateLogger<GlobalNavigationHtmlWriter>();
	private readonly NavigationRenderCache _renderedNavigationCache = new();
	private readonly ConcurrentDictionary<string, string> _islandHtmlCache = [];

	public async Task<NavigationRenderResult> RenderNavigation(
		IRootNavigationItem<INavigationModel, INavigationItem> currentRootNavigation,
		INavigationItem currentNavigationItem,
		Cancel ctx = default
	)
	{
		if (currentNavigationItem.IslandListingRoot is { } islandRoot)
			return await RenderIslandNavigation(islandRoot, ctx);
		if (currentNavigationItem is INodeNavigationItem<INavigationModel, INavigationItem> { IsIslandListing: true } listingRoot)
			return await RenderIslandNavigation(listingRoot, ctx);

		if (currentRootNavigation is SiteNavigation)
			return NavigationRenderResult.Empty;

		if (currentRootNavigation.Parent is null or not SiteNavigation)
			collector.EmitGlobalError($"Passed root is not actually a top level navigation item {currentRootNavigation.NavigationTitle} ({currentRootNavigation.Id}) in {currentRootNavigation.Url}, trying to render: {currentNavigationItem.Url}");

		if (currentRootNavigation is not INodeNavigationItem<INavigationModel, INavigationItem> group)
			return NavigationRenderResult.Empty;

		return await _renderedNavigationCache.GetOrRenderAsync(currentRootNavigation, () =>
		{
			_logger.LogInformation("Rendering navigation for {NavigationTitle} ({Id})", currentRootNavigation.NavigationTitle, currentRootNavigation.Id);
			return ((INavigationHtmlWriter)this).Render(CreateNavigationModel(group), ctx);
		});
	}

	private async Task<NavigationRenderResult> RenderIslandNavigation(
		INodeNavigationItem<INavigationModel, INavigationItem> islandRoot, Cancel ctx)
	{
		var cacheKey = islandRoot.Id;
		if (_islandHtmlCache.TryGetValue(cacheKey, out var html))
			return new NavigationRenderResult { Html = html, Id = cacheKey };

		var model = CreateIslandNavModel(islandRoot);
		var slice = _IslandNav.Create(model);
		html = await slice.RenderAsync(cancellationToken: ctx);
		_islandHtmlCache[cacheKey] = html;
		return new NavigationRenderResult { Html = html, Id = cacheKey };
	}

	private static IslandNavViewModel CreateIslandNavModel(
		INodeNavigationItem<INavigationModel, INavigationItem> islandRoot)
	{
		var groups = islandRoot.NavigationItems
			.OfType<INodeNavigationItem<INavigationModel, INavigationItem>>()
			.Select(group =>
			{
				var pages = group.NavigationItems
					.OfType<ILeafNavigationItem<INavigationModel>>()
					.Select(p => new IslandNavPage(p.NavigationTitle, p.Url))
					.ToList();
				return new IslandNavGroup(group.NavigationTitle, group.Url, pages);
			})
			.ToList();

		var backTarget = islandRoot.Parent ?? (INavigationItem)islandRoot;
		return new IslandNavViewModel
		{
			BackLinkUrl = backTarget.Url,
			BackLinkTitle = backTarget.NavigationTitle,
			ListingRootUrl = islandRoot.Url,
			ListingRootTitle = islandRoot.NavigationTitle,
			Groups = groups,
			Visual = islandRoot.IslandVisual
		};
	}

	private NavigationRenderModel CreateNavigationModel(INodeNavigationItem<INavigationModel, INavigationItem> group) =>
		NavigationRenderModel.Create(
			tree: group,
			topLevelItems: globalNavigation.TopLevelItems,
			isUsingNavigationDropdown: true,
			isPrimaryNavEnabled: true,
			isGlobalAssemblyBuild: true);
}
