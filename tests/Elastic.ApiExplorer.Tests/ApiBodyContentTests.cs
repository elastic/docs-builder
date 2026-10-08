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

/// <summary>What a request or response body lists: its properties, or the variants of a body that is itself a union.</summary>
public class ApiBodyContentTests
{
	[Test]
	public async Task VariantNames_ArrayAndPlainVariantOfOneSchema_StayDistinct()
	{
		var json =
			"""
			{
			  "openapi": "3.0.3",
			  "info": { "title": "t", "version": "1" },
			  "paths": {},
			  "components": {
			    "schemas": {
			      "Cat": { "type": "object", "properties": { "lives": { "type": "integer" } } },
			      "Body": {
			        "oneOf": [
			          { "$ref": "#/components/schemas/Cat" },
			          { "type": "array", "items": { "$ref": "#/components/schemas/Cat" } }
			        ]
			      }
			    }
			  }
			}
			""";
		var document = await LoadSpecAsync(json);
		var builder = BuilderFor(document);

		var variants = ApiBodyContent.BuildUnionVariants(
			new OpenApiSchemaReference("Body", document),
			new PropertyTreeScope { Prefix = "req", IsRequest = true },
			new SchemaAnalyzer(document),
			builder
		);

		variants!.Variants.Select(v => v.DisplayName).Should().Equal("Cat", "Cat");
		ApiBodyContent.VariantNames(variants).Should().Equal("Cat[]", "Cat");
	}

	[Test]
	public async Task BuildUnionVariants_RequestBodyAnyOf_ExpandsVariantsWithTheirProperties()
	{
		var json =
			"""
			{
			  "openapi": "3.0.3",
			  "info": { "title": "t", "version": "1" },
			  "paths": {},
			  "components": {
			    "schemas": {
			      "Cat": { "type": "object", "description": "A cat.\n\nSecond paragraph.", "required": ["lives"], "properties": { "lives": { "type": "integer" } } },
			      "Dog": { "type": "object", "properties": { "barks": { "type": "boolean" } } },
			      "Body": { "anyOf": [ { "$ref": "#/components/schemas/Cat" }, { "$ref": "#/components/schemas/Dog" } ] },
			      "Plain": { "type": "object", "properties": { "name": { "type": "string" } } }
			    }
			  }
			}
			""";
		var document = await LoadSpecAsync(json);
		var analyzer = new SchemaAnalyzer(document);
		var builder = BuilderFor(document);
		var body = new OpenApiSchemaReference("Body", document);
		var plain = new OpenApiSchemaReference("Plain", document);

		builder.BuildPropertyList(body, new PropertyTreeScope { Prefix = "req", IsRequest = true }).Should().BeNull();
		var variants = ApiBodyContent.BuildUnionVariants(
			body,
			new PropertyTreeScope { Prefix = "req", IsRequest = true },
			analyzer,
			builder
		);

		variants!.Variants.Select(v => v.DisplayName).Should().Equal("Cat", "Dog");
		variants.Variants[0].AnchorId.Should().StartWith("req-variant-");
		variants.Variants[0].Properties!.Items.Single().IsRequired.Should().BeTrue();
		variants.Variants[0].Properties!.Items.Single().IsRequest.Should().BeTrue("request variants keep the request flag");
		variants.Label.Should().Be("Any of:");
		variants.Variants[0].DescriptionMarkdown.Should().Be("A cat.", "only the first paragraph is shown");
		variants.Variants[1].DescriptionMarkdown.Should().BeNull();
		var markdown = new System.Text.StringBuilder();
		ApiPropertyMarkdown.WriteVariants(markdown, variants, "/api/doc/fixture");
		markdown.ToString().Should().StartWith("Any of:").And.Contain("- `lives` (integer) — required");
		ApiBodyContent.BuildUnionVariants(plain, new PropertyTreeScope { Prefix = "req" }, analyzer, builder).Should().BeNull();
	}

