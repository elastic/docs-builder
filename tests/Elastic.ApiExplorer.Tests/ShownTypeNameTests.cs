// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Components.PropertyTree;
using static Elastic.ApiExplorer.Tests.TestSpecs;

namespace Elastic.ApiExplorer.Tests;

/// <summary>
/// The spec's type names are code names, so readers see a type's shape or its page title instead, and a union's
/// variants go by what tells them apart.
/// </summary>
public class ShownTypeNameTests
{
	private const string Spec =
		"""
		{
		  "openapi": "3.1.0",
		  "info": { "title": "t", "version": "1" },
		  "paths": {},
		  "components": {
		    "schemas": {
		      "_types.aggregations.Aggregate": { "oneOf": [ { "type": "object", "properties": { "buckets": { "type": "array" } } }, { "type": "object", "properties": { "value": { "type": "number" } } } ] },
		      "_types.Mixed": { "type": "array", "items": { "type": ["string", "object"] } },
		      "_types.StringBase": { "type": "string" },
		      "_types.StringAlias": { "allOf": [ { "$ref": "#/components/schemas/_types.StringBase" } ] },
		      "_types.query_dsl.QueryContainer": { "type": "object", "properties": { "term": { "type": "object" } } },
		      "_types.Field": { "type": "string" },
		      "_types.Id": { "type": "string" },
		      "_types.QueryVector": { "type": "array", "items": { "type": "number" } },
		      "_types.SourceConfig": { "oneOf": [ { "type": "boolean" }, { "type": "object", "properties": { "includes": { "type": "string" } } } ] },
		      "titled.Cat": { "title": "A cat", "type": "object", "properties": { "lives": { "type": "integer" } } },
		      "titled.Dog": { "title": "A dog", "type": "object", "properties": { "barks": { "type": "boolean" } } },
		      "kind.Eql": { "type": "object", "properties": { "type": { "type": "string", "enum": ["eql"] }, "query": { "type": "string" } } },
		      "kind.Esql": { "type": "object", "properties": { "type": { "type": "string", "enum": ["esql"] }, "query": { "type": "string" } } },
		      "rule.Eql": { "type": "object", "properties": { "language": { "type": "string", "enum": ["eql"] }, "type": { "type": "string", "enum": ["eql"] } } },
		      "rule.Query": { "type": "object", "properties": { "language": { "type": "string", "enum": ["kuery", "lucene"] }, "type": { "type": "string", "enum": ["query"] } } },
		      "same.EqlRuleProps": { "type": "object", "properties": { "query": { "type": "string" } } },
		      "same.QueryRuleProps": { "type": "object", "properties": { "query": { "type": "string" } } },
		      "Holder": {
		        "type": "object",
		        "properties": {
		          "titled": { "oneOf": [ { "$ref": "#/components/schemas/titled.Cat" }, { "$ref": "#/components/schemas/titled.Dog" } ] },
		          "kind": { "oneOf": [ { "$ref": "#/components/schemas/kind.Eql" }, { "$ref": "#/components/schemas/kind.Esql" } ] },
		          "rule": { "oneOf": [ { "$ref": "#/components/schemas/rule.Eql" }, { "$ref": "#/components/schemas/rule.Query" } ] },
		          "same": { "oneOf": [ { "$ref": "#/components/schemas/same.EqlRuleProps" }, { "$ref": "#/components/schemas/same.QueryRuleProps" } ] },
		          "linked": { "oneOf": [ { "$ref": "#/components/schemas/_types.query_dsl.QueryContainer" }, { "$ref": "#/components/schemas/titled.Cat" } ] },
		          "titledArray": { "oneOf": [ { "type": "array", "items": { "$ref": "#/components/schemas/titled.Cat" } }, { "$ref": "#/components/schemas/titled.Dog" } ] },
		          "aggs": { "type": "object", "additionalProperties": { "$ref": "#/components/schemas/_types.aggregations.Aggregate" } },
		          "mixed": { "$ref": "#/components/schemas/_types.Mixed" },
		          "alias": { "$ref": "#/components/schemas/_types.StringAlias" },
		          "vector": { "allOf": [ { "$ref": "#/components/schemas/_types.QueryVector" } ] },
		          "source": { "allOf": [ { "$ref": "#/components/schemas/_types.SourceConfig" } ] },
		          "ids": { "oneOf": [ { "$ref": "#/components/schemas/_types.Field" }, { "$ref": "#/components/schemas/_types.Id" } ] }
		        }
		      }
		    }
		  }
		}
		""";

	[Test]
	[Arguments("titled", new[] { "A cat", "A dog" })]
	[Arguments("kind", new[] { "type: eql", "type: esql" })]
	[Arguments("rule", new[] { "type: eql", "type: query" })]
	[Arguments("same", new[] { "EqlRuleProps", "QueryRuleProps" })]
	[Arguments("linked", new[] { "Query", "A cat" })]
	[Arguments("titledArray", new[] { "A cat", "A dog" })]
	public async Task BuildPropertyList_UnionVariants_GoByTitleThenConstantFieldThenShortenedName(string property, string[] expected)
	{
		var row = await Row(property);

		row.Children.Variants!.Variants.Select(v => v.DisplayName).Should().Equal(expected);
	}

	[Test]
	[Arguments("vector", "number[]")]
	[Arguments("source", "union")]
	[Arguments("ids", "union string")]
	[Arguments("mixed", "(string | object)[]")]
	[Arguments("alias", "string")]
	public async Task BuildPropertyList_NamedType_ReadsAsItsShape(string property, string expected)
	{
		var row = await Row(property);

		row.Type.Text.Should().Be(expected);
	}

	[Test]
	public async Task BuildPropertyList_UnionWithItsOwnPage_ReadsAsThePageTitle()
	{
		var row = await Row("aggs");

		row.Type.Text.Should().Be("map string to {} Aggregation result");
		row.Type.Spans.Should().ContainSingle(s => s.Href != null).Which.Text.Should().Be("Aggregation result");
	}

	private static async Task<ApiProperty> Row(string name)
	{
		var document = await LoadSpecAsync(Spec);
		var list = BuilderFor(document).BuildPropertyList(document.Components!.Schemas!["Holder"], new PropertyTreeScope { Prefix = "" });
		return list!.Items.Single(p => p.Name == name);
	}
}
