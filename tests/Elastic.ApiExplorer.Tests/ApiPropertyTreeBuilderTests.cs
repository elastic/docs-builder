// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO;
using AwesomeAssertions;
using Elastic.ApiExplorer.Components.PropertyTree;
using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Operations;
using Microsoft.AspNetCore.Html;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;

namespace Elastic.ApiExplorer.Tests;

[ClassDataSource<ApiExplorerFixture>(Shared = SharedType.PerClass)]
public class ApiPropertyTreeBuilderTests(ApiExplorerFixture fixture)
{
	private ApiPropertyTreeBuilder CreateBuilder(string? currentPageType = null, CollapseMode collapseMode = CollapseMode.AlwaysCollapsed)
	{
		var options = new PropertyDisplayOptions
		{
			RenderMarkdown = s => new HtmlString($"<p>{s}</p>"),
			ApiRootUrl = "/api/doc/fixture",
			CollapseMode = collapseMode
		};
		return new ApiPropertyTreeBuilder(fixture.Document, options, currentPageType);
	}

	private static async Task<OpenApiDocument> LoadSpecAsync(string json)
	{
		var path = Path.Join(Path.GetTempPath(), $"api-explorer-spec-{Guid.NewGuid():N}.json");
		await File.WriteAllTextAsync(path, json, TestContext.Current!.Execution.CancellationToken);
		try
		{
			var loaded = await OpenApiDocument.LoadAsync(
				path,
				new OpenApiReaderSettings { LeaveStreamOpen = false },
				TestContext.Current!.Execution.CancellationToken
			);
			return loaded.Document!;
		}
		finally
		{
			if (File.Exists(path))
				File.Delete(path);
		}
	}

	private static ApiPropertyTreeBuilder BuilderFor(OpenApiDocument document) =>
		new(document, new PropertyDisplayOptions { RenderMarkdown = s => new HtmlString($"<p>{s}</p>"), ApiRootUrl = "/api/doc/fixture" });

	private IOpenApiSchema Schema(string id) => fixture.Document.Components!.Schemas![id];

	[Test]
	public void BuildPropertyList_RecursiveSchema_StopsAtAncestor()
	{
		var builder = CreateBuilder(currentPageType: "QueryContainer");
		var ancestors = new HashSet<string> { "QueryContainer" };

		var list = builder.BuildPropertyList(
			Schema("_types.query_dsl.QueryContainer"),
			new PropertyTreeScope { Prefix = "", Ancestors = ancestors }
		);

		list.Should().NotBeNull();
		var boolProp = list.Items.Single(p => p.Name == "bool");
		boolProp.IsRecursive.Should().BeFalse();
		boolProp.Children.Kind.Should().Be(ChildKind.PropertyList);

		var must = boolProp.Children.Properties!.Items.Single(p => p.Name == "must");
		must.IsRecursive.Should().BeTrue("must is an array of the ancestor type QueryContainer");
		must.Children.Kind.Should().Be(ChildKind.None);
	}

	[Test]
	public void BuildPropertyList_SimpleArrayUnion_DetectsFieldOrFieldArray()
	{
		var builder = CreateBuilder();

		var list = builder.BuildPropertyList(
			Schema("fixture.SearchRequestBody"),
			new PropertyTreeScope { Prefix = "req", IsRequest = true }
		);

		var fields = list!.Items.Single(p => p.Name == "fields");
		fields.Union.Should().BeNull("X | X[] is already in the type annotation");
		fields.Type.Text.Should().Be("union Field | [] Field");
		fields.AnchorId.Should().Be("req-fields");
	}

	[Test]
	public void BuildPropertyList_DictionaryOfLinkedType_LinksInsteadOfExpanding()
	{
		var builder = CreateBuilder();

		var list = builder.BuildPropertyList(
			Schema("fixture.SearchRequestBody"),
			new PropertyTreeScope { Prefix = "req", IsRequest = true }
		);

		var aggs = list!.Items.Single(p => p.Name == "aggs");
		aggs.Children.Kind.Should().Be(ChildKind.None, "the dictionary value type has its own page");
		aggs.TypeLink.Should().NotBeNull();
		aggs.TypeLink!.TypeName.Should().Be("AggregationContainer");
		aggs.TypeLink.Url.Should().Be("/api/doc/fixture/types/_types-aggregations-aggregationcontainer");
		aggs
			.Type
			.Spans
			.Should()
			.Contain(s => s.Text == "AggregationContainer" && s.CssClass == "type-linked" && s.Href == aggs.TypeLink.Url);
		aggs.Type.Spans.Where(s => s.Text is "map" or "{}").Should().OnlyContain(s => string.IsNullOrEmpty(s.Href));
	}

	[Test]
	public void BuildPropertyList_LinkedType_PutsHrefOnTypeName()
	{
		var builder = CreateBuilder();

		var list = builder.BuildPropertyList(
			Schema("fixture.SearchRequestBody"),
			new PropertyTreeScope { Prefix = "req", IsRequest = true }
		);

		var query = list!.Items.Single(p => p.Name == "query");
		query.TypeLink.Should().NotBeNull();
		query.Type.Spans.Should().Contain(s => s.Text == "QueryContainer" && s.CssClass == "type-linked" && s.Href == query.TypeLink!.Url);
	}

	[Test]
	public void BuildPropertyList_RequiredProperty_IsMarkedRequired()
	{
		var builder = CreateBuilder();

		var list = builder.BuildPropertyList(
			Schema("fixture.SearchRequestBody"),
			new PropertyTreeScope { Prefix = "req", IsRequest = true }
		);

		list!.Items.Single(p => p.Name == "query").IsRequired.Should().BeTrue();
		list.Items.Single(p => p.Name == "sort").IsRequired.Should().BeFalse();
	}

	[Test]
	public void Describe_EnumSchema_ShowsEnumKeyword()
	{
		var builder = CreateBuilder();

		var annotation = builder.Describe(Schema("fixture.SearchRequestBody").Properties!["mode"]);

		annotation.Spans.Should().Contain(s => s.CssClass == SchemaHelpers.WrapperEnumCssClass && s.Text == "enum");
	}

	[Test]
	public void Describe_ValueType_MarksKeywordAndAliasAsTypeValue()
	{
		var builder = CreateBuilder();

		var annotation = builder.Describe(Schema("_types.SortField").Properties!["field"]);

		annotation.Spans.Should().Contain(s => s.Text == "string" && s.CssClass != null && s.CssClass.Contains("type-value"));
		annotation.Spans.Should().Contain(s => s.Text == "Field" && s.CssClass == "type-value");
		annotation.Spans.Should().NotContain(s => s.CssClass != null && s.CssClass.Contains("type-primitive"));
	}

	[Test]
	public void Describe_PrimitiveAlias_HidesCodegenName()
	{
		var builder = CreateBuilder();

		var annotation = builder.Describe(Schema("fixture.ListCreateBody").Properties!["description"]);

		annotation.Text.Should().Be("string · min: 1");
		annotation.Spans.Should().Contain(s => s.Text == "string" && s.CssClass == SchemaHelpers.PrimitiveCssClass);
		annotation.Spans.Should().NotContain(s => s.Text.Contains("Security_Lists", StringComparison.Ordinal));
		annotation.Spans.Should().NotContain(s => s.CssClass != null && s.CssClass.Contains("type-value"));
	}

	[Test]
	public void Describe_CodegenObject_HidesSchemaName()
	{
		var builder = CreateBuilder();

		var annotation = builder.Describe(Schema("fixture.ListCreateBody").Properties!["meta"]);

		annotation.Text.Should().Be("object");
		annotation.Spans.Should().Contain(s => s.Text == "object" && s.CssClass == SchemaHelpers.PrimitiveCssClass);
		annotation.Spans.Should().NotContain(s => s.Text.Contains("Security_Lists", StringComparison.Ordinal));
	}

	[Test]
	public void Describe_CodegenEnum_HidesSchemaName()
	{
		var builder = CreateBuilder();

		var annotation = builder.Describe(Schema("fixture.ListCreateBody").Properties!["type"]);

		annotation.Text.Should().Be("enum");
		annotation.Spans.Should().Contain(s => s.Text == "enum" && s.CssClass == SchemaHelpers.WrapperEnumCssClass);
		annotation.Spans.Should().NotContain(s => s.Text.Contains("Security_Lists", StringComparison.Ordinal));
	}

	[Test]
	public void Describe_NamedEnum_KeepsSchemaName()
	{
		var builder = CreateBuilder();

		var annotation = builder.Describe(Schema("fixture.SearchRequestBody").Properties!["mode"]);

		annotation.Text.Should().Be("enum SearchMode");
	}

	[Test]
	public void Describe_SimpleArrayUnion_SplitsFormulaIntoAtoms()
	{
		var builder = CreateBuilder();

		var annotation = builder.Describe(Schema("fixture.SearchRequestBody").Properties!["fields"]);

		annotation.Text.Should().Be("union Field | [] Field");
		annotation.Spans.Should().Contain(s => s.Text == "union" && s.CssClass == SchemaHelpers.WrapperUnionCssClass);
		annotation.Spans.Should().Contain(s => s.Text == "[]" && s.CssClass == SchemaHelpers.WrapperArrayIconCssClass);
		annotation.Spans.Should().NotContain(s => s.CssClass == "type-object");
	}

	[Test]
	public void Describe_ArrayOfInlineObjects_UsesBracketPrefix()
	{
		var builder = CreateBuilder();

		var annotation = builder.Describe(Schema("_types.aggregations.TermsAggregate").Properties!["buckets"]);

		annotation.Text.Should().Be("[] object");
		annotation.Spans.Should().Contain(s => s.Text == "[]" && s.CssClass == SchemaHelpers.WrapperArrayIconCssClass);
		annotation.Spans.Should().Contain(s => s.Text == "object" && s.CssClass == "type-primitive");
	}

	[Test]
	public void Describe_ArrayOfLinkedType_UsesBracketPrefix()
	{
		var builder = CreateBuilder();

		var annotation = builder.Describe(Schema("_types.query_dsl.BoolQuery").Properties!["must"]);

		annotation.Text.Should().Be("[] {} QueryContainer");
		annotation.Spans.Should().Contain(s => s.Text == "QueryContainer" && s.CssClass == "type-linked");
	}

	[Test]
	public void Describe_LinkedType_MarksNameAsTypeLinked()
	{
		var builder = CreateBuilder();

		var annotation = builder.Describe(Schema("fixture.SearchRequestBody").Properties!["query"]);

		annotation.Spans.Should().Contain(s => s.Text == "QueryContainer" && s.CssClass == "type-linked");
		annotation.Spans.Should().Contain(s => s.Text == "{}" && s.CssClass != null && s.CssClass.Contains("type-wrapper"));
	}

