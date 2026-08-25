// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.Builder;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Extensions;
using Elastic.Documentation.Navigation.Assembler;
using Elastic.Documentation.Navigation.Tests.Isolation;
using Elastic.Documentation.Site;
using Elastic.Documentation.Site.FileProviders;
using Elastic.Documentation.Site.Layout;
using RazorSlices;

namespace Elastic.Documentation.Navigation.Tests.Rendering;

public class SecondaryNavRenderingTests(ITestOutputHelper output) : DocumentationSetNavigationTestBase(output)
{
	private const string ReferenceSectionId = "ref-section-id";

	private static readonly TopNavRenderModel TopNav = new([
		new TopNavLinkItem("Reference", "/docs/reference/", false, SectionId: ReferenceSectionId),
		new TopNavLinkItem("APIs", "https://www.elastic.co/docs/api/", true),
		new TopNavDropdownItem("Products", [
			new TopNavGroup("Stack products", [
				new TopNavLinkItem("Elasticsearch", "/docs/products/elasticsearch/", false)
			]),
			new TopNavGroup(null, [
				new TopNavLinkItem("All products", "/docs/products/", false)
			])
		])
	]);

	private static readonly TopNavRenderModel LinkOnlyTopNav = new([
		new TopNavLinkItem("Guides", "/docs/guides/", false),
		new TopNavLinkItem("Reference", "/docs/reference/", false, SectionId: ReferenceSectionId),
		new TopNavLinkItem("APIs", "https://www.elastic.co/docs/api/", true)
	]);

	[Fact]
	public async Task WithoutConfigurationTheBuiltInLinksAreRendered()
	{
		var html = await Render(topNav: null, currentUrl: "/docs/");

		// Built-in links present
		html.Should().Contain("Release notes").And.Contain("Troubleshoot").And.Contain("Reference");
		html.Should().Contain("href=\"#icon-refresh-time\"");
		html.Should().Contain("href=\"#icon-wrench\"");
		html.Should().Contain("href=\"#icon-list-bullet\"");
		html.Should().NotContain("secondary-nav-dropdown");
		html.Should().NotContain("id=\"htmx-indicator\"");
		html.Should().Contain("id=\"secondary-nav-host\"");
		html.Should().NotContain("hx-preserve");
		html.Should().Contain("secondary-nav-home").And.Contain(">Docs<");
		html.Should().Contain("href=\"/docs/\"");
	}

	[Fact]
	public async Task ConfiguredLinksReplaceTheBuiltInOnes()
	{
		var html = await Render(TopNav, currentUrl: "/docs/");

		html.Should().Contain("href=\"/docs/reference/\"");
		// the built-in links are gone once top_nav is configured
		html.Should().NotContain("Release notes").And.NotContain("Troubleshoot");
		html.Should().NotContain("id=\"htmx-indicator\"");
		html.Should().Contain("data-section-ids=\"ref-section-id\"");
	}

	[Fact]
	public async Task TopNavLinksRenderInMobileDrawer()
	{
		var html = await RenderPagesNav(LinkOnlyTopNav, currentUrl: "/docs/reference/some-page", root: new MockSectionRoot(ReferenceSectionId));

		html.Should().Contain("secondary-nav-mobile-menu");
		html.Should().Contain(">Section<");
		html.Should().Contain("<span>Reference</span>");
		html.Should().Contain("href=\"/docs/guides/\"");
		html.Should().Contain("href=\"/docs/reference/\"");
		html.Should().Contain("href=\"https://www.elastic.co/docs/api/\"");
		html.Should().Contain("target=\"_blank\"");
		html.Should().Contain("(opens in a new tab)");
	}

	[Fact]
	public async Task TopNavMobileDrawerUsesDocsHomeFallback()
	{
		var html = await RenderPagesNav(LinkOnlyTopNav, currentUrl: "/docs/");

		html.Should().Contain("<span>Docs Home</span>");
	}

	[Fact]
	public async Task MobileDrawerRendersVersionDropdown()
	{
		var html = await RenderPagesNav(topNav: null, currentUrl: "/docs/", showVersionDropdown: true);

		html.Should().Contain(">Version<");
		html.Should().Contain("<version-dropdown");
		html.Should().Contain("all-versions-url=\"/docs/versions/\"");
		html.Should().Contain("8.19");
		html.Should().Contain("items='[]'");
	}

	[Fact]
	public async Task TopNavMobileDrawerIncludesDocsHome()
	{
		var html = await RenderPagesNav(LinkOnlyTopNav, currentUrl: "/docs/");

		html.Should().Contain("secondary-nav-mobile-menu");
		html.Should().Contain("href=\"/docs/\"");
		html.Should().Contain(">Docs<");
	}

