// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Site.Navigation;
using RazorSlices;

namespace Elastic.Documentation.Navigation.Tests.Rendering;

public class TocTreeRenderingTests
{
	[Test]
	public async Task TocTree_RendersTheDesignSystemSidebar()
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

		var html = await _TocTree.Create(model).RenderAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);

		html.Should().Contain("class=\"nav-side\"");
		html.Should().Contain("nav-side__scroll");
		html.Should().Contain("link-menu", "a build with several sections gets a section menu");
		html.Should().Contain("nav-side__back");
		html.Should().Contain("nav-tree__link");
		html.Should().Contain("Guides");
		html.Should().NotContain("pages-nav-v2");
		html.Should().NotContain("navigation-search");
		html.Should().NotContain("hx-preserve");
	}

	[Test]
	public async Task FolderRow_KeepsThePageLinkAndTheToggleSeparate()
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

		var html = await _TocTree.Create(model).RenderAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);

		var linkStart = html.IndexOf("href=\"/docs/contribute\"", StringComparison.Ordinal);
		linkStart.Should().BeGreaterThanOrEqualTo(0);
		var linkEnd = html.IndexOf("</a>", linkStart, StringComparison.Ordinal);
		html[linkStart..linkEnd].Should().NotContain("chevron", "the page link and the expand control are separate targets");
		html.Should().Contain("<label class=\"nav-tree__chevron\" for=\"contribute\"");
		html.Should().Contain("id=\"contribute\"");
	}

	[Test]
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

		var html = await _TocTree.Create(model).RenderAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);

		html.Should().Contain("<label class=\"nav-tree__row nav-tree__row--heading\" for=\"search-docs\"");
		html.Should().Contain("Search &amp; Document APIs");
		html.Should().NotContain("href=\"/api/doc/es/\"");
		html.Should().Contain("href=\"/api/doc/es/operation/operation-search\"");
	}

	[Test]
	public async Task IslandStub_IsALinkWithoutAnExpandControl()
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

		var html = await _TocTree.Create(model).RenderAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);

		html.Should().Contain("nav-tree__link--island");
		html.Should().NotContain("nav-tree__chevron", "an island opens its own tree, so it has no expand control");
	}

	[Test]
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

		var html = await _TocTree.Create(model).RenderAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);

		html.Should().Contain("nav-side__back");
		html.Should().Contain("data-nav-heading=\"Elasticsearch\"");
		html.Should().Contain("nav-side__heading");
		html.Should().Contain("Overview");
		html.Should().Contain("REST APIs");
		html.Should().Contain("href=\"/docs/reference/elasticsearch\"");
		html.Should().NotContain("id=\"elasticsearch\"");
		html.Should().NotContain("nav-tree__separator");
		html.Should().NotContain("hx-preserve");
	}

	[Test]
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
				new NavigationSelectOption("latest", "/api/doc/elasticsearch/", Selected: false),
				new NavigationSelectOption("v9", "/api/doc/elasticsearch/v9/", Selected: true),
				new NavigationSelectOption("v8", "/api/doc/elasticsearch/v8/", Selected: false)
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

		var html = await _TocTree.Create(model).RenderAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);

		html.Should().NotContain("api-version-switcher");
		html.Should().NotContain("link-menu", "the version control comes from the layout, not from the tree");
	}

	[Test]
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

		var html = await _TocTree.Create(model).RenderAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);

		html.Should().Contain("Api Overview");
		html.Should().Contain("Authentication");
		html.Should().Contain("search");
		CountSeparators(html).Should().Be(1);

		var overview = html.IndexOf("Api Overview", StringComparison.Ordinal);
		var auth = html.IndexOf("Authentication", StringComparison.Ordinal);
		var separator = html.IndexOf("nav-tree__separator", StringComparison.Ordinal);
		var search = html.IndexOf(">search<", StringComparison.Ordinal);
		overview.Should().BeLessThan(auth);
		auth.Should().BeLessThan(separator);
		separator.Should().BeLessThan(search);
	}

	[Test]
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

		var html = await _TocTree.Create(model).RenderAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);

		html.Should().Contain("Home");
		html.Should().Contain("Setup");
		CountSeparators(html).Should().Be(1);
	}

	[Test]
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

		var html = await _TocTree.Create(model).RenderAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);

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
		while ((index = html.IndexOf("nav-tree__separator\"", index, StringComparison.Ordinal)) >= 0)
		{
			count++;
			index += "nav-tree__separator".Length;
		}

		return count;
	}
}
