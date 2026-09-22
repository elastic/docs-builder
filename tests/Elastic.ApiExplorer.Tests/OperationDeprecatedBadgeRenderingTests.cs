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
public class OperationDeprecatedBadgeRenderingTests(ApiExplorerFixture fixture)
{
	[Test]
	public async Task Render_DeprecatedOperation_ShowsBadgeOnTitleNotOnMethod()
	{
		var nav = fixture.Walk().OfType<OperationNavigationItem>().First(n => n.Model.Operation.OperationId == "docs-get-source");
		var html = await RenderAsync(nav);

		html.Should().Contain("Get the source of a document by id");
		html.Should().Contain("class=\"api-title-badges\"");

		var titleBadges = Slice(html, "class=\"api-title-badges\"", "id=\"paths\"");
		titleBadges.Should().Contain("deprecated-badge");

		var listing = Slice(html, "id=\"paths\"", "</ul>");
		listing.Should().Contain("api-url-list-item");
		listing.Should().NotContain("deprecated-badge");
		listing.Should().NotContain("deprecated-path");
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
			await nav.Model.RenderAsync(stream, renderContext, TestContext.Current!.Execution.CancellationToken);

		return fs.File.ReadAllText("/out.html");
	}

	private static string Slice(string html, string startMarker, string endMarker)
	{
		var start = html.IndexOf(startMarker, StringComparison.Ordinal);
		start.Should().BeGreaterThanOrEqualTo(0);
		var end = html.IndexOf(endMarker, start, StringComparison.Ordinal);
		end.Should().BeGreaterThan(start);
		return html[start..end];
	}
}
