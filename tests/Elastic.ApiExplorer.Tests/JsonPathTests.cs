// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Components.PropertyTree;
using Elastic.ApiExplorer.Components.PropertyTree._Partials;
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
		      "Root": {
		        "type": "object",
		        "properties": {
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
	public async Task Render_RowWithoutPath_LeavesTooltipEmpty()
	{
		var list = await BuildAsync(null);

		var html = await _PropertyList.Create(list).RenderAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);

		html.Should().Contain("""<code data-tippy-content="" data-tippy-theme="mono">bool</code>""", "tooltip.ts skips an empty tooltip");
	}
}