	[Test]
	public void Describe_DictionaryOfLinkedType_SplitsMapFormulaIntoAtoms()
	{
		var builder = CreateBuilder();

		var annotation = builder.Describe(Schema("fixture.SearchRequestBody").Properties!["aggs"]);

		annotation.Text.Should().Be("map string to {} AggregationContainer");
		annotation.Spans.Should().Contain(s => s.Text == "map" && s.CssClass == SchemaHelpers.WrapperMapKeywordCssClass);
		annotation.Spans.Should().Contain(s => s.Text == " to " && s.Bare);
		annotation.Spans.Should().Contain(s => s.Text == "{}" && s.CssClass == SchemaHelpers.WrapperObjectIconCssClass);
		annotation.Spans.Should().Contain(s => s.Text == "AggregationContainer" && s.CssClass == "type-linked");
	}

	[Test]
	public void BuildUnionVariantsForSchemas_TopLevelOneOf_BuildsVariantPerOption()
	{
		var builder = CreateBuilder(currentPageType: "Aggregate", collapseMode: CollapseMode.DepthBased);
		var aggregate = Schema("_types.aggregations.Aggregate");

		var variants = builder.BuildUnionVariantsForSchemas(
			aggregate.OneOf!,
			new PropertyTreeScope { Prefix = "oneof", Ancestors = new HashSet<string> { "Aggregate" } }
		);

		variants.Should().NotBeNull();
		variants!.Variants.Should().HaveCount(2);
		variants.ShouldCollapse.Should().BeFalse();
		variants.Variants.Select(v => v.DisplayName).Should().BeEquivalentTo(["TermsAggregate", "MaxAggregate"]);
	}

	[Test]
	public void BuildUnionVariantsForSchemas_CodegenOneOf_UsesReadableNames()
	{
		var builder = CreateBuilder();
		var schema = Schema("fixture.InvalidInputResponse");

		var variants = builder.BuildUnionVariantsForSchemas(schema.OneOf!, new PropertyTreeScope { Prefix = "res-400" });

		variants.Should().NotBeNull();
		variants!.Variants.Select(v => v.DisplayName).Should().BeEquivalentTo(["PlatformErrorResponse", "SiemErrorResponse"]);
		variants.Variants.Should().AllSatisfy(v => v.ShowProperties.Should().BeTrue());
		variants.ShouldCollapse.Should().BeFalse();
	}

	[Test]
	public void BuildPropertyList_AllOfEnumRef_ShowsEnumValues()
	{
		var builder = CreateBuilder();

		var list = builder.BuildPropertyList(Schema("fixture.EnumAllOfBody"), new PropertyTreeScope { Prefix = "req", IsRequest = true });

		var mode = list!.Items.Single(p => p.Name == "mode");
		mode.EnumValues.Should().BeEquivalentTo(["fast", "accurate"]);
		mode.Type.Spans.Should().Contain(s => s.CssClass == SchemaHelpers.WrapperEnumCssClass && s.Text == "enum");
	}

	[Test]
	public void BuildPropertyList_ArrayOfInlineEnum_ShowsEnumValues()
	{
		var builder = CreateBuilder();

		var list = builder.BuildPropertyList(
			Schema("fixture.InlineArrayEnumBody"),
			new PropertyTreeScope { Prefix = "req", IsRequest = true }
		);

		var group = list!.Items.Single(p => p.Name == "recipient_group");
		group.EnumValues.Should().BeEquivalentTo(["organization-admins", "billing-admins", "resource-viewers"]);
		group.Type.Spans.Should().Contain(s => s.CssClass == SchemaHelpers.WrapperEnumCssClass && s.Text == "enum");
		group.Type.Spans.Should().Contain(s => s.CssClass == SchemaHelpers.WrapperArrayIconCssClass && s.Text == "[]");
		group.Type.Text.Should().Be("[] enum");
	}

	[Test]
	public void BuildPropertyList_InlineEnumOverFiveValues_ShowsAllValues()
	{
		var weekday = EnumShapes().Single(p => p.Name == "weekday");

		weekday.EnumValues.Should().Equal("mon", "tue", "wed", "thu", "fri", "sat", "sun");
	}

	[Test]
	public void BuildPropertyList_OneOfInlineEnums_MergesValuesAsSingleEnum()
	{
		var union = EnumShapes().Single(p => p.Name == "literal_union");

		union.EnumValues.Should().Equal("red", "green", "blue");
		union.Type.Text.Should().Be("enum");
		union.Union.Should().BeNull();
	}

	[Test]
	public void BuildPropertyList_AnyOfEnumOrString_ShowsKnownValues()
	{
		var open = EnumShapes().Single(p => p.Name == "open_enum");

		open.EnumValues.Should().Equal("known_a", "known_b");
		open.Union.Should().BeNull("the Values row already lists the literals");
	}

	[Test]
	public void GetEnumValues_UnionOfValueAndArrayOfSameEnum_DeduplicatesValues()
	{
		var analyzer = new SchemaAnalyzer(fixture.Document);
		var schema = new OpenApiSchema
		{
			AnyOf =
			[
				new OpenApiSchemaReference("_types.SearchMode", fixture.Document),
				new OpenApiSchema { Type = JsonSchemaType.Array, Items = new OpenApiSchemaReference("_types.SearchMode", fixture.Document) }
			]
		};

		analyzer.GetEnumValues(schema).Should().Equal("fast", "accurate");
	}

	[Test]
	public void EnumValueList_OverTwentyValues_FoldsAllButTheFirstTwelve()
	{
		var values = Enumerable.Range(1, 21).Select(i => $"v{i}").ToArray();

		var list = new EnumValueList(values);

		list.Visible.Should().Equal(values.Take(12));
		list.Folded.Should().Equal(values.Skip(12));
	}

	[Test]
	public void EnumValueList_TwentyValuesOrFewer_ShowsAll()
	{
		var values = Enumerable.Range(1, 20).Select(i => $"v{i}").ToArray();

		var list = new EnumValueList(values);

		list.Visible.Should().Equal(values);
		list.Folded.Should().BeEmpty();
	}

	private IReadOnlyList<ApiProperty> EnumShapes() =>
		CreateBuilder().BuildPropertyList(
			Schema("fixture.EnumShapesBody"),
			new PropertyTreeScope { Prefix = "req", IsRequest = true }
		)!.Items;

	[Test]
	public void BuildConstraints_NumericBounds_ProducesLabels()
	{
		var boolQuery = Schema("_types.query_dsl.BoolQuery");

		var constraints = ApiPropertyTreeBuilder.BuildConstraints(boolQuery.Properties!["minimum_should_match"]);

		constraints.Should().ContainSingle(c => c.Text == "min: 0");
	}

	[Test]
	public void Describe_NumericBounds_AppendsMinToType()
	{
		var builder = CreateBuilder();

		var annotation = builder.Describe(Schema("_types.query_dsl.BoolQuery").Properties!["minimum_should_match"]);

		annotation.Text.Should().Be("integer · min: 0");
		annotation.Spans.Should().Contain(s => s.Text == "min: 0" && s.CssClass == SchemaHelpers.ConstraintCssClass);
	}

	[Test]
	public void BuildConstraints_StringAndArrayBounds_UseMinMaxWithoutQualifier()
	{
		var text = new OpenApiSchema { Type = JsonSchemaType.String, MinLength = 1, MaxLength = 50 };
		var items = new OpenApiSchema { Type = JsonSchemaType.Array, MinItems = 1, MaxItems = 100 };

		ApiPropertyTreeBuilder.BuildConstraints(text).Select(c => c.Text).Should().Equal("min: 1", "max: 50");
		ApiPropertyTreeBuilder.BuildConstraints(items).Select(c => c.Text).Should().Equal("min: 1", "max: 100");
	}

	[Test]
	public void BuildConstraints_ExclusiveUniqueAndDefault_UsesShortLabels()
	{
		var schema = new OpenApiSchema
		{
			Type = JsonSchemaType.Number,
			ExclusiveMinimum = "0",
			ExclusiveMaximum = "100",
			UniqueItems = true,
			MultipleOf = 5,
			Pattern = @"[smdh]$",
			Default = "1m"
		};

		ApiPropertyTreeBuilder
			.BuildConstraints(schema)
			.Select(c => c.Text)
			.Should()
			.Equal("> 0", "< 100", "unique", "× 5", "pattern: [smdh]$", "default: 1m");
	}

	[Test]
	public async Task DescribePathParameter_StringOrStringArray_ShowsBothAlternatives()
	{
		var json =
			"""
			{
			  "openapi": "3.0.3",
			  "info": { "title": "t", "version": "1" },
			  "paths": {},
			  "components": {
			    "schemas": {
			      "_types.Name": { "type": "string" },
			      "_types.Names": {
			        "oneOf": [
			          { "$ref": "#/components/schemas/_types.Name" },
			          { "type": "array", "items": { "$ref": "#/components/schemas/_types.Name" } }
			        ]
			      },
			      "_types.DataStreamName": { "type": "string" },
			      "_types.DataStreamNames": {
			        "oneOf": [
			          { "$ref": "#/components/schemas/_types.DataStreamName" },
			          { "type": "array", "items": { "$ref": "#/components/schemas/_types.DataStreamName" } }
			        ]
			      },
			      "inline.StringOrArray": {
			        "oneOf": [
			          { "type": "string" },
			          { "type": "array", "items": { "type": "string" } }
			        ]
			      }
			    }
			  }
			}
			""";
		var document = await LoadSpecAsync(json);
		var builder = BuilderFor(document);

		var names = new OpenApiSchemaReference("_types.Names", document);
		var dataStreams = new OpenApiSchemaReference("_types.DataStreamNames", document);
		var name = new OpenApiSchemaReference("_types.Name", document);

		builder.Describe(names).Text.Should().Be("union Names");
		builder.DescribePathParameter(names).Text.Should().Be("union Name | [] Name");
		builder.DescribePathParameter(dataStreams).Text.Should().Be("union string | [] string");
		builder.DescribePathParameter(document.Components!.Schemas!["inline.StringOrArray"]).Text.Should().Be("union string | [] string");
		builder.DescribePathParameter(name).Text.Should().Be("string Name");
		builder.Describe(name).Text.Should().Be("string Name");
		builder.DescribePathParameter(new OpenApiSchema { Type = JsonSchemaType.String }).Text.Should().Be("string");
	}

	[Test]
	public async Task BuildPropertyList_UnionWithOwnProperties_KeepsListingTheSharedProperties()
	{
		var json =
			"""
			{
			  "openapi": "3.0.3",
			  "info": { "title": "t", "version": "1" },
			  "paths": {},
			  "components": {
			    "schemas": {
			      "Cat": { "type": "object", "properties": { "lives": { "type": "integer" } } },
			      "Dog": { "type": "object", "properties": { "barks": { "type": "boolean" } } },
			      "Holder": {
			        "type": "object",
			        "properties": {
			          "pet": {
			            "type": "object",
			            "properties": { "kind": { "type": "string" } },
			            "oneOf": [ { "$ref": "#/components/schemas/Cat" }, { "$ref": "#/components/schemas/Dog" } ]
			          }
			        }
			      }
			    }
			  }
			}
			""";
		var document = await LoadSpecAsync(json);
		var builder = BuilderFor(document);

		var list = builder.BuildPropertyList(document.Components!.Schemas!["Holder"], new PropertyTreeScope { Prefix = "" });

		var pet = list!.Items.Single(p => p.Name == "pet");
		pet.Children.Kind.Should().Be(ChildKind.PropertyList);
		pet.Children.Properties!.Items.Select(p => p.Name).Should().Equal("kind");
	}

