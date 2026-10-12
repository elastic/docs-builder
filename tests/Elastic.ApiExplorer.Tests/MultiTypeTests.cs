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

/// <summary>Schemas whose <c>type</c> lists several types (OpenAPI 3.1).</summary>
public class MultiTypeTests
{
	[Test]
	public async Task BuildPropertyList_TypeArrayWithSeveralTypes_ShowsEveryType()
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
			          "defaultValue": { "type": ["boolean", "string"] },
			          "size": { "type": ["number", "string"] },
			          "nullableName": { "type": ["string", "null"] }
			        }
			      }
			    }
			  }
			}
			""";
		var document = await LoadSpecAsync(json);

		var list = BuilderFor(document).BuildPropertyList(document.Components!.Schemas!["Holder"], new PropertyTreeScope { Prefix = "" });

		string TypeOf(string name) => list!.Items.Single(p => p.Name == name).Type.Text;

		TypeOf("defaultValue").Should().Be("boolean | string");
		TypeOf("size").Should().Be("number | string");
		TypeOf("nullableName").Should().Be("string");
	}

	[Test]
	public async Task BuildPropertyList_TypeArrayOfStringAndObject_IsAPrimitiveType()
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
			          "value": { "type": ["string", "object"] },
			          "nested": { "type": "object", "properties": { "value": { "type": ["string", "object"] } } }
			        }
			      }
			    }
			  }
			}
			""";
		var document = await LoadSpecAsync(json);

		var info = new SchemaAnalyzer(document).GetTypeInfo(document.Components!.Schemas!["Holder"].Properties!["value"]);
		info.TypeName.Should().Be("string | object");
		info.IsObject.Should().BeFalse("it has no properties to list");
		SchemaHelpers.IsPrimitiveTypeName(info.TypeName).Should().BeTrue();

		var list = BuilderFor(document).BuildPropertyList(document.Components!.Schemas!["Holder"], new PropertyTreeScope { Prefix = "" })!;
		var nestedValue = list.Items.Single(p => p.Name == "nested").Children.Properties!.Items.Single();
		nestedValue.IsRecursive.Should().BeFalse("a primitive multi-type name is never an ancestor type");
	}
}
