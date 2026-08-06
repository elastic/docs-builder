// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Navigation;
using Elastic.Documentation.Navigation.Assembler;
using Elastic.Documentation.Site;
using Elastic.Documentation.Site.Navigation;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Assembler.Navigation;

public class GlobalNavigationHtmlWriter(ILoggerFactory logFactory, SiteNavigation globalNavigation, IDiagnosticsCollector collector) : INavigationHtmlWriter
{
	private readonly ILogger _logger = logFactory.CreateLogger<GlobalNavigationHtmlWriter>();
	private readonly NavigationRenderCache _renderedNavigationCache = new();

	public Task<NavigationRenderResult> RenderNavigation(
		IRootNavigationItem<INavigationModel, INavigationItem> currentRootNavigation,
#pragma warning disable IDE0060
		INavigationItem currentNavigationItem, // temporary https://github.com/elastic/docs-content/pull/3730
#pragma warning restore IDE0060
		Cancel ctx = default
	)
	{
		if (currentRootNavigation is SiteNavigation)
			return Task.FromResult(NavigationRenderResult.Empty);

		if (currentRootNavigation.Parent is null or not SiteNavigation)
			collector.EmitGlobalError($"Passed root is not actually a top level navigation item {currentRootNavigation.NavigationTitle} ({currentRootNavigation.Id}) in {currentRootNavigation.Url}, trying to render: {currentNavigationItem.Url}");

		if (currentRootNavigation is not INodeNavigationItem<INavigationModel, INavigationItem> group)
			return Task.FromResult(NavigationRenderResult.Empty);

		return _renderedNavigationCache.GetOrRenderAsync(currentRootNavigation, () =>
		{
			_logger.LogInformation("Rendering navigation for {NavigationTitle} ({Id})", currentRootNavigation.NavigationTitle, currentRootNavigation.Id);
			return ((INavigationHtmlWriter)this).Render(CreateNavigationModel(group), ctx);
		});
	}

	private NavigationViewModel CreateNavigationModel(INodeNavigationItem<INavigationModel, INavigationItem> group) =>
		new()
		{
			Tree = group,
			IsPrimaryNavEnabled = true,
			IsUsingNavigationDropdown = true,
			IsGlobalAssemblyBuild = true,
			TopLevelItems = globalNavigation.TopLevelItems,
			BuildType = BuildType.Assembler
		};
}
