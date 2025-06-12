// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using Elastic.Documentation.Configuration;

namespace Elastic.Documentation.Site.Navigation;

public class IsolatedBuildNavigationHtmlWriter(BuildContext context, INodeNavigationItem<INavigationModel, INavigationItem> siteRoot)
	: INavigationHtmlWriter
{
	private readonly ConcurrentDictionary<string, string> _renderedNavigationCache = [];

	public async Task<string> RenderNavigation(INodeNavigationItem<INavigationModel, INavigationItem> currentRootNavigation, Uri navigationSource, Cancel ctx = default)
	{
		var navigation = context.Configuration.Features.IsPrimaryNavEnabled
			? currentRootNavigation
			: siteRoot;

		if (_renderedNavigationCache.TryGetValue(navigation.Id, out var value))
			return value;

		var model = CreateNavigationModel(navigation);
		value = await ((INavigationHtmlWriter)this).Render(model, ctx);
		_renderedNavigationCache[navigation.Id] = value;
		return value;
	}

	private NavigationViewModel CreateNavigationModel(INodeNavigationItem<INavigationModel, INavigationItem> navigation) =>
		new()
		{
			Title = navigation.NavigationTitle,
			TitleUrl = navigation.Url,
			Tree = navigation,
			IsPrimaryNavEnabled = context.Configuration.Features.IsPrimaryNavEnabled,
			IsGlobalAssemblyBuild = false,
			TopLevelItems = siteRoot.NavigationItems.OfType<INodeNavigationItem<INavigationModel, INavigationItem>>().ToList()
		};
}
