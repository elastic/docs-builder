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
			ContentHash = "test"
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
			ContentHash = "folder"
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
	public async Task IslandStub_UsesTheSameChevronAsFolders()
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
			ContentHash = "island-stub"
		};

		var html = await _TocTree.Create(model).RenderAsync(cancellationToken: TestContext.Current.CancellationToken);

		html.Should().Contain("href=\"#icon-chevron-down\"");
		html.Should().NotContain("icon-chevron-double-down");
	}

	[Fact]
	public async Task IslandOverview_RendersAsFolderWithoutSeparator()
	{
		var model = new NavigationRenderModel
		{
			IsUsingNavigationDropdown = false,
			CurrentTopLevelNavigationTitle = "Reference",
			CurrentTopLevelUrl = "/docs/reference",
			DropdownItems = [],
			BackLinks = [new IslandBackLink("Reference", "/docs/reference")],
			Tree =
			[
				new NavigationRenderNode
				{
					Kind = NavigationRenderNodeKind.Node,
					IsTopLevel = true,
					NavigationTitle = "Elasticsearch",
					Url = "/docs/reference/elasticsearch",
					Id = "elasticsearch",
					ShowToggle = true,
					NavigationItems =
					[
						new NavigationRenderNode
						{
							Kind = NavigationRenderNodeKind.Leaf,
							IsTopLevel = false,
							NavigationTitle = "REST APIs",
							Url = "/docs/reference/elasticsearch/rest-apis"
						}
					]
				}
			],
			ContentHash = "island-overview"
		};

		var html = await _TocTree.Create(model).RenderAsync(cancellationToken: TestContext.Current.CancellationToken);

		html.Should().Contain("pages-nav-v2__back");
		html.Should().Contain("Elasticsearch");
		html.Should().Contain("REST APIs");
		html.Should().Contain("id=\"elasticsearch\"");
		html.Should().NotContain("nav-v2-separator");
	}
}
