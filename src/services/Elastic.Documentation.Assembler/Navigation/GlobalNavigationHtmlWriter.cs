// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Navigation;
using Elastic.Documentation.Navigation.Assembler;
using Elastic.Documentation.Navigation.V2;
using Elastic.Documentation.Site.Navigation;
using Microsoft.Extensions.Logging;
using RazorSlices;

namespace Elastic.Documentation.Assembler.Navigation;

#pragma warning disable CS9113 // collector kept for binary-compatibility; no longer used internally
public class GlobalNavigationHtmlWriter(ILoggerFactory logFactory, SiteNavigation globalNavigation, IDiagnosticsCollector collector) : INavigationHtmlWriter
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
		// V2 nav: always render the full V2 tree regardless of current section
		if (globalNavigation is SiteNavigationV2 navV2)
			return await RenderV2Navigation(navV2, ctx);

		// FindIslandRoot() resolves nested islands AND top-level sections (which are implicitly islands).
		// Narrative root file leafs (direct SiteNavigation children with no island ancestor) return null,
		// falling back to currentRootNavigation which is SiteNavigation itself → Empty.
		var renderRoot = currentNavigationItem.FindIslandRoot() ?? currentRootNavigation;

		if (renderRoot is SiteNavigation)
			return NavigationRenderResult.Empty;

		if (renderRoot is not INodeNavigationItem<INavigationModel, INavigationItem> group)
			return NavigationRenderResult.Empty;

		return await _renderedNavigationCache.GetOrRenderAsync(renderRoot, () =>
		{
			_logger.LogInformation("Rendering navigation for {NavigationTitle} ({Id})", renderRoot.NavigationTitle, renderRoot.Id);
			return ((INavigationHtmlWriter)this).Render(CreateNavigationModel(group), ctx);
		});
	}

	private NavigationRenderModel CreateNavigationModel(INodeNavigationItem<INavigationModel, INavigationItem> group) =>
		NavigationRenderModel.Create(
			tree: group,
			topLevelItems: globalNavigation.TopLevelItems,
			isUsingNavigationDropdown: true,
			isPrimaryNavEnabled: true,
			isGlobalAssemblyBuild: true);

	private async Task<NavigationRenderResult> RenderV2Navigation(SiteNavigationV2 navV2, Cancel ctx)
	{
		var syntheticV2Root = new SiteNavigationV2Wrapper(navV2);
		return await _renderedNavigationCache.GetOrRenderAsync(syntheticV2Root, async () =>
		{
			_logger.LogInformation("Rendering V2 navigation");
			var item = new NavigationTreeItem
			{
				IsPrimaryNavEnabled = true,
				IsGlobalAssemblyBuild = true,
				Level = 0,
				SubTree = syntheticV2Root,
				RootNavigationId = syntheticV2Root.Id
			};
			var slice = _TocTreeNavV2.Create(item);
			var html = await slice.RenderAsync(cancellationToken: ctx);
			return new NavigationRenderResult { Html = html, Id = "nav-v2" };
		});
	}

	/// <summary>
	/// Thin wrapper so <see cref="SiteNavigationV2.V2NavigationItems"/> is exposed as
	/// <see cref="INodeNavigationItem{TIndex,TChildNavigation}.NavigationItems"/> for the Razor partial.
	/// </summary>
	private sealed class SiteNavigationV2Wrapper(SiteNavigationV2 navV2)
		: INodeNavigationItem<INavigationModel, INavigationItem>
	{
		public string Id => "nav-v2-root";
		public string Url => navV2.Url;
		public string NavigationTitle => navV2.NavigationTitle;
		public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot => navV2;
		public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; }
		public bool Hidden => false;
		public int NavigationIndex { get; set; }
		public ILeafNavigationItem<INavigationModel> Index => navV2.Index;
		public IReadOnlyCollection<INavigationItem> NavigationItems => navV2.V2NavigationItems;
	}
}
