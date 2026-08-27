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

#pragma warning disable CS9113 // collector kept for binary-compatibility; no longer used internally
public class GlobalNavigationHtmlWriter(
	ILoggerFactory logFactory,
	SiteNavigation globalNavigation,
	IDiagnosticsCollector collector
) : INavigationHtmlWriter
#pragma warning restore CS9113
{
	private readonly ILogger _logger = logFactory.CreateLogger<GlobalNavigationHtmlWriter>();
	private readonly NavigationRenderCache _renderedNavigationCache = new();

	public async Task<NavigationRenderResult> RenderNavigation(
		IRootNavigationItem<INavigationModel, INavigationItem> currentRootNavigation,
		INavigationItem currentNavigationItem,
		Cancel ctx = default
	)
	{
		// FindIslandRoot() resolves nested islands AND top-level sections (which are implicitly islands).
		// Narrative root file leafs (direct SiteNavigation children with no island ancestor) return null,
		// falling back to currentRootNavigation which is SiteNavigation itself → Empty.
		var renderRoot = currentNavigationItem.FindIslandRoot() ?? currentRootNavigation;

		if (renderRoot is SiteNavigation)
			return NavigationRenderResult.Empty;

		if (renderRoot is not INodeNavigationItem<INavigationModel, INavigationItem> group)
			return NavigationRenderResult.Empty;

		return await _renderedNavigationCache.GetOrRenderAsync(
			renderRoot,
			() =>
			{
				_logger.LogInformation("Rendering navigation for {NavigationTitle} ({Id})", renderRoot.NavigationTitle, renderRoot.Id);
				return ((INavigationHtmlWriter)this).Render(CreateNavigationModel(group), ctx);
			}
		);
	}

	private NavigationRenderModel CreateNavigationModel(INodeNavigationItem<INavigationModel, INavigationItem> group) =>
		NavigationRenderModel.Create(
			tree: group,
			topLevelItems: globalNavigation.TopLevelItems,
			// The top nav (navigation-preview) replaces the sidebar dropdown.
			// Flag off → dropdown on (matches main); flag on → dropdown off, top nav takes over.
			isUsingNavigationDropdown: globalNavigation.TopNav is null,
			isPrimaryNavEnabled: true,
			isGlobalAssemblyBuild: true
		);
}
