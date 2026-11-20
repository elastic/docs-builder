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
			"GET /apis 200,500",
			"GET /apis/{api} 200,404,500",
			"GET /apis/{api}/groups 200,400,404,500",
			"GET /apis/{api}/operations 200,400,404,500,503",
			"GET /apis/{api}/operations/{operationId} 200,400,404,500",
			"GET /changes 200,400,500,503",
			"GET /navigation-search 200,400,500,503",
			"GET /search 200,400,500,503",
			"POST /ask-ai/message-feedback 204,400,500,503",
			"POST /ask-ai/stream 200,400,500",
			"PUT /page-feedback/{feedbackId} 204,400,500,503"
		);
	}

	[Test]
	public async Task Document_NamesEveryOperationOnce()
	{
		var document = await Document();

		var ids = document
			.GetProperty("paths")
			.EnumerateObject()
			.SelectMany(path => path.Value.EnumerateObject().Where(op => op.Name is "get" or "post" or "put" or "delete"))
			.Select(op => op.Value.TryGetProperty("operationId", out var id) ? id.GetString() : null)
			.ToList();

		// API Explorer turns these into page slugs and the api-operations ids, such as docs-api:search.
		ids.Should().NotContainNulls().And.OnlyHaveUniqueItems().And.HaveCount(11);
		ids.Should().Contain(["search", "get-api-operation", "list-api-operations"]);
	}

	[Test]
	public async Task Document_DescribesEveryErrorAsProblemDetails()
	{
		var document = await Document();

		var errorContent = document
			.GetProperty("paths")
			.EnumerateObject()
			.SelectMany(path => path.Value.EnumerateObject().Where(op => op.Name is "get" or "post" or "put" or "delete"))
			.SelectMany(op => op.Value.GetProperty("responses").EnumerateObject().Where(r => r.Name[0] is '4' or '5'))
			.SelectMany(
				r => r
					.Value
					.GetProperty("content")
					.EnumerateObject()
					.Select(c => $"{c.Name} {c.Value.GetProperty("schema").GetProperty("$ref").GetString()}")
			)
			.Distinct()
			.ToList();

		errorContent.Should().Equal("application/problem+json #/components/schemas/ProblemDetails");
		document
			.GetProperty("components")
			.GetProperty("schemas")
			.GetProperty("ProblemDetails")
			.GetProperty("properties")
			.EnumerateObject()
			.Select(p => p.Name)
			.Should()
			.Contain(["type", "title", "status", "detail", "instance"]);
	}

	[Test]
	public async Task Document_NamesTheApiAndThePublicServer()
	{
		var document = await Document();

		document.GetProperty("info").GetProperty("title").GetString().Should().Be("Elastic Docs API");
		// The CDN no longer gates the API behind a cookie, so the description names none.
		document.GetProperty("info").GetProperty("description").GetString().Should().NotContainEquivalentOf("cookie");
		document.GetProperty("servers")[0].GetProperty("url").GetString().Should().Be("https://www.elastic.co/docs/_api/v1");
	}

	[Test]
	public async Task Paths_AreRelativeToTheServer()
	{
		var document = await Document();

		var paths = document.GetProperty("paths").EnumerateObject().Select(p => p.Name).ToList();
		paths.Should().Contain(["/search", "/apis/{api}/operations/{operationId}"]);
		paths.Should().AllSatisfy(p => p.Should().NotStartWith("/docs"));
	}
}
