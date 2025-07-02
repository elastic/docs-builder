// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using RazorSlices;

namespace Elastic.Documentation.Site.Navigation;

public interface INavigationHtmlWriter
{
	const int AllLevels = -1;

	Task<string> RenderNavigation(IRootNavigationItem<INavigationModel, INavigationItem> currentRootNavigation, Uri navigationSource, int maxLevel, Cancel ctx = default);

	async Task<string> Render(NavigationViewModel model, Cancel ctx)
	{
		var slice = _TocTree.Create(model);
		return await slice.RenderAsync(cancellationToken: ctx);
	}
}
