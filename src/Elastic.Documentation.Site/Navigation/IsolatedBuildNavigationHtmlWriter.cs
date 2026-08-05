// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Extensions;
using Elastic.Documentation.Navigation;
using RazorSlices;

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
		if (currentNavigationItem.IslandListingRoot is { } islandRoot)
			return await RenderIslandNavigation(islandRoot, ctx);
		if (currentNavigationItem is INodeNavigationItem<INavigationModel, INavigationItem> { IsIslandListing: true } listingRoot)
			return await RenderIslandNavigation(listingRoot, ctx);

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

	private async Task<NavigationRenderResult> RenderIslandNavigation(
		INodeNavigationItem<INavigationModel, INavigationItem> islandRoot, Cancel ctx)
	{
		var cacheKey = $"island:{islandRoot.Id}";
		if (_renderedNavigationCache.TryGetValue(cacheKey, out var html))
			return new NavigationRenderResult { Html = html, Id = islandRoot.Id };

		var model = CreateIslandNavModel(islandRoot);
		var slice = _IslandNav.Create(model);
		html = await slice.RenderAsync(cancellationToken: ctx);
		_renderedNavigationCache[cacheKey] = html;
		return new NavigationRenderResult { Html = html, Id = islandRoot.Id };
	}

	private static IslandNavViewModel CreateIslandNavModel(
		INodeNavigationItem<INavigationModel, INavigationItem> islandRoot)
	{
		var groups = islandRoot.NavigationItems
			.OfType<INodeNavigationItem<INavigationModel, INavigationItem>>()
			.Select(group =>
			{
				var pages = group.NavigationItems
					.OfType<ILeafNavigationItem<INavigationModel>>()
					.Select(p => new IslandNavPage(p.NavigationTitle, p.Url))
					.ToList();
				return new IslandNavGroup(group.NavigationTitle, group.Url, pages);
			})
			.ToList();

		var backTarget = islandRoot.Parent ?? (INavigationItem)islandRoot;
		return new IslandNavViewModel
		{
			BackLinkUrl = backTarget.Url,
			BackLinkTitle = backTarget.NavigationTitle,
			ListingRootUrl = islandRoot.Url,
			ListingRootTitle = islandRoot.NavigationTitle,
			Groups = groups
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

	private NavigationViewModel CreateNavigationModel(IRootNavigationItem<INavigationModel, INavigationItem> navigation) =>
		new()
		{
			Title = navigation.NavigationTitle,
			TitleUrl = navigation.Url,
			Tree = navigation,
			IsPrimaryNavEnabled = context.Configuration.Features.PrimaryNavEnabled,
			IsUsingNavigationDropdown = context.Configuration.Features.PrimaryNavEnabled || navigation.IsUsingNavigationDropdown,
			IsGlobalAssemblyBuild = false,
			TopLevelItems = navigation.NavigationItems.OfType<INodeNavigationItem<INavigationModel, INavigationItem>>().ToList(),
			BuildType = context.BuildType,
			Branding = context.Configuration.Branding
		};
}
