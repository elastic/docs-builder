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

		html.Should().MatchHtmlSnapshot(DocsHtml.Body());
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

		html.Should().MatchHtmlSnapshot(DocsHtml.Body());
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

		html.Should().MatchHtmlSnapshot(DocsHtml.Body());
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

		html.Should().MatchHtmlSnapshot(DocsHtml.Body());
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

		html.Should().MatchHtmlSnapshot(DocsHtml.Body());
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

		html.Should().MatchHtmlSnapshot(DocsHtml.Body());
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

		html.Should().MatchHtmlSnapshot(DocsHtml.Body());
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

		html.Should().MatchHtmlSnapshot(DocsHtml.Body());
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

		html.Should().MatchHtmlSnapshot(DocsHtml.Body());
	}
}