	[Test]
	public async Task BuildUnionVariants_DescriptionWithCrlfParagraphs_KeepsOnlyTheFirstParagraph()
	{
		var json =
			"""
			{
			  "openapi": "3.0.3",
			  "info": { "title": "t", "version": "1" },
			  "paths": {},
			  "components": {
			    "schemas": {
			      "Cat": { "type": "object", "description": "A cat.\r\n\r\nSecond paragraph.", "properties": { "lives": { "type": "integer" } } },
			      "Dog": { "type": "object", "description": "A dog.\n\nSecond paragraph.", "properties": { "barks": { "type": "boolean" } } },
			      "Body": { "oneOf": [ { "$ref": "#/components/schemas/Cat" }, { "$ref": "#/components/schemas/Dog" } ] }
			    }
			  }
			}
			""";
		var document = await LoadSpecAsync(json);
		var builder = BuilderFor(document);

		var variants = ApiBodyContent.BuildUnionVariants(
			new OpenApiSchemaReference("Body", document),
			new PropertyTreeScope { Prefix = "res-200" },
			new SchemaAnalyzer(document),
			builder
		);

		variants!.Variants.Select(v => v.DescriptionMarkdown).Should().Equal("A cat.", "A dog.");
	}

	[Test]
	public async Task BuildUnionVariants_AllOfUnionWithDiscriminatorMapping_KeepsNamesAndMappedLabels()
	{
		var json =
			"""
			{
			  "openapi": "3.0.3",
			  "info": { "title": "t", "version": "1" },
			  "paths": {},
			  "components": {
			    "schemas": {
			      "Base": { "type": "object", "properties": { "id": { "type": "string" } } },
			      "Cat": { "type": "object", "properties": { "kind": { "type": "string" }, "lives": { "type": "integer" } } },
			      "Dog": { "type": "object", "properties": { "kind": { "type": "string" }, "barks": { "type": "boolean" } } },
			      "Body": {
			        "allOf": [
			          { "$ref": "#/components/schemas/Base" },
			          {
			            "oneOf": [ { "$ref": "#/components/schemas/Cat" }, { "$ref": "#/components/schemas/Dog" } ],
			            "discriminator": {
			              "propertyName": "kind",
			              "mapping": { "feline": "#/components/schemas/Cat", "canine": "#/components/schemas/Dog" }
			            }
			          }
			        ]
			      }
			    }
			  }
			}
			""";
		var document = await LoadSpecAsync(json);
		var builder = BuilderFor(document);

		var variants = ApiBodyContent.BuildUnionVariants(
			document.Components!.Schemas!["Body"],
			new PropertyTreeScope { Prefix = "req", IsRequest = true },
			new SchemaAnalyzer(document),
			builder
		);

		variants!.Variants.Select(v => v.DisplayName).Should().Equal("Cat", "Dog");
		variants.Variants.Select(v => v.DiscriminatorLabel).Should().Equal("kind: feline", "kind: canine");
		variants.Variants[0].Properties!.Items.Select(p => p.Name).Should().Equal("id", "kind", "lives");
	}

	[Test]
	public async Task Build_AllOfUnionWithBaseProperties_ListsTheVariantsNotJustTheBase()
	{
		var json =
			"""
			{
			  "openapi": "3.0.3",
			  "info": { "title": "t", "version": "1" },
			  "paths": {},
			  "components": {
			    "schemas": {
			      "Base": { "type": "object", "properties": { "id": { "type": "string" } } },
			      "Cat": { "type": "object", "properties": { "lives": { "type": "integer" } } },
			      "Dog": { "type": "object", "properties": { "barks": { "type": "boolean" } } },
			      "Composed": {
			        "allOf": [
			          { "$ref": "#/components/schemas/Base" },
			          { "oneOf": [ { "$ref": "#/components/schemas/Cat" }, { "$ref": "#/components/schemas/Dog" } ] }
			        ]
			      }
			    }
			  }
			}
			""";
		var document = await LoadSpecAsync(json);
		var builder = BuilderFor(document);
		var analyzer = new SchemaAnalyzer(document);

		var (properties, variants, _) = ApiBodyContent.Build(
			document.Components!.Schemas!["Composed"],
			new PropertyTreeScope { Prefix = "req", IsRequest = true },
			analyzer,
			builder
		);
		properties.Should().BeNull();
		variants!.Variants.Select(v => v.DisplayName).Should().Equal("Cat", "Dog");
		variants.Variants[0].Properties!.Items.Select(p => p.Name).Should().Equal("id", "lives");

		var (byReference, _, _) = ApiBodyContent.Build(
			new OpenApiSchemaReference("Composed", document),
			new PropertyTreeScope { Prefix = "res-200" },
			analyzer,
			builder
		);
		byReference.Should().BeNull("a $ref to the same union lists its variants too");
	}

