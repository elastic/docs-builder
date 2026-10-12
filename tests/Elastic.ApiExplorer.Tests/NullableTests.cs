// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Components.PropertyTree;
using static Elastic.ApiExplorer.Tests.TestSpecs;

namespace Elastic.ApiExplorer.Tests;

/// <summary>Values that can be <c>null</c> say so with a badge, and a type that names its options has no options row.</summary>
public class NullableTests
{
	[Test]
	public async Task BuildPropertyList_NullableMembers_AreFlaggedAndNotRepeated()
	{
		var json =
			"""
			{
			  "openapi": "3.1.0",
			  "info": { "title": "t", "version": "1" },
			  "paths": {},
			  "components": {
			    "schemas": {
			      "Cat": { "type": "object", "properties": { "lives": { "type": "integer" } } },
			      "Dog": { "type": "object", "properties": { "barks": { "type": "boolean" } } },
			      "Holder": {
			        "type": "object",
			        "properties": {
			          "max_score": { "oneOf": [ { "type": "number" }, { "type": ["null", "string"] } ] },
			          "name": { "type": ["string", "null"] },
			          "count": { "type": "integer" },
			          "pet": { "oneOf": [ { "$ref": "#/components/schemas/Cat" }, { "type": "null" } ] },
			          "kind": {
			            "oneOf": [ { "type": "string" }, { "type": "integer" } ],
			            "discriminator": { "propertyName": "type" }
			          }
			        }
			      }
			    }
			  }
			}
			""";
		var document = await LoadSpecAsync(json);

		var list = BuilderFor(document).BuildPropertyList(document.Components!.Schemas!["Holder"], new PropertyTreeScope { Prefix = "" })!;

		ApiProperty Row(string name) => list.Items.Single(p => p.Name == name);

		Row("max_score").Type.Text.Should().Be("union number | string");
		Row("max_score").Union.Should().BeNull("the type already reads number | string");
		Row("max_score").IsNullable.Should().BeTrue("a member allows null");
		Row("name").IsNullable.Should().BeTrue();
		Row("name").Type.Text.Should().Be("string", "null shows as a badge, not in the type");
		Row("count").IsNullable.Should().BeFalse();
		Row("pet").IsNullable.Should().BeTrue();
		Row("pet").Union?.Badges.Select(b => b.Text).Should().NotContain("null", "a null member is no option to pick");
		Row("kind").Union!.DiscriminatorProperty.Should().Be("type", "a row with more to say than its type keeps its options row");

		var markdown = new System.Text.StringBuilder();
		ApiPropertyMarkdown.WriteList(markdown, new ApiPropertyList([Row("name")]), "/api/doc/fixture");
		markdown.ToString().Should().Contain("— nullable");
	}

	[Test]
	public async Task BuildPropertyList_AnyUnionBranchAllowingNull_MakesTheFieldNullable()
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
			          "nested": { "oneOf": [ { "type": "integer" }, { "anyOf": [ { "type": "string" }, { "type": "null" } ] } ] },
			          "oneOfTwice": { "oneOf": [ { "type": ["string", "null"] }, { "type": ["integer", "null"] } ] },
			          "anyOfTwice": { "anyOf": [ { "type": ["string", "null"] }, { "type": ["integer", "null"] } ] }
			        }
			      }
			    }
			  }
			}
			""";
		var document = await LoadSpecAsync(json);

		var list = BuilderFor(document).BuildPropertyList(document.Components!.Schemas!["Holder"], new PropertyTreeScope { Prefix = "" })!;

		bool Nullable(string name) => list.Items.Single(p => p.Name == name).IsNullable;

		Nullable("nested").Should().BeTrue("null passes through the inner anyOf");
		Nullable("oneOfTwice").Should().BeTrue("generated specs write a nullable field this way, so it reads as nullable");
		Nullable("anyOfTwice").Should().BeTrue("an anyOf lets null through when any branch accepts it");
	}

	[Test]
	public async Task BuildPropertyList_OpenApi30Nullable_IsFlagged()
	{
		var json =
			"""
			{
			  "openapi": "3.0.3",
			  "info": { "title": "t", "version": "1" },
			  "paths": {},
			  "components": {
			    "schemas": {
			      "Holder": { "type": "object", "properties": { "note": { "type": "string", "nullable": true } } }
			    }
			  }
			}
			""";
		var document = await LoadSpecAsync(json);

		var note = BuilderFor(document).BuildPropertyList(
			document.Components!.Schemas!["Holder"],
			new PropertyTreeScope { Prefix = "" }
		)!.Items.Single();

		note.IsNullable.Should().BeTrue();
		note.Type.Text.Should().Be("string");
	}
}
