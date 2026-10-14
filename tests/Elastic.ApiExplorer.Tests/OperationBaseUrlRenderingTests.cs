// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO;
using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Operations;
using Elastic.Documentation.Site.FileProviders;

namespace Elastic.ApiExplorer.Tests;

[ClassDataSource<ApiExplorerFixture>(Shared = SharedType.PerClass)]
public class OperationBaseUrlRenderingTests(ApiExplorerFixture fixture)
{
	[Test]
	public async Task Render_Operation_PutsBaseUrlInTheRailAboveTheExamples()
	{
		var nav = fixture.Walk().OfType<OperationNavigationItem>().First(n => n.Model.Operation.OperationId == "docs-get-source");
		var html = await RenderAsync(nav);

		var rail = html.IndexOf("class=\"api-page-rail\"", StringComparison.Ordinal);
		rail.Should().BeGreaterThanOrEqualTo(0);
		var railBaseUrl = html.IndexOf("api-rail-facts api-rail-facts--rail", rail, StringComparison.Ordinal);
		railBaseUrl.Should().BeGreaterThan(rail);
		html.IndexOf("<dt>Base URL", railBaseUrl, StringComparison.Ordinal).Should().BeGreaterThan(railBaseUrl);
		html.IndexOf("api-server-field", railBaseUrl, StringComparison.Ordinal).Should().BeGreaterThan(railBaseUrl);
	}

	[Test]
	public async Task Render_Operation_KeepsAnInlineBaseUrlForNarrowScreens()
	{
		var nav = fixture.Walk().OfType<OperationNavigationItem>().First(n => n.Model.Operation.OperationId == "docs-get-source");
		var html = await RenderAsync(nav);

		html.Should().Contain("api-rail-facts api-rail-facts--inline");
		html.Should().NotContain("server-info");
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
}