	[Fact]
	public async Task ConfiguredItemsRenderEuiIcons()
	{
		var html = await Render(TopNav, currentUrl: "/docs/");

		html.Should().Contain("href=\"#icon-list-bullet\"");
		html.Should().Contain("href=\"#icon-code\"");
		html.Should().Contain("href=\"#icon-grid\"");
		html.Should().Contain("href=\"#icon-chevron-down\"");
		html.Should().Contain("href=\"#icon-external\"");

		var linkOnly = await Render(LinkOnlyTopNav, currentUrl: "/docs/");
		linkOnly.Should().Contain("href=\"#icon-documentation\"");
	}

	[Fact]
	public async Task VersionDropdownRendersOnTheRightOfTheTopBar()
	{
		var html = await Render(TopNav, currentUrl: "/docs/", showVersionDropdown: true);

		html.Should().Contain("secondary-nav-actions");
		html.Should().Contain("<version-dropdown");
		html.Should().Contain("all-versions-url=\"/docs/versions/\"");
		html.Should().Contain("8.19");
		html.IndexOf("secondary-nav-list", StringComparison.Ordinal)
			.Should()
			.BeLessThan(html.IndexOf("secondary-nav-actions", StringComparison.Ordinal));
	}

	[Fact]
	public async Task VersionDropdownRendersOnTheBuiltInBar()
	{
		var html = await Render(topNav: null, currentUrl: "/docs/", showVersionDropdown: true);

		html.Should().Contain("secondary-nav-actions");
		html.Should().Contain("<version-dropdown");
	}

	[Fact]
	public async Task WithTopNavTheBarIsLeftAlignedAndIncludesDocsHome()
	{
		var html = await Render(TopNav, "/docs/");

		html.Should().Contain("secondary-nav-home").And.Contain(">Docs<");
		html.Should().Contain("href=\"/docs/\"");
		html.Should().Contain("secondary-nav-bar--desktop");
		html.Should().Contain("secondary-nav-scroll-container");
		html.Should().Contain("secondary-nav-bar");
	}

	[Fact]
	public async Task WithoutTopNavTheBarHasDocsBrandLink()
	{
		var html = await Render(null, "/docs/");

		html.Should().Contain("secondary-nav-home").And.Contain(">Docs<");
		html.Should().Contain("href=\"/docs/\"");
	}

	[Fact]
	public async Task ExternalLinksOpenInANewTab()
	{
		var html = await Render(TopNav, currentUrl: "/docs/");

		html.Should().Contain("href=\"https://www.elastic.co/docs/api/\"");
		html.Should().Contain("target=\"_blank\"");
		html.Should().Contain("rel=\"noopener noreferrer\"");
		html.Should().Contain("(opens in a new tab)");
	}

	[Fact]
	public async Task DropdownRendersItsGroupsAndLinks()
	{
		var html = await Render(TopNav, currentUrl: "/docs/");

		html.Should().Contain("<details class=\"secondary-nav-dropdown\">");
		html.Should().Contain("secondary-nav-dropdown-group-label\">Stack products");
		html.Should().Contain("href=\"/docs/products/elasticsearch/\"");
		html.Should().Contain("href=\"/docs/products/\"");
		// the label toggles the panel, it is never a link itself
		html.Should().NotContain("<summary><a");
	}

	[Fact]
	public async Task TheItemCoveringTheCurrentPageIsMarkedActive()
	{
		// Active state is determined by NavigationRoot.Id matching the tab's SectionId.
		var refRoot = new MockSectionRoot(ReferenceSectionId);
		var reference = await Render(TopNav, currentUrl: "/docs/reference/some-page", root: refRoot);
		var desktopTabs = reference.Split("<ul").Last();
		var referenceListItem = desktopTabs.Split("<li").First(li => li.Contains("Reference"));
		referenceListItem.Should().Contain("secondary-nav-item--active");

		// Dropdown tabs have no tree backing — they are never marked active via section ID.
		var product = await Render(TopNav, currentUrl: "/docs/products/elasticsearch/index");
		desktopTabs = product.Split("<ul").Last();
		var productListItem = desktopTabs.Split("<li").First(li => li.Contains("Products"));
		productListItem.Should().NotContain("secondary-nav-item--active");
	}

	[Fact]
	public async Task UnrelatedPagesLeaveEveryItemInactive()
	{
		var html = await Render(TopNav, currentUrl: "/docs/troubleshoot/");

		foreach (var listItem in html.Split("<li").Skip(1))
			listItem.Should().NotContain("secondary-nav-item--active");
	}

