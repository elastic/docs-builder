// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using AwesomeAssertions;
using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Operations;
using Elastic.Documentation.Configuration;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;

namespace Elastic.ApiExplorer.Tests;

public class AuthSchemeTests
{
	[Test]
	public async Task Resolve_DocumentSecurity_MapsApiKeyBasicBearer()
	{
		var (op, doc) = await Load(EsShapedSpec(operationSecurity: null));

		var badges = OpenApiAuthSchemeResolver.Resolve(op, doc);

		badges.Select(b => b.Id).Should().Equal("apiKeyAuth", "basicAuth", "bearerAuth");
		badges.Select(b => b.PillLabel).Should().Equal("Api key auth", "Basic auth", "Bearer auth");
	}

	[Test]
	public async Task Resolve_OperationSecurity_OverridesDocument()
	{
		var (op, doc) = await Load(EsShapedSpec(operationSecurity: """{ "apiKeyAuth": [] }"""));

		var badges = OpenApiAuthSchemeResolver.Resolve(op, doc);

		badges.Select(b => b.Id).Should().Equal("apiKeyAuth");
		badges.Select(b => b.PillLabel).Should().Equal("Api key auth");
	}

	[Test]
	public async Task Resolve_EmptyOperationSecurity_ReturnsNoBadges()
	{
		var (op, doc) = await Load(EsShapedSpec(operationSecurity: ""));

		OpenApiAuthSchemeResolver.Resolve(op, doc).Should().BeEmpty();
	}

	[Test]
	public async Task Resolve_NoSchemes_ReturnsNoBadges()
	{
		var json = /*lang=json,strict*/
			"""
			{
			  "openapi": "3.0.3",
			  "info": { "title": "t", "version": "1" },
			  "paths": {
			    "/a": {
			      "get": {
			        "operationId": "op-a",
			        "responses": { "200": { "description": "ok" } }
			      }
			    }
			  }
			}
			""";
		var (op, doc) = await Load(json);

		OpenApiAuthSchemeResolver.Resolve(op, doc).Should().BeEmpty();
	}

	[Test]
	public async Task ElasticsearchSearch_InheritsDocumentSchemes()
	{
		var specPath = Path.Join(Paths.WorkingDirectoryRoot.FullName, "docs", "elasticsearch.json");
		File.Exists(specPath).Should().BeTrue($"Fixture missing: {specPath}");

		var doc = await OpenApiReader.Instance.ReadAsync(new FileSystem().FileInfo.New(specPath));
		doc.Should().NotBeNull();
		var search = doc!.Paths!["/_search"].Operations![HttpMethod.Get]!;

		var badges = OpenApiAuthSchemeResolver.Resolve(search, doc);

		badges.Select(b => b.Id).Should().Equal("apiKeyAuth", "basicAuth", "bearerAuth");
		badges.Select(b => b.PillLabel).Should().Equal("Api key auth", "Basic auth", "Bearer auth");
	}

	[Test]
	public async Task Resolve_WithAuthenticationUrl_AppendsLowercasedSchemeAnchors()
	{
		var (op, doc) = await Load(EsShapedSpec(operationSecurity: null));
		var badges = OpenApiAuthSchemeResolver.Resolve(op, doc, "/api/doc/elasticsearch/authentication");

		badges
			.Select(b => b.Href)
			.Should()
			.Equal(
				"/api/doc/elasticsearch/authentication#apikeyauth",
				"/api/doc/elasticsearch/authentication#basicauth",
				"/api/doc/elasticsearch/authentication#bearerauth"
			);
	}

	private static string EsShapedSpec(string? operationSecurity)
	{
		var operationSecurityJson = operationSecurity switch
		{
			null => "",
			"" => """ "security": [], """,
			_ => $""" "security": [ {operationSecurity} ], """
		};
		return /*lang=json,strict*/  $$"""
			{
			  "openapi": "3.0.3",
			  "info": { "title": "t", "version": "1" },
			  "paths": {
			    "/a": {
			      "get": {
			        "operationId": "op-a",
			        {{operationSecurityJson}}
			        "responses": { "200": { "description": "ok" } }
			      }
			    }
			  },
			  "components": {
			    "securitySchemes": {
			      "apiKeyAuth": { "type": "apiKey", "in": "header", "name": "Authorization" },
			      "basicAuth": { "type": "http", "scheme": "basic" },
			      "bearerAuth": { "type": "http", "scheme": "bearer" }
			    }
			  },
			  "security": [
			    { "apiKeyAuth": [] },
			    { "basicAuth": [] },
			    { "bearerAuth": [] }
			  ]
			}
			""";
	}

	private static async Task<(OpenApiOperation Op, OpenApiDocument Doc)> Load(string json)
	{
		var jsonPath = Path.Join(Path.GetTempPath(), $"auth-scheme-{Guid.NewGuid():N}.json");
		try
		{
			await File.WriteAllTextAsync(jsonPath, json, TestContext.Current!.Execution.CancellationToken);
			var loaded = await OpenApiDocument.LoadAsync(
				jsonPath,
				new OpenApiReaderSettings { LeaveStreamOpen = false },
				TestContext.Current!.Execution.CancellationToken
			);
			var doc = loaded.Document!;
			return (doc.Paths!["/a"].Operations![HttpMethod.Get]!, doc);
		}
		finally
		{
			if (File.Exists(jsonPath))
				File.Delete(jsonPath);
		}
	}
}
