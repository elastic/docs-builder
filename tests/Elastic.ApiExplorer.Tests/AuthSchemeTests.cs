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

		badges.SelectMany(b => b.Schemes).Select(s => s.Id).Should().Equal("apiKeyAuth", "basicAuth", "bearerAuth");
		badges.Select(b => b.Label).Should().Equal("Api key", "Basic", "Bearer");
	}

	[Test]
	public async Task Resolve_OperationSecurity_OverridesDocument()
	{
		var (op, doc) = await Load(EsShapedSpec(operationSecurity: """{ "apiKeyAuth": [] }"""));

		var badges = OpenApiAuthSchemeResolver.Resolve(op, doc);

		badges.SelectMany(b => b.Schemes).Select(s => s.Id).Should().Equal("apiKeyAuth");
		badges.Select(b => b.Label).Should().Equal("Api key");
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
	public async Task OverridesDocument_InheritedSecurity_IsFalse()
	{
		var (op, doc) = await Load(EsShapedSpec(operationSecurity: null));

		OpenApiAuthSchemeResolver.OverridesDocument(op, doc).Should().BeFalse();
	}

	[Test]
	public async Task OverridesDocument_SameAlternativesInAnotherOrder_IsFalse()
	{
		var (op, doc) = await Load(EsShapedSpec(operationSecurity: """{ "bearerAuth": [] }, { "basicAuth": [] }, { "apiKeyAuth": [] }"""));

		OpenApiAuthSchemeResolver.OverridesDocument(op, doc).Should().BeFalse();
	}

	[Test]
	public async Task OverridesDocument_OtherAlternatives_IsTrue()
	{
		var (op, doc) = await Load(EsShapedSpec(operationSecurity: """{ "apiKeyAuth": [] }"""));

		OpenApiAuthSchemeResolver.OverridesDocument(op, doc).Should().BeTrue();
	}

	[Test]
	public async Task Badge_InheritedSecurity_PointsAtThePage()
	{
		var (op, doc) = await Load(EsShapedSpec(operationSecurity: null));

		OpenApiAuthSchemeResolver.Badge(op, doc).Should().Be(new AuthBadge("auth", "required", AuthBadgeTarget.Page));
	}

	[Test]
	public async Task Badge_OverriddenSecurity_PointsAtTheEndpointSection()
	{
		var (op, doc) = await Load(EsShapedSpec(operationSecurity: """{ "apiKeyAuth": [] }"""));

		OpenApiAuthSchemeResolver.Badge(op, doc).Should().Be(new AuthBadge("auth", "required", AuthBadgeTarget.Section));
	}

	[Test]
	public async Task Badge_EmptyOperationSecurity_SaysNoAuthAndLinksNowhere()
	{
		var (op, doc) = await Load(EsShapedSpec(operationSecurity: ""));

		OpenApiAuthSchemeResolver.Badge(op, doc).Should().Be(new AuthBadge("no auth", "none", AuthBadgeTarget.None));
	}

	[Test]
	public async Task Badge_AnonymousAlternative_SaysAuthOptional()
	{
		var (op, doc) = await Load(EsShapedSpec(operationSecurity: """{}, { "apiKeyAuth": [] }"""));

		OpenApiAuthSchemeResolver.Badge(op, doc)!.Text.Should().Be("auth optional");
	}

	[Test]
	public async Task Badge_SpecWithoutSecurity_IsNone()
	{
		var (op, doc) = await Load(EsShapedSpec(operationSecurity: null));
		doc.Security = null;

		OpenApiAuthSchemeResolver.Badge(op, doc).Should().BeNull();
	}

	[Test]
	public async Task ResolveDocument_NoSecurityButSchemes_ListsEachScheme()
	{
		var (_, doc) = await Load(EsShapedSpec(operationSecurity: null));
		doc.Security = null;

		OpenApiAuthSchemeResolver.ResolveDocument(doc).Select(r => r.Label).Should().Equal("Api key", "Basic", "Bearer");
	}

	[Test]
	public async Task ElasticsearchSearch_InheritsDocumentSchemes()
	{
		var specPath = Path.Join(AppContext.BaseDirectory, "TestData", "elasticsearch-openapi.json");
		File.Exists(specPath).Should().BeTrue($"Fixture missing: {specPath}");

		var doc = await OpenApiReader.Instance.ReadAsync(new FileSystem().FileInfo.New(specPath));
		doc.Should().NotBeNull();
		var search = doc!.Paths!["/{index}/_search"].Operations![HttpMethod.Post]!;

		var badges = OpenApiAuthSchemeResolver.Resolve(search, doc);

		badges.SelectMany(b => b.Schemes).Select(s => s.Id).Should().Equal("apiKeyAuth", "basicAuth", "bearerAuth");
		badges.Select(b => b.Label).Should().Equal("Api key", "Basic", "Bearer");
	}

	[Test]
	public async Task Resolve_DocumentSecurity_CarriesCredentialExamples()
	{
		var (op, doc) = await Load(EsShapedSpec(operationSecurity: null));

		var schemes = OpenApiAuthSchemeResolver.Resolve(op, doc);

		schemes
			.SelectMany(r => r.Schemes)
			.Select(s => s.CredentialExample)
			.Should()
			.Equal("Authorization: <value>", "Authorization: Basic <credentials>", "Authorization: Bearer <token>");
	}

	[Test]
	public async Task Resolve_RequirementWithTwoSchemes_StaysOneAlternativeThatNeedsBoth()
	{
		var (op, doc) = await Load(EsShapedSpec(operationSecurity: """{ "basicAuth": [], "bearerAuth": [] }, { "apiKeyAuth": [] }"""));

		var requirements = OpenApiAuthSchemeResolver.Resolve(op, doc);

		requirements.Select(r => r.Label).Should().Equal("Basic + Bearer", "Api key");
		requirements[0].Schemes.Select(s => s.Id).Should().Equal("basicAuth", "bearerAuth");
		requirements[1].Schemes.Should().ContainSingle();
	}

	[Test]
	public async Task Resolve_SameRequirementTwice_ListsItOnce()
	{
		var (op, doc) = await Load(EsShapedSpec(operationSecurity: """{ "apiKeyAuth": [] }, { "apiKeyAuth": [] }"""));

		OpenApiAuthSchemeResolver.Resolve(op, doc).Should().ContainSingle().Which.Label.Should().Be("Api key");
	}

	[Test]
	public async Task Resolve_AlternativesWhoseIdsJoinToTheSameText_AreBothKept()
	{
		var json = /*lang=json,strict*/
			"""
			{
			  "openapi": "3.0.3",
			  "info": { "title": "t", "version": "1" },
			  "paths": { "/a": { "get": {
			    "operationId": "op-a",
			    "security": [ { "a": [], "b": [] }, { "a-b": [] } ],
			    "responses": { "200": { "description": "ok" } }
			  } } },
			  "components": { "securitySchemes": {
			    "a": { "type": "apiKey", "in": "header", "name": "X-A" },
			    "b": { "type": "http", "scheme": "basic" },
			    "a-b": { "type": "http", "scheme": "bearer" }
			  } }
			}
			""";
		var (op, doc) = await Load(json);

		OpenApiAuthSchemeResolver.Resolve(op, doc).Select(r => r.Label).Should().Equal("Api key + Basic", "Bearer");
	}

	[Test]
	[Arguments("""{ "cookieAuth": [] }, { "bearerAuth": [] }""", "bearer")]
	[Arguments("""{ "cookieAuth": [], "bearerAuth": [] }, { "basicAuth": [] }""", "basic")]
	[Arguments("""{ "apiKeyAuth": [], "bearerAuth": [] }""", "Authorization+bearer")]
	[Arguments("""{ "negotiateAuth": [] }, { "basicAuth": [] }""", "basic")]
	public async Task CurlSchemes_PicksTheFirstAlternativeCurlCanSend(string security, string expected)
	{
		var (op, doc) = await Load(
			EsShapedSpec(
				operationSecurity: security,
				extraScheme: """ "cookieAuth": { "type": "apiKey", "in": "cookie", "name": "sid" }, "negotiateAuth": { "type": "http", "scheme": "negotiate" }, """
			)
		);

		var schemes = OpenApiAuthSchemeResolver.CurlSchemes(op, doc);

		schemes.Select(static s => s.Scheme ?? s.Name).Should().Equal(expected.Split('+'));
	}

	[Test]
	public async Task CurlSchemes_WhenNoAlternativeCanBeSent_ReturnsNone()
	{
		var (op, doc) = await Load(
			EsShapedSpec(
				operationSecurity: """{ "cookieAuth": [] }""",
				extraScheme: """ "cookieAuth": { "type": "apiKey", "in": "cookie", "name": "sid" }, "negotiateAuth": { "type": "http", "scheme": "negotiate" }, """
			)
		);

		OpenApiAuthSchemeResolver.CurlSchemes(op, doc).Should().BeEmpty();
	}

	private static string EsShapedSpec(string? operationSecurity, string extraScheme = "")
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
			      {{extraScheme}}
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
