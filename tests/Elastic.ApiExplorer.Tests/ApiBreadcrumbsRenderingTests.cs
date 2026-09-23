// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation;
using Elastic.Documentation.Navigation;
using Elastic.Documentation.Site.Layout;
using FakeItEasy;
using RazorSlices;

namespace Elastic.ApiExplorer.Tests;

public class ApiBreadcrumbsRenderingTests
{
	[Fact]
	public async Task Render_ParentsAreLinks_AndCurrentPageIsAbsent()
	{
		var html = await Render([Crumb("/api/", "APIs"), Crumb("/api/es/", "[cmd]search")], BuildType.Assembler);

		html.Should().Contain("id=\"breadcrumbs\"");
		html.Should().Contain("href=\"/api/\"");
		html.Should().Contain("hx-disable=true");
		html.Should().Contain("href=\"/api/es/\"");
		html.Should().Contain(">APIs<");
		html.Should().Contain(">search<");
		html.Should().NotContain("[cmd]");
		html.Should().NotContain("aria-current");
		html.Should().NotContain("api-breadcrumbs");
	}

	[Fact]
	public async Task Render_IsolatedSingleParent_IsHidden()
	{
		var html = await Render([Crumb("/api/", "APIs")], BuildType.Isolated);

		html.Should().NotContain("id=\"breadcrumbs\"");
	}

	[Fact]
	public async Task Render_AssemblerSingleParent_IsShown()
	{
		var html = await Render([Crumb("/api/", "APIs")], BuildType.Assembler);

		html.Should().Contain("href=\"/api/\"");
	}

	private static async Task<string> Render(IReadOnlyList<INavigationItem> items, BuildType buildType) =>
		await _Breadcrumbs.Create(new BreadcrumbsView(items, buildType)).RenderAsync(
			cancellationToken: TestContext.Current.CancellationToken
		);

	private static INavigationItem Crumb(string url, string title)
	{
		var item = A.Fake<INavigationItem>();
		A.CallTo(() => item.Url).Returns(url);
		A.CallTo(() => item.NavigationTitle).Returns(title);
		return item;
	}
}
