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
		html.Should().NotContain("secondary-nav-dropdown");
		html.Should().Contain("id=\"htmx-indicator\"");
		// Flag-off: Docs brand link and justify-between layout match main exactly
		html.Should().Contain(">Docs<");
		html.Should().Contain("justify-between").And.NotContain("justify-start");
	}

	[Fact]
	public async Task ConfiguredLinksReplaceTheBuiltInOnes()
	{
		var html = await Render(TopNav, currentUrl: "/docs/");

		html.Should().Contain("href=\"/docs/reference/\"");
		// the built-in links are gone once top_nav is configured
		html.Should().NotContain("Release notes").And.NotContain("Troubleshoot");
		html.Should().Contain("id=\"htmx-indicator\"");
	}

	[Fact]
	public async Task TopNavLinksRenderMobileMenu()
	{
		var html = await Render(LinkOnlyTopNav, currentUrl: "/docs/reference/some-page", root: new MockSectionRoot(ReferenceSectionId));

		html.Should().Contain("secondary-nav-mobile-menu");
		html.Should().Contain("<span>Reference</span>");
		html.Should().Contain("href=\"/docs/guides/\"");
		html.Should().Contain("href=\"/docs/reference/\"");
		html.Should().Contain("href=\"https://www.elastic.co/docs/api/\"");
		html.Should().Contain("target=\"_blank\"");
		html.Should().Contain("(opens in a new tab)");
	}

	[Fact]
	public async Task TopNavMobileMenuUsesDocsHomeFallback()
	{
		var html = await Render(LinkOnlyTopNav, currentUrl: "/docs/");

		html.Should().Contain("<span>Docs Home</span>");
	}

	[Fact]
	public async Task WithTopNavTheBarIsLeftAlignedAndCarriesNoBrandLink()
	{
		var html = await Render(TopNav, "/docs/");

		html.Should().NotContain(">Docs<");
		html.Should().Contain("secondary-nav-scroll-container");
		html.Should().Contain("justify-start");
	}

	[Fact]
	public async Task WithoutTopNavTheBarHasDocsBrandLinkAndIsJustifiedBetween()
	{
		var html = await Render(null, "/docs/");

		html.Should().Contain(">Docs<");
		html.Should().Contain("justify-between").And.NotContain("justify-start");
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
		referenceListItem.Should().Contain("text-blue-elastic").And.NotContain("hover:text-blue-elastic");

		// Dropdown tabs have no tree backing — they are never marked active via section ID.
		var product = await Render(TopNav, currentUrl: "/docs/products/elasticsearch/index");
		desktopTabs = product.Split("<ul").Last();
		var productListItem = desktopTabs.Split("<li").First(li => li.Contains("Products"));
		// "hover:text-blue-elastic" present means the inactive CSS variant is applied, not the active one.
		productListItem.Should().Contain("hover:text-blue-elastic")
			.And.NotContain("relative text-blue-elastic\"");
	}

	[Fact]
	public async Task UnrelatedPagesLeaveEveryItemInactive()
	{
		var html = await Render(TopNav, currentUrl: "/docs/troubleshoot/");

		foreach (var listItem in html.Split("<li").Skip(1))
			listItem.Should().Contain("hover:text-blue-elastic");
	}

	private async Task<string> Render(
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

		return await _SecondaryNav.Create(model).RenderAsync(cancellationToken: TestContext.Current.CancellationToken);
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
