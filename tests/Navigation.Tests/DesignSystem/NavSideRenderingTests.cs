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

		html.Should().Contain("class=\"nav-side__back\"");
		html.Should().Contain("nav-side__heading nav-side__heading--island");
		html.Should().Contain("nav-tree__link--island");
	}

	[Test]
	public async Task SectionSidebar_WithoutBackLinks_KeepsTheQuietLabel()
	{
		var html = await RenderAsync(NavSideExamples.Navigation with { BackLinks = [] });

		html.Should().Contain("class=\"nav-side__heading\"");
		html.Should().NotContain("nav-side__heading--island");
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

		html.Should().Contain("nav-tree__link--external").And.Contain("target=\"_blank\"").And.Contain("rel=\"noopener\"");
	}
}
