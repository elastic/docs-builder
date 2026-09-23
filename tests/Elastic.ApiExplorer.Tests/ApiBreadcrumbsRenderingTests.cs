// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO;
using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Operations;
using Elastic.Documentation;
using Elastic.Documentation.Navigation;
using Elastic.Documentation.Site.FileProviders;
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

public class ApiBreadcrumbLayoutRenderingTests(ApiExplorerFixture fixture) : IClassFixture<ApiExplorerFixture>
{
	[Fact]
	public async Task OperationPage_BreadcrumbsRenderOutsideMarkdownContent()
	{
		var nav = fixture.Walk().OfType<OperationNavigationItem>().First(n => n.Model.Operation.OperationId == "docs-get-source");
		var html = await RenderAsync(nav);

		var crumbs = html.IndexOf("id=\"breadcrumbs\"", StringComparison.Ordinal);
		var toolbar = html.IndexOf("class=\"api-page-toolbar\"", StringComparison.Ordinal);
		var article = html.IndexOf("<article id=\"markdown-content\"", StringComparison.Ordinal);
		var hamburger = html.IndexOf("id=\"pages-nav-hamburger\"", StringComparison.Ordinal);
		crumbs.Should().BeGreaterThanOrEqualTo(0);
		toolbar.Should().BeGreaterThanOrEqualTo(0).And.BeLessThan(crumbs);
		hamburger.Should().BeGreaterThanOrEqualTo(0).And.BeLessThan(article);
		article.Should().BeGreaterThan(crumbs);

		var articleEnd = html.IndexOf("</article>", article, StringComparison.Ordinal);
		html[article..articleEnd].Should().NotContain("id=\"breadcrumbs\"");

		var container = html.IndexOf("id=\"content-container\"", StringComparison.Ordinal);
		container.Should().BeGreaterThanOrEqualTo(0).And.BeLessThan(toolbar);
	}

	private async Task<string> RenderAsync(OperationNavigationItem nav)
	{
		var renderContext = new ApiRenderContext(
			fixture.Context,
			fixture.Document,
			new StaticFileContentHashProvider(new EmbeddedOrPhysicalFileProvider(fixture.Context))
		)
		{ NavigationHtml = string.Empty, CurrentNavigation = nav, MarkdownRenderer = PassthroughMarkdownRenderer.Instance };

		var fs = new MockFileSystem();
		await using (var stream = fs.FileStream.New("/out.html", FileMode.Create, FileAccess.Write))
			await nav.Model.RenderAsync(stream, renderContext, null, TestContext.Current.CancellationToken);

		return fs.File.ReadAllText("/out.html");
	}
}