	[Test]
	public async Task BuildPropertyList_AllOfVariantRedefinesBaseProperty_VariantDefinitionWins()
	{
		var json =
			"""
			{
			  "openapi": "3.0.3",
			  "info": { "title": "t", "version": "1" },
			  "paths": {},
			  "components": {
			    "schemas": {
			      "Base": { "type": "object", "properties": { "kind": { "type": "string" }, "id": { "type": "string" } } },
			      "Cat": { "type": "object", "description": "A cat.", "properties": { "kind": { "type": "string", "enum": ["cat"] } } },
			      "Dog": { "type": "object", "properties": { "barks": { "type": "boolean" } } },
			      "Holder": {
			        "type": "object",
			        "properties": {
			          "pet": {
			            "allOf": [
			              { "$ref": "#/components/schemas/Base" },
			              { "oneOf": [ { "$ref": "#/components/schemas/Cat" }, { "$ref": "#/components/schemas/Dog" } ] }
			            ]
			          }
			        }
			      }
			    }
			  }
			}
			""";
		var document = await LoadSpecAsync(json);
		var builder = BuilderFor(document);

		var list = builder.BuildPropertyList(document.Components!.Schemas!["Holder"], new PropertyTreeScope { Prefix = "" });

		var variants = list!.Items.Single(p => p.Name == "pet").Children.Variants!.Variants;
		var cat = variants[0].Properties!.Items;
		cat.Select(p => p.Name).Should().Equal("kind", "id");
		cat.Single(p => p.Name == "kind").EnumValues.Should().Equal("cat");
		variants[1].Properties!.Items.Single(p => p.Name == "kind").EnumValues.Should().BeEmpty();
	}

	[Test]
	public async Task BuildPropertyList_AllOfWithAnyOfMember_ExpandsVariantsKeepingRequiredFromBaseAndVariant()
	{
		var json =
			"""
			{
			  "openapi": "3.0.3",
			  "info": { "title": "t", "version": "1" },
			  "paths": {},
			  "components": {
			    "schemas": {
			      "Base": { "type": "object", "required": ["id"], "properties": { "id": { "type": "string" } } },
			      "Cat": { "type": "object", "required": ["lives"], "properties": { "lives": { "type": "integer" } } },
			      "Dog": { "type": "object", "properties": { "barks": { "type": "boolean" } } },
			      "Holder": {
			        "type": "object",
			        "properties": {
			          "pet": {
			            "allOf": [
			              { "$ref": "#/components/schemas/Base" },
			              { "anyOf": [ { "$ref": "#/components/schemas/Cat" }, { "$ref": "#/components/schemas/Dog" } ] }
			            ]
			          }
			        }
			      }
			    }
			  }
			}
			""";
		var document = await LoadSpecAsync(json);
		var builder = BuilderFor(document);

		var list = builder.BuildPropertyList(document.Components!.Schemas!["Holder"], new PropertyTreeScope { Prefix = "" });

		var pet = list!.Items.Single(p => p.Name == "pet");
		pet.Children.Kind.Should().Be(ChildKind.UnionVariants);
		var variants = pet.Children.Variants!.Variants;
		variants.Select(v => v.DisplayName).Should().Equal("Cat", "Dog");
		variants[0].Properties!.Items.Where(p => p.IsRequired).Select(p => p.Name).Should().Equal("id", "lives");
		variants[1].Properties!.Items.Where(p => p.IsRequired).Select(p => p.Name).Should().Equal("id");
	}

	[Test]
	public async Task BuildPropertyList_AllOfWithOneOfMember_ExpandsVariantsSharingBaseProperties()
	{
		var json =
			"""
			{
			  "openapi": "3.0.3",
			  "info": { "title": "t", "version": "1" },
			  "paths": {},
			  "components": {
			    "schemas": {
			      "Base": { "type": "object", "properties": { "id": { "type": "string" } } },
			      "Cat": { "type": "object", "properties": { "lives": { "type": "integer" } } },
			      "Dog": { "type": "object", "properties": { "barks": { "type": "boolean" } } },
			      "Holder": {
			        "type": "object",
			        "properties": {
			          "pet": {
			            "allOf": [
			              { "$ref": "#/components/schemas/Base" },
			              { "oneOf": [ { "$ref": "#/components/schemas/Cat" }, { "$ref": "#/components/schemas/Dog" } ] }
			            ]
			          },
			          "wrapped": {
			            "allOf": [
			              { "oneOf": [ { "$ref": "#/components/schemas/Cat" }, { "$ref": "#/components/schemas/Dog" } ] }
			            ]
			          }
			        }
			      }
			    }
			  }
			}
			""";
		var document = await LoadSpecAsync(json);
		var builder = BuilderFor(document);

		var list = builder.BuildPropertyList(document.Components!.Schemas!["Holder"], new PropertyTreeScope { Prefix = "" });

		var pet = list!.Items.Single(p => p.Name == "pet");
		pet.Children.Kind.Should().Be(ChildKind.UnionVariants);
		var variants = pet.Children.Variants!.Variants;
		variants.Select(v => v.DisplayName).Should().Equal("Cat", "Dog");
		variants[0].Properties!.Items.Select(p => p.Name).Should().Equal("id", "lives");
		variants[1].Properties!.Items.Select(p => p.Name).Should().Equal("id", "barks");

		var wrapped = list.Items.Single(p => p.Name == "wrapped");
		wrapped.Children.Kind.Should().Be(ChildKind.None, "an allOf with no property-bearing base keeps its previous rendering");
	}

	[Test]
	public async Task BuildPropertyList_AllOfWithSeveralRefs_ListsTheOtherSchemasAsAlsoIncludes()
	{
		var json =
			"""
			{
			  "openapi": "3.0.3",
			  "info": { "title": "t", "version": "1" },
			  "paths": {},
			  "components": {
			    "schemas": {
			      "Base": { "type": "object", "properties": { "id": { "type": "string" } } },
			      "Timestamps": { "type": "object", "properties": { "created": { "type": "string" } } },
			      "Mode": { "type": "string", "enum": ["a", "b"] },
			      "Holder": {
			        "type": "object",
			        "properties": {
			          "merged": {
			            "allOf": [
			              { "$ref": "#/components/schemas/Base" },
			              { "$ref": "#/components/schemas/Timestamps" },
			              { "$ref": "#/components/schemas/Mode" }
			            ]
			          },
			          "repeated": {
			            "allOf": [
			              { "$ref": "#/components/schemas/Base" },
			              { "$ref": "#/components/schemas/Base" },
			              { "$ref": "#/components/schemas/Timestamps" }
			            ]
			          },
			          "single": { "allOf": [ { "$ref": "#/components/schemas/Base" } ] }
			        }
			      }
			    }
			  }
			}
			""";
		var document = await LoadSpecAsync(json);
		var builder = BuilderFor(document);

		var list = builder.BuildPropertyList(document.Components!.Schemas!["Holder"], new PropertyTreeScope { Prefix = "" });

		var merged = list!.Items.Single(p => p.Name == "merged");
		merged.AlsoIncludes.Select(t => t.TypeName).Should().Equal("Timestamps");
		list.Items.Single(p => p.Name == "repeated").AlsoIncludes.Select(t => t.TypeName).Should().Equal("Timestamps");
		list.Items.Single(p => p.Name == "single").AlsoIncludes.Should().BeEmpty();
	}

	[Test]
	public async Task BuildPropertyList_DiscriminatedOneOf_LabelsVariantsWithTheirDiscriminatorValue()
	{
		var json =
			"""
			{
			  "openapi": "3.0.3",
			  "info": { "title": "t", "version": "1" },
			  "paths": {},
			  "components": {
			    "schemas": {
			      "Base": { "type": "object", "properties": { "id": { "type": "string" } } },
			      "Cat": { "type": "object", "properties": { "kind": { "type": "string", "enum": ["cat"] }, "lives": { "type": "integer" } } },
			      "Dog": { "type": "object", "properties": { "kind": { "type": "string", "enum": ["dog"] }, "barks": { "type": "boolean" } } },
			      "Fish": { "type": "object", "properties": { "kind": { "type": "string" }, "fins": { "type": "integer" } } },
			      "Pet": {
			        "oneOf": [ { "$ref": "#/components/schemas/Cat" }, { "$ref": "#/components/schemas/Dog" } ],
			        "discriminator": { "propertyName": "kind", "mapping": { "tomcat": "#/components/schemas/Cat" } }
			      },
			      "Holder": {
			        "type": "object",
			        "properties": {
			          "mapped": {
			            "oneOf": [ { "$ref": "#/components/schemas/Cat" }, { "$ref": "#/components/schemas/Dog" } ],
			            "discriminator": {
			              "propertyName": "kind",
			              "mapping": { "feline": "#/components/schemas/Cat", "canine": "#/components/schemas/Dog" }
			            }
			          },
			          "implicit": {
			            "oneOf": [ { "$ref": "#/components/schemas/Cat" }, { "$ref": "#/components/schemas/Fish" } ],
			            "discriminator": { "propertyName": "kind" }
			          },
			          "composed": {
			            "allOf": [
			              { "$ref": "#/components/schemas/Base" },
			              {
			                "oneOf": [ { "$ref": "#/components/schemas/Cat" }, { "$ref": "#/components/schemas/Dog" } ],
			                "discriminator": {
			                  "propertyName": "kind",
			                  "mapping": { "feline": "#/components/schemas/Cat", "canine": "#/components/schemas/Dog" }
			                }
			              }
			            ]
			          },
			          "plain": {
			            "oneOf": [ { "$ref": "#/components/schemas/Cat" }, { "$ref": "#/components/schemas/Dog" } ]
			          }
			        }
			      }
			    }
			  }
			}
			""";
		var document = await LoadSpecAsync(json);
		var builder = BuilderFor(document);

		var list = builder.BuildPropertyList(document.Components!.Schemas!["Holder"], new PropertyTreeScope { Prefix = "" });

		List<string?> Labels(string name) =>
			list!.Items.Single(p => p.Name == name).Children.Variants!.Variants.Select(v => v.DiscriminatorLabel).ToList();

		Labels("mapped").Should().Equal("kind: feline", "kind: canine");
		Labels("implicit").Should().Equal("kind: cat", "kind: Fish");
		Labels("plain").Should().Equal(null, null);

		Labels("composed").Should().Equal("kind: feline", "kind: canine");
		list!.Items.Single(p => p.Name == "composed").Union!.DiscriminatorProperty.Should().Be("kind");

		var holder = new OpenApiSchemaReference("Pet", document);
		var pet = document.Components!.Schemas!["Pet"];
		var topLevel = builder.BuildUnionVariantsForSchemas(pet.OneOf!, new PropertyTreeScope { Prefix = "oneof" }, holder.Discriminator);
		topLevel!.Variants.Select(v => v.DiscriminatorLabel).Should().Equal("kind: tomcat", "kind: dog");
	}

