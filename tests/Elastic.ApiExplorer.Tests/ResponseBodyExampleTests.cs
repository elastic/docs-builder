// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Nodes;
using AwesomeAssertions;
using Elastic.ApiExplorer.Model;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;

namespace Elastic.ApiExplorer.Tests;

public class ResponseBodyExampleTests
{
	[Test]
	[Arguments("""{"type":"object","properties":{"state":{"type":"string","enum":["trial","active"]}}}""", """{"state":"trial"}""")]
	[Arguments("""{"type":"object","properties":{"id":{"type":"integer","readOnly":true},"secret":{"type":"string","writeOnly":true},"note":{"type":"null"}}}""", """{"id":0,"note":null}""")]
	[Arguments("""{"type":"array","items":{"type":"object","properties":{"enabled":{"type":"boolean"}}}}""", """[{"enabled":false}]""")]
	[Arguments("""{"type":"string","oneOf":[{"type":"string","const":"first"},{"type":"string","const":"second"}]}""", "\"first\"")]
	[Arguments("""{"type":"object","additionalProperties":{"type":"integer"}}""", """{"key":0}""")]
	[Arguments("""{"type":"object","example":{"state":"explicit","nested":null},"properties":{"state":{"type":"string","enum":["trial"]}}}""", """{"state":"explicit","nested":null}""")]
	[Arguments("""{"type":"string","default":"default","enum":["enum"]}""", "\"default\"")]
	[Arguments("""{"type":"string","allOf":[{"enum":["trial","active"]}]}""", "\"trial\"")]
	[Arguments("""{"type":"null"}""", "null")]
	public async Task Build_Schema_ReturnsRepresentativeJson(string schema, string expected)
	{
		var body = await BuildFrom(schema);

		body.Should().NotBeNull();
		JsonNode.Parse(body!)?.ToJsonString().Should().Be(expected == "null" ? null : expected);
	}

	[Test]
	public async Task Build_ReferencesAndAllOf_IncludeOptionalResponseProperties()
	{
		var body = await BuildFrom(
			"""{"allOf":[{"$ref":"#/components/schemas/Subscription"},{"type":"object","properties":{"count":{"type":"integer"}}}]}""",
			"""
			"Subscription":{"type":"object","properties":{"state":{"$ref":"#/components/schemas/State"},"secret":{"$ref":"#/components/schemas/Secret"}}},"State":{"type":"string","enum":["trial","active"]},"Secret":{"type":"string","writeOnly":true}
			"""
		);

		JsonNode.Parse(body!)!.ToJsonString().Should().Be("""{"state":"trial","count":0}""");
	}

	[Test]
	[Arguments("oneOf", false)]
	[Arguments("anyOf", false)]
	[Arguments("oneOf", true)]
	[Arguments("anyOf", true)]
	public async Task Build_AllOfWithUnion_IncludesBaseAndSelectedVariant(string keyword, bool referenced)
	{
		var union = $$"""{"{{keyword}}":[{"type":"null"},{"$ref":"#/components/schemas/Cat"},{"$ref":"#/components/schemas/Dog"}]}""";
		var member = referenced ? """{"$ref":"#/components/schemas/Animal"}""" : union;
		var body = await BuildFrom(
			$$"""{"allOf":[{"$ref":"#/components/schemas/Base"},{{member}}]}""",
			$$$$"""
			"Base":{"type":"object","properties":{"id":{"type":"integer"},"kind":{"type":"string","default":"base"}}},
			"Cat":{"type":"object","properties":{"kind":{"type":"string","const":"cat"},"meows":{"type":"boolean","example":true}}},
			"Dog":{"type":"object","properties":{"barks":{"type":"boolean"}}},
			"Animal":{{{{union}}}}
			"""
		);

		JsonNode.Parse(body!)!.ToJsonString().Should().Be("""{"kind":"cat","meows":true,"id":0}""");
	}

	[Test]
	public async Task Build_ReferenceWithExampleSibling_PrefersTheSibling()
	{
		var body = await BuildFrom(
			"""{"$ref":"#/components/schemas/State","examples":["sibling"]}""",
			"""
			"State":{"type":"string","enum":["target"]}
			"""
		);

		body.Should().Be("\"sibling\"");
	}

	[Test]
	public async Task Build_RecursiveSchema_StopsBeforeRepeatingWithoutBound()
	{
		var body = await BuildFrom(
			"""{"$ref":"#/components/schemas/Node"}""",
			"""
			"Node":{"type":"object","properties":{"id":{"type":"integer"},"child":{"$ref":"#/components/schemas/Node"}}}
			"""
		);

		body.Should().NotBeNull();
		body!.Length.Should().BeLessThan(1000);
		JsonNode.Parse(body).Should().NotBeNull();
	}

	[Test]
	public void Build_WideSchema_LimitsTheNumberOfValues()
	{
		var schema = new OpenApiSchema
		{
			Type = JsonSchemaType.Object,
			Properties = Enumerable.Range(0, 1000).ToDictionary(
				i => $"field{i}",
				_ => (IOpenApiSchema)new OpenApiSchema { Type = JsonSchemaType.String }
			)
		};

		var body = ResponseBodyExample.Build(new SchemaAnalyzer(new OpenApiDocument()), "application/json", schema);

		JsonNode.Parse(body!)!.AsObject().Count.Should().Be(199);
	}

	[Test]
	[Arguments("text/plain")]
	[Arguments("application/xml")]
	[Arguments("application/x-ndjson")]
	public void Build_NonJsonContent_DoesNotInventJson(string mediaType) =>
		ResponseBodyExample
			.Build(new SchemaAnalyzer(new OpenApiDocument()), mediaType, new OpenApiSchema { Type = JsonSchemaType.String })
			.Should()
			.BeNull();

	[Test]
	[Arguments("application/problem+json")]
	[Arguments("application/json; charset=utf-8")]
	public void Build_JsonContentParametersAndSuffix_GenerateJson(string mediaType) =>
		ResponseBodyExample
			.Build(new SchemaAnalyzer(new OpenApiDocument()), mediaType, new OpenApiSchema { Type = JsonSchemaType.Boolean })
			.Should()
			.Be("false");

	private static async Task<string?> BuildFrom(string schema, string extraSchemas = "")
	{
		var root =
			JsonNode.Parse(
				"""
			{"openapi":"3.1.0","info":{"title":"t","version":"1"},"paths":{"/subscription":{"get":{"operationId":"getSubscription","responses":{"200":{"description":"ok","content":{"application/json":{}}}}}}}}
			"""
			)!;
		root["paths"]!["/subscription"]!["get"]!["responses"]!["200"]!["content"]!["application/json"]!["schema"] = JsonNode.Parse(schema);
		root["components"] = JsonNode.Parse("{\"schemas\":{" + extraSchemas + "}}");
		var json = root.ToJsonString();
		var path = Path.Join(Path.GetTempPath(), $"response-body-{Guid.NewGuid():N}.json");
		try
		{
			await File.WriteAllTextAsync(path, json, TestContext.Current!.Execution.CancellationToken);
			var loaded = await OpenApiDocument.LoadAsync(
				path,
				new OpenApiReaderSettings { LeaveStreamOpen = false },
				TestContext.Current!.Execution.CancellationToken
			);
			var document = loaded.Document!;
			return ResponseBodyExample.Build(
				new SchemaAnalyzer(document),
				"application/json",
				document.Paths!["/subscription"].Operations![HttpMethod.Get].Responses!["200"].Content!["application/json"].Schema
			);
		}
		finally
		{
			File.Delete(path);
		}
	}
}
