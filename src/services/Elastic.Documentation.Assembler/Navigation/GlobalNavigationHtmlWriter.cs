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
		INavigationItem currentNavigationItem,
		Cancel ctx = default
	)
	{
		// V2 nav: render per-section sidebar based on current page
		if (globalNavigation is SiteNavigationV2 navV2)
			return await RenderSectionNavigation(navV2, currentNavigationItem, ctx);

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

	private async Task<NavigationRenderResult> RenderSectionNavigation(
		SiteNavigationV2 navV2,
		INavigationItem currentNavigationItem,
		Cancel ctx
	)
	{
		var section = navV2.GetSectionForUrl(currentNavigationItem.Url);
		if (section is null)
		{
			// Fallback: render full V2 tree if no sections are defined
			return await RenderFullV2Navigation(navV2, ctx);
		}

		var cacheKey = $"nav-v2-section-{section.Id}";
		if (_renderedNavigationCache.TryGetValue(cacheKey, out var cachedHtml))
			return CreateSectionResult(cachedHtml, section, navV2);

		await _semaphore.WaitAsync(ctx);
		try
		{
			if (_renderedNavigationCache.TryGetValue(cacheKey, out cachedHtml))
				return CreateSectionResult(cachedHtml, section, navV2);

			_logger.LogInformation("Rendering V2 section navigation: {SectionLabel} ({SectionId})", section.Label, section.Id);

			var wrapper = new SectionNavigationV2Wrapper(section, navV2);
			var model = new NavigationViewModel
			{
				Title = section.Label,
				TitleUrl = section.Url,
				Tree = wrapper,
				IsPrimaryNavEnabled = true,
				IsUsingNavigationDropdown = false,
				IsGlobalAssemblyBuild = true,
				TopLevelItems = [],
				Htmx = new DefaultHtmxAttributeProvider("/"),
				BuildType = BuildType.Assembler,
				IsNavV2 = true,
				IsIsolatedSection = section.Isolated,
				SectionUrl = CombineWithSitePrefix(navV2, section.Url)
			};

			var html = await ((INavigationHtmlWriter)this).Render(model, ctx);
			_renderedNavigationCache[cacheKey] = html;
			return CreateSectionResult(html, section, navV2);
		}
		finally
		{
			_ = _semaphore.Release();
		}
	}

	private static string CombineWithSitePrefix(SiteNavigation nav, string sectionUrl)
	{
		var prefix = nav.Url.TrimEnd('/');
		var path = sectionUrl.TrimStart('/');
		return string.IsNullOrEmpty(path) ? $"{prefix}/" : $"{prefix}/{path}";
	}

	private static NavigationRenderResult CreateSectionResult(string html, NavigationSection activeSection, SiteNavigationV2 navV2) =>
		new()
		{
			Html = html,
			Id = $"nav-v2-section-{activeSection.Id}",
			Sections = navV2.Sections,
			ActiveSectionId = activeSection.Id
		};

	/// <summary>Fallback when no <c>section:</c> items exist — renders the full V2 tree as before.</summary>
	private async Task<NavigationRenderResult> RenderFullV2Navigation(SiteNavigationV2 navV2, Cancel ctx)
	{
		const string cacheKey = "nav-v2";
		if (_renderedNavigationCache.TryGetValue(cacheKey, out var cachedHtml))
			return new NavigationRenderResult { Html = cachedHtml, Id = cacheKey };

		await _semaphore.WaitAsync(ctx);
		try
		{
			if (_renderedNavigationCache.TryGetValue(cacheKey, out cachedHtml))
				return new NavigationRenderResult { Html = cachedHtml, Id = cacheKey };

			_logger.LogInformation("Rendering V2 navigation (full tree fallback)");

			var syntheticV2Root = new FullV2Wrapper(navV2);
			var model = new NavigationViewModel
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

			var html = await ((INavigationHtmlWriter)this).Render(model, ctx);
			_renderedNavigationCache[cacheKey] = html;
			return new NavigationRenderResult { Html = html, Id = cacheKey };
		}
		finally
		{
			_ = _semaphore.Release();
		}
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

	/// <summary>Wraps a single <see cref="NavigationSection"/> as the sidebar tree root.</summary>
	private sealed class SectionNavigationV2Wrapper(NavigationSection section, SiteNavigationV2 navV2)
		: INodeNavigationItem<INavigationModel, INavigationItem>
	{
		public string Id => section.Id;
		public string Url => section.Url;
		public string NavigationTitle => section.Label;
		public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot => navV2;
		public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; }
		public bool Hidden => false;
		public int NavigationIndex { get; set; }
		public ILeafNavigationItem<INavigationModel> Index => navV2.Index;
		public IReadOnlyCollection<INavigationItem> NavigationItems => section.NavigationItems;
	}

	/// <summary>Fallback wrapper exposing the full V2 tree (used when no sections are defined).</summary>
	private sealed class FullV2Wrapper(SiteNavigationV2 navV2)
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