	[Test]
	public async Task BuildPropertyList_AnyOfUnion_LabelsTheRowAnyOfAndOneOfUnionsOneOf()
	{
		var json =
			"""
			{
			  "openapi": "3.0.3",
			  "info": { "title": "t", "version": "1" },
			  "paths": {},
			  "components": {
			    "schemas": {
			      "Cat": { "type": "object", "properties": { "lives": { "type": "integer" } } },
			      "Dog": { "type": "object", "properties": { "barks": { "type": "boolean" } } },
			      "Flexible": { "anyOf": [ { "$ref": "#/components/schemas/Cat" }, { "$ref": "#/components/schemas/Dog" } ] },
			      "Holder": {
			        "type": "object",
			        "properties": {
			          "inlineAny": { "anyOf": [ { "$ref": "#/components/schemas/Cat" }, { "$ref": "#/components/schemas/Dog" } ] },
			          "inlineOne": { "oneOf": [ { "$ref": "#/components/schemas/Cat" }, { "$ref": "#/components/schemas/Dog" } ] },
			          "refAny": { "$ref": "#/components/schemas/Flexible" }
			        }
			      }
			    }
			  }
			}
			""";
		var document = await LoadSpecAsync(json);
		var builder = BuilderFor(document);

		var list = builder.BuildPropertyList(document.Components!.Schemas!["Holder"], new PropertyTreeScope { Prefix = "" });

		string? Label(string name) => list!.Items.Single(p => p.Name == name).Union?.Label;

		Label("inlineAny").Should().Be("Any of:");
		Label("inlineOne").Should().Be("One of:");
		Label("refAny").Should().Be("Any of:");
	}

	[Test]
	public async Task VariantNames_ArrayAndPlainVariantOfOneSchema_StayDistinct()
	{
		var json =
			"""
			{
			  "openapi": "3.0.3",
			  "info": { "title": "t", "version": "1" },
			  "paths": {},
			  "components": {
			    "schemas": {
			      "Cat": { "type": "object", "properties": { "lives": { "type": "integer" } } },
			      "Body": {
			        "oneOf": [
			          { "$ref": "#/components/schemas/Cat" },
			          { "type": "array", "items": { "$ref": "#/components/schemas/Cat" } }
			        ]
			      }
			    }
			  }
			}
			""";
		var document = await LoadSpecAsync(json);
		var builder = BuilderFor(document);

		var variants = ApiBodyContent.BuildUnionVariants(
			new OpenApiSchemaReference("Body", document),
			new PropertyTreeScope { Prefix = "req", IsRequest = true },
			new SchemaAnalyzer(document),
			builder
		);

		variants!.Variants.Select(v => v.DisplayName).Should().Equal("Cat", "Cat");
		ApiBodyContent.VariantNames(variants).Should().Equal("Cat[]", "Cat");
	}

	[Test]
	public async Task BuildTopLevelUnionVariants_RequestBodyAnyOf_ExpandsVariantsWithTheirProperties()
	{
		var json =
			"""
			{
			  "openapi": "3.0.3",
			  "info": { "title": "t", "version": "1" },
			  "paths": {},
			  "components": {
			    "schemas": {
			      "Cat": { "type": "object", "description": "A cat.\n\nSecond paragraph.", "required": ["lives"], "properties": { "lives": { "type": "integer" } } },
			      "Dog": { "type": "object", "properties": { "barks": { "type": "boolean" } } },
			      "Body": { "anyOf": [ { "$ref": "#/components/schemas/Cat" }, { "$ref": "#/components/schemas/Dog" } ] },
			      "Plain": { "type": "object", "properties": { "name": { "type": "string" } } }
			    }
			  }
			}
			""";
		var document = await LoadSpecAsync(json);
		var analyzer = new SchemaAnalyzer(document);
		var builder = BuilderFor(document);
		var body = new OpenApiSchemaReference("Body", document);
		var plain = new OpenApiSchemaReference("Plain", document);

		builder.BuildPropertyList(body, new PropertyTreeScope { Prefix = "req", IsRequest = true }).Should().BeNull();
		var variants = ApiBodyContent.BuildUnionVariants(
			body,
			new PropertyTreeScope { Prefix = "req", IsRequest = true },
			analyzer,
			builder
		);

		variants!.Variants.Select(v => v.DisplayName).Should().Equal("Cat", "Dog");
		variants.Variants[0].AnchorId.Should().StartWith("req-variant-");
		variants.Variants[0].Properties!.Items.Single().IsRequired.Should().BeTrue();
		variants.Variants[0].Properties!.Items.Single().IsRequest.Should().BeTrue("request variants keep the request flag");
		variants.Label.Should().Be("Any of:");
		variants.Variants[0].DescriptionMarkdown.Should().Be("A cat.", "only the first paragraph is shown");
		variants.Variants[1].DescriptionMarkdown.Should().BeNull();
		var markdown = new System.Text.StringBuilder();
		ApiPropertyMarkdown.WriteVariants(markdown, variants, "/api/doc/fixture");
		markdown.ToString().Should().StartWith("Any of:").And.Contain("- `lives` (integer) — required");
		ApiBodyContent.BuildUnionVariants(plain, new PropertyTreeScope { Prefix = "req" }, analyzer, builder).Should().BeNull();
	}

	[Test]
	public async Task BuildTopLevelUnionVariants_DescriptionWithCrlfParagraphs_KeepsOnlyTheFirstParagraph()
	{
		var json =
			"""
			{
			  "openapi": "3.0.3",
			  "info": { "title": "t", "version": "1" },
			  "paths": {},
			  "components": {
			    "schemas": {
			      "Cat": { "type": "object", "description": "A cat.\r\n\r\nSecond paragraph.", "properties": { "lives": { "type": "integer" } } },
			      "Dog": { "type": "object", "description": "A dog.\n\nSecond paragraph.", "properties": { "barks": { "type": "boolean" } } },
			      "Body": { "oneOf": [ { "$ref": "#/components/schemas/Cat" }, { "$ref": "#/components/schemas/Dog" } ] }
			    }
			  }
			}
			""";
		var document = await LoadSpecAsync(json);
		var builder = BuilderFor(document);

		var variants = ApiBodyContent.BuildUnionVariants(
			new OpenApiSchemaReference("Body", document),
			new PropertyTreeScope { Prefix = "res-200" },
			new SchemaAnalyzer(document),
			builder
		);

		variants!.Variants.Select(v => v.DescriptionMarkdown).Should().Equal("A cat.", "A dog.");
	}

	[Test]
	public async Task BuildPropertyList_UnionMemberThatIsAMap_ListsTheValuePropertiesUnderAKeyRow()
	{
		var json =
			"""
			{
			  "openapi": "3.0.3",
			  "info": { "title": "t", "version": "1" },
			  "paths": {},
			  "components": {
			    "schemas": {
			      "Input": { "type": "object", "properties": { "enabled": { "type": "boolean" }, "vars": { "type": "object" } } },
			      "Holder": {
			        "type": "object",
			        "properties": {
			          "inputs": {
			            "anyOf": [
			              { "type": "array", "items": { "$ref": "#/components/schemas/Input" } },
			              { "type": "object", "additionalProperties": { "$ref": "#/components/schemas/Input" } }
			            ]
			          },
			          "query": {
			            "anyOf": [ { "type": "string" }, { "type": "object", "additionalProperties": {} } ]
			          }
			        }
			      }
			    }
			  }
			}
			""";
		var document = await LoadSpecAsync(json);
		var builder = BuilderFor(document);

		var list = builder.BuildPropertyList(document.Components!.Schemas!["Holder"], new PropertyTreeScope { Prefix = "" });

		var inputs = list!.Items.Single(p => p.Name == "inputs");
		inputs.Children.Kind.Should().Be(ChildKind.UnionVariants);
		var map = inputs.Children.Variants!.Variants.Single(v => v.Properties?.Items.Any(p => p.Name == "<string>") == true);
		var keyRow = map.Properties!.Items.Single();
		keyRow.AnchorId.Should().EndWith("-string", "the id stays free of angle brackets");
		keyRow.Children.Properties!.Items.Select(p => p.Name).Should().Equal("enabled", "vars");

		list.Items.Single(p => p.Name == "query").Children.Kind.Should().Be(ChildKind.None, "a map of anything has no properties to list");
	}

	[Test]
	public async Task BuildPropertyList_UnionOfArrays_ListsThePropertiesOfTheObjectItems()
	{
		var json =
			"""
			{
			  "openapi": "3.0.3",
			  "info": { "title": "t", "version": "1" },
			  "paths": {},
			  "components": {
			    "schemas": {
			      "Policy": { "type": "object", "properties": { "name": { "type": "string" }, "inputs": { "type": "object" } } },
			      "Holder": {
			        "type": "object",
			        "properties": {
			          "policies": {
			            "anyOf": [
			              { "type": "array", "items": { "type": "string" } },
			              { "type": "array", "items": { "$ref": "#/components/schemas/Policy" } }
			            ]
			          }
			        }
			      }
			    }
			  }
			}
			""";
		var document = await LoadSpecAsync(json);
		var builder = BuilderFor(document);

		var list = builder.BuildPropertyList(document.Components!.Schemas!["Holder"], new PropertyTreeScope { Prefix = "" });

		var variants = list!.Items.Single(p => p.Name == "policies").Children.Variants!.Variants;
		var policy = variants.Single(v => v.DisplayName == "Policy");
		policy.IsArrayVariant.Should().BeTrue();
		policy.ShowProperties.Should().BeTrue("an array-only variant lists the properties of its items");
		policy.Properties!.Items.Select(p => p.Name).Should().Equal("name", "inputs");
		variants.Single(v => v.DisplayName == "string").Properties.Should().BeNull();
	}

	[Test]
	public async Task BuildTopLevelUnionVariants_AllOfUnionWithDiscriminatorMapping_KeepsNamesAndMappedLabels()
	{
		var json =
			"""
			{
			  "openapi": "3.0.3",
			  "info": { "title": "t", "version": "1" },
			  "paths": {},
			  "components": {
			    "schemas": {
			      "Base": { "type": "object", "properties": { "id": { "type": "string" } } },
			      "Cat": { "type": "object", "properties": { "kind": { "type": "string" }, "lives": { "type": "integer" } } },
			      "Dog": { "type": "object", "properties": { "kind": { "type": "string" }, "barks": { "type": "boolean" } } },
			      "Body": {
			        "allOf": [
			          { "$ref": "#/components/schemas/Base" },
			          {
			            "oneOf": [ { "$ref": "#/components/schemas/Cat" }, { "$ref": "#/components/schemas/Dog" } ],
			            "discriminator": {
			              "propertyName": "kind",
			              "mapping": { "feline": "#/components/schemas/Cat", "canine": "#/components/schemas/Dog" }
			            }
			          }
			        ]
			      }
			    }
			  }
			}
			""";
		var document = await LoadSpecAsync(json);
		var builder = BuilderFor(document);

		var variants = ApiBodyContent.BuildUnionVariants(
			document.Components!.Schemas!["Body"],
			new PropertyTreeScope { Prefix = "req", IsRequest = true },
			new SchemaAnalyzer(document),
			builder
		);

		variants!.Variants.Select(v => v.DisplayName).Should().Equal("Cat", "Dog");
		variants.Variants.Select(v => v.DiscriminatorLabel).Should().Equal("kind: feline", "kind: canine");
		variants.Variants[0].Properties!.Items.Select(p => p.Name).Should().Equal("id", "kind", "lives");
	}

