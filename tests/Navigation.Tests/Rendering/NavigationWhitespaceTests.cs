// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Navigation;
using Elastic.Documentation.Site.Navigation;

namespace Navigation.Tests.Rendering;

public class NavigationWhitespaceTests
{
	[Test]
	public void Collapse_Indentation_BecomesOneLineBreakAndKeepsInlineSpaces() =>
		NavigationWhitespace
			.Collapse(
				"<ul>\n\t\t<li>\n\t\t\t<a href=\"/a\" class=\"sidebar-link\"><span>Run a search</span> <span>GET</span></a>\n\t\t</li>\n\t\t<input\n\t\t\tid=\"x\"\n\t\t\ttype=\"checkbox\">\n</ul>"
			)
			.Should()
			.Be(
				"<ul>\n<li>\n<a href=\"/a\" class=\"sidebar-link\"><span>Run a search</span> <span>GET</span></a>\n</li>\n<input\nid=\"x\"\ntype=\"checkbox\">\n</ul>"
			);

	[Test]
	public void Collapse_NonBreakingSpaceBesideALineBreak_IsKept() =>
		NavigationWhitespace.Collapse("<span>a\u00A0\n\t\tb</span>").Should().Be("<span>a\u00A0\nb</span>");

	[Test]
	public void Collapse_LongSpaceRunWithoutALineBreak_IsKeptAsIs()
	{
		var html = $"<span>a{new string(' ', 200_000)}b</span>";

		NavigationWhitespace.Collapse(html).Should().Be(html);
	}

	[Test]
	public async Task Render_ThroughTheWriter_DropsTheTemplateIndentation()
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
					Kind = NavigationRenderNodeKind.Leaf,
					IsTopLevel = true,
					NavigationTitle = "Get started",
					Url = "/docs/get-started"
				}
			],
			ContentHash = "test"
		};

		var result = await ((INavigationHtmlWriter)new UnusedNavigationWriter()).Render(
			model,
			TestContext.Current!.Execution.CancellationToken
		);

		result.Html.Should().NotContain("\n\t");
	}

	private sealed class UnusedNavigationWriter : INavigationHtmlWriter
	{
		public Task<NavigationRenderResult> RenderNavigation(
			IRootNavigationItem<INavigationModel, INavigationItem> currentRootNavigation,
			INavigationItem currentNavigationItem,
			Cancel ctx = default
		) => throw new NotSupportedException();
	}
}
