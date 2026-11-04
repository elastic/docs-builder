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
public partial class EndpointPathLinkRenderingTests(ApiExplorerFixture fixture)
{
	[Test]
	public void AnchorFor_UndocumentedParameter_ReturnsNull()
	{
		var row = new EndpointRow("/{index}/{id}", "get", [], []);
		var model = new EndpointPathModel(row, row.Route, new HashSet<string> { "index" });

		model.AnchorFor(new EndpointSegment("{index}", false)).Should().Be("path-index");
		model.AnchorFor(new EndpointSegment("{id}", false)).Should().BeNull();
		new EndpointPathModel(row, row.Route).AnchorFor(new EndpointSegment("{index}", false)).Should().BeNull();
	}

	[Test]
	public async Task Render_PathPlaceholders_LinkToDocumentedPathParameters()
	{
		var nav = fixture.Walk().OfType<OperationNavigationItem>().First(n => n.Model.Operation.OperationId == "docs-get-source");
		var html = await RenderAsync(nav);

		var listing = Slice(html, "id=\"paths\"", "</ul>");
		var targets = PlaceholderLink().Matches(listing).Select(static m => m.Groups[1].Value).ToArray();

		targets.Should().NotBeEmpty();
		foreach (var target in targets)
			html.Should().Contain($"id=\"{target}\"");
	}

	[GeneratedRegex("""<a class="api-url-param" href="#([^"]+)">\{[^}]+\}</a>""")]
	private static partial Regex PlaceholderLink();

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

	private static string Slice(string html, string startMarker, string endMarker)
	{
		var start = html.IndexOf(startMarker, StringComparison.Ordinal);
		start.Should().BeGreaterThanOrEqualTo(0);
		var end = html.IndexOf(endMarker, start, StringComparison.Ordinal);
		end.Should().BeGreaterThan(start);
		return html[start..end];
	}
}
