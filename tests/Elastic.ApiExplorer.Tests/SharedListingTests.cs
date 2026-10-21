// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Components.PropertyTree;
using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Operations;
using Microsoft.AspNetCore.Html;
using Microsoft.OpenApi;
using static Elastic.ApiExplorer.Tests.TestSpecs;

namespace Elastic.ApiExplorer.Tests;

/// <summary>A large structure is listed once per page; later copies link to it only when they are identical.</summary>
public class SharedListingTests
{
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
}
