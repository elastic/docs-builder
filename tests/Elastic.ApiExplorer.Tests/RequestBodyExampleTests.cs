// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Nodes;
using AwesomeAssertions;
using Elastic.ApiExplorer.Model;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;

namespace Elastic.ApiExplorer.Tests;

public class RequestBodyExampleTests
{
	[Test]
	public async Task Build_HoldsTheRequiredPropertiesOnly()
	{
		var body = await BuildFrom(
			/*lang=json,strict*/
			"""
			{ "type": "object", "required": ["name", "count", "enabled"],
			  "properties": { "name": { "type": "string" }, "count": { "type": "integer" },
			                  "enabled": { "type": "boolean" }, "nickname": { "type": "string" } } }
			"""
		);

		Parse(body).ToJsonString().Should().Be("""{"name":"string","count":0,"enabled":false}""");
	}

	[Test]
	public async Task Build_PrefersWhatTheSpecStates()
	{
		var body = await BuildFrom(
			/*lang=json,strict*/
			"""
			{ "type": "object", "required": ["id", "mode", "size", "kind", "tag"],
			  "properties": {
			    "id": { "type": "string", "examples": ["ext-1"], "default": "ignored" },
			    "mode": { "type": "string", "default": "fast", "enum": ["slow", "fast"] },
			    "size": { "type": "integer", "enum": [3, 5] },
			    "kind": { "type": "string", "const": "extension" },
			    "tag": { "type": "string", "enum": ["a", "b"] } } }
			"""
		);

		Parse(body).ToJsonString().Should().Be("""{"id":"ext-1","mode":"fast","size":3,"kind":"extension","tag":"a"}""");
	}

	[Test]
	public async Task Build_ReadsTheExampleOfAnOpenApi30Schema()
	{
		var body = await BuildFrom(
			/*lang=json,strict*/
			"""{ "type": "object", "required": ["id"], "properties": { "id": { "type": "string", "example": "ext-1" } } }""",
			openApi: "3.0.3"
		);

		Parse(body).ToJsonString().Should().Be("""{"id":"ext-1"}""");
	}

	[Test]
	public async Task Build_NestsRequiredObjectsAndArrays()
	{
		var body = await BuildFrom(
			/*lang=json,strict*/
			"""
			{ "type": "object", "required": ["owner", "ids"],
			  "properties": {
			    "owner": { "type": "object", "required": ["id"], "properties": { "id": { "type": "string" }, "note": { "type": "string" } } },
			    "ids": { "type": "array", "items": { "type": "integer" } } } }
			"""
		);

		Parse(body).ToJsonString().Should().Be("""{"owner":{"id":"string"},"ids":[0]}""");
	}

	[Test]
	public async Task Build_LeavesOutReadOnlyProperties()
	{
		var body = await BuildFrom(
			/*lang=json,strict*/
			"""
			{ "type": "object", "required": ["id", "name"],
			  "properties": { "id": { "type": "string", "readOnly": true }, "name": { "type": "string" } } }
			"""
		);

		Parse(body).ToJsonString().Should().Be("""{"name":"string"}""");
	}

	[Test]
	public async Task Build_FoldsAllOfAndFollowsRefs()
	{
		var body = await BuildFrom(
			/*lang=json,strict*/
			"""
			{ "allOf": [ { "$ref": "#/components/schemas/Base" },
			             { "type": "object", "required": ["name"], "properties": { "name": { "type": "string" } } } ] }
			""",
			/*lang=json,strict*/
			""" "Base": { "type": "object", "required": ["version"], "properties": { "version": { "type": "integer" } } } """
		);

		Parse(body).ToJsonString().Should().Be("""{"version":0,"name":"string"}""");
	}

	[Test]
	public async Task Build_TakesTheFirstVariantOfAUnion()
	{
		var body = await BuildFrom(
			/*lang=json,strict*/
			"""
			{ "oneOf": [
			    { "type": "object", "required": ["a"], "properties": { "a": { "type": "string" } } },
			    { "type": "object", "required": ["b"], "properties": { "b": { "type": "string" } } } ] }
			"""
		);

		Parse(body).ToJsonString().Should().Be("""{"a":"string"}""");
	}

	[Test]
	public async Task Build_WithoutRequiredProperties_IsAnEmptyObject()
	{
		var body = await BuildFrom(
			/*lang=json,strict*/
			"""{ "type": "object", "properties": { "name": { "type": "string" } } }"""
		);

		Parse(body).ToJsonString().Should().Be("{}");
	}

	[Test]
	public async Task Build_WithARequiredCycle_StopsAtTheDepthLimit()
	{
		var body = await BuildFrom(
			/*lang=json,strict*/
			"""{ "$ref": "#/components/schemas/Node" }""",
			/*lang=json,strict*/
			""" "Node": { "type": "object", "required": ["next"], "properties": { "next": { "$ref": "#/components/schemas/Node" } } } """
		);

		body.Should().NotBeNull().And.Contain("null");
	}

	[Test]
	[Arguments("application/json", true)]
	[Arguments("application/problem+json", true)]
	[Arguments("application/x-ndjson", false)]
	[Arguments("multipart/form-data", false)]
	[Arguments(null, false)]
	public void IsJson_OnlyMatchesOneJsonDocument(string? mediaType, bool expected) =>
		RequestBodyExample.IsJson(mediaType).Should().Be(expected);

	[Test]
	public void Build_ForAMediaTypeThatIsNotJson_IsNull()
	{
		var document = new OpenApiDocument();

		RequestBodyExample
			.Build(new SchemaAnalyzer(document), "multipart/form-data", new OpenApiSchema { Type = JsonSchemaType.Object })
			.Should()
			.BeNull();
	}

	private static JsonNode Parse(string? json) => JsonNode.Parse(json!)!;

	/// <summary>The body for a request whose schema is <paramref name="schema"/>; <paramref name="extraSchemas"/> adds component schemas, as a JSON member list.</summary>
	private static async Task<string?> BuildFrom(string schema, string extraSchemas = "", string openApi = "3.1.0")
	{
		var json =
			$$"""
			{ "openapi": "{{openApi}}", "info": { "title": "t", "version": "1" },
			  "paths": { "/a": { "post": { "operationId": "op",
			    "requestBody": { "content": { "application/json": { "schema": {{schema}} } } },
			    "responses": { "200": { "description": "ok" } } } } },
			  "components": { "schemas": { {{(extraSchemas.Length == 0 ? "\"Unused\": { \"type\": \"string\" }" : extraSchemas)}} } } }
			""";
		var jsonPath = Path.Join(Path.GetTempPath(), $"request-body-{Guid.NewGuid():N}.json");
		try
		{
			await File.WriteAllTextAsync(jsonPath, json, TestContext.Current!.Execution.CancellationToken);
			var loaded = await OpenApiDocument.LoadAsync(
				jsonPath,
				new OpenApiReaderSettings { LeaveStreamOpen = false },
				TestContext.Current!.Execution.CancellationToken
			);
			var document = loaded.Document!;
			var media = document.Paths!["/a"].Operations![HttpMethod.Post]!.RequestBody!.Content!["application/json"];
			return RequestBodyExample.Build(new SchemaAnalyzer(document), "application/json", media.Schema);
		}
		finally
		{
			if (File.Exists(jsonPath))
				File.Delete(jsonPath);
		}
	}
}
