// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Site.Components.Navigation.NavSide;
using Elastic.Documentation.Site.Navigation;
using RazorSlices;

namespace Elastic.Documentation.Navigation.Tests.DesignSystem;

public class NavSideRenderingTests
{
	private static ValueTask<string> RenderAsync(NavigationRenderModel navigation) =>
		_NavSide.Create(new NavSideModel { Navigation = navigation }).RenderAsync(
			cancellationToken: TestContext.Current!.Execution.CancellationToken
		);

	[Test]
	public async Task IslandSidebar_ShowsBackLinksAsPillsAndTheSectionTitleAsAHeading()
	{
		var html = await RenderAsync(NavSideExamples.Navigation);

		html.Should().MatchHtmlSnapshot(DocsHtml.Body());
	}

	[Test]
	public async Task SectionSidebar_WithoutBackLinks_KeepsTheQuietLabel()
	{
		var html = await RenderAsync(NavSideExamples.Navigation with { BackLinks = [] });

		html.Should().MatchHtmlSnapshot(DocsHtml.Body());
	}

	[Test]
	public async Task Tree_MarksAbsoluteLinksAsExternal()
	{
		var external = new NavigationRenderNode
		{
			Kind = NavigationRenderNodeKind.Leaf,
			IsTopLevel = false,
			NavigationTitle = "Try the Cloud console",
			Url = "https://cloud.elastic.co"
		};
		var html = await RenderAsync(NavSideExamples.Navigation with { Tree = [external] });

		html.Should().MatchHtmlSnapshot(DocsHtml.Body());
	}

	[Test]
	public async Task Tree_DeprecatedOperation_DimsTheRowAndNamesItForScreenReaders()
	{
		var html = await RenderAsync(NavSideExamples.Navigation with { Tree = [Operation(deprecated: true)] });

		html.Should().Contain("class=\"nav-tree__link nav-tree__link--deprecated\"");
		var text = html.IndexOf("class=\"nav-tree__text\"", StringComparison.Ordinal);
		var hidden = html.IndexOf("<span class=\"sr-only\">Deprecated</span>", StringComparison.Ordinal);
		var method = html.IndexOf("method method--sm", StringComparison.Ordinal);
		hidden.Should().BeGreaterThan(text);
		hidden.Should().BeLessThan(method);
		html.Should().Contain("class=\"badge badge--info method method--sm\"");
	}

	[Test]
	public async Task Tree_OperationNotDeprecated_HasNoDeprecatedModifier()
	{
		var html = await RenderAsync(NavSideExamples.Navigation with { Tree = [Operation(deprecated: false)] });

		html.Should().Contain("class=\"nav-tree__link\"");
		html.Should().NotContain("deprecated").And.NotContain("Deprecated");
	}

	private static NavigationRenderNode Operation(bool deprecated) =>
		new()
		{
			Kind = NavigationRenderNodeKind.Leaf,
			IsTopLevel = false,
			NavigationTitle = "Get document source",
			Url = "/api/doc/elasticsearch/operation/operation-get-source",
			HttpMethod = "get",
			Deprecated = deprecated
		};
}