	private async Task<string> Render(
		TopNavRenderModel? topNav,
		string currentUrl,
		IRootNavigationItem<INavigationModel, INavigationItem>? root = null,
		bool showVersionDropdown = false)
	{
		var model = CreateModel(topNav, currentUrl, root);
		if (showVersionDropdown)
		{
			model = model with
			{
				ShowVersionDropdown = true,
				AllVersionsUrl = "/docs/versions/",
				CurrentVersion = "8.19",
				VersionDropdownSerializedModel = "[]"
			};
		}

		return await _SecondaryNav.Create(model).RenderAsync(cancellationToken: TestContext.Current.CancellationToken);
	}

	private async Task<string> RenderPagesNav(
		TopNavRenderModel? topNav,
		string currentUrl,
		IRootNavigationItem<INavigationModel, INavigationItem>? root = null,
		bool showVersionDropdown = false)
	{
		var model = CreateModel(topNav, currentUrl, root) with
		{
			NavigationHtml = "<ul id=\"nav-tree-test\"></ul>",
			ShowVersionDropdown = showVersionDropdown,
			AllVersionsUrl = "/docs/versions/",
			CurrentVersion = "8.19",
			VersionDropdownSerializedModel = "[]"
		};

		return await _PagesNav.Create(model).RenderAsync(cancellationToken: TestContext.Current.CancellationToken);
	}

	private GlobalLayoutViewModel CreateModel(
		TopNavRenderModel? topNav,
		string currentUrl,
		IRootNavigationItem<INavigationModel, INavigationItem>? root = null)
	{
		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs");
		var context = CreateContext(fileSystem);

		// TopNav is now derived from the parent chain. Wire a MockSiteNavigationRoot as the
		// immediate parent so GlobalLayoutViewModel.TopNav returns the expected value.
		var siteRoot = new MockSiteNavigationRoot(topNav);
		var currentNavItem = new StubNavigationItem(currentUrl, root) { Parent = siteRoot };

		var model = new GlobalLayoutViewModel
		{
			DocsBuilderVersion = "test",
			DocSetName = "test",
			Description = "",
			CurrentNavigationItem = currentNavItem,
			Previous = null,
			Next = null,
			NavigationHtml = "",
			UrlPathPrefix = "/docs",
			CanonicalBaseUrl = null,
			AllowIndexing = false,
			Features = new FeatureFlags([]),
			GoogleTagManager = new GoogleTagManagerConfiguration(),
			Optimizely = new OptimizelyConfiguration(),
			StaticFileContentHashProvider = new StaticFileContentHashProvider(new EmbeddedOrPhysicalFileProvider(context)),
		};

		return model;
	}

	/// <summary>The secondary nav only reads <see cref="INavigationItem.Url"/> off the current page.</summary>
	private sealed record StubNavigationItem(
		string Url,
		IRootNavigationItem<INavigationModel, INavigationItem>? Root = null) : INavigationItem
	{
		public string NavigationTitle => "stub";
		public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot => Root ?? null!;
		public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; }
		public bool Hidden => false;
		public int NavigationIndex { get; set; }
	}

	/// <summary>
	/// Stands in for <c>SiteNavigation</c> as the outermost parent so
	/// <see cref="GlobalLayoutViewModel.TopNav"/> resolves correctly.
	/// </summary>
	private sealed class MockSiteNavigationRoot(TopNavRenderModel? topNav)
		: INodeNavigationItem<INavigationModel, INavigationItem>, ISiteNavigationRoot
	{
		public TopNavRenderModel? TopNav { get; } = topNav;
		public string Id => "mock-site";
		public string Url => "/";
		public string NavigationTitle => "Mock Site";
		public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot => null!;
		public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; }
		public bool Hidden => false;
		public int NavigationIndex { get; set; }
		public ILeafNavigationItem<INavigationModel> Index => null!;
		public IReadOnlyCollection<INavigationItem> NavigationItems => [];
	}

	/// <summary>
	/// Minimal root stub — <c>_SecondaryNav.cshtml</c> reads <see cref="INavigationItem.NavigationRoot"/>
	/// and compares its Id against each tab's SectionId(s).
	/// </summary>
	private sealed class MockSectionRoot(string id)
		: IRootNavigationItem<INavigationModel, INavigationItem>
	{
		public string Id => id;
		public Uri Identifier => new($"section://{id}");
		public ILeafNavigationItem<INavigationModel> Index => null!;
		public IReadOnlyCollection<INavigationItem> NavigationItems => [];
		public string Url => $"/{id}/";
		public string NavigationTitle => id;
		public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot => this;
		public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; }
		public bool Hidden => false;
		public int NavigationIndex { get; set; }
		public bool IsUsingNavigationDropdown => false;
		public void SetNavigationItems(IReadOnlyCollection<INavigationItem> navigationItems) { }
	}
}
