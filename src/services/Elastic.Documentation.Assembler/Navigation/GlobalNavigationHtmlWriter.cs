// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

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

	public async Task<NavigationRenderResult> RenderNavigation(
		IRootNavigationItem<INavigationModel, INavigationItem> currentRootNavigation,
		INavigationItem currentNavigationItem,
		Cancel ctx = default
	)
	{
		// Island check must come before the SiteNavigation short-circuit so island pages under
		// the narrative root (which is a top-level docset directly under SiteNavigation) still
		// get the island sidebar rather than the empty result.
		if (currentNavigationItem.FindIslandRoot() is { } islandRoot)
			return await _renderedNavigationCache.GetOrRenderAsync(islandRoot, () => RenderIslandAsync(islandRoot, ctx));

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

	private static async Task<NavigationRenderResult> RenderIslandAsync(
		INodeNavigationItem<INavigationModel, INavigationItem> islandRoot, Cancel ctx)
	{
		var model = NavigationRenderModel.CreateIsland(islandRoot);
		var html = await _IslandNav.Create(model).RenderAsync(cancellationToken: ctx);
		return new NavigationRenderResult { Html = html, Id = model.ContentHash };
	}

	private NavigationRenderModel CreateNavigationModel(INodeNavigationItem<INavigationModel, INavigationItem> group) =>
		NavigationRenderModel.Create(
			tree: group,
			topLevelItems: globalNavigation.TopLevelItems,
			isUsingNavigationDropdown: true,
			isPrimaryNavEnabled: true,
			isGlobalAssemblyBuild: true);
}
