// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Components.PropertyTree;
using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Operations;
using Microsoft.AspNetCore.Html;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Tests;

public class ApiPropertyTreeBuilderTests(ApiExplorerFixture fixture) : IClassFixture<ApiExplorerFixture>
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

	private IOpenApiSchema Schema(string id) => fixture.Document.Components!.Schemas![id];

	[Fact]
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

	[Fact]
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

	[Fact]
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
	}

	[Fact]
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

	[Fact]
	public void Describe_EnumSchema_ShowsEnumKeyword()
	{
		var builder = CreateBuilder();

		var annotation = builder.Describe(Schema("fixture.SearchRequestBody").Properties!["mode"]);

		annotation.Spans.Should().Contain(s => s.CssClass == SchemaHelpers.WrapperEnumCssClass && s.Text == "enum");
	}

	[Fact]
	public void Describe_ValueType_MarksKeywordAndAliasAsTypeValue()
	{
		var builder = CreateBuilder();

		var annotation = builder.Describe(Schema("_types.SortField").Properties!["field"]);

		annotation.Spans.Should().Contain(s => s.Text == "string" && s.CssClass != null && s.CssClass.Contains("type-value"));
		annotation.Spans.Should().Contain(s => s.Text == "Field" && s.CssClass == "type-value");
		annotation.Spans.Should().NotContain(s => s.CssClass != null && s.CssClass.Contains("type-primitive"));
	}

	[Fact]
	public void Describe_SimpleArrayUnion_SplitsFormulaIntoAtoms()
	{
		var builder = CreateBuilder();

		var annotation = builder.Describe(Schema("fixture.SearchRequestBody").Properties!["fields"]);

		annotation.Text.Should().Be("union Field | [] Field");
		annotation.Spans.Should().Contain(s => s.Text == "union" && s.CssClass == SchemaHelpers.WrapperUnionCssClass);
		annotation.Spans.Should().Contain(s => s.Text == "[]" && s.CssClass == SchemaHelpers.WrapperArrayIconCssClass);
		annotation.Spans.Should().NotContain(s => s.CssClass == "type-object");
	}

	[Fact]
	public void Describe_ArrayOfInlineObjects_UsesBracketPrefix()
	{
		var builder = CreateBuilder();

		var annotation = builder.Describe(Schema("_types.aggregations.TermsAggregate").Properties!["buckets"]);

		annotation.Text.Should().Be("[] object");
		annotation.Spans.Should().Contain(s => s.Text == "[]" && s.CssClass == SchemaHelpers.WrapperArrayIconCssClass);
		annotation.Spans.Should().Contain(s => s.Text == "object" && s.CssClass == "type-primitive");
	}

	[Fact]
	public void Describe_ArrayOfLinkedType_UsesBracketPrefix()
	{
		var builder = CreateBuilder();

		var annotation = builder.Describe(Schema("_types.query_dsl.BoolQuery").Properties!["must"]);

		annotation.Text.Should().Be("[] {} QueryContainer");
		annotation.Spans.Should().Contain(s => s.Text == "QueryContainer" && s.CssClass == "type-linked");
	}

	[Fact]
	public void Describe_LinkedType_MarksNameAsTypeLinked()
	{
		var builder = CreateBuilder();

		var annotation = builder.Describe(Schema("fixture.SearchRequestBody").Properties!["query"]);

		annotation.Spans.Should().Contain(s => s.Text == "QueryContainer" && s.CssClass == "type-linked");
		annotation.Spans.Should().Contain(s => s.Text == "{}" && s.CssClass != null && s.CssClass.Contains("type-wrapper"));
	}

	[Fact]
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

	[Fact]
	public void BuildUnionVariantsForSchemas_TopLevelOneOf_BuildsVariantPerOption()
	{
		var builder = CreateBuilder(currentPageType: "Aggregate", collapseMode: CollapseMode.DepthBased);
		var aggregate = Schema("_types.aggregations.Aggregate");

		var variants = builder.BuildUnionVariantsForSchemas(aggregate.OneOf!, "oneof", new HashSet<string> { "Aggregate" });

		variants.Should().NotBeNull();
		variants!.Variants.Should().HaveCount(2);
		variants.ShouldCollapse.Should().BeFalse();
		variants.Variants.Select(v => v.DisplayName).Should().BeEquivalentTo(["TermsAggregate", "MaxAggregate"]);
	}

	[Fact]
	public void BuildConstraints_NumericBounds_ProducesLabels()
	{
		var boolQuery = Schema("_types.query_dsl.BoolQuery");

		var constraints = ApiPropertyTreeBuilder.BuildConstraints(boolQuery.Properties!["minimum_should_match"]);

		constraints.Should().ContainSingle(c => c.Text == "min: 0");
	}

	[Fact]
	public void Describe_NumericBounds_AppendsMinToType()
	{
		var builder = CreateBuilder();

		var annotation = builder.Describe(Schema("_types.query_dsl.BoolQuery").Properties!["minimum_should_match"]);

		annotation.Text.Should().Be("integer · min: 0");
		annotation.Spans.Should().Contain(s => s.Text == "min: 0" && s.CssClass == SchemaHelpers.ConstraintCssClass);
	}

	[Fact]
	public void BuildConstraints_StringAndArrayBounds_UseMinMaxWithoutQualifier()
	{
		var text = new OpenApiSchema { Type = JsonSchemaType.String, MinLength = 1, MaxLength = 50 };
		var items = new OpenApiSchema { Type = JsonSchemaType.Array, MinItems = 1, MaxItems = 100 };

		ApiPropertyTreeBuilder.BuildConstraints(text).Select(c => c.Text).Should().Equal("min: 1", "max: 50");
		ApiPropertyTreeBuilder.BuildConstraints(items).Select(c => c.Text).Should().Equal("min: 1", "max: 100");
	}

	[Fact]
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
}
