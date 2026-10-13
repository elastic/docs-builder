// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Model;
using Microsoft.OpenApi;
using static Elastic.ApiExplorer.Tests.TestSpecs;

namespace Elastic.ApiExplorer.Tests;

/// <summary>
/// A type's identity (its <c>$ref</c> id) travels with its display name, so links and formulas never depend on how
/// the name reads.
/// </summary>
public class TypeIdentityTests
{
	private const string Spec =
		"""
		{
		  "openapi": "3.1.0",
		  "info": { "title": "t", "version": "1" },
		  "paths": {},
		  "components": {
		    "schemas": {
		      "_types.query_dsl.QueryContainer": { "type": "object", "properties": { "term": { "type": "object" } } },
		      "_types.aggregations.AggregationContainer": { "type": "object", "properties": { "avg": { "type": "object" } } },
		      "_types.Field": { "type": "string" },
		      "Holder": {
		        "type": "object",
		        "properties": {
		          "query": { "$ref": "#/components/schemas/_types.query_dsl.QueryContainer" },
		          "must": { "type": "array", "items": { "$ref": "#/components/schemas/_types.query_dsl.QueryContainer" } },
		          "aggs": { "type": "object", "additionalProperties": { "$ref": "#/components/schemas/_types.aggregations.AggregationContainer" } },
		          "fields": {
		            "oneOf": [
		              { "$ref": "#/components/schemas/_types.Field" },
		              { "type": "array", "items": { "$ref": "#/components/schemas/_types.Field" } }
		            ]
		          },
		          "size": { "type": ["number", "string"] },
		          "labels": { "type": "object", "additionalProperties": { "type": "string" } }
		        }
		      }
		    }
		  }
		}
		""";

	[Test]
	[Arguments("query", "_types.query_dsl.QueryContainer")]
	[Arguments("must", "_types.query_dsl.QueryContainer")]
	[Arguments("aggs", "_types.aggregations.AggregationContainer")]
	public async Task Describe_LinkedType_LinksTheSpanNamingItsSchemaId(string property, string schemaId)
	{
		var document = await LoadSpecAsync(Spec);

		var annotation = BuilderFor(document).Describe(Holder(document).Properties![property]);

		var linked = annotation.Spans.Where(s => s.Href is not null).ToArray();
		linked.Should().ContainSingle();
		linked[0].SchemaId.Should().Be(schemaId);
		annotation.Spans.Where(s => s.SchemaId is null).Should().OnlyContain(s => s.Href == null);
	}

	[Test]
	public async Task GetTypeInfo_UnionOfRefAndArrayOfIt_ListsAlternativesWithTheirRef()
	{
		var document = await LoadSpecAsync(Spec);

		var info = new SchemaAnalyzer(document).GetTypeInfo(Holder(document).Properties!["fields"]);

		info.TypeName.Should().Be("Field | Field[]");
		info
			.Alternatives
			.Should()
			.Equal(
				new TypeAlternative("Field", "_types.Field", IsArray: false),
				new TypeAlternative("Field", "_types.Field", IsArray: true)
			);
	}

	[Test]
	public async Task GetTypeInfo_SeveralPrimitiveTypes_ListsEachAsAnAlternative()
	{
		var document = await LoadSpecAsync(Spec);

		var info = new SchemaAnalyzer(document).GetTypeInfo(Holder(document).Properties!["size"]);

		info.TypeName.Should().Be("number | string");
		info.Alternatives!.Select(a => a.Text).Should().Equal("number", "string");
		info.Alternatives.Should().OnlyContain(a => a.SchemaRef == null);
	}

	[Test]
	[Arguments("query")]
	[Arguments("must")]
	[Arguments("aggs")]
	public async Task GetTypeInfo_SingleType_IsNotAFormula(string property)
	{
		var document = await LoadSpecAsync(Spec);

		var info = new SchemaAnalyzer(document).GetTypeInfo(Holder(document).Properties![property]);

		info.IsFormula.Should().BeFalse();
	}

	[Test]
	public async Task GetTypeInfo_Map_CarriesItsValueType()
	{
		var document = await LoadSpecAsync(Spec);
		var analyzer = new SchemaAnalyzer(document);

		var aggs = analyzer.GetTypeInfo(Holder(document).Properties!["aggs"]);
		var labels = analyzer.GetTypeInfo(Holder(document).Properties!["labels"]);

		aggs.DictValueType!.TypeName.Should().Be("AggregationContainer");
		aggs.DictValueType.SchemaRef.Should().Be("_types.aggregations.AggregationContainer");
		labels.DictValueType!.TypeName.Should().Be("string");
	}

	private static IOpenApiSchema Holder(OpenApiDocument document) => document.Components!.Schemas!["Holder"];
}