	[Test]
	public async Task Build_UnionThatDeclaresProperties_ListsThePropertiesThenTheVariants()
	{
		var json =
			"""
			{
			  "openapi": "3.0.3",
			  "info": { "title": "t", "version": "1" },
			  "paths": {},
			  "components": {
			    "schemas": {
			      "Cat": { "type": "object", "properties": { "lives": { "type": "integer" } } },
			      "Dog": { "type": "object", "properties": { "barks": { "type": "boolean" } } },
			      "Declared": {
			        "type": "object",
			        "properties": { "kind": { "type": "string" } },
			        "oneOf": [ { "$ref": "#/components/schemas/Cat" }, { "$ref": "#/components/schemas/Dog" } ]
			      },
			      "Holder": { "type": "object", "properties": { "pet": { "$ref": "#/components/schemas/Declared" } } }
			    }
			  }
			}
			""";
		var document = await LoadSpecAsync(json);
		var builder = BuilderFor(document);
		var analyzer = new SchemaAnalyzer(document);

		var (properties, variants, _) = ApiBodyContent.Build(
			new OpenApiSchemaReference("Declared", document),
			new PropertyTreeScope { Prefix = "res-200" },
			analyzer,
			builder
		);
		properties!.Items.Select(p => p.Name).Should().Equal("kind");
		variants!.Label.Should().Be("One of:");
		variants.Variants.Select(v => v.DisplayName).Should().Equal("Cat", "Dog");

		var pet = builder.BuildPropertyList(document.Components!.Schemas!["Holder"], new PropertyTreeScope { Prefix = "" })!.Items.Single();
		pet.Children.Properties!.Items.Select(p => p.Name).Should().Equal("kind");
		pet.Children.Variants!.Label.Should().Be("One of:", "a label on the row would sit above the properties instead");
		pet.Children.Variants.Variants.Select(v => v.DisplayName).Should().Equal("Cat", "Dog");
		pet.Union.Should().BeNull();
		var markdown = new System.Text.StringBuilder();
		ApiPropertyMarkdown.WriteList(markdown, new ApiPropertyList([pet]), "/api/doc/fixture");
		markdown.ToString().Should().Contain("`kind`").And.Contain("One of:").And.Contain("`lives`").And.Contain("`barks`");
	}

	[Test]
	public async Task Build_BodyWithRequiredOnlyAnyOf_SaysWhichFieldsAreRequired()
	{
		var json =
			"""
			{
			  "openapi": "3.0.3",
			  "info": { "title": "t", "version": "1" },
			  "paths": {},
			  "components": {
			    "schemas": {
			      "Body": {
			        "type": "object",
			        "anyOf": [ { "required": ["a"] }, { "required": ["b"] } ],
			        "properties": { "a": { "type": "string" }, "b": { "type": "string" } }
			      }
			    }
			  }
			}
			""";
		var document = await LoadSpecAsync(json);

		var body = ApiBodyContent.Build(
			new OpenApiSchemaReference("Body", document),
			new PropertyTreeScope { Prefix = "req", IsRequest = true },
			new SchemaAnalyzer(document),
			BuilderFor(document)
		);

		body.Properties!.Items.Select(p => p.Name).Should().Equal("a", "b");
		body.UnionVariants.Should().BeNull();
		ApiPropertyMarkdown.Format(body.Requires!).Should().Be("Requires at least one of: `a` or `b`");
	}

