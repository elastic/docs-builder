// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO;
using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Operations;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Site.FileProviders;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Tests;

[ClassDataSource<ApiExplorerFixture>(Shared = SharedType.PerClass)]
public class OperationBaseUrlRenderingTests(ApiExplorerFixture fixture)
{
	[Test]
	[Arguments("elasticsearch", "ELASTICSEARCH_URL")]
	[Arguments("serverless-elasticsearch", "ELASTICSEARCH_URL")]
	[Arguments("kibana", "KIBANA_URL")]
	[Arguments("logstash", "LOGSTASH_URL")]
	[Arguments("cloud-enterprise", "API_URL")]
	public void Create_WithoutServers_PassesTheProductsHostVariableToHttpExamples(string productId, string expected)
	{
		var nav = fixture.Walk().OfType<OperationNavigationItem>().First();
		var context = fixture.RenderContextFor(nav) with
		{
			Model = new OpenApiDocument(),
			Product = new Product { Id = productId, DisplayName = productId }
		};
		var operation = new ApiOperation(HttpMethod.Get, new OpenApiOperation(), "/_cat", new OpenApiPathItem(), "Help");

		var page = OperationPageModel.Create(operation, context);

		page.Scenarios.Single().CodeSamples.Single(static s => s.IsHttp).Source.Should().Be("###\nGET {{" + expected + "}}/_cat\n");
	}

	[Test]
	public void Create_SeveralRoutesWithoutSpecSamples_SendsOneRequestFromEveryGeneratedCard()
	{
		var nav = fixture.Walk().OfType<OperationNavigationItem>().First();
		var context = fixture.RenderContextFor(nav) with
		{
			Model = new OpenApiDocument(),
			Product = new Product { Id = "elasticsearch", DisplayName = "Elasticsearch" }
		};
		var operation = new OpenApiOperation
		{
			Description =
				"**All methods and paths for this operation:**\n\n<div>\n  <span class=\"operation-verb post\">POST</span>\n  <span class=\"operation-path\">/_search</span>\n</div>\n<div>\n  <span class=\"operation-verb post\">POST</span>\n  <span class=\"operation-path\">/{index}/_search</span>\n</div>\n\n",
			RequestBody = new OpenApiRequestBody
			{
				Content = new Dictionary<string, IOpenApiMediaType>
				{
					["application/json"] = new OpenApiMediaType
					{
						Examples = new Dictionary<string, IOpenApiExample>
						{
							["match"] = new OpenApiExample { Value = new System.Text.Json.Nodes.JsonObject { ["size"] = 1 } }
						}
					}
				}
			}
		};

		var page = OperationPageModel.Create(
			new ApiOperation(HttpMethod.Post, operation, "/{index}/_search", new OpenApiPathItem(), "Search"),
			context
		);

		page
			.Scenarios
			.Single()
			.CodeSamples
			.Select(static s => s.Source.Split('\n')[s.IsHttp ? 1 : 0])
			.Should()
			.Equal("POST /_search", "curl -X POST \"${ELASTICSEARCH_URL}/_search\" \\", "POST {{ELASTICSEARCH_URL}}/_search");
	}

	[Test]
	public async Task Render_Operation_PutsBaseUrlInTheRailAboveTheExamples()
	{
		var nav = fixture.Walk().OfType<OperationNavigationItem>().First(n => n.Model.Operation.OperationId == "docs-get-source");
		var html = await fixture.RenderAsync(nav);

		var rail = html.IndexOf("class=\"api-page-rail\"", StringComparison.Ordinal);
		rail.Should().BeGreaterThanOrEqualTo(0);
		var railBaseUrl = html.IndexOf("api-rail-facts api-rail-facts--rail", rail, StringComparison.Ordinal);
		railBaseUrl.Should().BeGreaterThan(rail);
		html.IndexOf("<dt>Base URL", railBaseUrl, StringComparison.Ordinal).Should().BeGreaterThan(railBaseUrl);
		html.IndexOf("api-server-field", railBaseUrl, StringComparison.Ordinal).Should().BeGreaterThan(railBaseUrl);
	}

	[Test]
	public async Task Render_OperationWithExamples_PutsTheExamplesPanelAfterTheBaseUrl()
	{
		string? html = null;
		foreach (var nav in fixture.Walk().OfType<OperationNavigationItem>())
		{
			var candidate = await fixture.RenderAsync(nav);
			if (!candidate.Contains("id=\"api-examples-panel\"", StringComparison.Ordinal))
				continue;
			html = candidate;
			break;
		}

		html.Should().NotBeNull("the fixture has an operation with examples");
		var rail = html.IndexOf("class=\"api-page-rail\"", StringComparison.Ordinal);
		var baseUrl = html.IndexOf("api-rail-facts--rail", rail, StringComparison.Ordinal);
		baseUrl.Should().BeGreaterThan(rail);
		html.IndexOf("id=\"api-examples-panel\"", baseUrl, StringComparison.Ordinal).Should().BeGreaterThan(baseUrl);
	}

	[Test]
	public async Task Render_Operation_KeepsAnInlineBaseUrlForNarrowScreens()
	{
		var nav = fixture.Walk().OfType<OperationNavigationItem>().First(n => n.Model.Operation.OperationId == "docs-get-source");
		var html = await fixture.RenderAsync(nav);

		html.Should().Contain("api-rail-facts api-rail-facts--inline");
		html.Should().NotContain("server-info");
	}
}
