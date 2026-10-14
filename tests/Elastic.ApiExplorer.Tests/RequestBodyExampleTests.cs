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
	public async Task Build_LeavesOutAReferenceMarkedReadOnlyBesideItsRef()
	{
		var body = await BuildFrom(
			/*lang=json,strict*/
			"""
			{ "type": "object", "required": ["id", "name"],
			  "properties": { "id": { "$ref": "#/components/schemas/Id", "readOnly": true }, "name": { "type": "string" } } }
			""",
			/*lang=json,strict*/
			""" "Id": { "type": "string" } """
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
	public async Task Build_KeepsTheSharedRequiredPropertiesBesideAUnionVariant()
	{
		var body = await BuildFrom(
			/*lang=json,strict*/
			"""
			{ "type": "object", "required": ["kind"], "properties": { "kind": { "type": "string", "enum": ["a", "b"] } },
			  "oneOf": [
			    { "type": "object", "required": ["a"], "properties": { "a": { "type": "integer" } } },
			    { "type": "object", "required": ["b"], "properties": { "b": { "type": "integer" } } } ] }
			"""
		);

		Parse(body).ToJsonString().Should().Be("""{"a":0,"kind":"a"}""");
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

		body.Should().NotBeNull().And.NotContain("null");
		// Objects at depth 0 to 5 are built; the one at depth 6 is cut and its property left out.
		Parse(body).ToJsonString().Should().Be("""{"next":{"next":{"next":{"next":{"next":{}}}}}}""");
	}

	[Test]
	public async Task Build_BelowTheDepthLimit_StillGivesScalarsTheirValue()
	{
		// Seven required objects deep: the innermost object is cut, but the scalar beside it keeps its example.
		var nested = /*lang=json,strict*/  """{ "type": "object", "required": ["leaf"], "properties": { "leaf": { "type": "string" } } }""";
		for (var i = 0; i < 6; i++)
		{
			nested =
				$$"""
				{ "type": "object", "required": ["next", "tag"],
				  "properties": { "next": {{nested}}, "tag": { "type": "string", "examples": ["t{{i}}"] } } }
				""";
		}

		var body = await BuildFrom(nested);

		var json = Parse(body);
		json["tag"]!.ToString().Should().Be("t5");
		var deepest = json;
		for (var i = 0; i < 5; i++)
			deepest = deepest!["next"]!;
		deepest["tag"]!.ToString().Should().Be("t0");
		deepest["next"].Should().BeNull();
		body.Should().NotContain("null");
	}

	[Test]
	public async Task Build_UnwrapsAnAllOfAroundAScalarOrAnArray()
	{
		var body = await BuildFrom(
			/*lang=json,strict*/
			"""
			{ "type": "object", "required": ["name", "ids"],
			  "properties": {
			    "name": { "description": "The name.", "allOf": [ { "type": "string" } ] },
			    "ids": { "description": "The ids.", "allOf": [ { "type": "array", "items": { "type": "integer" } } ] } } }
			"""
		);

		Parse(body).ToJsonString().Should().Be("""{"name":"string","ids":[0]}""");
	}

	[Test]
	public async Task Build_UnwrapsAnAllOfAroundAReference()
	{
		var body = await BuildFrom(
			/*lang=json,strict*/
			"""
			{ "type": "object", "required": ["index", "status"],
			  "properties": {
			    "index": { "description": "The index.", "allOf": [ { "$ref": "#/components/schemas/IndexName" } ] },
			    "status": { "description": "The status.", "allOf": [ { "$ref": "#/components/schemas/Status" } ] } } }
			""",
			/*lang=json,strict*/
			""" "IndexName": { "type": "string" }, "Status": { "type": "string", "enum": ["created", "needs_configuration"] } """
		);

		Parse(body).ToJsonString().Should().Be("""{"index":"string","status":"created"}""");
	}

	[Test]
	public async Task Build_SkipsALiteralNullExampleDefaultOrEnumMember()
	{
		var body = await BuildFrom(
			/*lang=json,strict*/
			"""
			{ "type": "object", "required": ["a", "b", "c"],
			  "properties": {
			    "a": { "type": ["string", "null"], "example": null },
			    "b": { "type": ["string", "null"], "default": null, "enum": [null, "x"] },
			    "c": { "type": "string", "examples": [null, "y"] } } }
			"""
		);

		body.Should().NotContain("sentinel");
		Parse(body).ToJsonString().Should().Be("""{"a":"string","b":"x","c":"y"}""");
	}

	[Test]
	public async Task Build_GivesAnObjectExampleItsLiteralNullsBack()
	{
		var body = await BuildFrom(
			/*lang=json,strict*/
			"""
			{ "type": "object", "required": ["settings"],
			  "properties": { "settings": { "type": "object", "examples": [ { "refresh_interval": null, "list": [null, 1] } ] } } }
			"""
		);

		body.Should().NotContain("sentinel");
		Parse(body).ToJsonString().Should().Be("""{"settings":{"refresh_interval":null,"list":[null,1]}}""");
	}

	[Test]
	public async Task Build_IncludesTheFirstRequiredAlternative()
	{
		var body = await BuildFrom(
			/*lang=json,strict*/
			"""
			{ "type": "object", "required": ["name"],
			  "properties": { "name": { "type": "string" }, "correlation_id": { "type": "string" }, "externalId": { "type": "string" } },
			  "oneOf": [ { "required": ["correlation_id"] }, { "required": ["externalId"] } ] }
			"""
		);

		Parse(body).ToJsonString().Should().Be("""{"name":"string","correlation_id":"string"}""");
	}

	[Test]
	public async Task Build_KeepsTheJsonTypeOfAnUntypedConst()
	{
		var body = await BuildFrom(
			/*lang=json,strict*/
			"""
			{ "type": "object", "required": ["version", "flag", "kind"],
			  "properties": { "version": { "const": 5 }, "flag": { "const": true }, "kind": { "const": "extension" } } }
			"""
		);

		Parse(body).ToJsonString().Should().Be("""{"version":5,"flag":true,"kind":"extension"}""");
	}

	[Test]
	public async Task Build_AddsNoAlternativeWhenTheRequiredFieldsAlreadySatisfyOne()
	{
		var body = await BuildFrom(
			/*lang=json,strict*/
			"""
			{ "type": "object", "required": ["externalId"],
			  "properties": { "correlation_id": { "type": "string" }, "externalId": { "type": "string" } },
			  "oneOf": [ { "required": ["correlation_id"] }, { "required": ["externalId"] } ] }
			"""
		);

		Parse(body).ToJsonString().Should().Be("""{"externalId":"string"}""");
	}

	[Test]
	public async Task Build_ParsesANumericConst()
	{
		var body = await BuildFrom(
			/*lang=json,strict*/
			"""
			{ "type": "object", "required": ["version", "kind"],
			  "properties": { "version": { "type": "integer", "const": 5 }, "kind": { "type": "string", "const": "7" } } }
			"""
		);

		Parse(body).ToJsonString().Should().Be("""{"version":5,"kind":"7"}""");
	}

	[Test]
	public async Task Build_LeavesOutARequiredPropertyThatHasNoValue()
	{
		var body = await BuildFrom(
			/*lang=json,strict*/
			"""
			{ "type": "object", "required": ["name", "data"],
			  "properties": { "name": { "not": {} }, "data": { "type": "string" } } }
			"""
		);

		Parse(body).ToJsonString().Should().Be("""{"data":"string"}""");
	}

	[Test]
	public async Task Build_SendsNullForARequiredPropertyOfTypeNull()
	{
		var body = await BuildFrom(
			/*lang=json,strict*/
			"""{ "type": "object", "required": ["value"], "properties": { "value": { "type": "null" } } }"""
		);

		Parse(body).ToJsonString().Should().Be("""{"value":null}""");
	}

	[Test]
	public async Task Build_SkipsANullVariantOfAUnion()
	{
		var body = await BuildFrom(
			/*lang=json,strict*/
			"""
			{ "type": "object", "required": ["value"],
			  "properties": { "value": { "oneOf": [ { "type": "null" }, { "type": "integer" } ] } } }
			"""
		);

		Parse(body).ToJsonString().Should().Be("""{"value":0}""");
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
