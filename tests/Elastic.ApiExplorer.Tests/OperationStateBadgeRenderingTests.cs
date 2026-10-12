// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Text.Json.Nodes;
using AwesomeAssertions;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Operations;
using Elastic.Documentation.Site.FileProviders;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Tests;

[ClassDataSource<ApiExplorerFixture>(Shared = SharedType.PerClass)]
public class OperationStateBadgeRenderingTests(ApiExplorerFixture fixture)
{
	[Test]
	public async Task Render_OperationWithXState_ShowsRawStateInsideTitle()
	{
		var nav = fixture.Walk().OfType<OperationNavigationItem>().First(n => n.Model.Operation.OperationId == "search");
		var html = await RenderAsync(nav);

		var title = Slice(html, "<h1>", "</h1>");
		title.Should().Contain("Run a search");
		title.Should().Contain("class=\"api-state-badge\">Generally available; Added in 7.7.0</span>");
		title.Should().NotContain("7.7+");
		title.Should().NotContain("applies-to-popover");
	}

	[Test]
	public async Task Render_OperationWithoutXState_OmitsStateBadge()
	{
		var nav = fixture.Walk().OfType<OperationNavigationItem>().First(n => n.Model.Operation.OperationId == "docs-get");
		var html = await RenderAsync(nav);

		html.Should().Contain("Get a document by id");
		html.Should().NotContain("api-state-badge");
	}

	[Test]
	[Arguments("\"  Technical preview  \"", "Technical preview")]
	[Arguments("\"   \"", null)]
	[Arguments("true", null)]
	[Arguments("{\"a\":1}", null)]
	public void GetState_ExtensionShapes_ReturnsTrimmedStringOrNull(string json, string? expected)
	{
		var operation = new OpenApiOperation
		{
			Extensions = new Dictionary<string, IOpenApiExtension> { ["x-state"] = new JsonNodeExtension(JsonNode.Parse(json)!) }
		};

		OpenApiExtensionReader.GetState(operation).Should().Be(expected);
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

	private static string Slice(string html, string startMarker, string endMarker)
	{
		var start = html.IndexOf(startMarker, StringComparison.Ordinal);
		start.Should().BeGreaterThanOrEqualTo(0);
		var end = html.IndexOf(endMarker, start, StringComparison.Ordinal);
		end.Should().BeGreaterThan(start);
		return html[start..end];
	}
}
