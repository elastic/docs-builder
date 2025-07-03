// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using RazorSlices;

namespace Elastic.Documentation.Site.Navigation;

public interface INavigationHtmlWriter
{
	const int AllLevels = -1;

	Task<INavigationRenderResult> RenderNavigation(IRootNavigationItem<INavigationModel, INavigationItem> currentRootNavigation, Uri navigationSource,
		int maxLevel, Cancel ctx = default);

	async Task<string> Render(NavigationViewModel model, Cancel ctx)
	{
		var slice = _TocTree.Create(model);
		return await slice.RenderAsync(cancellationToken: ctx);
	}
}

public interface INavigationRenderResult
{
	string Html { get; init; }
	string Id { get; init; }
}

public record OkNavigationRenderResult : INavigationRenderResult
{
	public required string Html { get; init; }
	public required string Id { get; init; }
}

public record EmptyNavigationRenderResult : INavigationRenderResult
{
	public string Html { get; init; } = string.Empty;
	public string Id { get; init; } = string.Empty;
}