	[Test]
	public async Task BuildPropertyList_UnionWithInlineObjectMembers_ExpandsEachOneSeparately()
	{
		var json =
			"""
			{
			  "openapi": "3.0.3",
			  "info": { "title": "t", "version": "1" },
			  "paths": {},
			  "components": {
			    "schemas": {
			      "Named": {
			        "oneOf": [
			          { "type": "string" },
			          { "type": "object", "title": "ByQuery", "properties": { "query": { "type": "string" } } },
			          { "type": "object", "properties": { "ids": { "type": "array", "items": { "type": "string" } } } },
			          { "type": "object", "properties": { "all": { "type": "boolean" } } }
			        ]
			      },
			      "Holder": {
			        "type": "object",
			        "properties": { "selector": { "$ref": "#/components/schemas/Named" } }
			      }
			    }
			  }
			}
			""";
		var document = await LoadSpecAsync(json);
		var builder = BuilderFor(document);

		var list = builder.BuildPropertyList(document.Components!.Schemas!["Holder"], new PropertyTreeScope { Prefix = "" });

		var selector = list!.Items.Single(p => p.Name == "selector");
		selector.Children.Kind.Should().Be(ChildKind.UnionVariants);
		var variants = selector.Children.Variants!.Variants;
		variants.Select(v => v.DisplayName).Should().Equal("string", "ByQuery", "{ ids }", "{ all }");
		variants
			.Select(v => v.AnchorId)
			.Should()
			.Equal("selector-variant-string", "selector-variant-byquery", "selector-variant-object", "selector-variant-object-2");
		variants.Select(v => v.Properties?.Items.Single().Name).Should().Equal(null, "query", "ids", "all");
		variants.Select(v => v.AnchorId).Should().OnlyHaveUniqueItems();
	}

	[Test]
	public async Task BuildPropertyList_RefToAllOfUnion_ExpandsTheVariantsWithLabels()
	{
		var json =
			"""
			{
			  "openapi": "3.0.3",
			  "info": { "title": "t", "version": "1" },
			  "paths": {},
			  "components": {
			    "schemas": {
			      "Base": { "type": "object", "required": ["id"], "properties": { "id": { "type": "string" } } },
			      "Cat": { "type": "object", "properties": { "kind": { "type": "string" }, "lives": { "type": "integer" } } },
			      "Dog": { "type": "object", "properties": { "kind": { "type": "string" }, "barks": { "type": "boolean" } } },
			      "Pet": {
			        "allOf": [
			          { "$ref": "#/components/schemas/Base" },
			          {
			            "oneOf": [ { "$ref": "#/components/schemas/Cat" }, { "$ref": "#/components/schemas/Dog" } ],
			            "discriminator": {
			              "propertyName": "kind",
			              "mapping": { "feline": "#/components/schemas/Cat", "canine": "#/components/schemas/Dog" }
			            }
			          }
			        ]
			      },
			      "Holder": { "type": "object", "properties": { "pet": { "$ref": "#/components/schemas/Pet" } } }
			    }
			  }
			}
			""";
		var document = await LoadSpecAsync(json);
		var builder = BuilderFor(document);

		var pet = builder.BuildPropertyList(document.Components!.Schemas!["Holder"], new PropertyTreeScope { Prefix = "" })!.Items.Single(
			p => p.Name == "pet"
		);

		pet.Children.Kind.Should().Be(ChildKind.UnionVariants);
		var variants = pet.Children.Variants!.Variants;
		variants.Select(v => v.DisplayName).Should().Equal("Cat", "Dog");
		variants.Select(v => v.DiscriminatorLabel).Should().Equal("kind: feline", "kind: canine");
		variants[0].Properties!.Items.Where(p => p.IsRequired).Select(p => p.Name).Should().Equal("id");

		var body = ApiBodyContent.BuildUnionVariants(
			new OpenApiSchemaReference("Pet", document),
			new PropertyTreeScope { Prefix = "req", IsRequest = true },
			new SchemaAnalyzer(document),
			builder
		);
		body!.Variants.Select(v => v.DisplayName).Should().Equal("Cat", "Dog");
	}

	[Test]
	public async Task GetTypeInfo_RefToAllOfUnionThatRefersBackToItself_DoesNotRecurseForever()
	{
		var json =
			"""
			{
			  "openapi": "3.0.3",
			  "info": { "title": "t", "version": "1" },
			  "paths": {},
			  "components": {
			    "schemas": {
			      "Leaf": { "type": "object", "properties": { "value": { "type": "string" } } },
			      "Node": {
			        "allOf": [
			          { "type": "object", "properties": { "name": { "type": "string" } } },
			          { "oneOf": [ { "$ref": "#/components/schemas/Leaf" }, { "$ref": "#/components/schemas/Node" } ] }
			        ]
			      },
			      "Holder": { "type": "object", "properties": { "node": { "$ref": "#/components/schemas/Node" } } }
			    }
			  }
			}
			""";
		var document = await LoadSpecAsync(json);

		var info = new SchemaAnalyzer(document).GetTypeInfo(new OpenApiSchemaReference("Node", document));

		info.IsUnion.Should().BeTrue();
		info.UnionOptions!.Select(o => o.Name).Should().Equal("Leaf", "Node");
		var node = BuilderFor(document).BuildPropertyList(
			document.Components!.Schemas!["Holder"],
			new PropertyTreeScope { Prefix = "" }
		)!.Items.Single();
		node.Children.Kind.Should().Be(ChildKind.UnionVariants);
	}

	[Test]
	public async Task BuildPropertyList_AllOfUnionWhoseBaseIsAlsoAMap_KeepsTheMapValuesOnEachVariant()
	{
		var json =
			"""
			{
			  "openapi": "3.0.3",
			  "info": { "title": "t", "version": "1" },
			  "paths": {},
			  "components": {
			    "schemas": {
			      "Input": { "type": "object", "properties": { "enabled": { "type": "boolean" } } },
			      "Cat": { "type": "object", "properties": { "lives": { "type": "integer" } } },
			      "Dog": { "type": "object", "properties": { "barks": { "type": "boolean" } } },
			      "Holder": {
			        "type": "object",
			        "properties": {
			          "pet": {
			            "allOf": [
			              {
			                "type": "object",
			                "properties": { "id": { "type": "string" } },
			                "additionalProperties": { "$ref": "#/components/schemas/Input" }
			              },
			              { "oneOf": [ { "$ref": "#/components/schemas/Cat" }, { "$ref": "#/components/schemas/Dog" } ] }
			            ]
			          }
			        }
			      }
			    }
			  }
			}
			""";
		var document = await LoadSpecAsync(json);

		var variants = BuilderFor(document).BuildPropertyList(
			document.Components!.Schemas!["Holder"],
			new PropertyTreeScope { Prefix = "" }
		)!.Items.Single(p => p.Name == "pet").Children.Variants!.Variants;

		variants.Select(v => v.DisplayName).Should().Equal("Cat", "Dog");
		var cat = variants[0].Properties!.Items;
		cat.Select(p => p.Name).Should().Equal("id", "lives", "<string>");
		cat.Single(p => p.Name == "<string>").Children.Properties!.Items.Select(p => p.Name).Should().Equal("enabled");
	}

	[Test]
	public async Task BuildBodyContent_AllOfUnionWithBaseProperties_ListsTheVariantsNotJustTheBase()
	{
		var json =
			"""
			{
			  "openapi": "3.0.3",
			  "info": { "title": "t", "version": "1" },
			  "paths": {},
			  "components": {
			    "schemas": {
			      "Base": { "type": "object", "properties": { "id": { "type": "string" } } },
			      "Cat": { "type": "object", "properties": { "lives": { "type": "integer" } } },
			      "Dog": { "type": "object", "properties": { "barks": { "type": "boolean" } } },
			      "Composed": {
			        "allOf": [
			          { "$ref": "#/components/schemas/Base" },
			          { "oneOf": [ { "$ref": "#/components/schemas/Cat" }, { "$ref": "#/components/schemas/Dog" } ] }
			        ]
			      },
			      "Declared": {
			        "type": "object",
			        "properties": { "kind": { "type": "string" } },
			        "oneOf": [ { "$ref": "#/components/schemas/Cat" }, { "$ref": "#/components/schemas/Dog" } ]
			      }
			    }
			  }
			}
			""";
		var document = await LoadSpecAsync(json);
		var builder = BuilderFor(document);
		var analyzer = new SchemaAnalyzer(document);

		var (properties, variants) = ApiBodyContent.Build(
			document.Components!.Schemas!["Composed"],
			new PropertyTreeScope { Prefix = "req", IsRequest = true },
			analyzer,
			builder
		);
		properties.Should().BeNull();
		variants!.Variants.Select(v => v.DisplayName).Should().Equal("Cat", "Dog");
		variants.Variants[0].Properties!.Items.Select(p => p.Name).Should().Equal("id", "lives");

		var (byReference, _) = ApiBodyContent.Build(
			new OpenApiSchemaReference("Composed", document),
			new PropertyTreeScope { Prefix = "res-200" },
			analyzer,
			builder
		);
		byReference.Should().BeNull("a $ref to the same union lists its variants too");

		var (declared, declaredVariants) = ApiBodyContent.Build(
			document.Components!.Schemas!["Declared"],
			new PropertyTreeScope { Prefix = "res-200" },
			analyzer,
			builder
		);
		declared!.Items.Select(p => p.Name).Should().Equal("kind");
		declaredVariants.Should().BeNull();
	}

