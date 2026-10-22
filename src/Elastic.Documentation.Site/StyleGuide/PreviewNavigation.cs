// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Navigation;
using Elastic.Documentation.Navigation.Assembler;
using Elastic.Documentation.Site.Navigation;

namespace Elastic.Documentation.Site.StyleGuide;

/// <summary>Placeholder navigation for the style guide's previews, so the real layouts have something to hang their chrome on.</summary>
public static class PreviewNavigation
{
	public static TopNavRenderModel TopNav { get; } = new([
		new TopNavLinkItem("Guides", "/docs/get-started", false, "guides"),
		new TopNavLinkItem("APIs", "https://www.elastic.co/docs/api", true),
		new TopNavDropdownItem(
			"Products",
			[
				new TopNavGroup(
					"Search",
					[
						new TopNavLinkItem("Elasticsearch", "/docs/solutions/search", false),
						new TopNavLinkItem("Vector Database", "/docs/solutions/vector", false)
					]
				),
				new TopNavGroup(
					"Platform",
					[
						new TopNavLinkItem("Kibana", "/docs/explore-analyze", false),
						new TopNavLinkItem("Observability", "/docs/solutions/observability", false)
					]
				)
			]
		),
		new TopNavLinkItem("Reference", "/docs/reference", false, "reference"),
		new TopNavLinkItem("Troubleshoot", "/docs/troubleshoot", false),
		new TopNavLinkItem("Release notes", "/docs/release-notes", false)
	]);

	private static NavigationRenderNode Leaf(string title) =>
		new() { Kind = NavigationRenderNodeKind.Leaf, IsTopLevel = false, NavigationTitle = title, Url = "#" };

	private static NavigationRenderNode Docset(string title, params string[] pages) =>
		new()
		{
			Kind = NavigationRenderNodeKind.Heading,
			IsTopLevel = false,
			NavigationTitle = title,
			Url = "#",
			NavigationItems = [.. pages.Select(Leaf)]
		};

	/// <summary>
	/// The tree beside a documentation page of a Codex site. A set that belongs to a group sits beside its sibling set under the group's
	/// title, with a way back to the codex; one that belongs to no group shows only its own pages.
	/// </summary>
	public static NavigationRenderModel CodexDocs(bool inGroup) =>
		new()
		{
			IsUsingNavigationDropdown = false,
			CurrentTopLevelNavigationTitle = "Lorem Ipsum",
			CurrentTopLevelUrl = "#",
			DropdownItems = [],
			BackLinks = inGroup ? [new IslandBackLink("Codex", "/")] : [],
			TreeHeading = inGroup ? "Search" : null,
			ContentHash = inGroup ? "style-guide-codex-group" : "style-guide-codex-set",
			Tree = inGroup
				? [
					Docset("Lorem Ipsum", "Overview", "Install", "Index data", "Search"),
					Docset("Dolor Sit", "Overview", "Configure", "Operate")
				]
				: [Leaf("Overview"), Leaf("Install"), Leaf("Index data"), Leaf("Search"), Leaf("Operate")],
			NavigationPreviewEnabled = true
		};

	public static INavigationItem Page(string title, string url, bool underSite) =>
		new PreviewItem(title, url) { Parent = underSite ? new PreviewSiteRoot(TopNav) : null };

	/// <summary>Hangs a set's navigation under a site root, as an assembler build does, so the docs bar has the site's sections.</summary>
	public static void UnderSite(INavigationItem root) => root.Parent = new PreviewSiteRoot(TopNav);

	public static INavigationItem Crumb(string title, string url) => new PreviewItem(title, url);

	private sealed record PreviewItem(string NavigationTitle, string Url) : INavigationItem
	{
		public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot => null!;
		public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; }
		public bool Hidden => false;
		public int NavigationIndex { get; set; }
	}

	private sealed class PreviewSiteRoot(
		TopNavRenderModel topNav
	) : INodeNavigationItem<INavigationModel, INavigationItem>, ISiteNavigationRoot
	{
		public TopNavRenderModel? TopNav { get; } = topNav;
		public string Id => "style-guide-preview";
		public string Url => "/";
		public string NavigationTitle => "Preview";
		public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot => null!;
		public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; }
		public bool Hidden => false;
		public int NavigationIndex { get; set; }
		public ILeafNavigationItem<INavigationModel> Index => null!;
		public IReadOnlyCollection<INavigationItem> NavigationItems => [];
	}
}
