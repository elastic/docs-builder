// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Navigation;
using RazorSlices;

namespace Elastic.Documentation.Site.Navigation;

public interface INavigationHtmlWriter
{
	Task<NavigationRenderResult> RenderNavigation(
		IRootNavigationItem<INavigationModel, INavigationItem> currentRootNavigation,
		INavigationItem currentNavigationItem,
		Cancel ctx = default
	);

	async Task<NavigationRenderResult> Render(NavigationViewModel model, Cancel ctx)
	{
		var renderModel = NavigationRenderModel.Create(model);
		var slice = _TocTree.Create(renderModel);
		var html = await slice.RenderAsync(cancellationToken: ctx);
		return new NavigationRenderResult
		{
			Html = html,
			Id = renderModel.ContentHash
		};
	}
}
public record NavigationRenderResult
{
	public static NavigationRenderResult Empty { get; } = new()
	{
		Html = string.Empty,
		Id = "empty-navigation"
	};

	public required string Html { get; init; }
	public required string Id { get; init; }
}
