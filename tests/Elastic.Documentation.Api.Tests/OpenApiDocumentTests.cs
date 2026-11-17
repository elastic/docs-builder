// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Elastic.Documentation.Api.Tests.Fixtures;

namespace Elastic.Documentation.Api.Tests;

public class OpenApiDocumentTests
{
	private static readonly Lazy<ApiWebApplicationFactory> SharedFactory = new(() => new ApiWebApplicationFactory());

	[After(Class)]
	public static void DisposeSharedFactory()
	{
		if (SharedFactory.IsValueCreated)
			SharedFactory.Value.Dispose();
	}

	private static async Task<JsonElement> Document()
	{
		using var client = SharedFactory.Value.CreateClient();
		using var response = await client.GetAsync("/docs/_api/v1/openapi.json", TestContext.Current!.Execution.CancellationToken);
		response.StatusCode.Should().Be(HttpStatusCode.OK);
		return JsonElement.Parse(await response.Content.ReadAsStringAsync(TestContext.Current!.Execution.CancellationToken));
	}

	[Test]
	public async Task Document_DescribesEveryEndpointWithItsStatusCodes()
	{
		var document = await Document();

		var operations = document
			.GetProperty("paths")
			.EnumerateObject()
			.SelectMany(
				path => path
					.Value
					.EnumerateObject()
					.Where(op => op.Name is "get" or "post" or "put" or "delete")
					.Select(
						op =>
							$"{op.Name.ToUpperInvariant()} {path.Name} {string.Join(",", op.Value.GetProperty("responses").EnumerateObject().Select(r => r.Name).Order())}"
					)
			)
			.Order()
			.ToList();

		operations.Should().Equal(
			"DELETE /docs/_api/v1/page-feedback/{feedbackId} 204,500,503",
			"GET /docs/_api/v1/apis 200,500",
			"GET /docs/_api/v1/apis/{api} 200,404,500",
			"GET /docs/_api/v1/apis/{api}/groups 200,400,404,500",
			"GET /docs/_api/v1/apis/{api}/operations 200,400,404,500,503",
			"GET /docs/_api/v1/apis/{api}/operations/{operationId} 200,400,404,500",
			"GET /docs/_api/v1/changes 200,400,500,503",
			"GET /docs/_api/v1/navigation-search 200,400,500,503",
			"GET /docs/_api/v1/search 200,400,500,503",
			"POST /docs/_api/v1/ask-ai/message-feedback 204,400,500,503",
			"POST /docs/_api/v1/ask-ai/stream 200,400,500",
			"PUT /docs/_api/v1/page-feedback/{feedbackId} 204,400,500,503"
		);
	}

	[Test]
	public async Task Document_GivesEveryErrorTheApiErrorSchema()
	{
		var document = await Document();

		var errorSchemas = document
			.GetProperty("paths")
			.EnumerateObject()
			.SelectMany(path => path.Value.EnumerateObject().Where(op => op.Name is "get" or "post" or "put" or "delete"))
			.SelectMany(op => op.Value.GetProperty("responses").EnumerateObject().Where(r => r.Name[0] is '4' or '5'))
			.Select(
				r => r.Value.GetProperty("content").GetProperty("application/json").GetProperty("schema").GetProperty("$ref").GetString()
			)
			.Distinct()
			.ToList();

		errorSchemas.Should().Equal("#/components/schemas/ApiError");
		document
			.GetProperty("components")
			.GetProperty("schemas")
			.GetProperty("ApiError")
			.GetProperty("properties")
			.EnumerateObject()
			.Select(p => p.Name)
			.Should()
			.Equal("errorType", "message");
	}

	[Test]
	public async Task Document_NamesTheApiAndThePublicServer()
	{
		var document = await Document();

		document.GetProperty("info").GetProperty("title").GetString().Should().Be("Elastic Docs API");
		document.GetProperty("info").GetProperty("description").GetString().Should().Contain("feature_search_or_askai_enabled=true");
		document.GetProperty("servers")[0].GetProperty("url").GetString().Should().Be("https://www.elastic.co");
	}
}
