// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Components.PropertyTree;
using Elastic.ApiExplorer.Components.PropertyTree._Partials;
using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Operations;
using Microsoft.AspNetCore.Html;
using Microsoft.OpenApi;
using RazorSlices;
using static Elastic.ApiExplorer.Tests.TestSpecs;

namespace Elastic.ApiExplorer.Tests;

[ClassDataSource<ApiExplorerFixture>(Shared = SharedType.PerClass)]
public class ApiPropertyTreeBuilderTests(ApiExplorerFixture fixture)
{
	private ApiPropertyTreeBuilder CreateBuilder(string? currentPageSchemaId = null)
	{
		var options = new PropertyDisplayOptions { RenderMarkdown = s => new HtmlString($"<p>{s}</p>"), ApiRootUrl = "/api/doc/fixture" };
		return new ApiPropertyTreeBuilder(fixture.Document, options, currentPageSchemaId);
	}

	private IOpenApiSchema Schema(string id) => fixture.Document.Components!.Schemas![id];

	[Test]
	public void BuildPropertyList_RecursiveSchema_StopsAtAncestor()
	{
		var builder = CreateBuilder(currentPageSchemaId: "_types.query_dsl.QueryContainer");
		var ancestors = new HashSet<string> { "_types.query_dsl.QueryContainer" };

		var list = builder.BuildPropertyList(
			Schema("_types.query_dsl.QueryContainer"),
			new PropertyTreeScope { Prefix = "", AncestorRefs = ancestors }
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
	public void BuildUnionVariants_TopLevelOneOf_BuildsVariantPerOption()
	{
		var builder = CreateBuilder(currentPageSchemaId: "_types.aggregations.Aggregate");
		var aggregate = Schema("_types.aggregations.Aggregate");

		var variants = ApiBodyContent.BuildUnionVariants(
			aggregate,
			new PropertyTreeScope { Prefix = "oneof", AncestorRefs = new HashSet<string> { "_types.aggregations.Aggregate" } },
			new SchemaAnalyzer(fixture.Document, "_types.aggregations.Aggregate"),
			builder
		);

		variants.Should().NotBeNull();
		variants!.Variants.Should().HaveCount(2);
		variants.ShouldCollapse.Should().BeFalse();
		variants.Variants.Select(v => v.DisplayName).Should().BeEquivalentTo(["TermsAggregate", "MaxAggregate"]);
	}

	[Test]
	public void BuildUnionVariants_CodegenOneOf_UsesReadableNames()
	{
		var builder = CreateBuilder();
		var schema = Schema("fixture.InvalidInputResponse");

		var variants = ApiBodyContent.BuildUnionVariants(
			schema,
			new PropertyTreeScope { Prefix = "res-400" },
			new SchemaAnalyzer(fixture.Document),
			builder
		);

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
	[Arguments("^[a-z]+$", false)]
	[Arguments("^(?:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa)$", true)]
	public void Describe_Pattern_SetsTitleOnlyWhenTooLongToShowInline(string pattern, bool expectTitle)
	{
		var schema = new OpenApiSchema { Type = JsonSchemaType.String, Pattern = pattern };

		var span = CreateBuilder().Describe(schema).Spans.Single(s => s.CssClass == SchemaHelpers.ConstraintCssClass);

		span.Title.Should().Be(expectTitle ? $"pattern: {pattern}" : null);
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
		builder.DescribePathParameter(dataStreams).Text.Should().Be("union DataStreamName | [] DataStreamName");
		builder.DescribePathParameter(document.Components!.Schemas!["inline.StringOrArray"]).Text.Should().Be("union string | [] string");
		builder.DescribePathParameter(name).Text.Should().Be("string Name");
		builder.Describe(name).Text.Should().Be("string Name");
		builder.DescribePathParameter(new OpenApiSchema { Type = JsonSchemaType.String }).Text.Should().Be("string");
	}

	[Test]
	[Arguments(false)]
	[Arguments(true)]
	public async Task BuildPropertyList_ObjectsAtSameDepth_StartCollapsed(bool isRequest)
	{
		var builder = CreateBuilder();
		var list = builder.BuildPropertyList(ObjectsAtTwoDepths(), new PropertyTreeScope { Prefix = "body", IsRequest = isRequest });

		list.Should().NotBeNull();
		var html = await _PropertyList.Create(list!).RenderAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);

		AssertCollapsedPair(list.Items, "small", "large", html);
		AssertStartsCollapsed(html, list.Items.Single(p => p.Name == "only").AnchorId);
		var group = list.Items.Single(p => p.Name == "group");
		group.Children.Properties.Should().NotBeNull();
		AssertCollapsedPair(group.Children.Properties!.Items, "small", "large", html);
		AssertStartsCollapsed(html, group.Children.Properties.Items.Single(p => p.Name == "only").AnchorId);
	}

	private static void AssertCollapsedPair(IReadOnlyList<ApiProperty> items, string smallName, string largeName, string html)
	{
		var small = items.Single(p => p.Name == smallName);
		var large = items.Single(p => p.Name == largeName);
		small.Depth.Should().Be(large.Depth);
		small.IsCollapsible.Should().BeTrue();
		large.IsCollapsible.Should().BeTrue();
		AssertStartsCollapsed(html, small.AnchorId);
		AssertStartsCollapsed(html, large.AnchorId);
	}

	private static void AssertStartsCollapsed(string html, string anchorId)
	{
		var match = System
			.Text
			.RegularExpressions
			.Regex
			.Match(html, $"id=\"{anchorId}\"([\\s\\S]*?)aria-expanded=\"(true|false)\"([\\s\\S]*?)(show|hide) properties");
		match.Success.Should().BeTrue($"property {anchorId} renders an expand control");
		match.Groups[2].Value.Should().Be("false", $"{anchorId} starts collapsed");
		match.Groups[4].Value.Should().Be("show", $"{anchorId} offers show properties");
		html.Should().Contain($"id=\"{anchorId}-children\"");
	}

	private static OpenApiSchema ObjectsAtTwoDepths() =>
		new()
		{
			Type = JsonSchemaType.Object,
			Properties = new Dictionary<string, IOpenApiSchema>
			{
				["only"] = StringFields("one"),
				["small"] = StringFields("one", "two"),
				["large"] = StringFields("one", "two", "three", "four", "five", "six"),
				["group"] = new OpenApiSchema
				{
					Type = JsonSchemaType.Object,
					Properties = new Dictionary<string, IOpenApiSchema>
					{
						["only"] = StringFields("one"),
						["small"] = StringFields("one", "two"),
						["large"] = StringFields("one", "two", "three", "four", "five", "six")
					}
				}
			}
		};

	private static OpenApiSchema StringFields(params string[] names) =>
		new()
		{
			Type = JsonSchemaType.Object,
			Properties = names.ToDictionary(name => name, name => (IOpenApiSchema)new OpenApiSchema { Type = JsonSchemaType.String })
		};
}
