// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Components.PropertyTree;
using Elastic.ApiExplorer.Components.PropertyTree._Partials;
using Elastic.ApiExplorer.Model;
using RazorSlices;
using static Elastic.ApiExplorer.Tests.TestSpecs;

namespace Elastic.ApiExplorer.Tests;

/// <summary>A row's name tells where the field sits in the body: dots for objects, <c>[]</c> for array items, <c>&lt;string&gt;</c> for map keys.</summary>
public class JsonPathTests
{
	private const string Spec =
		"""
		{
		  "openapi": "3.0.3",
		  "info": { "title": "t", "version": "1" },
		  "paths": {},
		  "components": {
		    "schemas": {
		      "Match": { "type": "object", "properties": { "field": { "type": "string" } } },
		      "Bool": { "type": "object", "properties": { "must": { "type": "array", "items": { "$ref": "#/components/schemas/Match" } } } },
		      "Agg": { "type": "object", "properties": { "terms": { "type": "string" } } },
		      "Many": { "type": "array", "items": { "$ref": "#/components/schemas/Match" } },
		      "Cat": { "type": "object", "properties": { "kind": { "type": "string", "enum": ["cat"] }, "lives": { "type": "integer" } } },
		      "Dog": { "type": "object", "properties": { "kind": { "type": "string", "enum": ["dog"] }, "barks": { "type": "boolean" } } },
		      "Pets": { "type": "array", "items": { "oneOf": [ { "$ref": "#/components/schemas/Cat" }, { "$ref": "#/components/schemas/Dog" } ] } },
		      "Root": {
		        "type": "object",
		        "properties": {
		          "pets": { "type": "array", "items": { "oneOf": [ { "$ref": "#/components/schemas/Cat" }, { "$ref": "#/components/schemas/Dog" } ] } },
		          "bool": { "$ref": "#/components/schemas/Bool" },
		          "aggs": { "type": "object", "additionalProperties": { "$ref": "#/components/schemas/Agg" } }
		        }
		      }
		    }
		  }
		}
		""";

	private static async Task<ApiPropertyList> BuildAsync(string? jsonPath)
	{
		var document = await LoadSpecAsync(Spec);
		return BuilderFor(document).BuildPropertyList(
			document.Components!.Schemas!["Root"],
			new PropertyTreeScope { Prefix = "req", JsonPath = jsonPath }
		)!;
	}

	[Test]
	public async Task BuildPropertyList_NestedObjectAndArrayItems_ChainsPath()
	{
		var list = await BuildAsync("");

		var bool_ = list.Items.Single(p => p.Name == "bool");
		var must = bool_.Children.Properties!.Items.Single();
		var field = must.Children.Properties!.Items.Single();

		bool_.JsonPath.Should().Be("bool");
		must.JsonPath.Should().Be("bool.must");
		field.JsonPath.Should().Be("bool.must[].field");
	}

	[Test]
	public async Task BuildPropertyList_MapValue_PathNamesTheKey()
	{
		var list = await BuildAsync("");

		var aggs = list.Items.Single(p => p.Name == "aggs");
		var key = aggs.Children.Dictionary!;

		aggs.JsonPath.Should().Be("aggs");
		key.JsonPath.Should().Be("aggs.<string>");
		key.Properties.Items.Single().JsonPath.Should().Be("aggs.<string>.terms");
	}

	[Test]
	public async Task BuildPropertyList_WithoutRootPath_HasNoPaths()
	{
		var list = await BuildAsync(null);

		list.Items.Select(p => p.JsonPath).Should().AllBeEquivalentTo((string?)null);
		list.Items.Single(p => p.Name == "bool").Children.Properties!.Items.Single().JsonPath.Should().BeNull();
	}

	[Test]
	public async Task Render_Row_PutsPathOnName()
	{
		var list = await BuildAsync("");

		var html = await _PropertyList.Create(list).RenderAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);

		html.Should().Contain("""<code data-tippy-content="bool.must[].field" data-tippy-theme="mono">field</code>""");
	}

	[Test]
	public async Task Render_TopLevelRow_HasNoTooltip()
	{
		var list = await BuildAsync("");

		var html = await _PropertyList.Create(list).RenderAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);

		html.Should().Contain("<code>bool</code>", "a top-level path would only repeat the name");
	}

	[Test]
	public async Task Render_RowWithoutPaths_HasNoTooltip()
	{
		var list = await BuildAsync(null);

		var html = await _PropertyList.Create(list).RenderAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);

		html.Should().Contain("<code>bool</code>").And.NotContain("data-tippy-theme");
	}

	[Test]
	public async Task BuildPropertyList_ArrayOfUnion_VariantFieldsReadAsItems()
	{
		var list = await BuildAsync("");

		var pets = list.Items.Single(p => p.Name == "pets");
		var cat = pets.Children.Variants!.Variants.First(v => v.Properties!.Items.Any(p => p.Name == "lives"));

		cat.Properties!.Items.Single(p => p.Name == "lives").JsonPath.Should().Be("pets[].lives");
	}

	[Test]
	public async Task Build_RootArrayOfUnion_PathStartsWithItems()
	{
		var document = await LoadSpecAsync(Spec);

		var body = ApiBodyContent.Build(
			document.Components!.Schemas!["Pets"],
			new PropertyTreeScope { Prefix = "res-200", JsonPath = "", IsResponse = true },
			new SchemaAnalyzer(document),
			BuilderFor(document)
		);

		body.UnionVariants!.Variants.SelectMany(v => v.Properties!.Items).Select(p => p.JsonPath).Should().Contain("[].lives");
	}

	[Test]
	public async Task Build_RootArrayOfObjects_PathStartsWithItems()
	{
		var document = await LoadSpecAsync(Spec);

		var body = ApiBodyContent.Build(
			document.Components!.Schemas!["Many"],
			new PropertyTreeScope { Prefix = "req", JsonPath = "", IsRequest = true },
			new SchemaAnalyzer(document),
			BuilderFor(document)
		);

		body.Properties!.Items.Single().JsonPath.Should().Be("[].field");
	}

	[Test]
	public async Task Render_Permalink_AnnouncesPath()
	{
		var list = await BuildAsync("");

		var html = await _PropertyList.Create(list).RenderAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);

		html.Should().Contain("""aria-label="Link to bool.must[].field" """.TrimEnd());
	}
}
