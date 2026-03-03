// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Extensions;
using Elastic.Documentation.Navigation;
using Elastic.Documentation.Site;

namespace Elastic.Documentation.Site.Navigation;

public class IsolatedBuildNavigationHtmlWriter(BuildContext context, IRootNavigationItem<INavigationModel, INavigationItem> siteRoot)
	: INavigationHtmlWriter
{
	private readonly ConcurrentDictionary<string, string> _renderedNavigationCache = [];

	public async Task<NavigationRenderResult> RenderNavigation(
		IRootNavigationItem<INavigationModel, INavigationItem> currentRootNavigation,
		INavigationItem currentNavigationItem,
		Cancel ctx = default)
	{
		var navigation = SelectNavigationRoot(currentRootNavigation);
		var id = ShortId.Create($"{navigation.Id.GetHashCode()}");
		if (_renderedNavigationCache.TryGetValue(navigation.Id, out var value))
		{
			return new NavigationRenderResult
			{
				Html = value,
				Id = id
			};
		}
		var model = CreateNavigationModel(navigation);
		value = await ((INavigationHtmlWriter)this).Render(model, ctx);
		_renderedNavigationCache[navigation.Id] = value;
		return new NavigationRenderResult
		{
			Html = value,
			Id = id
		};
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

	private NavigationViewModel CreateNavigationModel(IRootNavigationItem<INavigationModel, INavigationItem> navigation)
	{
		var rootPath = context.SiteRootPath ?? GetDefaultRootPath(context.UrlPathPrefix);
		var htmx = context.BuildType == BuildType.Codex
			? new CodexHtmxAttributeProvider(rootPath)
			: new DefaultHtmxAttributeProvider(rootPath);
		return new()
		{
			Title = navigation.NavigationTitle,
			TitleUrl = navigation.Url,
			Tree = navigation,
			IsPrimaryNavEnabled = context.Configuration.Features.PrimaryNavEnabled,
			IsUsingNavigationDropdown = context.Configuration.Features.PrimaryNavEnabled || navigation.IsUsingNavigationDropdown,
			IsGlobalAssemblyBuild = false,
			TopLevelItems = navigation.NavigationItems.OfType<INodeNavigationItem<INavigationModel, INavigationItem>>().ToList(),
			Htmx = htmx,
			BuildType = context.BuildType
		};
	}

	private static string GetDefaultRootPath(string? urlPathPrefix)
	{
		var prefix = urlPathPrefix?.Trim('/') ?? "";
		return string.IsNullOrEmpty(prefix) ? "/" : $"/{prefix}/";
	}
}
