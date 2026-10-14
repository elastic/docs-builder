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
using Microsoft.OpenApi;
using RazorSlices;
using static Elastic.ApiExplorer.Tests.TestSpecs;

namespace Elastic.ApiExplorer.Tests;

/// <summary>A request body that declares several content types lists each one, leading with the one that has a schema.</summary>
[ClassDataSource<ApiExplorerFixture>(Shared = SharedType.PerClass)]
public class RequestContentTypesTests(ApiExplorerFixture fixture)
{
	private const string Spec =
		"""
		{
		  "openapi": "3.0.3",
		  "info": { "title": "Fleet", "version": "1" },
		  "paths": {
		    "/api/fleet/epm/packages": {
		      "post": {
		        "operationId": "post-upload",
		        "summary": "Install a package by upload",
		        "tags": ["fleet"],
		        "requestBody": {
		          "content": {
		            "application/gzip": {
		              "examples": { "upload": { "value": "<binary package archive>" } }
		            },
		            "application/gzip; application/zip": {
		              "schema": { "type": "string", "format": "binary" }
		            }
		          }
		        },
		        "responses": { "200": { "description": "OK" } }
		      }
		    }
		  }
		}
		""";

	[Test]
	public async Task Create_SchemalessFirstContentType_LeadsWithTheEntryThatHasASchema()
	{
		var (page, _, _) = await Build();

		page.RequestContents.Select(c => c.ContentType).Should().Equal("application/gzip; application/zip", "application/gzip");
		page.RequestContents[0].Type!.Text.Should().Contain("binary");
		page.RequestContents[1].Type.Should().BeNull();
	}

	[Test]
	public async Task Write_TwoContentTypes_ListsBothContentTypes()
	{
		var (page, operation, context) = await Build();

		var markdown = OperationCommonMark.Write(operation.Model, page, context);

		markdown.Should().Contain("## Request body");
		markdown.Should().Contain("`application/gzip; application/zip`");
		markdown.Should().Contain("`application/gzip`");
	}

	[Test]
	public async Task Render_TwoContentTypes_BodyHoldsTheLabelledPickerAndHeadingStaysPlain()
	{
		var (page, _, _) = await Build();
		var contents = MediaContentsModel.ForRequest(page.RequestContents);
		var token = TestContext.Current!.Execution.CancellationToken;

		var heading = await _ParamSectionHeader.Create(new ParamSectionHeader("Request body", "request-body", [])).RenderAsync(
			cancellationToken: token
		);
		var body = await _MediaContents.Create(contents).RenderAsync(cancellationToken: token);

		heading.Should().Contain("aria-label=\"Request body\"", "the heading's name is its title, not the controls in it");
		heading.Should().NotContain("content-type-tag", "the heading is a plain disclosure");
		body.Should().Contain("<label for=\"request-body-content-select\">Content type</label>");
		body.Should().Contain("<option value=\"request-body-content-0\" selected=\"selected\">application/gzip; application/zip</option>");
		body.Should().Contain("<option value=\"request-body-content-1\">application/gzip</option>");
		body.Should().Contain("aria-controls=\"request-body-content\"");
		body.Should().Contain("role=\"group\" aria-label=\"Request body content types\"");
		body.Should().Contain("id=\"request-body-content-1\" hidden=\"until-found\"");
		body.Should().Contain("No schema is declared for this content type.");
	}

	private async Task<(OperationPageModel Page, OperationNavigationItem Operation, ApiRenderContext Context)> Build()
	{
		var document = await LoadSpecAsync(Spec);
		var navigation = new OpenApiGenerator(
			NullLoggerFactory.Instance,
			fixture.Context,
			PassthroughMarkdownRenderer.Instance
		).CreateNavigation("fleet-wiring", document);
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
