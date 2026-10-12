// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Components.PropertyTree;
using static Elastic.ApiExplorer.Tests.TestSpecs;

namespace Elastic.ApiExplorer.Tests;

/// <summary>How the options of a union read in its "One of:" row and type.</summary>
public class UnionOptionTests
{
	private const string Spec =
		"""
		{
		  "openapi": "3.1.0",
		  "info": { "title": "t", "version": "1" },
		  "paths": {},
		  "components": {
		    "schemas": {
		      "_types.NodeId": { "type": "string" },
		      "_types.NodeIds": { "oneOf": [
		        { "$ref": "#/components/schemas/_types.NodeId" },
		        { "type": "array", "items": { "$ref": "#/components/schemas/_types.NodeId" } } ] },
		      "Security_Detections_API_AlertsSortCombinations": { "type": ["string", "object"] },
		      "Security_Detections_API_AlertsSort": { "oneOf": [
		        { "$ref": "#/components/schemas/Security_Detections_API_AlertsSortCombinations" },
		        { "type": "array", "items": { "$ref": "#/components/schemas/Security_Detections_API_AlertsSortCombinations" } } ] },
		      "Cases_string": { "type": "string" },
		      "Cases_string_array": { "type": "array", "items": { "$ref": "#/components/schemas/Cases_string" } },
		      "_spec_utils.Stringifieddouble": { "type": ["number", "string"] },
		      "Holder": {
		        "type": "object",
		        "properties": {
		          "nodes": { "$ref": "#/components/schemas/_types.NodeIds" },
		          "sort": { "$ref": "#/components/schemas/Security_Detections_API_AlertsSort" },
		          "node_or_count": { "oneOf": [ { "$ref": "#/components/schemas/_types.NodeId" }, { "type": "integer" } ] },
		          "assignees": { "oneOf": [
		            { "$ref": "#/components/schemas/Cases_string" },
		            { "$ref": "#/components/schemas/Cases_string_array" } ] },
		          "load": { "oneOf": [ { "$ref": "#/components/schemas/_spec_utils.Stringifieddouble" }, { "type": "boolean" } ] }
		        }
		      }
		    }
		  }
		}
		""";

	private static async Task<ApiProperty> PropertyAsync(string name)
	{
		var document = await LoadSpecAsync(Spec);
		var list = BuilderFor(document).BuildPropertyList(document.Components!.Schemas!["Holder"], new PropertyTreeScope { Prefix = "" });
		return list!.Items.Single(p => p.Name == name);
	}

	private static string[] Badges(ApiProperty property) => [.. property.Union!.Badges.Select(static b => b.Text)];

	[Test]
	public async Task BuildPropertyList_UnionOfAliasAndItsArray_KeepsTheAliasName()
	{
		var nodes = await PropertyAsync("nodes");

		Badges(nodes).Should().Equal("NodeId[]", "NodeId");
	}

	[Test]
	public async Task BuildPropertyList_NamedArrayUnionThatDoesNotExpand_StillListsItsOptions()
	{
		var sort = await PropertyAsync("sort");

		Badges(sort).Should().Equal("AlertsSortCombinations[]", "AlertsSortCombinations");
	}

	[Test]
	public async Task BuildPropertyList_InlineUnionWithAlias_NamesTheAliasInItsType()
	{
		var property = await PropertyAsync("node_or_count");

		property.Type.Text.Should().Be("union NodeId | integer");
		property.Union.Should().BeNull("the type already reads NodeId | integer, so an options row would only repeat it");
	}

	[Test]
	public async Task BuildPropertyList_CodegenAliasWithNoReadableName_ShowsItsPrimitive()
	{
		var assignees = await PropertyAsync("assignees");

		assignees.Type.Text.Should().Be("union string | [] string");
	}

	[Test]
	public async Task BuildPropertyList_AliasOfSeveralPrimitives_SpellsThemOut()
	{
		var load = await PropertyAsync("load");

		Badges(load).Should().Equal("number | string", "boolean");
	}
}
