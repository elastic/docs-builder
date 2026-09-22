// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Site.Navigation;
using RazorSlices;

namespace Elastic.Documentation.Navigation.Tests.Rendering;

public class TocTreeRenderingTests
{
	[Fact]
	public async Task TocTree_RendersFigmaShellWithoutNavV2Hook()
	{
		var model = new NavigationRenderModel
		{
			IsUsingNavigationDropdown = true,
			CurrentTopLevelNavigationTitle = "Guides",
			CurrentTopLevelUrl = "/docs/get-started",
			DropdownItems = [new NavigationDropdownItem("Guides", "/docs/get-started", true)],
			BackLinks = [new IslandBackLink("Docs Home", "/docs/")],
			Tree =
			[
				new NavigationRenderNode
				{
					Kind = NavigationRenderNodeKind.Leaf,
					IsTopLevel = true,
					NavigationTitle = "Get started",
					Url = "/docs/get-started"
				}
			],
			ContentHash = "test",
			NavigationPreviewEnabled = true
		};

		var html = await _TocTree.Create(model).RenderAsync(cancellationToken: TestContext.Current.CancellationToken);

		html.Should().Contain("pages-nav-v2-shell");
		html.Should().Contain("pages-nav-v2__scroll");
		html.Should().Contain("id=\"nav-dropdown\"");
		html.Should().Contain("pages-nav-v2__back");
		html.Should().Contain("nav-v2-link");
		html.Should().Contain("Guides");
		html.Should().NotContain("data-nav-v2");
		html.Should().NotContain("navigation-search");
		html.Should().NotContain("hx-preserve");
	}

	[Fact]
	public async Task FolderRow_PutsChevronInsideTheSameLink()
	{
		var model = new NavigationRenderModel
		{
			IsUsingNavigationDropdown = false,
			CurrentTopLevelNavigationTitle = "Docs",
			CurrentTopLevelUrl = "/docs/",
			DropdownItems = [],
			BackLinks = [],
			Tree =
			[
				new NavigationRenderNode
				{
					Kind = NavigationRenderNodeKind.Node,
					IsTopLevel = true,
					NavigationTitle = "Contribute",
					Url = "/docs/contribute",
					Id = "contribute",
					ShowToggle = true,
					NavigationItems =
					[
						new NavigationRenderNode
						{
							Kind = NavigationRenderNodeKind.Leaf,
							IsTopLevel = false,
							NavigationTitle = "How to",
							Url = "/docs/contribute/how"
						}
					]
				}
			],
			ContentHash = "folder",
			NavigationPreviewEnabled = true
		};

		var html = await _TocTree.Create(model).RenderAsync(cancellationToken: TestContext.Current.CancellationToken);

		var linkStart = html.IndexOf("href=\"/docs/contribute\"", StringComparison.Ordinal);
		linkStart.Should().BeGreaterThanOrEqualTo(0);
		var linkEnd = html.IndexOf("</a>", linkStart, StringComparison.Ordinal);
		var row = html[linkStart..linkEnd];
		row.Should().Contain("nav-folder-chevron");
		row.Should().Contain("nav-v2-nav-text");
		html.Should().NotContain("<label for=\"contribute\"");
		html.Should().Contain("id=\"contribute\"");
	}

	[Fact]
	public async Task HeadingRow_IsALabelNotAPageLink()
	{
		var model = new NavigationRenderModel
		{
			IsUsingNavigationDropdown = false,
			CurrentTopLevelNavigationTitle = "API",
			CurrentTopLevelUrl = "/api/doc/es/",
			DropdownItems = [],
			BackLinks = [],
			Tree =
			[
				new NavigationRenderNode
				{
					Kind = NavigationRenderNodeKind.Heading,
					IsTopLevel = true,
					NavigationTitle = "Search & Document APIs",
					Url = "",
					Id = "search-docs",
					ShowToggle = true,
					NavigationItems =
					[
						new NavigationRenderNode
						{
							Kind = NavigationRenderNodeKind.Leaf,
							IsTopLevel = false,
							NavigationTitle = "Run a search",
							Url = "/api/doc/es/operation/operation-search"
						}
					]
				}
			],
			ContentHash = "heading",
			NavigationPreviewEnabled = true
		};

		var html = await _TocTree.Create(model).RenderAsync(cancellationToken: TestContext.Current.CancellationToken);

		html.Should().Contain("<label for=\"search-docs\"");
		html.Should().Contain("Search &amp; Document APIs");
		html.Should().NotContain("href=\"/api/doc/es/\"");
		html.Should().Contain("href=\"/api/doc/es/operation/operation-search\"");
	}

