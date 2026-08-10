// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration;
using Elastic.Documentation.Navigation;

namespace Elastic.Documentation.Site.Navigation;

public class IsolatedBuildNavigationHtmlWriter(BuildContext context, IRootNavigationItem<INavigationModel, INavigationItem> siteRoot)
	: INavigationHtmlWriter
{
	private readonly NavigationRenderCache _renderedNavigationCache = new();

	public Task<NavigationRenderResult> RenderNavigation(
		IRootNavigationItem<INavigationModel, INavigationItem> currentRootNavigation,
		INavigationItem currentNavigationItem,
		Cancel ctx = default)
	{
		var navigation = SelectNavigationRoot(currentRootNavigation);
		return _renderedNavigationCache.GetOrRenderAsync(
			navigation,
			() => ((INavigationHtmlWriter)this).Render(CreateNavigationModel(navigation), ctx));
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

	private NavigationRenderModel CreateNavigationModel(IRootNavigationItem<INavigationModel, INavigationItem> navigation) =>
		NavigationRenderModel.Create(
			tree: navigation,
			topLevelItems: navigation.NavigationItems.OfType<INodeNavigationItem<INavigationModel, INavigationItem>>().ToList(),
			isUsingNavigationDropdown: context.Configuration.Features.PrimaryNavEnabled || navigation.IsUsingNavigationDropdown,
			isPrimaryNavEnabled: context.Configuration.Features.PrimaryNavEnabled,
			isGlobalAssemblyBuild: false);
}
