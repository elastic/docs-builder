// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Navigation;
using Elastic.Documentation.Navigation.V2;
using RazorSlices;

namespace Elastic.Documentation.Site.Navigation;

public interface INavigationHtmlWriter
{
	Task<NavigationRenderResult> RenderNavigation(
		IRootNavigationItem<INavigationModel, INavigationItem> currentRootNavigation,
		INavigationItem currentNavigationItem,
		Cancel ctx = default
	);

	async Task<string> Render(NavigationViewModel model, Cancel ctx)
	{
		var slice = _TocTree.Create(model);
		return await slice.RenderAsync(cancellationToken: ctx);
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

	/// <summary>V2 section metadata for the secondary nav bar. Null for V1 builds.</summary>
	public IReadOnlyList<NavigationSection>? Sections { get; init; }

	/// <summary>The active section ID for highlighting the current tab.</summary>
	public string? ActiveSectionId { get; init; }
}