	[Fact]
	public async Task IslandStub_UsesTheForwardArrowNotTheFolderChevron()
	{
		var model = new NavigationRenderModel
		{
			IsUsingNavigationDropdown = false,
			CurrentTopLevelNavigationTitle = "Reference",
			CurrentTopLevelUrl = "/docs/reference",
			DropdownItems = [],
			BackLinks = [],
			Tree =
			[
				new NavigationRenderNode
				{
					Kind = NavigationRenderNodeKind.Island,
					IsTopLevel = true,
					NavigationTitle = "Elasticsearch",
					Url = "/docs/reference/elasticsearch"
				}
			],
			ContentHash = "island-stub",
			NavigationPreviewEnabled = true
		};

		var html = await _TocTree.Create(model).RenderAsync(cancellationToken: TestContext.Current.CancellationToken);

		html.Should().Contain("nav-island-arrow");
		html.Should().Contain("href=\"#icon-chevron-limit-right\"");
		html.Should().NotContain("href=\"#icon-chevron-down\"");
	}

	[Fact]
	public async Task IslandOverview_RendersHeadingAndOverviewLeaf()
	{
		var model = new NavigationRenderModel
		{
			IsUsingNavigationDropdown = false,
			CurrentTopLevelNavigationTitle = "Reference",
			CurrentTopLevelUrl = "/docs/reference",
			DropdownItems = [],
			BackLinks = [new IslandBackLink("Reference", "/docs/reference")],
			TreeHeading = "Elasticsearch",
			TreeHeadingIcon = "elasticsearch",
			Tree =
			[
				new NavigationRenderNode
				{
					Kind = NavigationRenderNodeKind.Leaf,
					IsTopLevel = true,
					NavigationTitle = "Overview",
					Url = "/docs/reference/elasticsearch"
				},
				new NavigationRenderNode
				{
					Kind = NavigationRenderNodeKind.Leaf,
					IsTopLevel = true,
					NavigationTitle = "REST APIs",
					Url = "/docs/reference/elasticsearch/rest-apis"
				}
			],
			ContentHash = "island-overview",
			NavigationPreviewEnabled = true
		};

		var html = await _TocTree.Create(model).RenderAsync(cancellationToken: TestContext.Current.CancellationToken);

		html.Should().Contain("pages-nav-v2__back");
		html.Should().Contain("data-nav-heading=\"Elasticsearch\"");
		html.Should().Contain("pages-nav-v2__heading-text");
		html.Should().NotContain("pages-nav-v2__heading-icon");
		html.Should().Contain("Elasticsearch");
		html.Should().Contain("Overview");
		html.Should().Contain("REST APIs");
		html.Should().Contain("href=\"/docs/reference/elasticsearch\"");
		html.Should().NotContain("href=\"#\" class=\"pages-nav-v2__heading");
		html.Should().NotContain("id=\"elasticsearch\"");
		html.Should().NotContain("nav-v2-separator");
		html.Should().NotContain("hx-preserve");
	}

	[Fact]
	public async Task VersionSwitcher_StaysOutOfTheTree()
	{
		var model = new NavigationRenderModel
		{
			IsUsingNavigationDropdown = false,
			CurrentTopLevelNavigationTitle = "APIs",
			CurrentTopLevelUrl = "/api/doc/elasticsearch/",
			DropdownItems = [],
			BackLinks = [],
			VersionSwitcher =
			[
				new NavigationSelectOption("Latest", "/api/doc/elasticsearch/", Selected: false),
				new NavigationSelectOption("9.x", "/api/doc/elasticsearch/v9/", Selected: true),
				new NavigationSelectOption("8.x", "/api/doc/elasticsearch/v8/", Selected: false)
			],
			Tree =
			[
				new NavigationRenderNode
				{
					Kind = NavigationRenderNodeKind.Leaf,
					IsTopLevel = true,
					NavigationTitle = "Overview",
					Url = "/api/doc/elasticsearch/"
				}
			],
			ContentHash = "api-versions",
			NavigationPreviewEnabled = true
		};

		var html = await _TocTree.Create(model).RenderAsync(cancellationToken: TestContext.Current.CancellationToken);

		html.Should().NotContain("api-version-switcher");
		html.Should().NotContain("pages-nav-v2__back-chrome");
	}

