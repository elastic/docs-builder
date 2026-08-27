// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration;
using Elastic.Documentation.Navigation;
using RazorSlices;

namespace Elastic.Documentation.Site.Navigation;

public class IsolatedBuildNavigationHtmlWriter(BuildContext context, IRootNavigationItem<INavigationModel, INavigationItem> siteRoot)
	: INavigationHtmlWriter
{
	private readonly NavigationRenderCache _renderedNavigationCache = new();

	public async Task<NavigationRenderResult> RenderNavigation(
		IRootNavigationItem<INavigationModel, INavigationItem> currentRootNavigation,
		INavigationItem currentNavigationItem,
		Cancel ctx = default)
	{
		var renderRoot = currentNavigationItem.FindIslandRoot() ?? SelectNavigationRoot(currentRootNavigation);

		if (renderRoot is not INodeNavigationItem<INavigationModel, INavigationItem> group)
			return NavigationRenderResult.Empty;

		var rendered = await _renderedNavigationCache.GetOrRenderAsync(
			renderRoot,
			() => ((INavigationHtmlWriter)this).Render(CreateNavigationModel(group), ctx));
		return NavigationCurrentMarker.Apply(rendered, currentNavigationItem);
	}

	/// <summary>
	/// Determines which navigation root to use for rendering.
	/// Uses the requested root when it differs from site root (e.g. group nav in codex)
	/// or when primary nav/dropdown features are enabled.
	/// </summary>
	private IRootNavigationItem<INavigationModel, INavigationItem> SelectNavigationRoot(
		IRootNavigationItem<INavigationModel, INavigationItem> requestedRoot)
	{
		var useRequestedRoot = requestedRoot != siteRoot
			|| context.Configuration.Features.PrimaryNavEnabled
			|| requestedRoot.IsUsingNavigationDropdown;

		return useRequestedRoot ? requestedRoot : siteRoot;
	}

	private NavigationRenderModel CreateNavigationModel(INodeNavigationItem<INavigationModel, INavigationItem> renderRoot)
	{
		// Top-level items always come from the docset root (siteRoot) so the dropdown
		// correctly lists all sections even when renderRoot is a nested island.
		var topLevelItems = siteRoot.NavigationItems.OfType<INodeNavigationItem<INavigationModel, INavigationItem>>().ToList();
		var isUsingDropdown = context.Configuration.Features.PrimaryNavEnabled || siteRoot.IsUsingNavigationDropdown;
		return NavigationRenderModel.Create(
			tree: renderRoot,
			topLevelItems: topLevelItems,
			isUsingNavigationDropdown: isUsingDropdown,
			isPrimaryNavEnabled: context.Configuration.Features.PrimaryNavEnabled,
			isGlobalAssemblyBuild: false,
			navigationPreviewEnabled: context.Configuration.Features.NavigationPreviewEnabled);
	}
}
