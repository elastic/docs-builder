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

	async Task<NavigationRenderResult> Render(NavigationRenderModel model, Cancel ctx)
	{
		var slice = _TocTree.Create(model);
		var html = await slice.RenderAsync(cancellationToken: ctx);
		// Build the sidebar link index once here so every per-page Apply call is O(1).
		var index = NavigationCurrentMarker.BuildSidebarIndex(html);
		return new NavigationRenderResult { Html = html, Id = model.ContentHash, SidebarIndex = index };
	}
}

public record NavigationRenderResult
{
	public static NavigationRenderResult Empty { get; } = new() { Html = string.Empty, Id = "empty-navigation" };

	public required string Html { get; init; }
	public required string Id { get; init; }

	/// <summary>
	/// Pre-built index mapping each normalized sidebar-link href to the position and pre-modified tag
	/// for stamping the <c>current</c> class.  Built once when the navigation HTML is rendered and
	/// shared across all pages that use the same sidebar root, making per-page
	/// <see cref="NavigationCurrentMarker.Apply(NavigationRenderResult, INavigationItem)"/>
	/// an O(1) dictionary lookup instead of a linear scan of the full HTML.
	/// <para>
	/// May be <see langword="null"/> for test stubs and other callers that build
	/// <see cref="NavigationRenderResult"/> directly without going through
	/// <see cref="INavigationHtmlWriter.Render"/>; the marker falls back to the linear scan in that case.
	/// </para>
	/// </summary>
	internal IReadOnlyDictionary<string, SidebarLinkEntry>? SidebarIndex { get; init; }
}

/// <summary>
/// Pre-computed position of one <c>sidebar-link</c> anchor in the cached sidebar HTML,
/// used by <see cref="NavigationCurrentMarker"/> to stamp the <c>current</c> class without rescanning.
/// </summary>
internal readonly record struct SidebarLinkEntry(
	/// <summary>Index of the opening <c>&lt;</c> of the <c>&lt;a </c> tag.</summary>
	int TagStart,
	/// <summary>
	/// The tag text with <c>current</c> already appended to its <c>class</c> attribute,
	/// ready to splice in as a replacement.
	/// </summary>
	string ModifiedTag,
	/// <summary>Index of the <c>&gt;</c> that closes the opening tag (exclusive end of the original tag).</summary>
	int TagEnd
);
