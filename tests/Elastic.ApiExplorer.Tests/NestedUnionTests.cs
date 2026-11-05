// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;
using AwesomeAssertions;
using Elastic.ApiExplorer.Components.PropertyTree;
using static Elastic.ApiExplorer.Tests.TestSpecs;

namespace Elastic.ApiExplorer.Tests;

/// <summary>
/// Unions that sit behind another shape still list their variants: a named array of a union, a map of a union, and a union
/// variant that is itself a union (the Kibana dashboard, field settings and chart filter shapes).
/// </summary>
public class NestedUnionTests
{
	private const string Schemas =
		"""
		{
		  "Cond": { "type": "object", "properties": { "type": { "type": "string", "enum": ["condition"] }, "negate": { "type": "boolean" } } },
		  "Group": { "type": "object", "properties": { "type": { "type": "string", "enum": ["group"] }, "group": { "type": "string" } } },
		  "Filter": {
		    "discriminator": { "propertyName": "type" },
		    "oneOf": [ { "$ref": "#/components/schemas/Cond" }, { "$ref": "#/components/schemas/Group" } ]
		  },
		  "Filters": { "type": "array", "maxItems": 100, "items": { "$ref": "#/components/schemas/Filter" } },
		  "Setting": { "type": "object", "title": "Field settings", "properties": { "custom_label": { "type": "string" } } },
		  "Runtime": { "type": "object", "title": "Runtime field", "properties": { "script": { "type": "string" } } },
		  "Entry": { "anyOf": [ { "$ref": "#/components/schemas/Runtime" }, { "$ref": "#/components/schemas/Setting" } ] },
		  "ChartDsl": { "type": "object", "title": "Metric (DSL)", "properties": { "data_source": { "type": "string" } } },
		  "ChartEsql": { "type": "object", "title": "Metric (ES|QL)", "properties": { "esql": { "type": "string" } } },
		  "Metric": { "title": "Metric", "anyOf": [ { "$ref": "#/components/schemas/ChartDsl" }, { "$ref": "#/components/schemas/ChartEsql" } ] },
		  "Gauge": { "type": "object", "title": "Gauge", "properties": { "goal": { "type": "number" } } },
		  "Config": { "anyOf": [ { "$ref": "#/components/schemas/Metric" }, { "$ref": "#/components/schemas/Gauge" } ] },
		  "Loop": { "anyOf": [ { "$ref": "#/components/schemas/Metric" }, { "$ref": "#/components/schemas/LoopLeaf" } ] },
		  "LoopLeaf": { "type": "object", "properties": { "again": { "$ref": "#/components/schemas/Loop" } } },
		  "Holder": {
		    "type": "object",
		    "properties": {
		      "named_filters": { "$ref": "#/components/schemas/Filters" },
		      "inline_filters": { "type": "array", "items": { "$ref": "#/components/schemas/Filter" } },
		      "field_settings": { "type": "object", "additionalProperties": { "$ref": "#/components/schemas/Entry" } },
		      "config": {
		        "anyOf": [
		          {
		            "allOf": [
		              { "$ref": "#/components/schemas/Config" },
		              { "type": "object", "properties": { "title": { "type": "string" } } }
		            ]
		          },
		          { "type": "object", "properties": { "ref_id": { "type": "string" } } }
		        ]
		      },
		      "loop": { "$ref": "#/components/schemas/Loop" }
		    }
		  }
		}
		""";

	[Test]
	public async Task BuildPropertyList_RefToArrayOfUnion_ListsTheItemVariantsLikeAnInlineArray()
	{
		var named = await RowAsync("named_filters");
		var inline = await RowAsync("inline_filters");

		named.Children.Variants!
			.Variants
			.Select(v => v.DisplayName)
			.Should()
			.Equal(inline.Children.Variants!.Variants.Select(v => v.DisplayName));
		named.Children.Variants.Variants.Select(v => v.ChipName).Should().Equal("type: condition", "type: group");
		named.Type.Text.Should().StartWith(inline.Type.Text, "the named array reads like the inline one, plus its own limits");
		named.Children.Variants.Variants[0].Properties!.Items.Select(p => p.Name).Should().Equal("type", "negate");
	}

