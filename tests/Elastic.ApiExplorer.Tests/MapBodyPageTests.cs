// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Operations;
using Elastic.Documentation.Site.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using static Elastic.ApiExplorer.Tests.TestSpecs;

namespace Elastic.ApiExplorer.Tests;

/// <summary>A body that is a map names its key row the way the tree does, wherever the page shows the body's fields.</summary>
[ClassDataSource<ApiExplorerFixture>(Shared = SharedType.PerClass)]
public class MapBodyPageTests(ApiExplorerFixture fixture)
{
	private const string Spec =
		"""
		{
		  "openapi": "3.0.3",
		  "info": { "title": "Maps", "version": "1" },
		  "paths": {
		    "/inputs": {
		      "post": {
		        "operationId": "post-inputs",
		        "summary": "Set inputs",
		        "tags": ["maps"],
		        "requestBody": {
		          "content": { "application/json": { "schema": { "$ref": "#/components/schemas/Inputs" } } }
		        },
		        "responses": { "200": { "description": "OK" } }
		      }
		    }
		  },
		  "components": {
		    "schemas": {
		      "Input": { "type": "object", "properties": { "enabled": { "type": "boolean" } } },
		      "Inputs": { "type": "object", "additionalProperties": { "$ref": "#/components/schemas/Input" } }
		    }
		  }
		}
		""";

	[Test]
	public async Task Create_MapBody_SummarisesTheRequestByTheKeyRowLabel()
	{
		var page = await Build();

		page.RequestSummaryStyle.Should().Be(SummaryStyle.Fields);
		page.RequestPropertyNames.Should().Equal("property name*");
	}

	private async Task<OperationPageModel> Build()
	{
		var document = await LoadSpecAsync(Spec);
		var navigation = new OpenApiGenerator(
			NullLoggerFactory.Instance,
			fixture.Context,
			PassthroughMarkdownRenderer.Instance
		).CreateNavigation("maps-wiring", document);
		var operation = ApiExplorerFixture.Walk(navigation).OfType<OperationNavigationItem>().Single();
		var context = new ApiRenderContext(
			fixture.Context,
			document,
			new StaticFileContentHashProvider(new EmbeddedOrPhysicalFileProvider(fixture.Context))
		)
		{ NavigationHtml = string.Empty, CurrentNavigation = operation, MarkdownRenderer = PassthroughMarkdownRenderer.Instance };
		return OperationPageModel.Create(operation.Model, context);
	}
}