	[Test]
	public async Task BuildPropertyList_DiscriminatorWithoutMappingOrEnum_LabelsVariantsWithTheirSchemaName()
	{
		var json =
			"""
			{
			  "openapi": "3.0.3",
			  "info": { "title": "t", "version": "1" },
			  "paths": {},
			  "components": {
			    "schemas": {
			      "Cat": { "type": "object", "properties": { "kind": { "type": "string" }, "lives": { "type": "integer" } } },
			      "Dog": { "type": "object", "properties": { "kind": { "type": "string" }, "barks": { "type": "boolean" } } },
			      "Pup": { "type": "object", "properties": { "kind": { "type": "string", "enum": ["dog", "puppy"] } } },
			      "Holder": {
			        "type": "object",
			        "properties": {
			          "enumerated": {
			            "oneOf": [ { "$ref": "#/components/schemas/Cat" }, { "$ref": "#/components/schemas/Pup" } ],
			            "discriminator": { "propertyName": "kind" }
			          },
			          "implicit": {
			            "oneOf": [ { "$ref": "#/components/schemas/Cat" }, { "$ref": "#/components/schemas/Dog" } ],
			            "discriminator": { "propertyName": "kind" }
			          },
			          "partial": {
			            "oneOf": [ { "$ref": "#/components/schemas/Cat" }, { "$ref": "#/components/schemas/Dog" } ],
			            "discriminator": { "propertyName": "kind", "mapping": { "feline": "#/components/schemas/Cat" } }
			          }
			        }
			      }
			    }
			  }
			}
			""";
		var document = await LoadSpecAsync(json);

		var list = BuilderFor(document).BuildPropertyList(document.Components!.Schemas!["Holder"], new PropertyTreeScope { Prefix = "" });

		List<string?> Labels(string name) =>
			list!.Items.Single(p => p.Name == name).Children.Variants!.Variants.Select(v => v.DiscriminatorLabel).ToList();

		Labels("implicit").Should().Equal("kind: Cat", "kind: Dog");
		Labels("partial").Should().Equal("kind: feline", "kind: Dog");
		Labels("enumerated").Should().Equal("kind: Cat", "kind: dog | puppy");
	}

	[Test]
	public async Task BuildPropertyList_TypeArrayWithSeveralTypes_ShowsEveryType()
	{
		var json =
			"""
			{
			  "openapi": "3.1.0",
			  "info": { "title": "t", "version": "1" },
			  "paths": {},
			  "components": {
			    "schemas": {
			      "Holder": {
			        "type": "object",
			        "properties": {
			          "defaultValue": { "type": ["boolean", "string"] },
			          "size": { "type": ["number", "string"] },
			          "nullableName": { "type": ["string", "null"] }
			        }
			      }
			    }
			  }
			}
			""";
		var document = await LoadSpecAsync(json);

		var list = BuilderFor(document).BuildPropertyList(document.Components!.Schemas!["Holder"], new PropertyTreeScope { Prefix = "" });

		string TypeOf(string name) => list!.Items.Single(p => p.Name == name).Type.Text;

		TypeOf("defaultValue").Should().Be("boolean | string");
		TypeOf("size").Should().Be("number | string");
		TypeOf("nullableName").Should().Be("string");
	}

	[Test]
	public async Task BuildPropertyList_AllOfUnionWithAMapOnlyBase_StaysAUnionAndKeepsTheMap()
	{
		var json =
			"""
			{
			  "openapi": "3.0.3",
			  "info": { "title": "t", "version": "1" },
			  "paths": {},
			  "components": {
			    "schemas": {
			      "Input": { "type": "object", "properties": { "enabled": { "type": "boolean" } } },
			      "Cat": { "type": "object", "properties": { "lives": { "type": "integer" } } },
			      "Dog": { "type": "object", "properties": { "barks": { "type": "boolean" } } },
			      "Pet": {
			        "allOf": [
			          { "type": "object", "additionalProperties": { "$ref": "#/components/schemas/Input" } },
			          { "oneOf": [ { "$ref": "#/components/schemas/Cat" }, { "$ref": "#/components/schemas/Dog" } ] }
			        ]
			      },
			      "Holder": { "type": "object", "properties": { "pet": { "$ref": "#/components/schemas/Pet" } } }
			    }
			  }
			}
			""";
		var document = await LoadSpecAsync(json);
		var builder = BuilderFor(document);

		var pet = builder.BuildPropertyList(document.Components!.Schemas!["Holder"], new PropertyTreeScope { Prefix = "" })!.Items.Single();
		pet.Children.Kind.Should().Be(ChildKind.UnionVariants);
		var cat = pet.Children.Variants!.Variants[0];
		cat.DisplayName.Should().Be("Cat");
		cat.Properties!.Items.Select(p => p.Name).Should().Equal("lives", "<string>");

		var (properties, variants) = ApiBodyContent.Build(
			document.Components!.Schemas!["Pet"],
			new PropertyTreeScope { Prefix = "req", IsRequest = true },
			new SchemaAnalyzer(document),
			builder
		);
		properties.Should().BeNull();
		variants!.Variants.Select(v => v.DisplayName).Should().Equal("Cat", "Dog");
	}

	[Test]
	public async Task BuildPropertyList_ArrayOfUnion_ExpandsTheItemVariants()
	{
		var json =
			"""
			{
			  "openapi": "3.1.0",
			  "info": { "title": "t", "version": "1" },
			  "paths": {},
			  "components": {
			    "schemas": {
			      "Holder": {
			        "type": "object",
			        "properties": {
			          "and": {
			            "type": "array",
			            "maxItems": 50,
			            "items": {
			              "anyOf": [
			                { "anyOf": [
			                  { "type": "object", "required": ["field"], "properties": { "field": { "type": "string" }, "eq": { "type": "string" } } },
			                  { "type": "object", "properties": { "field": { "type": "string" }, "exists": { "type": "boolean" } } }
			                ] },
			                { "type": "object", "properties": { "always": { "type": "object" } } }
			              ]
			            }
			          }
			        }
			      }
			    }
			  }
			}
			""";
		var document = await LoadSpecAsync(json);

		var and = BuilderFor(document).BuildPropertyList(
			document.Components!.Schemas!["Holder"],
			new PropertyTreeScope { Prefix = "" }
		)!.Items.Single();

		and.Type.Text.Should().Contain("anyOf");
		and.Children.Kind.Should().Be(ChildKind.UnionVariants);
		and.Children.Variants!
			.Variants
			.Select(v => v.Properties!.Items.Select(p => p.Name).First())
			.Should()
			.Equal("field", "field", "always");
	}

	[Test]
	public async Task BuildPropertyList_InlineRecursiveUnion_ExpandsOnceAndPointsBackToTheParent()
	{
		var condition = """{ "type": "object", "properties": { "field": { "type": "string" }, "eq": { "type": "string" } } }""";
		var always = """{ "type": "object", "properties": { "always": { "type": "object" } } }""";
		var leaf = $$"""{ "anyOf": [ { "anyOf": [ {{condition}} ] }, {{always}} ] }""";
		var level2 =
			$$"""{ "anyOf": [ { "anyOf": [ {{condition}} ] }, { "type": "object", "properties": { "and": { "type": "array", "items": {{leaf}} } } }, {{always}} ] }""";
		var json =
			$$"""
			{
			  "openapi": "3.1.0",
			  "info": { "title": "t", "version": "1" },
			  "paths": {},
			  "components": {
			    "schemas": {
			      "Holder": {
			        "type": "object",
			        "properties": {
			          "where": {
			            "anyOf": [
			              { "anyOf": [ {{condition}} ] },
			              { "type": "object", "properties": { "and": { "type": "array", "items": {{level2}} } } },
			              {{always}}
			            ]
			          }
			        }
			      }
			    }
			  }
			}
			""";
		var document = await LoadSpecAsync(json);

		var where = BuilderFor(document).BuildPropertyList(
			document.Components!.Schemas!["Holder"],
			new PropertyTreeScope { Prefix = "" }
		)!.Items.Single();

		where.Children.Kind.Should().Be(ChildKind.UnionVariants);
		var variants = where.Children.Variants!.Variants;
		variants[0].Properties!.Items.Select(p => p.Name).Should().Equal("field", "eq");
		var and = variants[1].Properties!.Items.Single();
		and.Repeats.Should().Be(new RepeatedShape("where", "where", IsUnion: true));
		and.Children.Kind.Should().Be(ChildKind.None);
		var markdown = new System.Text.StringBuilder();
		ApiPropertyMarkdown.WriteList(markdown, variants[1].Properties, "/api/doc/fixture");
		markdown
			.ToString()
			.Should()
			.Contain("Same options as `where`")
			.And
			.NotContain("Any of:", "the repeat line replaces the empty \"Any of:\" row");
	}

	[Test]
	public async Task BuildPropertyList_ArrayOfUnionWithALinkedType_LinksTheVariantInsteadOfExpandingIt()
	{
		var json =
			"""
			{
			  "openapi": "3.0.3",
			  "info": { "title": "t", "version": "1" },
			  "paths": {},
			  "components": {
			    "schemas": {
			      "_types.query_dsl.QueryContainer": { "type": "object", "properties": { "match": { "type": "object" }, "term": { "type": "object" } } },
			      "security._types.RoleTemplateQuery": { "type": "object", "properties": { "template": { "type": "object" } } },
			      "security._types.IndicesPrivilegesQuery": {
			        "oneOf": [
			          { "type": "string" },
			          { "$ref": "#/components/schemas/_types.query_dsl.QueryContainer" },
			          { "$ref": "#/components/schemas/security._types.RoleTemplateQuery" }
			        ]
			      },
			      "Holder": {
			        "type": "object",
			        "properties": {
			          "query": { "type": "array", "items": { "$ref": "#/components/schemas/security._types.IndicesPrivilegesQuery" } }
			        }
			      }
			    }
			  }
			}
			""";
		var document = await LoadSpecAsync(json);

		var query = BuilderFor(document).BuildPropertyList(
			document.Components!.Schemas!["Holder"],
			new PropertyTreeScope { Prefix = "" }
		)!.Items.Single();

		var variants = query.Children.Variants!.Variants;
		var container = variants.Single(v => v.DisplayName == "QueryContainer");
		container.PageUrl.Should().NotBeNullOrEmpty();
		container.Properties.Should().BeNull("QueryContainer has its own page");
		variants.Single(v => v.DisplayName == "RoleTemplateQuery").Properties!.Items.Select(p => p.Name).Should().Equal("template");
	}

	[Test]
	public async Task BuildPropertyList_LargeTypeRepeatedOnThePage_IsListedOnceAndLinkedAfterwards()
	{
		var json =
			"""
			{
			  "openapi": "3.0.3",
			  "info": { "title": "t", "version": "1" },
			  "paths": {},
			  "components": {
			    "schemas": {
			      "Big": { "type": "object", "properties": { "f0": { "type": "string" }, "f1": { "type": "string" }, "f2": { "type": "string" }, "f3": { "type": "string" }, "f4": { "type": "string" }, "f5": { "type": "string" }, "f6": { "type": "string" }, "f7": { "type": "string" }, "f8": { "type": "string" }, "f9": { "type": "string" }, "f10": { "type": "string" }, "f11": { "type": "string" } } },
			      "Small": { "type": "object", "properties": { "id": { "type": "string" }, "name": { "type": "string" } } },
			      "Holder": {
			        "type": "object",
			        "properties": {
			          "created": { "$ref": "#/components/schemas/Big" },
			          "updated": { "$ref": "#/components/schemas/Big" },
			          "owner": { "$ref": "#/components/schemas/Small" },
			          "editor": { "$ref": "#/components/schemas/Small" }
			        }
			      }
			    }
			  }
			}
			""";
		var document = await LoadSpecAsync(json);

		var builder = BuilderFor(document);
		var list = builder.BuildPropertyList(document.Components!.Schemas!["Holder"], new PropertyTreeScope { Prefix = "res-200" })!;

		ApiProperty Row(string name) => list.Items.Single(p => p.Name == name);

		Row("created").Children.Properties!.Items.Should().HaveCount(12);
		Row("updated").Repeats.Should().Be(new RepeatedShape("created", "res-200-created", IsUnion: false));
		Row("updated").Children.Kind.Should().Be(ChildKind.None);
		Row("updated").IsRecursive.Should().BeFalse("a sibling repeat is not a recursion");
		Row("editor").Repeats.Should().BeNull("small types read better inline");
		Row("editor").Children.Properties!.Items.Should().HaveCount(2);

		var later = builder.BuildPropertyList(document.Components!.Schemas!["Holder"], new PropertyTreeScope { Prefix = "req" })!;
		later.Items.Single(p => p.Name == "created").Repeats!.AnchorId.Should().Be("res-200-created", "one builder serves one page");
	}