	[Test]
	public async Task BuildPropertyList_MapOfUnionValue_ListsVariantsUnderTheKeyRow()
	{
		var row = await RowAsync("field_settings");

		row.Children.Kind.Should().Be(ChildKind.Dictionary);
		row.Children.Dictionary!.IsCollapsible.Should().BeTrue("the variants count as the key row's nested rows");
		var variants = row.Children.Dictionary!.Variants!.Variants;
		variants.Select(v => v.DisplayName).Should().Equal("Runtime field", "Field settings");
		var label = variants[1].Properties!.Items.Single();
		label.Name.Should().Be("custom_label");
		label.JsonPath.Should().Be("field_settings.<string>.custom_label");
	}

	[Test]
	public async Task BuildPropertyList_VariantThatIsAUnion_ListsItsOwnFieldsThenItsVariants()
	{
		var config = (await RowAsync("config")).Children.Variants!.Variants;

		var byValue = config[0];
		byValue.Properties!.Items.Select(p => p.Name).Should().Equal("title");
		byValue.Variants!.Variants.Select(v => v.DisplayName).Should().Equal("Metric", "Gauge");
		var metric = byValue.Variants.Variants[0];
		metric.Properties!.Items.Should().BeEmpty("a chart that is only a union has no fields of its own");
		metric.Variants!.Variants.Select(v => v.DisplayName).Should().Equal("Metric (DSL)", "Metric (ES|QL)");
		metric.Variants.Variants[1].Properties!.Items.Select(p => p.Name).Should().Equal("esql");
		config[1].Variants.Should().BeNull();
	}

	[Test]
	public async Task BuildPropertyList_NestedVariantsShareNoAnchorIds()
	{
		var config = (await RowAsync("config")).Children.Variants!;

		var anchors = Anchors(config).ToArray();

		anchors.Should().OnlyHaveUniqueItems();
		anchors.Should().Contain(a => a.EndsWith("-title"));
	}

	[Test]
	public async Task BuildPropertyList_UnionThatReachesItselfThroughAVariant_Terminates()
	{
		var row = await RowAsync("loop");

		row.Children.Variants.Should().NotBeNull();
	}

	[Test]
	public async Task Flatten_NestedVariants_IndexTheirFieldsUnderTheRow()
	{
		var holder = await HolderAsync();

		var paths = ApiPropertyIndex.Flatten(holder).Select(r => r.Property.Name).ToArray();

		paths.Should().Contain(["title", "esql", "data_source", "custom_label"]);
	}

	[Test]
	public async Task WriteList_NestedVariants_ListsTheirFields()
	{
		var markdown = new StringBuilder();

		ApiPropertyMarkdown.WriteList(markdown, await HolderAsync(), "/docs/api");

		markdown.ToString().Should().Contain("Metric (ES|QL)").And.Contain("`esql`").And.Contain("`custom_label`");
	}

	private static IEnumerable<string> Anchors(ApiUnionVariants variants) =>
		variants.Variants.SelectMany(
			v => new[] { v.AnchorId }.Concat((v.Properties?.Items ?? []).Select(p => p.AnchorId)).Concat(
				v.Variants is null ? [] : Anchors(v.Variants)
			)
		);

	private static async Task<ApiPropertyList> HolderAsync()
	{
		var document = await LoadSpecAsync(WithSchemas(Schemas));
		return BuilderFor(document).BuildPropertyList(
			document.Components!.Schemas!["Holder"],
			new PropertyTreeScope { Prefix = "", JsonPath = "" }
		)!;
	}

	private static async Task<ApiProperty> RowAsync(string name) => (await HolderAsync()).Items.Single(p => p.Name == name);
}
