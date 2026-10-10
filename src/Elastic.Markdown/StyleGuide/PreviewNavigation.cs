// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Navigation;
using Elastic.Documentation.Navigation.Assembler;

namespace Elastic.Markdown.StyleGuide;

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

	public static INavigationItem Page(string title, string url, bool underSite) =>
		new PreviewItem(title, url) { Parent = underSite ? new PreviewSiteRoot(TopNav) : null };

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
