// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Components.PropertyTree;
using static Elastic.ApiExplorer.Tests.TestSpecs;

namespace Elastic.ApiExplorer.Tests;

/// <summary>
/// A map reads <c>map string to X</c> whether the spec names it or not, and lists a <c>&lt;string&gt;</c> row when its
/// values say more than their type: fields, limits or allowed values.
/// </summary>
public class MapValueRowTests
{
	private const string Spec =
		"""
		{
		  "openapi": "3.0.3",
		  "info": { "title": "t", "version": "1" },
		  "paths": {
		    "/items": {
		      "get": {
		        "operationId": "list-items",
		        "parameters": [
		          {
		            "name": "filter",
		            "in": "query",
		            "style": "deepObject",
		            "required": true,
		            "deprecated": true,
		            "description": "Narrows the items.",
		            "schema": { "$ref": "#/components/schemas/Filter" }
		          },
		          { "name": "size", "in": "query", "schema": { "type": "integer", "maximum": 100 } }
		        ],
		        "responses": { "200": { "description": "ok" } }
		      }
		    }
		  },
		  "components": {
		    "schemas": {
		      "Filter": {
		        "type": "object",
		        "description": "The schema's own description.",
		        "required": ["status"],
		        "properties": { "status": { "type": "string" }, "owner": { "type": "string" } }
		      },
		      "Labels": { "type": "object", "additionalProperties": { "type": "string", "maxLength": 32 } },
		      "Modes": { "type": "object", "additionalProperties": { "type": "string", "enum": ["read", "write"] } },
		      "Plain": { "type": "object", "additionalProperties": { "type": "string" } },
		      "Defaulted": { "type": "object", "additionalProperties": { "type": "string", "default": "none" } },
		      "Tree": { "type": "object", "additionalProperties": { "$ref": "#/components/schemas/Tree" } },
		      "Holder": {
		        "type": "object",
		        "properties": {
		          "labels": { "$ref": "#/components/schemas/Labels" },
		          "modes": { "$ref": "#/components/schemas/Modes" },
		          "plain": { "$ref": "#/components/schemas/Plain" },
		          "defaulted": { "$ref": "#/components/schemas/Defaulted" },
		          "tree": { "$ref": "#/components/schemas/Tree" }
		        }
		      }
		    }
		  }
		}
		""";

	[Test]
	public async Task BuildPropertyList_ReferencedMapWithLimitedValues_ListsValueRow()
	{
		var row = await HolderRowAsync("labels");

		row.Type.Text.Should().Be("map string to string", "the spec's name for the map never shows");
		row.Children.Kind.Should().Be(ChildKind.Dictionary);
		row.Children.Dictionary!.ValueType.Text.Should().Be("string · max length: 32");
	}

	[Test]
	public async Task BuildPropertyList_ReferencedMapOfEnum_ListsValuesUnderKeyRow()
	{
		var row = await HolderRowAsync("modes");

		row.Children.Dictionary!.EnumValues.Should().Equal("read", "write");
	}

	[Test]
	[Arguments("plain")]
	[Arguments("defaulted")]
	public async Task BuildPropertyList_MapValueSaysNoMoreThanItsType_HasNoValueRow(string name)
	{
		var row = await HolderRowAsync(name);

		row.Type.Text.Should().StartWith("map string to string");
		row.Children.Kind.Should().Be(ChildKind.None, "a default alone does not say what a value may be");
	}

	[Test]
	public async Task BuildPropertyList_SelfReferencingMap_Terminates()
	{
		var row = await HolderRowAsync("tree");

		row.Type.Text.Should().StartWith("map string to");
	}

	[Test]
	public async Task BuildParameterList_ObjectParameter_ListsFieldsAndKeepsParameterFlags()
	{
		var document = await LoadSpecAsync(Spec);
		var parameters = document.Paths!["/items"].Operations![HttpMethod.Get].Parameters!.Select(p => (p, p.Description)).ToArray();

		var list = BuilderFor(document).BuildParameterList(parameters, new PropertyTreeScope { Prefix = "query" });

		var filter = list.Items[0];
		filter.AnchorId.Should().Be("query-filter");
		filter.IsRequired.Should().BeTrue();
		filter.ShowDeprecatedBadge.Should().BeTrue();
		filter.DescriptionMarkdown.Should().Be("Narrows the items.");
		filter.IsCollapsible.Should().BeTrue();
		filter.Children.Properties!.Items.Select(p => (p.Name, p.IsRequired)).Should().Equal(("status", true), ("owner", false));
		filter.Children.Properties.Items[0].AnchorId.Should().Be("query-filter-status");

		var size = list.Items[1];
		size.Type.Text.Should().Be("integer · max: 100");
		size.IsLast.Should().BeTrue();
		size.Children.Kind.Should().Be(ChildKind.None);
	}

	private static async Task<ApiProperty> HolderRowAsync(string name)
	{
		var document = await LoadSpecAsync(Spec);
		var list = BuilderFor(document).BuildPropertyList(document.Components!.Schemas!["Holder"], new PropertyTreeScope { Prefix = "" })!;
		return list.Items.Single(p => p.Name == name);
	}
}
