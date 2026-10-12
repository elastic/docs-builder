// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Site.Components.Navigation.NavTree;
using Elastic.Documentation.Site.Navigation;

namespace Elastic.Documentation.Site.Components.Navigation.NavSide;

public static class NavSideExamples
{
	private static NavigationRenderNode Leaf(string title, string url, string? badge = null, string? method = null) =>
		new()
		{
			Kind = NavigationRenderNodeKind.Leaf,
			IsTopLevel = false,
			NavigationTitle = title,
			Url = url,
			Badge = badge,
			HttpMethod = method
		};

	private static NavigationRenderNode Group(string id, string title, string url, params NavigationRenderNode[] children) =>
		new()
		{
			Kind = NavigationRenderNodeKind.Node,
			IsTopLevel = false,
			NavigationTitle = title,
			Url = url,
			Id = id,
			ShowToggle = true,
			NavigationItems = children
		};

	public static NavigationRenderModel Navigation { get; } = new()
	{
		IsUsingNavigationDropdown = false,
		CurrentTopLevelNavigationTitle = "Elasticsearch",
		CurrentTopLevelUrl = "#",
		DropdownItems = [],
		BackLinks = [new IslandBackLink("Docs", "#")],
		TreeHeading = "Elasticsearch",
		ContentHash = "style-guide",
		Tree =
		[
			Leaf("Overview", "#"),
			new() { Kind = NavigationRenderNodeKind.Separator, IsTopLevel = false, NavigationTitle = string.Empty, Url = string.Empty },
			Group("sg-nav-1", "Get started", "#", Leaf("Install", "#"), Leaf("Index data", "#"), Leaf("Search", "#")),
			Group(
				"sg-nav-2",
				"REST API",
				"#",
				Leaf("Search documents", "#", method: "post"),
				Leaf("Get document", "#", method: "get"),
				Leaf("Delete document", "#", method: "delete")
			),
			Leaf("Query DSL", "#", badge: "ns"),
			new() { Kind = NavigationRenderNodeKind.Island, IsTopLevel = false, NavigationTitle = "Clients", Url = "#" }
		],
		NavigationPreviewEnabled = true
	};

	/// <summary>The top-level documentation tree: no back links and no section title, because the docs home is never an island.</summary>
	public static NavigationRenderModel TopLevel { get; } = Navigation with
	{
		BackLinks = [],
		TreeHeading = null,
		ContentHash = "style-guide-top",
		Tree =
		[
			Leaf("Overview", "#"),
			new() { Kind = NavigationRenderNodeKind.Separator, IsTopLevel = false, NavigationTitle = string.Empty, Url = string.Empty },
			Group("sg-top-1", "Get started", "#", Leaf("Install", "#"), Leaf("Index data", "#"), Leaf("Search", "#")),
			Group("sg-top-2", "Guides", "#", Leaf("Ingest data", "#"), Leaf("Query your data", "#")),
			Leaf("Reference", "#")
		]
	};

	public static ComponentSection Sidebars { get; } = new(
		"Sidebar navigation",
		"A server-rendered tree. Folders expand with a native checkbox, so they work without JavaScript; a small enhancement marks the current page, opens its ancestors and remembers what you expanded. An island link opens a section as its own tree, with back links and the section's title.",
		"nav-side",
		[
			ComponentExample.Of("Top level, without an island", _NavSide.Create, new NavSideModel { Navigation = TopLevel }),
			ComponentExample.Of("Inside an island", _NavSide.Create, new NavSideModel { Navigation = Navigation })
		]
	);
}
