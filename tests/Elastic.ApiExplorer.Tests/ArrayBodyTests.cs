// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Operations;
using Elastic.ApiExplorer.Operations._Partials;
using Elastic.Documentation.Navigation;
using Elastic.Documentation.Site.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using RazorSlices;
using static Elastic.ApiExplorer.Tests.TestSpecs;

namespace Elastic.ApiExplorer.Tests;

/// <summary>A body that is an array of objects says so above the item fields, in a request and a response alike.</summary>
[ClassDataSource<ApiExplorerFixture>(Shared = SharedType.PerClass)]
public class ArrayBodyTests(ApiExplorerFixture fixture)
{
	private const string Label = "An array; each item has these fields:";

	private const string Spec =
		"""
		{
		  "openapi": "3.0.3",
		  "info": { "title": "Cat", "version": "1" },
		  "paths": {
		    "/aliases": {
		      "post": {
		        "operationId": "post-aliases",
		        "summary": "Set aliases",
		        "tags": ["cat"],
		        "requestBody": {
		          "content": { "application/json": { "schema": { "type": "array", "items": { "$ref": "#/components/schemas/Alias" } } } }
		        },
		        "responses": {
		          "200": {
		            "description": "OK",
		            "content": { "application/json": { "schema": { "type": "array", "items": { "$ref": "#/components/schemas/Alias" } } } }
		          },
		          "400": {
		            "description": "Bad",
		            "content": { "application/json": { "schema": { "type": "array", "items": { "type": "object" } } } }
		          }
		        }
		      }
		    }
		  },
		  "components": {
		    "schemas": {
		      "Alias": {
		        "type": "object",
		        "properties": { "alias": { "type": "string" }, "index": { "type": "string" } }
		      }
		    }
		  }
		}
		""";

	[Test]
	public async Task Create_ArrayOfObjects_ListsTheItemFieldsInRequestAndResponse()
	{
		var (page, _, _) = await Build();

		var request = page.RequestContents.Single();
		request.IsArray.Should().BeTrue();
		request.Properties!.Items.Select(p => p.Name).Should().Equal("alias", "index");
		var response = page.Responses.Single(r => r.StatusCode == "200").Contents.Single();
		response.IsArray.Should().BeTrue();
		response.Properties!.Items.Select(p => p.Name).Should().Equal("alias", "index");
		page.RequestSummaryStyle.Should().Be(SummaryStyle.ArrayOfFields);
		page.RequestPropertyNames.Should().Equal("alias", "index");
	}

	[Test]
	public async Task Create_ArrayOfFreeFormObjects_KeepsItsTypeLine()
	{
		var (page, _, _) = await Build();

		var content = page.Responses.Single(r => r.StatusCode == "400").Contents.Single();
		content.IsArray.Should().BeFalse();
		content.Properties.Should().BeNull();
	}

	[Test]
	public async Task Render_ArrayResponse_LabelsTheFieldsInPlaceOfTheTypeLine()
	{
		var (page, _, _) = await Build();
		var contents = MediaContentsModel.ForResponse("200", page.Responses.Single(r => r.StatusCode == "200").Contents);

		var html = await _MediaContents.Create(contents).RenderAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);

		html.Should().Contain($"<span class=\"values-label\">{Label}</span>");
		html.Should().NotContain("Response Type:");
		html.IndexOf(Label, StringComparison.Ordinal).Should().BeLessThan(html.IndexOf("id=\"res-200-alias\"", StringComparison.Ordinal));
	}

	[Test]
	public async Task Render_ArrayRequestSummary_WrapsTheFieldNamesInBracketsAndBraces()
	{
		var html = await _ParamSummary.Create(
			new ParamSectionHeader("Request body", "request-body", ["alias"], SummaryStyle.ArrayOfFields)
		).RenderAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);

		html.Should().Contain(">[{ </span>").And.Contain("> }]</span>");
	}

	[Test]
	public async Task Write_ArrayResponse_LabelsTheFieldsInPlaceOfTheTypeLine()
	{
		var (page, operation, context) = await Build();

		var markdown = OperationCommonMark.Write(operation.Model, page, context);

		markdown.Should().Contain(Label).And.Contain("`alias`");
		markdown
			.Split("Type: `object[]`")
			.Should()
			.HaveCount(2, "only the free-form 400 body keeps a type line; the listed ones carry the label");
	}

	private async Task<(OperationPageModel Page, OperationNavigationItem Operation, ApiRenderContext Context)> Build()
	{
		var document = await LoadSpecAsync(Spec);
		var navigation = new OpenApiGenerator(
			NullLoggerFactory.Instance,
			fixture.Context,
			PassthroughMarkdownRenderer.Instance
		).CreateNavigation("cat-wiring", document);
		var operation = ApiExplorerFixture.Walk(navigation).OfType<OperationNavigationItem>().Single();
		var context = new ApiRenderContext(
			fixture.Context,
			document,
			new StaticFileContentHashProvider(new EmbeddedOrPhysicalFileProvider(fixture.Context))
		)
		{ NavigationHtml = string.Empty, CurrentNavigation = operation, MarkdownRenderer = PassthroughMarkdownRenderer.Instance };
		return (OperationPageModel.Create(operation.Model, context), operation, context);
	}
}
