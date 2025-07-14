// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using Elastic.Documentation.Site.Navigation;
using Elastic.Markdown.IO.Navigation;
using Microsoft.Extensions.Logging;

namespace Documentation.Assembler.Navigation;

public class GlobalNavigationHtmlWriter(ILoggerFactory logFactory, GlobalNavigation globalNavigation) : INavigationHtmlWriter
{
	private readonly ILogger<Program> _logger = logFactory.CreateLogger<Program>();

	private readonly ConcurrentDictionary<(string, int), string> _renderedNavigationCache = [];

	public async Task<NavigationRenderResult> RenderNavigation(IRootNavigationItem<INavigationModel, INavigationItem> currentRootNavigation, int maxLevel, Cancel ctx = default)
	{
		INodeNavigationItem<INavigationModel, INavigationItem> lastParentBeforeRoot = currentRootNavigation;
		INodeNavigationItem<INavigationModel, INavigationItem> parent = currentRootNavigation;
		while (parent.Parent is not null)
		{
			lastParentBeforeRoot = parent;
			parent = parent.Parent;
		}
		if (_renderedNavigationCache.TryGetValue((lastParentBeforeRoot.Id, maxLevel), out var html))
		{
			return new NavigationRenderResult
			{
				Html = html,
				Id = lastParentBeforeRoot.Id
			};
		}

		_logger.LogInformation("Rendering navigation for {NavigationTitle} ({Id})", lastParentBeforeRoot.NavigationTitle, lastParentBeforeRoot.Id);

		if (lastParentBeforeRoot is not DocumentationGroup group)
			return NavigationRenderResult.Empty;

		var model = CreateNavigationModel(group, maxLevel);
		html = await ((INavigationHtmlWriter)this).Render(model, ctx);
		_renderedNavigationCache[(lastParentBeforeRoot.Id, maxLevel)] = html;
		return new NavigationRenderResult
		{
			Html = html,
			Id = lastParentBeforeRoot.Id
		};
	}

	private NavigationViewModel CreateNavigationModel(DocumentationGroup group, int maxLevel)
	{
		var topLevelItems = globalNavigation.TopLevelItems;
		return new NavigationViewModel
		{
			Title = group.Index.NavigationTitle,
			TitleUrl = group.Index.Url,
			Tree = group,
			IsPrimaryNavEnabled = true,
			IsUsingNavigationDropdown = true,
			IsGlobalAssemblyBuild = true,
			TopLevelItems = topLevelItems,
			MaxLevel = maxLevel
		};
	}
}
