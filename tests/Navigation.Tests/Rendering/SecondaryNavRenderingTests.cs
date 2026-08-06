// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.Builder;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Extensions;
using Elastic.Documentation.Navigation.Tests.Isolation;
using Elastic.Documentation.Site;
using Elastic.Documentation.Site.FileProviders;
using Elastic.Documentation.Site.Layout;
using RazorSlices;

namespace Elastic.Documentation.Navigation.Tests.Rendering;

public class SecondaryNavRenderingTests(ITestOutputHelper output) : DocumentationSetNavigationTestBase(output)
{
	private static readonly TopNavRenderModel TopNav = new([
		new TopNavLinkItem("Reference", "/docs/reference/", false),
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

	[Fact]
	public async Task WithoutConfigurationTheBuiltInLinksAreRendered()
	{
		var html = await Render(topNav: null, currentUrl: "/docs/");

		html.Should().Contain("Release notes").And.Contain("Troubleshoot").And.Contain("Reference");
		html.Should().NotContain("secondary-nav-dropdown");
		html.Should().Contain("id=\"htmx-indicator\"");
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
	public async Task TheBarIsLeftAlignedAndCarriesNoBrandLink()
	{
		foreach (var html in new[] { await Render(TopNav, "/docs/"), await Render(null, "/docs/") })
		{
			html.Should().NotContain(">Docs<");
			html.Should().Contain("justify-start").And.NotContain("justify-between");
		}
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
		var reference = await Render(TopNav, currentUrl: "/docs/reference/some-page");
		var referenceListItem = reference.Split("<li").First(li => li.Contains("Reference"));
		referenceListItem.Should().Contain("text-blue-elastic").And.NotContain("hover:text-blue-elastic");

		// a page under a dropdown child highlights the dropdown label
		var product = await Render(TopNav, currentUrl: "/docs/products/elasticsearch/index");
		var productListItem = product.Split("<li").First(li => li.Contains("Products"));
		productListItem.Should().Contain("text-blue-elastic");
	}

	[Fact]
	public async Task UnrelatedPagesLeaveEveryItemInactive()
	{
		var html = await Render(TopNav, currentUrl: "/docs/troubleshoot/");

		foreach (var listItem in html.Split("<li").Skip(1))
			listItem.Should().Contain("hover:text-blue-elastic");
	}

	private async Task<string> Render(TopNavRenderModel? topNav, string currentUrl)
	{
		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs");
		var context = CreateContext(fileSystem);

		var model = new GlobalLayoutViewModel
		{
			DocsBuilderVersion = "test",
			DocSetName = "test",
			Description = "",
			CurrentNavigationItem = new StubNavigationItem(currentUrl),
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
			TopNav = topNav
		};

		return await _SecondaryNav.Create(model).RenderAsync(cancellationToken: TestContext.Current.CancellationToken);
	}

	/// <summary>The secondary nav only reads <see cref="INavigationItem.Url"/> off the current page.</summary>
	private sealed record StubNavigationItem(string Url) : INavigationItem
	{
		public string NavigationTitle => "stub";
		public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot => null!;
		public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; }
		public bool Hidden => false;
		public int NavigationIndex { get; set; }
	}
}
