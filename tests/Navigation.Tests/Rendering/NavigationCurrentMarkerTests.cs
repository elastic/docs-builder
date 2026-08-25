// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Site.Navigation;
using RazorSlices;

namespace Elastic.Documentation.Navigation.Tests.Rendering;

public class NavigationCurrentMarkerTests
{
	[Fact]
	public void Apply_MatchingSidebarLink_AddsCurrent()
	{
		const string html = """
		                    <a href="/getting-started/installation" class="sidebar-link nav-link nav-v2-link">
		                    	<span>Installation</span>
		                    </a>
		                    """;

		var marked = NavigationCurrentMarker.Apply(html, "/getting-started/installation");

		marked.Should().Contain("class=\"sidebar-link nav-link nav-v2-link current\"");
	}

	[Fact]
	public void Apply_TrailingSlash_StillMatches()
	{
		const string html = """<a href="/getting-started/" class="sidebar-link nav-folder-link nav-v2-link">Started</a>""";

		var marked = NavigationCurrentMarker.Apply(html, "/getting-started");

		marked.Should().Contain("nav-v2-link current\"");
	}

	[Fact]
	public void Apply_PrefixUrl_DoesNotMatch()
	{
		const string html = """
		                    <a href="/getting-started" class="sidebar-link nav-folder-link nav-v2-link">Started</a>
		                    <a href="/getting-started/installation" class="sidebar-link nav-link nav-v2-link">Install</a>
		                    """;

		var marked = NavigationCurrentMarker.Apply(html, "/getting-started/installation");

		marked.Should().Contain("href=\"/getting-started/installation\" class=\"sidebar-link nav-link nav-v2-link current\"");
		marked.Should().Contain("href=\"/getting-started\" class=\"sidebar-link nav-folder-link nav-v2-link\">");
		marked.Should().NotContain("href=\"/getting-started\" class=\"sidebar-link nav-folder-link nav-v2-link current\"");
	}

	[Fact]
	public void Apply_AlreadyCurrent_IsIdempotent()
	{
		const string html = """<a href="/guide" class="sidebar-link nav-link nav-v2-link current">Guide</a>""";

		var marked = NavigationCurrentMarker.Apply(html, "/guide");

		marked.Should().Be(html);
	}

	[Fact]
	public void Apply_RootPath_MatchesIndex()
	{
		const string html = """<a href="/" class="sidebar-link nav-link nav-v2-link">docs-builder</a>""";

		var marked = NavigationCurrentMarker.Apply(html, "/");

		marked.Should().Contain("nav-v2-link current\"");
	}

	[Fact]
	public void Apply_NonSidebarAnchor_IsIgnored()
	{
		const string html = """<a href="/getting-started" class="pages-nav-v2__back">Back</a>""";

		var marked = NavigationCurrentMarker.Apply(html, "/getting-started");

		marked.Should().Be(html);
	}

	[Fact]
	public async Task TocTree_StampedHtml_MarksTheCurrentLeaf()
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
				NavigationTitle = "docs-builder",
				Url = "/"
			},
			Tree =
			[
				new NavigationRenderNode
				{
					Kind = NavigationRenderNodeKind.Leaf,
					IsTopLevel = true,
					NavigationTitle = "Installation",
					Url = "/getting-started/installation"
				}
			],
			ContentHash = "current-leaf",
			NavigationPreviewEnabled = true
		};

		var html = await _TocTree.Create(model).RenderAsync(cancellationToken: TestContext.Current.CancellationToken);
		var marked = NavigationCurrentMarker.Apply(html, "/getting-started/installation");

		marked.Should().Contain("href=\"/getting-started/installation\" class=\"sidebar-link nav-link nav-v2-link current\"");
		marked.Should().Contain("href=\"/\" class=\"sidebar-link nav-link nav-v2-link\"");
		marked.Should().NotContain("href=\"/\" class=\"sidebar-link nav-link nav-v2-link current\"");
	}
}
