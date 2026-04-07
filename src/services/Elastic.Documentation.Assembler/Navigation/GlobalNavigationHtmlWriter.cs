// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using Elastic.Documentation;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Navigation;
using Elastic.Documentation.Navigation.Assembler;
using Elastic.Documentation.Navigation.V2;
using Elastic.Documentation.Site;
using Elastic.Documentation.Site.Navigation;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Assembler.Navigation;

public class GlobalNavigationHtmlWriter(ILoggerFactory logFactory, SiteNavigation globalNavigation, IDiagnosticsCollector collector) : INavigationHtmlWriter, IDisposable
{
	private readonly ILogger _logger = logFactory.CreateLogger<GlobalNavigationHtmlWriter>();
	private readonly SemaphoreSlim _semaphore = new(1, 1);

	private readonly ConcurrentDictionary<string, string> _renderedNavigationCache = [];

	public async Task<NavigationRenderResult> RenderNavigation(
		IRootNavigationItem<INavigationModel, INavigationItem> currentRootNavigation,
#pragma warning disable IDE0060
		INavigationItem currentNavigationItem, // temporary https://github.com/elastic/docs-content/pull/3730
#pragma warning restore IDE0060
		Cancel ctx = default
	)
	{
		// V2 nav: always render the full V2 tree regardless of current section
		if (globalNavigation is SiteNavigationV2 navV2)
			return await RenderV2Navigation(navV2, ctx);

		if (currentRootNavigation is SiteNavigation)
			return NavigationRenderResult.Empty;

		if (currentRootNavigation.Parent is null or not SiteNavigation)
			collector.EmitGlobalError($"Passed root is not actually a top level navigation item {currentRootNavigation.NavigationTitle} ({currentRootNavigation.Id}) in {currentRootNavigation.Url}, trying to render: {currentNavigationItem.Url}");

		if (_renderedNavigationCache.TryGetValue(currentRootNavigation.Id, out var html))
			return new NavigationRenderResult { Html = html, Id = currentRootNavigation.Id };

		if (currentRootNavigation is not INodeNavigationItem<INavigationModel, INavigationItem> group)
			return NavigationRenderResult.Empty;

		await _semaphore.WaitAsync(ctx);

		try
		{
			if (_renderedNavigationCache.TryGetValue(currentRootNavigation.Id, out html))
				return new NavigationRenderResult { Html = html, Id = currentRootNavigation.Id };

			_logger.LogInformation("Rendering navigation for {NavigationTitle} ({Id})", currentRootNavigation.NavigationTitle, currentRootNavigation.Id);

			var model = CreateNavigationModel(group);
			html = await ((INavigationHtmlWriter)this).Render(model, ctx);
			_renderedNavigationCache[currentRootNavigation.Id] = html;
			return new NavigationRenderResult
			{
				Html = html,
				Id = currentRootNavigation.Id
			};
		}
		finally
		{
			_ = _semaphore.Release();
		}
	}

	private const string NavV2CacheKey = "nav-v2";

	private async Task<NavigationRenderResult> RenderV2Navigation(SiteNavigationV2 navV2, Cancel ctx)
	{
		if (_renderedNavigationCache.TryGetValue(NavV2CacheKey, out var cachedHtml))
			return new NavigationRenderResult { Html = cachedHtml, Id = NavV2CacheKey };

		await _semaphore.WaitAsync(ctx);
		try
		{
			if (_renderedNavigationCache.TryGetValue(NavV2CacheKey, out cachedHtml))
				return new NavigationRenderResult { Html = cachedHtml, Id = NavV2CacheKey };

			_logger.LogInformation("Rendering V2 navigation");

			var model = CreateV2NavigationModel(navV2);
			var html = await ((INavigationHtmlWriter)this).Render(model, ctx);
			_renderedNavigationCache[NavV2CacheKey] = html;
			return new NavigationRenderResult { Html = html, Id = NavV2CacheKey };
		}
		finally
		{
			_ = _semaphore.Release();
		}
	}

	private static NavigationViewModel CreateV2NavigationModel(SiteNavigationV2 navV2)
	{
		var syntheticV2Root = new SiteNavigationV2Wrapper(navV2);
		return new NavigationViewModel
		{
			Title = "Elastic Docs",
			TitleUrl = navV2.Url,
			Tree = syntheticV2Root,
			IsPrimaryNavEnabled = true,
			IsUsingNavigationDropdown = false,
			IsGlobalAssemblyBuild = true,
			TopLevelItems = [],
			Htmx = new DefaultHtmxAttributeProvider("/"),
			BuildType = BuildType.Assembler,
			IsNavV2 = true
		};
	}

	private NavigationViewModel CreateNavigationModel(INodeNavigationItem<INavigationModel, INavigationItem> group)
	{
		var topLevelItems = globalNavigation.TopLevelItems;
		return new NavigationViewModel
		{
			Title = group.NavigationTitle,
			TitleUrl = group.Url,
			Tree = group,
			IsPrimaryNavEnabled = true,
			IsUsingNavigationDropdown = true,
			IsGlobalAssemblyBuild = true,
			TopLevelItems = topLevelItems,
			Htmx = new DefaultHtmxAttributeProvider("/"),
			BuildType = BuildType.Assembler
		};
	}

	public void Dispose()
	{
		_semaphore.Dispose();
		GC.SuppressFinalize(this);
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
