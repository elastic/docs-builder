// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Text.RegularExpressions;
using AwesomeAssertions;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Operations;
using Elastic.Documentation.Site.FileProviders;

namespace Elastic.ApiExplorer.Tests;

[ClassDataSource<ApiExplorerFixture>(Shared = SharedType.PerClass)]
public partial class OperationHeadingLevelTests(ApiExplorerFixture fixture)
{
	[Test]
	public async Task Render_SearchOperation_UsesOneH1ThenH2SectionsAndH3Nested()
	{
		var nav = fixture.Walk().OfType<OperationNavigationItem>().First(n => n.Model.Operation.OperationId == "search");
		var html = await RenderAsync(nav);

		HeadingOpenTags().Matches(html).Select(match => match.Groups[1].Value).Should().Equal("h1", "h2", "h2", "h2", "h2", "h2", "h3");
		html.Should().Contain("<h1>Run a search</h1>");
		html.Should().Contain("<h2 class=\"section-header api-param-section-header\" id=\"prerequisites\" data-section=\"prerequisites\">");
		html.Should().Contain("<h2 class=\"section-header\" id=\"parameters\" data-section=\"parameters\">");
		html.Should().Contain("<h2 class=\"section-header api-param-section-header\" id=\"query-params\" data-section=\"query-params\">");
		html.Should().Contain("<h2 class=\"section-header api-param-section-header\" id=\"request-body\" data-section=\"request-body\">");
		html.Should().Contain("<h2 class=\"section-header responses-header\" id=\"responses\" data-section=\"responses\">");
		html.Should().Contain("<h3>Response Headers</h3>");
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
			await nav.Model.RenderAsync(stream, renderContext, null, TestContext.Current!.Execution.CancellationToken);

		return fs.File.ReadAllText("/out.html");
	}

	[GeneratedRegex("<(h[1-6])\\b")]
	private static partial Regex HeadingOpenTags();
}