	[Test]
	public async Task BuildUnionVariants_UnnamedInlineMembers_AreLabelledByConstantOrDistinctFields()
	{
		var json =
			"""
			{
			  "openapi": "3.1.0",
			  "info": { "title": "t", "version": "1" },
			  "paths": {},
			  "components": {
			    "schemas": {
			      "Range": {
			        "oneOf": [
			          { "type": "object", "properties": { "type": { "type": "string", "enum": ["relative"] }, "from": { "type": "string" } } },
			          { "type": "object", "properties": { "type": { "type": "string", "enum": ["absolute"] }, "start": { "type": "string" } } }
			        ]
			      },
			      "Note": {
			        "oneOf": [
			          { "type": "object", "required": ["noteId"], "properties": { "noteId": { "type": "string" }, "version": { "type": "string" } } },
			          { "type": "object", "required": ["noteIds"], "properties": { "noteIds": { "type": "array", "items": { "type": "string" } }, "version": { "type": "string" } } },
			          { "type": "object", "title": "ByQuery", "properties": { "query": { "type": "string" } } }
			        ]
			      }
			    }
			  }
			}
			""";
		var document = await LoadSpecAsync(json);
		var builder = BuilderFor(document);
		var analyzer = new SchemaAnalyzer(document);

		var range = ApiBodyContent.BuildUnionVariants(
			document.Components!.Schemas!["Range"],
			new PropertyTreeScope { Prefix = "req" },
			analyzer,
			builder
		);
		range!.Variants.Select(v => v.DisplayName).Should().Equal("type: relative", "type: absolute");

		var note = ApiBodyContent.BuildUnionVariants(
			document.Components!.Schemas!["Note"],
			new PropertyTreeScope { Prefix = "req" },
			analyzer,
			builder
		);
		note!.Variants.Select(v => v.DisplayName).Should().Equal("{ noteId }", "{ noteIds }", "ByQuery");
	}

	[Test]
	public async Task Build_ArrayOfUnion_SaysEachItemIsAVariant()
	{
		var json =
			"""
			{
			  "openapi": "3.0.3",
			  "info": { "title": "t", "version": "1" },
			  "paths": {},
			  "components": {
			    "schemas": {
			      "Cat": { "type": "object", "properties": { "lives": { "type": "integer" } } },
			      "Dog": { "type": "object", "properties": { "barks": { "type": "boolean" } } },
			      "Pets": { "type": "array", "items": { "oneOf": [ { "$ref": "#/components/schemas/Cat" }, { "$ref": "#/components/schemas/Dog" } ] } },
			      "Pet": { "oneOf": [ { "$ref": "#/components/schemas/Cat" }, { "$ref": "#/components/schemas/Dog" } ] }
			    }
			  }
			}
			""";
		var document = await LoadSpecAsync(json);
		var analyzer = new SchemaAnalyzer(document);
		var builder = BuilderFor(document);

		var (_, array, _) = ApiBodyContent.Build(
			document.Components!.Schemas!["Pets"],
			new PropertyTreeScope { Prefix = "res-200" },
			analyzer,
			builder
		);
		array!.Label.Should().Be("An array; each item is one of:");
		array.Variants.Select(v => v.DisplayName).Should().Equal("Cat", "Dog");
		var markdown = new System.Text.StringBuilder();
		ApiPropertyMarkdown.WriteVariants(markdown, array, "/api/doc/fixture");
		markdown.ToString().Should().StartWith("An array; each item is one of:");

		var (_, single, _) = ApiBodyContent.Build(
			document.Components!.Schemas!["Pet"],
			new PropertyTreeScope { Prefix = "req" },
			analyzer,
			builder
		);
		single!.Label.Should().Be("One of:");
	}
}