	[Fact]
	public async Task TreeSeparator_RendersAfterIntroLeaves()
	{
		var model = new NavigationRenderModel
		{
			IsUsingNavigationDropdown = false,
			CurrentTopLevelNavigationTitle = "APIs",
			CurrentTopLevelUrl = "/api/doc/elasticsearch/",
			DropdownItems = [],
			BackLinks = [],
			RootIndex = new NavigationRenderNode
			{
				Kind = NavigationRenderNodeKind.Leaf,
				IsTopLevel = true,
				NavigationTitle = "Api Overview",
				Url = "/api/doc/elasticsearch/"
			},
			Tree =
			[
				new NavigationRenderNode
				{
					Kind = NavigationRenderNodeKind.Leaf,
					IsTopLevel = true,
					NavigationTitle = "Authentication",
					Url = "/api/doc/elasticsearch/authentication"
				},
				new NavigationRenderNode { Kind = NavigationRenderNodeKind.Separator, IsTopLevel = true, NavigationTitle = "", Url = "" },
				new NavigationRenderNode
				{
					Kind = NavigationRenderNodeKind.Node,
					IsTopLevel = true,
					NavigationTitle = "search",
					Url = "/api/doc/elasticsearch/group/endpoint-search",
					Id = "search",
					ShowToggle = true
				}
			],
			ContentHash = "api-intro-separator",
			NavigationPreviewEnabled = true
		};

		var html = await _TocTree.Create(model).RenderAsync(cancellationToken: TestContext.Current.CancellationToken);

		html.Should().Contain("Api Overview");
		html.Should().Contain("Authentication");
		html.Should().Contain("search");
		html.Should().Contain("nav-v2-separator");
		CountSeparators(html).Should().Be(1);

		var overview = html.IndexOf("Api Overview", StringComparison.Ordinal);
		var auth = html.IndexOf("Authentication", StringComparison.Ordinal);
		var separator = html.IndexOf("nav-v2-separator", StringComparison.Ordinal);
		var search = html.IndexOf(">search<", StringComparison.Ordinal);
		overview.Should().BeLessThan(auth);
		auth.Should().BeLessThan(separator);
		separator.Should().BeLessThan(search);
	}

	[Fact]
	public async Task RootIndex_WithoutTreeSeparator_KeepsAutomaticDivider()
	{
		var model = new NavigationRenderModel
		{
			IsUsingNavigationDropdown = false,
			CurrentTopLevelNavigationTitle = "Docs",
			CurrentTopLevelUrl = "/",
			DropdownItems = [],
			BackLinks = [],
			RootIndex = new NavigationRenderNode
			{
				Kind = NavigationRenderNodeKind.Leaf,
				IsTopLevel = true,
				NavigationTitle = "Home",
				Url = "/"
			},
			Tree =
			[
				new NavigationRenderNode
				{
					Kind = NavigationRenderNodeKind.Leaf,
					IsTopLevel = true,
					NavigationTitle = "Setup",
					Url = "/setup"
				}
			],
			ContentHash = "root-index-divider",
			NavigationPreviewEnabled = true
		};

		var html = await _TocTree.Create(model).RenderAsync(cancellationToken: TestContext.Current.CancellationToken);

		html.Should().Contain("Home");
		html.Should().Contain("Setup");
		html.Should().Contain("nav-v2-separator");
		CountSeparators(html).Should().Be(1);
	}

	[Fact]
	public async Task LegacyHeadingRow_RendersTitleWithoutAnEmptyLink()
	{
		var model = new NavigationRenderModel
		{
			IsUsingNavigationDropdown = false,
			CurrentTopLevelNavigationTitle = "API",
			CurrentTopLevelUrl = "/api/doc/es/",
			DropdownItems = [],
			BackLinks = [],
			Tree =
			[
				new NavigationRenderNode
				{
					Kind = NavigationRenderNodeKind.Heading,
					IsTopLevel = true,
					NavigationTitle = "Search & Document APIs",
					Url = "",
					Id = "search-docs",
					ShowToggle = true,
					NavigationItems =
					[
						new NavigationRenderNode
						{
							Kind = NavigationRenderNodeKind.Leaf,
							IsTopLevel = false,
							NavigationTitle = "Run a search",
							Url = "/api/doc/es/operation/operation-search"
						}
					]
				}
			],
			ContentHash = "legacy-heading",
			NavigationPreviewEnabled = false
		};

		var html = await _TocTree.Create(model).RenderAsync(cancellationToken: TestContext.Current.CancellationToken);

		html.Should().Contain("Search &amp; Document APIs");
		html.Should().NotContain("href=\"\"");
		html.Should().Contain("href=\"/api/doc/es/operation/operation-search\"");
		var titleAt = html.IndexOf("Search &amp; Document APIs", StringComparison.Ordinal);
		html[..titleAt].Should().NotContain("<a ");
	}

	private static int CountSeparators(string html)
	{
		var count = 0;
		var index = 0;
		while ((index = html.IndexOf("nav-v2-separator", index, StringComparison.Ordinal)) >= 0)
		{
			count++;
			index += "nav-v2-separator".Length;
		}

		return count;
	}
}
