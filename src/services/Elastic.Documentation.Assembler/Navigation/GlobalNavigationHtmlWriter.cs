// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Navigation;
using Elastic.Documentation.Navigation.Assembler;
using Elastic.Documentation.Site.Navigation;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Assembler.Navigation;

public class GlobalNavigationHtmlWriter(ILoggerFactory logFactory, SiteNavigation globalNavigation, IDiagnosticsCollector collector) : INavigationHtmlWriter, IDisposable
{
	private readonly ILogger _logger = logFactory.CreateLogger<GlobalNavigationHtmlWriter>();
	private readonly SemaphoreSlim _semaphore = new(1, 1);

	private readonly ConcurrentDictionary<(string, int), string> _renderedNavigationCache = [];

	public async Task<NavigationRenderResult> RenderNavigation(
		IRootNavigationItem<INavigationModel, INavigationItem> currentRootNavigation,
#pragma warning disable IDE0060
		INavigationItem currentNavigationItem, // temporary https://github.com/elastic/docs-content/pull/3730
#pragma warning restore IDE0060
		int maxLevel,
		Cancel ctx = default
	)
	{
		if (currentRootNavigation is SiteNavigation)
			return NavigationRenderResult.Empty;

		if (currentRootNavigation.Parent is null or not SiteNavigation)
			collector.EmitGlobalError($"Passed root is not actually a top level navigation item {currentRootNavigation.NavigationTitle} ({currentRootNavigation.Id}) in {currentRootNavigation.Url}, trying to render: {currentNavigationItem.Url}");

		if (_renderedNavigationCache.TryGetValue((currentRootNavigation.Id, maxLevel), out var html))
			return new NavigationRenderResult { Html = html, Id = currentRootNavigation.Id };

		if (currentRootNavigation is not INodeNavigationItem<INavigationModel, INavigationItem> group)
			return NavigationRenderResult.Empty;

		await _semaphore.WaitAsync(ctx);

		if (currentNavigationItem.Url == "/docs/versions")
		{
		}
		if (currentNavigationItem.Url == "/docs/reference/ecs/logging/java")
		{
		}

		try
		{
			if (_renderedNavigationCache.TryGetValue((currentRootNavigation.Id, maxLevel), out html))
				return new NavigationRenderResult { Html = html, Id = currentRootNavigation.Id };

			_logger.LogInformation("Rendering navigation for {NavigationTitle} ({Id})", currentRootNavigation.NavigationTitle, currentRootNavigation.Id);

			var model = CreateNavigationModel(group, maxLevel);
			html = await ((INavigationHtmlWriter)this).Render(model, ctx);
			_renderedNavigationCache[(currentRootNavigation.Id, maxLevel)] = html;
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

	private NavigationViewModel CreateNavigationModel(INodeNavigationItem<INavigationModel, INavigationItem> group, int maxLevel)
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
			MaxLevel = maxLevel
		};
	}

	public void Dispose()
	{
		_semaphore.Dispose();
		GC.SuppressFinalize(this);
	}
}
