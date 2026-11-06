// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Nodes;
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
		fields.Type.Text.Should().Be("union string | string[]");
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
		aggs.TypeLink!.TypeName.Should().Be("Aggregation");
		aggs.TypeLink.Url.Should().Be("/api/doc/fixture/types/_types-aggregations-aggregationcontainer");
		aggs.Type.Spans.Should().Contain(s => s.Text == "Aggregation" && s.CssClass == "type-linked" && s.Href == aggs.TypeLink.Url);
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
		query.Type.Spans.Should().Contain(s => s.Text == "Query" && s.CssClass == "type-linked" && s.Href == query.TypeLink!.Url);
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
	public void Describe_ValueType_ReadsAsItsPrimitive()
	{
		var builder = CreateBuilder();

		var annotation = builder.Describe(Schema("_types.SortField").Properties!["field"]);

		annotation.Text.Should().Be("string");
		annotation.Spans.Should().ContainSingle(s => s.Text == "string" && s.CssClass == SchemaHelpers.PrimitiveCssClass);
	}

	[Test]
	public void Describe_PrimitiveAlias_HidesCodegenName()
	{
		var builder = CreateBuilder();

		var annotation = builder.Describe(Schema("fixture.ListCreateBody").Properties!["description"]);

		annotation.Text.Should().Be("string · min length: 1");
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
	public void Describe_NamedEnum_HidesSchemaName()
	{
		var builder = CreateBuilder();

		var annotation = builder.Describe(Schema("fixture.SearchRequestBody").Properties!["mode"]);

		annotation.Text.Should().Be("enum");
		annotation.Spans.Should().NotContain(s => s.Bare && s.Text == " ", "no name follows the keyword");
	}

	[Test]
	public void Describe_SimpleArrayUnion_SplitsFormulaIntoAtoms()
	{
		var builder = CreateBuilder();

		var annotation = builder.Describe(Schema("fixture.SearchRequestBody").Properties!["fields"]);

		annotation.Text.Should().Be("union string | string[]");
		annotation.Spans.Should().Contain(s => s.Text == "union" && s.CssClass == SchemaHelpers.WrapperUnionCssClass);
		annotation.Spans.Should().Contain(s => s.Text == "[]" && s.CssClass == SchemaHelpers.WrapperArrayIconCssClass);
		annotation.Spans.Should().NotContain(s => s.CssClass == "type-object");
	}

	[Test]
	public void Describe_ArrayOfInlineObjects_UsesBracketSuffix()
	{
		var builder = CreateBuilder();

		var annotation = builder.Describe(Schema("_types.aggregations.TermsAggregate").Properties!["buckets"]);

		annotation.Text.Should().Be("object[]");
		annotation.Spans.Should().Contain(s => s.Text == "[]" && s.CssClass == SchemaHelpers.WrapperArrayIconCssClass);
		annotation.Spans.Should().Contain(s => s.Text == "object" && s.CssClass == "type-primitive");
	}

	[Test]
	public void Describe_ArrayOfLinkedType_UsesBracketSuffix()
	{
		var builder = CreateBuilder();

		var annotation = builder.Describe(Schema("_types.query_dsl.BoolQuery").Properties!["must"]);

		annotation.Text.Should().Be("{} Query[]");
		annotation.Spans.Should().Contain(s => s.Text == "Query" && s.CssClass == "type-linked");
	}

	[Test]
	public void Describe_LinkedType_MarksNameAsTypeLinked()
	{
		var builder = CreateBuilder();

		var annotation = builder.Describe(Schema("fixture.SearchRequestBody").Properties!["query"]);

		annotation.Spans.Should().Contain(s => s.Text == "Query" && s.CssClass == "type-linked");
		annotation.Spans.Should().Contain(s => s.Text == "{}" && s.CssClass != null && s.CssClass.Contains("type-wrapper"));
		annotation.Spans.Should().OnlyContain(s => s.Title == null, "a type's schema id never shows, not even as a tooltip");
	}

	[Test]
	public void Describe_DictionaryOfLinkedType_SplitsMapFormulaIntoAtoms()
	{
		var builder = CreateBuilder();

		var annotation = builder.Describe(Schema("fixture.SearchRequestBody").Properties!["aggs"]);

		annotation.Text.Should().Be("map string to {} Aggregation");
		annotation.Spans.Should().Contain(s => s.Text == "map" && s.CssClass == SchemaHelpers.WrapperMapKeywordCssClass);
		annotation.Spans.Should().Contain(s => s.Text == " to " && s.Bare);
		annotation.Spans.Should().Contain(s => s.Text == "{}" && s.CssClass == SchemaHelpers.WrapperObjectIconCssClass);
		annotation.Spans.Should().Contain(s => s.Text == "Aggregation" && s.CssClass == "type-linked");
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
		variants.Variants.Should().HaveCount(2);
		variants.UseChips.Should().BeTrue("two object variants show one at a time behind chips");
		variants.Variants.Select(v => v.DisplayName).Should().BeEquivalentTo(["TermsAggregate", "MaxAggregate"]);
	}

	[Test]
	public void BuildUnionVariants_CodegenOneOf_NamesReferencedVariantsByTheirSchema()
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
		variants.Variants.Select(v => v.DisplayName).Should().BeEquivalentTo(["PlatformErrorResponse", "SiemErrorResponse"]);
		variants.Variants.Should().AllSatisfy(v => v.ShowProperties.Should().BeTrue());
		variants.UseChips.Should().BeTrue("two object variants show one at a time behind chips");
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
	public void BuildPropertyList_AllOfUnionRef_ReadsLikeTheUnionReferencedDirectly()
	{
		var builder = CreateBuilder();

		var list = builder.BuildPropertyList(Schema("fixture.AllOfUnionBody"), new PropertyTreeScope { Prefix = "req", IsRequest = true });

		var direct = list!.Items.Single(p => p.Name == "direct");
		var wrapped = list.Items.Single(p => p.Name == "text");
		wrapped.Union.Should().NotBeNull("the One of: row names the union's options");
		wrapped.Type.Spans.Select(s => s.Text).Should().Equal(direct.Type.Spans.Select(s => s.Text));
		wrapped.Union.Should().BeEquivalentTo(direct.Union);
	}

	[Test]
	public void BuildPropertyList_AllOfWrappedRecursiveUnion_Terminates()
	{
		var builder = CreateBuilder();

		var list = builder.BuildPropertyList(
			Schema("fixture.WrappedUnionEdgesBody"),
			new PropertyTreeScope { Prefix = "req", IsRequest = true }
		);

		list!.Items.Single(p => p.Name == "nested").Union.Should().NotBeNull();
	}

	[Test]
	public void BuildPropertyList_AllOfWrapperWithItsOwnDiscriminator_KeepsIt()
	{
		var builder = CreateBuilder();

		var list = builder.BuildPropertyList(
			Schema("fixture.WrappedUnionEdgesBody"),
			new PropertyTreeScope { Prefix = "req", IsRequest = true }
		);

		var selected = list!.Items.Single(p => p.Name == "selected");
		selected.Union!.DiscriminatorProperty.Should().Be("kind");
		selected.Children.Variants!.Variants.Select(v => v.DiscriminatorLabel).Should().Equal("kind: a", "kind: b");
	}

	[Test]
	public void BuildPropertyList_AllOfWrapperWithItsOwnRequiredSets_KeepsThem()
	{
		var builder = CreateBuilder();

		var list = builder.BuildPropertyList(
			Schema("fixture.WrappedUnionEdgesBody"),
			new PropertyTreeScope { Prefix = "req", IsRequest = true }
		);

		var needs = list!.Items.Single(p => p.Name == "needs");
		needs.Requires.Should().BeEquivalentTo(new RequiredAlternatives("Requires exactly one of:", ["a", "b"]));
	}

	[Test]
	public void BuildPropertyList_AllOfWrappedUnionWithSharedFields_ListsThemLikeTheDirectRef()
	{
		var builder = CreateBuilder();

		var list = builder.BuildPropertyList(
			Schema("fixture.WrappedUnionEdgesBody"),
			new PropertyTreeScope { Prefix = "req", IsRequest = true }
		);

		var direct = list!.Items.Single(p => p.Name == "sharedDirect");
		var wrapped = list.Items.Single(p => p.Name == "shared");
		direct.Children.Properties!.Items.Select(p => p.Name).Should().Contain("common");
		wrapped.Children.Kind.Should().Be(direct.Children.Kind);
		wrapped.Children.Properties!.Items.Select(p => p.Name).Should().Contain("common");
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
		group.Type.Text.Should().Be("enum[]");
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
	public void BuildConstraints_StringAndArrayBounds_NameWhatTheyBound()
	{
		var text = new OpenApiSchema { Type = JsonSchemaType.String, MinLength = 1, MaxLength = 50 };
		var items = new OpenApiSchema { Type = JsonSchemaType.Array, MinItems = 1, MaxItems = 100 };

		ApiPropertyTreeBuilder.BuildConstraints(text).Select(c => c.Text).Should().Equal("min length: 1", "max length: 50");
		ApiPropertyTreeBuilder.BuildConstraints(items).Select(c => c.Text).Should().Equal("min items: 1", "max items: 100");
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
	public void BuildConstraints_Format_ReadsFirstBeforeTheDefault()
	{
		var schema = new OpenApiSchema { Type = JsonSchemaType.String, Format = "date-time", Default = "now" };

		ApiPropertyTreeBuilder.BuildConstraints(schema).Select(c => c.Text).Should().Equal("format: date-time", "default: now");
		CreateBuilder().Describe(schema).Text.Should().Be("string · format: date-time · default: now");
	}

	[Test]
	public async Task BuildPropertyList_LongObjectDefault_ListsItInAWrappingRowNotAChip()
	{
		var document = await LoadSpecAsync(
			WithSchemas(
				"""
				{ "Holder": { "type": "object", "properties": {
				    "options": { "type": "object", "default": { "auto_apply_filters": true, "use_margins": true, "sync_cursor": true } },
				    "small": { "type": "object", "default": { "a": 1 } } } } }
				"""
			)
		);

		var list = BuilderFor(document).BuildPropertyList(document.Components!.Schemas!["Holder"], new PropertyTreeScope { Prefix = "" })!;

		var options = list.Items.Single(p => p.Name == "options");
		options.DefaultValue.Should().Be("""{"auto_apply_filters":true,"use_margins":true,"sync_cursor":true}""");
		options.Type.Text.Should().NotContain("default", "a chip would cut it");
		var small = list.Items.Single(p => p.Name == "small");
		small.DefaultValue.Should().BeNull();
		small.Type.Text.Should().EndWith("""default: {"a":1}""");
	}

	[Test]
	public void Describe_LongObjectDefault_KeepsItAsAChipWhereARowHasNoDefaultRow()
	{
		var schema = new OpenApiSchema
		{
			Type = JsonSchemaType.Object,
			Default = new JsonObject { ["auto_apply_filters"] = true, ["use_margins"] = true, ["sync_cursor"] = true }
		};

		CreateBuilder().Describe(schema).Text.Should().Contain("""default: {"auto_apply_filters":true""");
		ApiPropertyTreeBuilder.BuildConstraints(schema).Single().Text.Should().NotContain("\n", "one line, as a chip reads it");
	}

	[Test]
	public void BuildConstraints_EmptyObjectAndScalarDefaults_StayInTheChip()
	{
		ApiPropertyTreeBuilder.BuildConstraints(new OpenApiSchema { Default = new JsonObject() }).Single().Text.Should().Be("default: {}");
		ApiPropertyTreeBuilder.BuildConstraints(new OpenApiSchema { Default = 5 }).Single().Text.Should().Be("default: 5");
	}

	[Test]
	public void BuildConstraints_ArrayWithFormattedItems_TakesTheItemsFormat()
	{
		var schema = new OpenApiSchema
		{
			Type = JsonSchemaType.Array,
			Items = new OpenApiSchema { Type = JsonSchemaType.String, Format = "uri" }
		};

		ApiPropertyTreeBuilder.BuildConstraints(schema).Select(c => c.Text).Should().Equal("format: uri");
		CreateBuilder().Describe(schema).Text.Should().Be("string[] · format: uri");
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

		builder.Describe(names).Text.Should().Be("union oneOf");
		builder.Describe(name).Text.Should().Be("string");

		string PathType(IOpenApiSchema schema) => PathRow(builder, schema).Type.Text;

		string QueryType(IOpenApiSchema schema) => ParameterRow(builder, schema, ParameterLocation.Query).Type.Text;

		PathType(names).Should().Be("union string | string[]");
		PathType(dataStreams).Should().Be("union string | string[]");
		PathType(document.Components!.Schemas!["inline.StringOrArray"]).Should().Be("union string | string[]");
		PathType(name).Should().Be("string");
		PathType(new OpenApiSchema { Type = JsonSchemaType.String }).Should().Be("string");
		QueryType(names).Should().Be("union string | string[]", "a query row reads like a path row");
	}

	private static ApiProperty PathRow(ApiPropertyTreeBuilder builder, IOpenApiSchema schema) =>
		ParameterRow(builder, schema, ParameterLocation.Path);

	private static ApiProperty ParameterRow(ApiPropertyTreeBuilder builder, IOpenApiSchema schema, ParameterLocation location) =>
		builder
			.BuildParameterList(
				[new ParameterRowInput(new OpenApiParameter { Name = "p", In = location, Required = true, Schema = schema }, null)],
				new PropertyTreeScope { Prefix = location.ToString().ToLowerInvariant(), IsRequest = true }
			)
			.Items
			.Single();

	[Test]
	[Arguments(false)]
	[Arguments(true)]
	public async Task BuildPropertyList_ObjectRows_ExpandOnlyWhenRequired(bool isRequest)
	{
		var builder = CreateBuilder();
		var list = builder.BuildPropertyList(ObjectsAtTwoDepths(), new PropertyTreeScope { Prefix = "body", IsRequest = isRequest });

		list.Should().NotBeNull();
		var html = await _PropertyList.Create(list!).RenderAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);

		AssertExpansion(list.Items, html);
		var group = list.Items.Single(p => p.Name == "group");
		group.Children.Properties.Should().NotBeNull();
		AssertExpansion(group.Children.Properties!.Items, html);
	}

	[Test]
	public async Task PropertyItem_Name_IsNotInsideAnchorLink()
	{
		var builder = CreateBuilder();
		var list = builder.BuildPropertyList(ObjectsAtTwoDepths(), new PropertyTreeScope { Prefix = "body", IsRequest = true });

		list.Should().NotBeNull();
		var html = await _PropertyList.Create(list!).RenderAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);

		var anchorId = list.Items.Single(p => p.Name == "only").AnchorId;
		html.Should().Contain($"href=\"#{anchorId}\" aria-label=\"Link to only\"");
		html.Should().MatchRegex("</a>\\s*<code[^>]*>only</code>");
	}

	private static void AssertExpansion(IReadOnlyList<ApiProperty> items, string html)
	{
		AssertToggle(html, items.Single(p => p.Name == "small"), expanded: true);
		AssertToggle(html, items.Single(p => p.Name == "large"), expanded: false);
		AssertToggle(html, items.Single(p => p.Name == "only"), expanded: false);
	}

	private static void AssertToggle(string html, ApiProperty property, bool expanded)
	{
		property.IsCollapsible.Should().BeTrue();
		property.DefaultExpanded.Should().Be(expanded, $"{property.Name} is {(property.IsRequired ? "" : "not ")}required");
		var match = System
			.Text
			.RegularExpressions
			.Regex
			.Match(html, $"id=\"{property.AnchorId}\"([\\s\\S]*?)aria-expanded=\"(true|false)\"([\\s\\S]*?)(Show|Hide) \\d+ propert");
		match.Success.Should().BeTrue($"property {property.AnchorId} renders an expand control");
		match.Groups[2].Value.Should().Be(expanded ? "true" : "false");
		var count = property.NestedCount;
		html.Should().Contain($"{(expanded ? "Hide" : "Show")} {count} {(count == 1 ? "property" : "properties")}");
		match.Groups[4].Value.Should().Be(expanded ? "Hide" : "Show");
		html.Should().Contain($"id=\"{property.AnchorId}--children\"");
		html.Should().Contain(
			expanded ? $"id=\"{property.AnchorId}--children\">" : $"id=\"{property.AnchorId}--children\" hidden=\"until-found\""
		);
	}

	[Test]
	public void BuildPropertyList_RequiredObjectInsideAnOptionalOne_StaysFolded()
	{
		var builder = CreateBuilder();
		var optionalMid = new OpenApiSchema
		{
			Type = JsonSchemaType.Object,
			Required = new HashSet<string> { "needed" },
			Properties = new Dictionary<string, IOpenApiSchema> { ["needed"] = StringFields("one"), ["extra"] = StringFields("one") }
		};
		var schema = new OpenApiSchema
		{
			Type = JsonSchemaType.Object,
			Required = new HashSet<string> { "top" },
			Properties = new Dictionary<string, IOpenApiSchema>
			{
				["top"] = new OpenApiSchema
				{
					Type = JsonSchemaType.Object,
					Properties = new Dictionary<string, IOpenApiSchema> { ["optional_mid"] = optionalMid }
				}
			}
		};

		var list = builder.BuildPropertyList(schema, new PropertyTreeScope { Prefix = "body", IsRequest = true });

		var mid = list!.Items.Single().Children.Properties!.Items.Single();
		mid.DefaultExpanded.Should().BeFalse("it is optional, so the page leaves it folded");
		var children = mid.Children.Properties!.Items;
		children
			.Single(p => p.Name == "needed")
			.DefaultExpanded
			.Should()
			.BeFalse("it is only required inside an optional object, so the chain is not required");
		children.Single(p => p.Name == "extra").DefaultExpanded.Should().BeFalse();
	}

	[Test]
	public void BuildPropertyList_RequiredObjectUnderAUnionRowThatCannotFold_OpensWithTheRow()
	{
		var builder = CreateBuilder();
		var union = new OpenApiSchema
		{
			Type = JsonSchemaType.Object,
			Required = new HashSet<string> { "inner" },
			Properties = new Dictionary<string, IOpenApiSchema> { ["inner"] = StringFields("one", "two") },
			OneOf =
			[
				new OpenApiSchema
				{
					Type = JsonSchemaType.Object,
					Properties = new Dictionary<string, IOpenApiSchema> { ["a"] = StringFields("x") }
				},
				new OpenApiSchema
				{
					Type = JsonSchemaType.Object,
					Properties = new Dictionary<string, IOpenApiSchema> { ["b"] = StringFields("y") }
				}
			]
		};
		var schema = new OpenApiSchema
		{
			Type = JsonSchemaType.Object,
			Properties = new Dictionary<string, IOpenApiSchema> { ["u"] = union }
		};

		var list = builder.BuildPropertyList(schema, new PropertyTreeScope { Prefix = "body", IsRequest = true });

		var row = list!.Items.Single();
		row.IsCollapsible.Should().BeFalse("a union row lists its shared fields without a toggle");
		row.DefaultExpanded.Should().BeFalse("it is optional");
		row.Children.Properties!.Items.Single(p => p.Name == "inner").DefaultExpanded.Should().BeTrue("the row above it is showing");
	}

	[Test]
	public void BuildPropertyList_RequiredObjectInAVariantUnderAFoldedRow_StaysFolded()
	{
		var builder = CreateBuilder();

		OpenApiSchema Variant(string field) =>
			new()
			{
				Type = JsonSchemaType.Object,
				Required = new HashSet<string> { "needed" },
				Properties = new Dictionary<string, IOpenApiSchema> { ["needed"] = StringFields("one", "two"), [field] = StringFields("x") }
			};

		var schema = new OpenApiSchema
		{
			Type = JsonSchemaType.Object,
			Properties = new Dictionary<string, IOpenApiSchema>
			{
				["optional_parent"] = new OpenApiSchema
				{
					Type = JsonSchemaType.Object,
					Properties = new Dictionary<string, IOpenApiSchema>
					{
						["choice"] = new OpenApiSchema { OneOf = [Variant("a"), Variant("b")] },
						["other"] = StringFields("one")
					}
				}
			}
		};

		var list = builder.BuildPropertyList(schema, new PropertyTreeScope { Prefix = "body", IsRequest = true });

		var parent = list!.Items.Single();
		parent.DefaultExpanded.Should().BeFalse("it is optional");
		var choice = parent.Children.Properties!.Items.Single(p => p.Name == "choice");
		var variants = choice.Children.Variants!.Variants;
		variants.Should().NotBeEmpty();
		variants
			.SelectMany(v => v.Properties!.Items)
			.Where(p => p.Name == "needed")
			.Should()
			.OnlyContain(p => !p.DefaultExpanded, "the optional parent above it is folded");
	}

	[Test]
	public void BuildPropertyList_VariantToggleCount_LeavesOutFieldsTheRequestDoesNotList()
	{
		var builder = CreateBuilder();

		OpenApiSchema Variant(string extra) =>
			new()
			{
				Type = JsonSchemaType.Object,
				Properties = new Dictionary<string, IOpenApiSchema>
				{
					["id"] = new OpenApiSchema { Type = JsonSchemaType.String, ReadOnly = true },
					["name"] = new OpenApiSchema { Type = JsonSchemaType.String },
					[extra] = new OpenApiSchema { Type = JsonSchemaType.String }
				}
			};

		var schema = new OpenApiSchema
		{
			Type = JsonSchemaType.Object,
			Properties = new Dictionary<string, IOpenApiSchema> { ["choice"] = new OpenApiSchema { OneOf = [Variant("a"), Variant("b")] } }
		};

		var list = builder.BuildPropertyList(schema, new PropertyTreeScope { Prefix = "body", IsRequest = true });

		var variants = list!.Items.Single().Children.Variants!.Variants;
		variants.Should().OnlyContain(v => v.NestedCount == 2, "id is readOnly, so a request lists name and one more");
		variants.Should().OnlyContain(v => v.Properties!.Items.Count == 2);
	}

	private static OpenApiSchema ObjectsAtTwoDepths() =>
		new()
		{
			Type = JsonSchemaType.Object,
			Required = new HashSet<string> { "small", "group" },
			Properties = new Dictionary<string, IOpenApiSchema>
			{
				["only"] = StringFields("one"),
				["small"] = StringFields("one", "two"),
				["large"] = StringFields("one", "two", "three", "four", "five", "six"),
				["group"] = new OpenApiSchema
				{
					Type = JsonSchemaType.Object,
					Required = new HashSet<string> { "small" },
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