	[Test]
	public async Task BuildPropertyList_LargeTypeSharedAcrossVariants_NamesTheVariantAndShowsNoToggle()
	{
		var json =
			"""
			{
			  "openapi": "3.0.3",
			  "info": { "title": "t", "version": "1" },
			  "paths": {},
			  "components": {
			    "schemas": {
			      "Actions": { "type": "object", "properties": { "f0": { "type": "string" }, "f1": { "type": "string" }, "f2": { "type": "string" }, "f3": { "type": "string" }, "f4": { "type": "string" }, "f5": { "type": "string" }, "f6": { "type": "string" }, "f7": { "type": "string" }, "f8": { "type": "string" }, "f9": { "type": "string" }, "f10": { "type": "string" }, "f11": { "type": "string" } } },
			      "EqlRule": { "type": "object", "properties": { "query": { "type": "string" }, "actions": { "type": "array", "items": { "$ref": "#/components/schemas/Actions" } } } },
			      "QueryRule": { "type": "object", "properties": { "filters": { "type": "string" }, "actions": { "type": "array", "items": { "$ref": "#/components/schemas/Actions" } } } },
			      "Holder": {
			        "type": "object",
			        "properties": {
			          "rule": { "oneOf": [ { "$ref": "#/components/schemas/EqlRule" }, { "$ref": "#/components/schemas/QueryRule" } ] }
			        }
			      }
			    }
			  }
			}
			""";
		var document = await LoadSpecAsync(json);

		var variants = BuilderFor(document).BuildPropertyList(
			document.Components!.Schemas!["Holder"],
			new PropertyTreeScope { Prefix = "" }
		)!.Items.Single().Children.Variants!.Variants;

		var first = variants[0].Properties!.Items.Single(p => p.Name == "actions");
		first.Children.Properties!.Items.Should().HaveCount(12);
		var repeat = variants[1].Properties!.Items.Single(p => p.Name == "actions");
		repeat.Repeats.Should().Be(new RepeatedShape("actions", first.AnchorId, IsUnion: false, Owner: "EqlRule"));
		repeat.IsCollapsible.Should().BeFalse("a repeat lists nothing, so it has nothing to show or hide");
		repeat.NestedCount.Should().Be(0);
	}

	[Test]
	public async Task BuildTopLevelUnionVariants_UnnamedInlineMembers_AreLabelledByConstantOrDistinctFields()
	{
		var json =
			"""
			{
			  "openapi": "3.1.0",
			  "info": { "title": "t", "version": "1" },
			  "paths": {},
			  "components": {
			    "schemas": {
			      "Range": {
			        "oneOf": [
			          { "type": "object", "properties": { "type": { "type": "string", "enum": ["relative"] }, "from": { "type": "string" } } },
			          { "type": "object", "properties": { "type": { "type": "string", "enum": ["absolute"] }, "start": { "type": "string" } } }
			        ]
			      },
			      "Note": {
			        "oneOf": [
			          { "type": "object", "required": ["noteId"], "properties": { "noteId": { "type": "string" }, "version": { "type": "string" } } },
			          { "type": "object", "required": ["noteIds"], "properties": { "noteIds": { "type": "array", "items": { "type": "string" } }, "version": { "type": "string" } } },
			          { "type": "object", "title": "ByQuery", "properties": { "query": { "type": "string" } } }
			        ]
			      }
			    }
			  }
			}
			""";
		var document = await LoadSpecAsync(json);
		var builder = BuilderFor(document);
		var analyzer = new SchemaAnalyzer(document);

		var range = ApiBodyContent.BuildUnionVariants(
			document.Components!.Schemas!["Range"],
			new PropertyTreeScope { Prefix = "req" },
			analyzer,
			builder
		);
		range!.Variants.Select(v => v.DisplayName).Should().Equal("type: relative", "type: absolute");

		var note = ApiBodyContent.BuildUnionVariants(
			document.Components!.Schemas!["Note"],
			new PropertyTreeScope { Prefix = "req" },
			analyzer,
			builder
		);
		note!.Variants.Select(v => v.DisplayName).Should().Equal("{ noteId }", "{ noteIds }", "ByQuery");
	}

	[Test]
	public async Task BuildPropertyList_UnionsWithSameNamesButDifferentFieldTypes_AreNotShared()
	{
		var json =
			"""
			{
			  "openapi": "3.1.0",
			  "info": { "title": "t", "version": "1" },
			  "paths": {},
			  "components": {
			    "schemas": {
			      "Holder": {
			        "type": "object",
			        "properties": {
			          "first": { "oneOf": [
			            { "type": "object", "properties": { "f0": { "type": "string" }, "f1": { "type": "string" }, "f2": { "type": "string" }, "f3": { "type": "string" }, "f4": { "type": "string" }, "f5": { "type": "string" }, "f6": { "type": "string" }, "f7": { "type": "string" }, "f8": { "type": "string" }, "f9": { "type": "string" }, "value": { "type": "string" } } },
			            { "type": "object", "properties": { "other": { "type": "string" } } } ] },
			          "second": { "oneOf": [
			            { "type": "object", "properties": { "f0": { "type": "string" }, "f1": { "type": "string" }, "f2": { "type": "string" }, "f3": { "type": "string" }, "f4": { "type": "string" }, "f5": { "type": "string" }, "f6": { "type": "string" }, "f7": { "type": "string" }, "f8": { "type": "string" }, "f9": { "type": "string" }, "value": { "type": "integer" } } },
			            { "type": "object", "properties": { "other": { "type": "string" } } } ] },
			          "third": { "oneOf": [
			            { "type": "object", "properties": { "f0": { "type": "string" }, "f1": { "type": "string" }, "f2": { "type": "string" }, "f3": { "type": "string" }, "f4": { "type": "string" }, "f5": { "type": "string" }, "f6": { "type": "string" }, "f7": { "type": "string" }, "f8": { "type": "string" }, "f9": { "type": "string" }, "value": { "type": "string" } } },
			            { "type": "object", "properties": { "other": { "type": "string" } } } ] }
			        }
			      }
			    }
			  }
			}
			""";
		var document = await LoadSpecAsync(json);

		var list = BuilderFor(document).BuildPropertyList(document.Components!.Schemas!["Holder"], new PropertyTreeScope { Prefix = "" })!;

		ApiProperty Row(string name) => list.Items.Single(p => p.Name == name);

		Row("second").Repeats.Should().BeNull("its value is an integer, not a string");
		Row("third").Repeats!.Name.Should().Be("first");
	}

	[Test]
	public async Task BuildPropertyList_MapVariantWithARealStringProperty_KeepsDistinctAnchors()
	{
		var json =
			"""
			{
			  "openapi": "3.1.0",
			  "info": { "title": "t", "version": "1" },
			  "paths": {},
			  "components": {
			    "schemas": {
			      "Input": { "type": "object", "properties": { "enabled": { "type": "boolean" } } },
			      "Holder": {
			        "type": "object",
			        "properties": {
			          "inputs": { "anyOf": [
			            { "type": "string" },
			            { "type": "object", "properties": { "string": { "type": "string" } }, "additionalProperties": { "$ref": "#/components/schemas/Input" } } ] }
			        }
			      }
			    }
			  }
			}
			""";
		var document = await LoadSpecAsync(json);

		var map = BuilderFor(document).BuildPropertyList(document.Components!.Schemas!["Holder"], new PropertyTreeScope { Prefix = "" })!
			.Items
			.Single()
			.Children
			.Variants!.Variants.Single(v => v.Properties is not null);

		var anchors = map.Properties!.Items.Select(p => p.AnchorId).ToList();
		anchors.Should().OnlyHaveUniqueItems();
		map.Properties.Items.Single(p => p.Name == "<string>").AnchorId.Should().EndWith("-string-map");
	}

	[Test]
	public async Task BuildPropertyList_UnionsDifferingOnlyInMapValue_AreNotShared()
	{
		var json =
			"""
			{
			  "openapi": "3.1.0",
			  "info": { "title": "t", "version": "1" },
			  "paths": {},
			  "components": {
			    "schemas": {
			      "Input": { "type": "object", "properties": { "enabled": { "type": "boolean" } } },
			      "Holder": {
			        "type": "object",
			        "properties": {
			          "first": { "oneOf": [
			            { "type": "object", "properties": { "f0": { "type": "string" }, "f1": { "type": "string" }, "f2": { "type": "string" }, "f3": { "type": "string" }, "f4": { "type": "string" }, "f5": { "type": "string" }, "f6": { "type": "string" }, "f7": { "type": "string" }, "f8": { "type": "string" }, "f9": { "type": "string" } }, "additionalProperties": { "type": "object", "properties": { "x": { "type": "string" } } } },
			            { "type": "object", "properties": { "other": { "type": "string" } } } ] },
			          "second": { "oneOf": [
			            { "type": "object", "properties": { "f0": { "type": "string" }, "f1": { "type": "string" }, "f2": { "type": "string" }, "f3": { "type": "string" }, "f4": { "type": "string" }, "f5": { "type": "string" }, "f6": { "type": "string" }, "f7": { "type": "string" }, "f8": { "type": "string" }, "f9": { "type": "string" } }, "additionalProperties": { "type": "object", "properties": { "y": { "type": "integer" } } } },
			            { "type": "object", "properties": { "other": { "type": "string" } } } ] },
			          "third": { "oneOf": [
			            { "type": "object", "properties": { "f0": { "type": "string" }, "f1": { "type": "string" }, "f2": { "type": "string" }, "f3": { "type": "string" }, "f4": { "type": "string" }, "f5": { "type": "string" }, "f6": { "type": "string" }, "f7": { "type": "string" }, "f8": { "type": "string" }, "f9": { "type": "string" } }, "additionalProperties": { "type": "object", "properties": { "x": { "type": "string" } } } },
			            { "type": "object", "properties": { "other": { "type": "string" } } } ] }
			        }
			      }
			    }
			  }
			}
			""";
		var document = await LoadSpecAsync(json);

		var list = BuilderFor(document).BuildPropertyList(document.Components!.Schemas!["Holder"], new PropertyTreeScope { Prefix = "" })!;

		ApiProperty Row(string name) => list.Items.Single(p => p.Name == name);

		Row("second").Repeats.Should().BeNull("its map values have different fields");
		Row("third").Repeats!.Name.Should().Be("first");
	}

