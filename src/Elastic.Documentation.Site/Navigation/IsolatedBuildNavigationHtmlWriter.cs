// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Extensions;

namespace Elastic.Documentation.Site.Navigation;

public class IsolatedBuildNavigationHtmlWriter(BuildContext context, IRootNavigationItem<INavigationModel, INavigationItem> siteRoot)
	: INavigationHtmlWriter
{
	private readonly ConcurrentDictionary<(string, int), string> _renderedNavigationCache = [];

	public async Task<INavigationRenderResult> RenderNavigation(IRootNavigationItem<INavigationModel, INavigationItem> currentRootNavigation,
		Uri navigationSource, int maxLevel, Cancel ctx = default)
	{
		var navigation = context.Configuration.Features.PrimaryNavEnabled || currentRootNavigation.IsUsingNavigationDropdown
			? currentRootNavigation
			: siteRoot;

		var id = ShortId.Create($"{(navigation.Id, maxLevel).GetHashCode()}");

		if (_renderedNavigationCache.TryGetValue((navigation.Id, maxLevel), out var value))
			return new OkNavigationRenderResult
			{
				Html = value,
				Id = id
			};

		var model = CreateNavigationModel(navigation, maxLevel);
		value = await ((INavigationHtmlWriter)this).Render(model, ctx);
		_renderedNavigationCache[(navigation.Id, maxLevel)] = value;
		return new OkNavigationRenderResult
		{
			Html = value,
			Id = id
		};
	}

	private NavigationViewModel CreateNavigationModel(IRootNavigationItem<INavigationModel, INavigationItem> navigation, int maxLevel) =>
		new()
		{
			Title = navigation.NavigationTitle,
			TitleUrl = navigation.Url,
			Tree = navigation,
			IsPrimaryNavEnabled = context.Configuration.Features.PrimaryNavEnabled,
			IsUsingNavigationDropdown = context.Configuration.Features.PrimaryNavEnabled || navigation.IsUsingNavigationDropdown,
			IsGlobalAssemblyBuild = false,
			TopLevelItems = siteRoot.NavigationItems.OfType<INodeNavigationItem<INavigationModel, INavigationItem>>().ToList(),
			MaxLevel = maxLevel
		};
}
