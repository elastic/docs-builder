// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Schema;
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
			ApiRootUrl = "/api/fixture",
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

		var list = builder.BuildPropertyList(Schema("_types.query_dsl.QueryContainer"), "", isRequest: false, ancestors: ancestors);

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

		var list = builder.BuildPropertyList(Schema("fixture.SearchRequestBody"), "req", isRequest: true);

		var fields = list!.Items.Single(p => p.Name == "fields");
		fields.Union.Should().NotBeNull();
		fields.Union!.Kind.Should().Be(UnionDisplayKind.SimpleArrayUnion);
		fields.Union.SimpleUnionBaseName.Should().Be("Field");
		fields.Union.SimpleUnionValueTypePrefix.Should().Be("string ");
		fields.AnchorId.Should().Be("req-fields");
	}

	[Fact]
	public void BuildPropertyList_DictionaryOfLinkedType_LinksInsteadOfExpanding()
	{
		var builder = CreateBuilder();

		var list = builder.BuildPropertyList(Schema("fixture.SearchRequestBody"), "req", isRequest: true);

		var aggs = list!.Items.Single(p => p.Name == "aggs");
		aggs.Children.Kind.Should().Be(ChildKind.None, "the dictionary value type has its own page");
		aggs.TypeLink.Should().NotBeNull();
		aggs.TypeLink!.TypeName.Should().Be("AggregationContainer");
		aggs.TypeLink.Url.Should().Be("/api/fixture/types/_types-aggregations-aggregationcontainer");
	}

	[Fact]
	public void BuildPropertyList_RequiredProperty_IsMarkedRequired()
	{
		var builder = CreateBuilder();

		var list = builder.BuildPropertyList(Schema("fixture.SearchRequestBody"), "req", isRequest: true);

		list!.Items.Single(p => p.Name == "query").IsRequired.Should().BeTrue();
		list.Items.Single(p => p.Name == "sort").IsRequired.Should().BeFalse();
	}

	[Fact]
	public void Describe_EnumSchema_ShowsEnumKeyword()
	{
		var builder = CreateBuilder();

		var annotation = builder.Describe(Schema("fixture.SearchRequestBody").Properties!["mode"]);

		annotation.Spans.Should().Contain(s => s.CssClass == "enum-icon" && s.Text == "enum ");
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
}