	[Test]
	public async Task GetTypeInfo_TwoAllOfUnionsReferringToEachOther_GivesTheSameResultInAnyOrder()
	{
		var json =
			"""
			{
			  "openapi": "3.0.3",
			  "info": { "title": "t", "version": "1" },
			  "paths": {},
			  "components": {
			    "schemas": {
			      "Leaf": { "type": "object", "properties": { "value": { "type": "string" } } },
			      "A": { "allOf": [
			        { "type": "object", "properties": { "a": { "type": "string" } } },
			        { "oneOf": [ { "$ref": "#/components/schemas/Leaf" }, { "$ref": "#/components/schemas/B" } ] } ] },
			      "B": { "allOf": [
			        { "type": "object", "properties": { "b": { "type": "string" } } },
			        { "oneOf": [ { "$ref": "#/components/schemas/Leaf" }, { "$ref": "#/components/schemas/A" } ] } ] }
			    }
			  }
			}
			""";
		var document = await LoadSpecAsync(json);

		string Describe(Model.TypeInfo info) =>
			$"{info.TypeName}:{info.UnionKeyword}:{string.Join(",", info.UnionOptions!.Select(o => $"{o.Name}/{o.Ref}/{o.IsObject}"))}";

		var forward = new SchemaAnalyzer(document);
		var aFirst = Describe(forward.GetTypeInfo(new OpenApiSchemaReference("A", document)));
		var bAfter = Describe(forward.GetTypeInfo(new OpenApiSchemaReference("B", document)));
		var aAgain = Describe(forward.GetTypeInfo(new OpenApiSchemaReference("A", document)));

		var backward = new SchemaAnalyzer(document);
		var bFirst = Describe(backward.GetTypeInfo(new OpenApiSchemaReference("B", document)));
		var aAfter = Describe(backward.GetTypeInfo(new OpenApiSchemaReference("A", document)));

		aAgain.Should().Be(aFirst, "the cycle guard is empty again once a classification returns");
		aAfter.Should().Be(aFirst);
		bFirst.Should().Be(bAfter);
		aFirst.Should().Contain("B/B/True");
	}

	[Test]
	public async Task BuildPropertyList_TypeArrayOfStringAndObject_IsAPrimitiveType()
	{
		var json =
			"""
			{
			  "openapi": "3.1.0",
			  "info": { "title": "t", "version": "1" },
			  "paths": {},
			  "components": {
			    "schemas": {
			      "Holder": {
			        "type": "object",
			        "properties": {
			          "value": { "type": ["string", "object"] },
			          "nested": { "type": "object", "properties": { "value": { "type": ["string", "object"] } } }
			        }
			      }
			    }
			  }
			}
			""";
		var document = await LoadSpecAsync(json);

		var info = new SchemaAnalyzer(document).GetTypeInfo(document.Components!.Schemas!["Holder"].Properties!["value"]);
		info.TypeName.Should().Be("string | object");
		info.IsObject.Should().BeFalse("it has no properties to list");
		SchemaHelpers.IsPrimitiveTypeName(info.TypeName).Should().BeTrue();

		var list = BuilderFor(document).BuildPropertyList(document.Components!.Schemas!["Holder"], new PropertyTreeScope { Prefix = "" })!;
		var nestedValue = list.Items.Single(p => p.Name == "nested").Children.Properties!.Items.Single();
		nestedValue.IsRecursive.Should().BeFalse("a primitive multi-type name is never an ancestor type");
	}

	[Test]
	public async Task BuildPropertyList_UnionsDifferingInEnumValuesOrDeepFields_AreNotShared()
	{
		var json =
			"""
			{
			  "openapi": "3.1.0",
			  "info": { "title": "t", "version": "1" },
			  "paths": {},
			  "components": {
			    "schemas": {
			      "Holder": {
			        "type": "object",
			        "properties": {
			          "first": { "oneOf": [
			            { "type": "object", "properties": { "f0": { "type": "string" }, "f1": { "type": "string" }, "f2": { "type": "string" }, "f3": { "type": "string" }, "f4": { "type": "string" }, "f5": { "type": "string" }, "f6": { "type": "string" }, "f7": { "type": "string" }, "f8": { "type": "string" }, "f9": { "type": "string" }, "status": { "type": "string", "enum": ["open", "closed"] }, "meta": { "type": "object", "properties": { "inner": { "type": "object", "properties": { "deep": { "type": "string" } } } } } } },
			            { "type": "object", "properties": { "other": { "type": "string" } } } ] },
			          "otherEnum": { "oneOf": [
			            { "type": "object", "properties": { "f0": { "type": "string" }, "f1": { "type": "string" }, "f2": { "type": "string" }, "f3": { "type": "string" }, "f4": { "type": "string" }, "f5": { "type": "string" }, "f6": { "type": "string" }, "f7": { "type": "string" }, "f8": { "type": "string" }, "f9": { "type": "string" }, "status": { "type": "string", "enum": ["open", "acknowledged"] }, "meta": { "type": "object", "properties": { "inner": { "type": "object", "properties": { "deep": { "type": "string" } } } } } } },
			            { "type": "object", "properties": { "other": { "type": "string" } } } ] },
			          "otherDeep": { "oneOf": [
			            { "type": "object", "properties": { "f0": { "type": "string" }, "f1": { "type": "string" }, "f2": { "type": "string" }, "f3": { "type": "string" }, "f4": { "type": "string" }, "f5": { "type": "string" }, "f6": { "type": "string" }, "f7": { "type": "string" }, "f8": { "type": "string" }, "f9": { "type": "string" }, "status": { "type": "string", "enum": ["open", "closed"] }, "meta": { "type": "object", "properties": { "inner": { "type": "object", "properties": { "deep": { "type": "integer" } } } } } } },
			            { "type": "object", "properties": { "other": { "type": "string" } } } ] },
			          "same": { "oneOf": [
			            { "type": "object", "properties": { "f0": { "type": "string" }, "f1": { "type": "string" }, "f2": { "type": "string" }, "f3": { "type": "string" }, "f4": { "type": "string" }, "f5": { "type": "string" }, "f6": { "type": "string" }, "f7": { "type": "string" }, "f8": { "type": "string" }, "f9": { "type": "string" }, "status": { "type": "string", "enum": ["open", "closed"] }, "meta": { "type": "object", "properties": { "inner": { "type": "object", "properties": { "deep": { "type": "string" } } } } } } },
			            { "type": "object", "properties": { "other": { "type": "string" } } } ] }
			        }
			      }
			    }
			  }
			}
			""";
		var document = await LoadSpecAsync(json);

		var list = BuilderFor(document).BuildPropertyList(document.Components!.Schemas!["Holder"], new PropertyTreeScope { Prefix = "" })!;

		ApiProperty Row(string name) => list.Items.Single(p => p.Name == name);

		Row("otherEnum").Repeats.Should().BeNull("its status values differ");
		Row("otherDeep").Repeats.Should().BeNull("a field three levels down differs");
		Row("same").Repeats!.Name.Should().Be("first");
	}

	[Test]
	public async Task BuildPropertyList_CopyWhoseNestedFieldsWereLinked_StillSharesTheWholeListing()
	{
		var json =
			"""
			{
			  "openapi": "3.0.3",
			  "info": { "title": "t", "version": "1" },
			  "paths": {},
			  "components": {
			    "schemas": {
			      "Actions": { "type": "object", "properties": { "f0": { "type": "string" }, "f1": { "type": "string" }, "f2": { "type": "string" }, "f3": { "type": "string" }, "f4": { "type": "string" }, "f5": { "type": "string" }, "f6": { "type": "string" }, "f7": { "type": "string" }, "f8": { "type": "string" }, "f9": { "type": "string" } } },
			      "EqlRule": { "type": "object", "properties": { "query": { "type": "string" }, "actions": { "$ref": "#/components/schemas/Actions" } } },
			      "QueryRule": { "type": "object", "properties": { "filters": { "type": "string" }, "actions": { "$ref": "#/components/schemas/Actions" } } },
			      "Rule": { "oneOf": [ { "$ref": "#/components/schemas/EqlRule" }, { "$ref": "#/components/schemas/QueryRule" } ] },
			      "Results": {
			        "type": "object",
			        "properties": {
			          "created": { "type": "array", "items": { "$ref": "#/components/schemas/Rule" } },
			          "updated": { "type": "array", "items": { "$ref": "#/components/schemas/Rule" } }
			        }
			      }
			    }
			  }
			}
			""";
		var document = await LoadSpecAsync(json);

		var list = BuilderFor(document).BuildPropertyList(document.Components!.Schemas!["Results"], new PropertyTreeScope { Prefix = "" })!;

		var created = list.Items.Single(p => p.Name == "created");
		created.Children.Variants!.Variants[1].Properties!.Items.Single(p => p.Name == "actions").Repeats!.Owner.Should().Be("EqlRule");
		list.Items.Single(p => p.Name == "updated").Repeats.Should().Be(new RepeatedShape("created", "created", IsUnion: true));
	}

	[Test]
	public async Task BuildBodyContent_ArrayOfUnion_SaysEachItemIsAVariant()
	{
		var json =
			"""
			{
			  "openapi": "3.0.3",
			  "info": { "title": "t", "version": "1" },
			  "paths": {},
			  "components": {
			    "schemas": {
			      "Cat": { "type": "object", "properties": { "lives": { "type": "integer" } } },
			      "Dog": { "type": "object", "properties": { "barks": { "type": "boolean" } } },
			      "Pets": { "type": "array", "items": { "oneOf": [ { "$ref": "#/components/schemas/Cat" }, { "$ref": "#/components/schemas/Dog" } ] } },
			      "Pet": { "oneOf": [ { "$ref": "#/components/schemas/Cat" }, { "$ref": "#/components/schemas/Dog" } ] }
			    }
			  }
			}
			""";
		var document = await LoadSpecAsync(json);
		var analyzer = new SchemaAnalyzer(document);
		var builder = BuilderFor(document);

		var (_, array) = ApiBodyContent.Build(
			document.Components!.Schemas!["Pets"],
			new PropertyTreeScope { Prefix = "res-200" },
			analyzer,
			builder
		);
		array!.Label.Should().Be("An array; each item is one of:");
		array.Variants.Select(v => v.DisplayName).Should().Equal("Cat", "Dog");
		var markdown = new System.Text.StringBuilder();
		ApiPropertyMarkdown.WriteVariants(markdown, array, "/api/doc/fixture");
		markdown.ToString().Should().StartWith("An array; each item is one of:");

		var (_, single) = ApiBodyContent.Build(
			document.Components!.Schemas!["Pet"],
			new PropertyTreeScope { Prefix = "req" },
			analyzer,
			builder
		);
		single!.Label.Should().Be("One of:");
	}
}
