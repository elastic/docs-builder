// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Nodes;
using AwesomeAssertions;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Landing;
using Elastic.ApiExplorer.Operations;
using Elastic.ApiExplorer.Types;
using Microsoft.OpenApi;
using Nullean.Snapshots;
using static Elastic.ApiExplorer.Tests.TestSpecs;

namespace Elastic.ApiExplorer.Tests;

public class ApiMarkdownFrontMatterTests
{
	[Test]
	public void Write_EmitsLlmAndOkfFields()
	{
		var markdown = ApiMarkdownFrontMatter.Write(
			"# Run a search\n\nReturns hits.\n",
			new ApiPageFrontMatter(
				"Run a search",
				"Returns hits that match the query",
				"https://cdn.example/api/elasticsearch/operation/operation-search",
				"Elasticsearch"
			)
		);

		markdown.Should().MatchInlineSnapshot(
			"""
			---
			type: api
			title: "Run a search"
			description: "Returns hits that match the query"
			url: https://cdn.example/api/elasticsearch/operation/operation-search
			resource: https://cdn.example/api/elasticsearch/operation/operation-search
			products:
			  - Elasticsearch
			---

			# Run a search

			Returns hits.
			"""
		);
	}

	[Test]
	public void Write_QuotesDescriptionWithColonAndQuote()
	{
		var markdown = ApiMarkdownFrontMatter.Write(
			"# X\n",
			new ApiPageFrontMatter("X", "Send a header like Authorization: \"ApiKey abc\" to authenticate.", "/api", null)
		);

		markdown.Should().MatchInlineSnapshot(
			"""
			---
			type: api
			title: "X"
			description: "Send a header like Authorization: \"ApiKey abc\" to authenticate."
			url: /api
			resource: /api
			---

			# X
			"""
		);
	}

	[Test]
	public void Write_PageWithASource_PutsTheSourceInResource()
	{
		var meta = new ApiPageFrontMatter(
			"Run a search",
			null,
			"/api/elasticsearch/operation/operation-search",
			null,
			Resource: "/api/elasticsearch.json#/paths/~1_search/post"
		);

		var markdown = ApiMarkdownFrontMatter.Write("# Run a search\n", meta);

		markdown.Should().MatchInlineSnapshot(
			"""
			---
			type: api
			title: "Run a search"
			url: /api/elasticsearch/operation/operation-search
			resource: /api/elasticsearch.json#/paths/~1_search/post
			---

			# Run a search
			"""
		);
	}

	[Test]
	public async Task SpecResource_PathItemThatIsAReference_PointsAtThePathItemTheSpecPublishes()
	{
		var document = await LoadSpecAsync(
			"""
			{
			  "openapi": "3.1.0",
			  "info": { "title": "t", "version": "1" },
			  "paths": { "/foo": { "$ref": "#/components/pathItems/Foo" } },
			  "components": {
			    "pathItems": {
			      "Foo": { "get": { "operationId": "foo-get", "responses": { "200": { "description": "ok" } } } }
			    }
			  }
			}
			"""
		);
		var pathItem = document.Paths["/foo"];
		var operation = new ApiOperation(HttpMethod.Get, pathItem.Operations![HttpMethod.Get], "/foo", pathItem, "foo");
		using var stream = new MemoryStream();
		await document.SerializeAsJsonAsync(stream, OpenApiSpecVersion.OpenApi3_1, TestContext.Current!.Execution.CancellationToken);
		var published = JsonNode.Parse(stream.ToArray())!["paths"]!["/foo"]!.AsObject();

		published.ContainsKey("$ref").Should().BeTrue("the spec publishes the reference, which is why the pointer cannot name `get`");
		published.ContainsKey("get").Should().BeFalse();
		ApiMarkdownFrontMatter
			.SpecResource(operation, "/api/elasticsearch", canonicalBaseUrl: null)
			.Should()
			.Be("/api/elasticsearch.json#/paths/~1foo");
	}

	[Test]
	[Arguments("_types.query_dsl:QueryContainer", "/api/elasticsearch.json#/components/schemas/_types.query_dsl%3AQueryContainer")]
	[Arguments("a/b~c", "/api/elasticsearch.json#/components/schemas/a~1b~0c")]
	public void SpecResource_TypePage_PointsAtItsComponentSchema(string schemaId, string expected) =>
		ApiMarkdownFrontMatter
			.SpecResource(new ApiSchema(schemaId, "Name", "Category", new OpenApiSchema()), "/api/elasticsearch", canonicalBaseUrl: null)
			.Should()
			.Be(expected);

	[Test]
	public void SpecResource_GroupPage_HasNoSpecObjectToPointAt() =>
		ApiMarkdownFrontMatter
			.SpecResource(new ApiTag("tag", "Tag", "", null, "endpoint-tag", []), "/api/elasticsearch", canonicalBaseUrl: null)
			.Should()
			.BeNull();

	[Test]
	public void Write_OmitsOptionalKeysWhenMissing()
	{
		var markdown = ApiMarkdownFrontMatter.Write("# API Explorer\n", new ApiPageFrontMatter("API Explorer", null, "/api", null));

		markdown.Should().MatchInlineSnapshot(
			"""
			---
			type: api
			title: "API Explorer"
			url: /api
			resource: /api
			---

			# API Explorer
			"""
		);
	}

	[Test]
	public void Write_EmitsRailFactsWhenPresent()
	{
		var facts = new ApiRailFacts(
			"1.2",
			"Apache 2.0: see terms",
			["https://api.example.com", "{scheme}://x"],
			"https://cdn.example/api/es.json",
			"https://cdn.example/api/es.yaml"
		);

		// AppendLine writes the platform newline; normalise so the multi-line assertions hold on Windows too.
		var markdown = ApiMarkdownFrontMatter.Write(
			"# Elasticsearch\n",
			new ApiPageFrontMatter("Elasticsearch", null, "/api", null, facts)
		).ReplaceLineEndings("\n");

		markdown.Should().MatchInlineSnapshot(
			"""
			---
			type: api
			title: "Elasticsearch"
			url: /api
			resource: /api
			version: "1.2"
			license: "Apache 2.0: see terms"
			servers:
			  - "https://api.example.com"
			  - "{scheme}://x"
			spec:
			  json: https://cdn.example/api/es.json
			  yaml: https://cdn.example/api/es.yaml
			---

			# Elasticsearch
			"""
		);
	}

	[Test]
	public void Write_OmitsEmptyRailFacts()
	{
		var facts = new ApiRailFacts(null, null, [], null, null);

		var markdown = ApiMarkdownFrontMatter.Write("# X\n", new ApiPageFrontMatter("X", null, "/api", null, facts));

		markdown.Should().MatchInlineSnapshot(
			"""
			---
			type: api
			title: "X"
			url: /api
			resource: /api
			---

			# X
			"""
		);
	}

	[Test]
	public void StripLeadingFrontMatter_RemovesAuthoredYaml()
	{
		var source =
			"""
			---
			navigation_title: Spaces
			---
			# Kibana spaces

			Spaces enable you to organize dashboards.
			""";

		var stripped = ApiMarkdownFrontMatter.StripLeadingFrontMatter(source);

		stripped.Should().MatchInlineSnapshot(
			"""
			# Kibana spaces

			Spaces enable you to organize dashboards.
			"""
		);
	}
}
