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

/// <summary><c>allOf</c> compositions: unions inside an <c>allOf</c>, merged variants and the schemas an <c>allOf</c> also includes.</summary>
public class AllOfCompositionTests
{
	[Test]
	public async Task BuildPropertyList_AllOfVariantRedefinesBaseProperty_VariantDefinitionWins()
	{
		var json =
			"""
			{
			  "openapi": "3.0.3",
			  "info": { "title": "t", "version": "1" },
			  "paths": {},
			  "components": {
			    "schemas": {
			      "Base": { "type": "object", "properties": { "kind": { "type": "string" }, "id": { "type": "string" } } },
			      "Cat": { "type": "object", "description": "A cat.", "properties": { "kind": { "type": "string", "enum": ["cat"] } } },
			      "Dog": { "type": "object", "properties": { "barks": { "type": "boolean" } } },
			      "Holder": {
			        "type": "object",
			        "properties": {
			          "pet": {
			            "allOf": [
			              { "$ref": "#/components/schemas/Base" },
			              { "oneOf": [ { "$ref": "#/components/schemas/Cat" }, { "$ref": "#/components/schemas/Dog" } ] }
			            ]
			          }
			        }
			      }
			    }
			  }
			}
			""";
		var document = await LoadSpecAsync(json);
		var builder = BuilderFor(document);

		var list = builder.BuildPropertyList(document.Components!.Schemas!["Holder"], new PropertyTreeScope { Prefix = "" });

		var variants = list!.Items.Single(p => p.Name == "pet").Children.Variants!.Variants;
		var cat = variants[0].Properties!.Items;
		cat.Select(p => p.Name).Should().Equal("kind", "id");
		cat.Single(p => p.Name == "kind").EnumValues.Should().Equal("cat");
		variants[1].Properties!.Items.Single(p => p.Name == "kind").EnumValues.Should().BeEmpty();
	}

	[Test]
	public async Task BuildPropertyList_AllOfWithAnyOfMember_ExpandsVariantsKeepingRequiredFromBaseAndVariant()
	{
		var json =
			"""
			{
			  "openapi": "3.0.3",
			  "info": { "title": "t", "version": "1" },
			  "paths": {},
			  "components": {
			    "schemas": {
			      "Base": { "type": "object", "required": ["id"], "properties": { "id": { "type": "string" } } },
			      "Cat": { "type": "object", "required": ["lives"], "properties": { "lives": { "type": "integer" } } },
			      "Dog": { "type": "object", "properties": { "barks": { "type": "boolean" } } },
			      "Holder": {
			        "type": "object",
			        "properties": {
			          "pet": {
			            "allOf": [
			              { "$ref": "#/components/schemas/Base" },
			              { "anyOf": [ { "$ref": "#/components/schemas/Cat" }, { "$ref": "#/components/schemas/Dog" } ] }
			            ]
			          }
			        }
			      }
			    }
			  }
			}
			""";
		var document = await LoadSpecAsync(json);
		var builder = BuilderFor(document);

		var list = builder.BuildPropertyList(document.Components!.Schemas!["Holder"], new PropertyTreeScope { Prefix = "" });

		var pet = list!.Items.Single(p => p.Name == "pet");
		pet.Children.Kind.Should().Be(ChildKind.UnionVariants);
		var variants = pet.Children.Variants!.Variants;
		variants.Select(v => v.DisplayName).Should().Equal("Cat", "Dog");
		variants[0].Properties!.Items.Where(p => p.IsRequired).Select(p => p.Name).Should().Equal("id", "lives");
		variants[1].Properties!.Items.Where(p => p.IsRequired).Select(p => p.Name).Should().Equal("id");
	}

	[Test]
	public async Task BuildPropertyList_AllOfWithOneOfMember_ExpandsVariantsSharingBaseProperties()
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
			      "Holder": {
			        "type": "object",
			        "properties": {
			          "pet": {
			            "allOf": [
			              { "$ref": "#/components/schemas/Base" },
			              { "oneOf": [ { "$ref": "#/components/schemas/Cat" }, { "$ref": "#/components/schemas/Dog" } ] }
			            ]
			          },
			          "wrapped": {
			            "allOf": [
			              { "oneOf": [ { "$ref": "#/components/schemas/Cat" }, { "$ref": "#/components/schemas/Dog" } ] }
			            ]
			          }
			        }
			      }
			    }
			  }
			}
			""";
		var document = await LoadSpecAsync(json);
		var builder = BuilderFor(document);

		var list = builder.BuildPropertyList(document.Components!.Schemas!["Holder"], new PropertyTreeScope { Prefix = "" });

		var pet = list!.Items.Single(p => p.Name == "pet");
		pet.Children.Kind.Should().Be(ChildKind.UnionVariants);
		var variants = pet.Children.Variants!.Variants;
		variants.Select(v => v.DisplayName).Should().Equal("Cat", "Dog");
		variants[0].Properties!.Items.Select(p => p.Name).Should().Equal("id", "lives");
		variants[1].Properties!.Items.Select(p => p.Name).Should().Equal("id", "barks");

		var wrapped = list.Items.Single(p => p.Name == "wrapped");
		wrapped.Children.Kind.Should().Be(ChildKind.None, "an allOf with no property-bearing base keeps its previous rendering");
	}

	[Test]
	public async Task BuildPropertyList_AllOfWithSeveralRefs_ListsTheOtherSchemasAsAlsoIncludes()
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
			      "Timestamps": { "type": "object", "properties": { "created": { "type": "string" } } },
			      "Mode": { "type": "string", "enum": ["a", "b"] },
			      "Holder": {
			        "type": "object",
			        "properties": {
			          "merged": {
			            "allOf": [
			              { "$ref": "#/components/schemas/Base" },
			              { "$ref": "#/components/schemas/Timestamps" },
			              { "$ref": "#/components/schemas/Mode" }
			            ]
			          },
			          "repeated": {
			            "allOf": [
			              { "$ref": "#/components/schemas/Base" },
			              { "$ref": "#/components/schemas/Base" },
			              { "$ref": "#/components/schemas/Timestamps" }
			            ]
			          },
			          "single": { "allOf": [ { "$ref": "#/components/schemas/Base" } ] }
			        }
			      }
			    }
			  }
			}
			""";
		var document = await LoadSpecAsync(json);
		var builder = BuilderFor(document);

		var list = builder.BuildPropertyList(document.Components!.Schemas!["Holder"], new PropertyTreeScope { Prefix = "" });

		var merged = list!.Items.Single(p => p.Name == "merged");
		merged.AlsoIncludes.Select(t => t.TypeName).Should().Equal("Timestamps");
		list.Items.Single(p => p.Name == "repeated").AlsoIncludes.Select(t => t.TypeName).Should().Equal("Timestamps");
		list.Items.Single(p => p.Name == "single").AlsoIncludes.Should().BeEmpty();
	}

	[Test]
	public async Task BuildPropertyList_RefToAllOfUnion_ExpandsTheVariantsWithLabels()
	{
		var json =
			"""
			{
			  "openapi": "3.0.3",
			  "info": { "title": "t", "version": "1" },
			  "paths": {},
			  "components": {
			    "schemas": {
			      "Base": { "type": "object", "required": ["id"], "properties": { "id": { "type": "string" } } },
			      "Cat": { "type": "object", "properties": { "kind": { "type": "string" }, "lives": { "type": "integer" } } },
			      "Dog": { "type": "object", "properties": { "kind": { "type": "string" }, "barks": { "type": "boolean" } } },
			      "Pet": {
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
			      },
			      "Holder": { "type": "object", "properties": { "pet": { "$ref": "#/components/schemas/Pet" } } }
			    }
			  }
			}
			""";
		var document = await LoadSpecAsync(json);
		var builder = BuilderFor(document);

		var pet = builder.BuildPropertyList(document.Components!.Schemas!["Holder"], new PropertyTreeScope { Prefix = "" })!.Items.Single(
			p => p.Name == "pet"
		);

		pet.Children.Kind.Should().Be(ChildKind.UnionVariants);
		var variants = pet.Children.Variants!.Variants;
		variants.Select(v => v.DisplayName).Should().Equal("Cat", "Dog");
		variants.Select(v => v.DiscriminatorLabel).Should().Equal("kind: feline", "kind: canine");
		variants[0].Properties!.Items.Where(p => p.IsRequired).Select(p => p.Name).Should().Equal("id");

		var body = ApiBodyContent.BuildUnionVariants(
			new OpenApiSchemaReference("Pet", document),
			new PropertyTreeScope { Prefix = "req", IsRequest = true },
			new SchemaAnalyzer(document),
			builder
		);
		body!.Variants.Select(v => v.DisplayName).Should().Equal("Cat", "Dog");
	}

	[Test]
	public async Task GetTypeInfo_RefToAllOfUnionThatRefersBackToItself_DoesNotRecurseForever()
	{
		var json =
			"""
			{
			  "openapi": "3.0.3",
			  "info": { "title": "t", "version": "1" },
			  "paths": {},
			  "components": {
			    "schemas": {
			      "Leaf": { "type": "object", "properties": { "value": { "type": "string" } } },
			      "Node": {
			        "allOf": [
			          { "type": "object", "properties": { "name": { "type": "string" } } },
			          { "oneOf": [ { "$ref": "#/components/schemas/Leaf" }, { "$ref": "#/components/schemas/Node" } ] }
			        ]
			      },
			      "Holder": { "type": "object", "properties": { "node": { "$ref": "#/components/schemas/Node" } } }
			    }
			  }
			}
			""";
		var document = await LoadSpecAsync(json);

		var info = new SchemaAnalyzer(document).GetTypeInfo(new OpenApiSchemaReference("Node", document));

		info.IsUnion.Should().BeTrue();
		info.UnionOptions!.Select(o => o.Name).Should().Equal("Leaf", "Node");
		var node = BuilderFor(document).BuildPropertyList(
			document.Components!.Schemas!["Holder"],
			new PropertyTreeScope { Prefix = "" }
		)!.Items.Single();
		node.Children.Kind.Should().Be(ChildKind.UnionVariants);
	}

	[Test]
	public async Task BuildPropertyList_AllOfUnionWhoseBaseIsAlsoAMap_KeepsTheMapValuesOnEachVariant()
	{
		var json =
			"""
			{
			  "openapi": "3.0.3",
			  "info": { "title": "t", "version": "1" },
			  "paths": {},
			  "components": {
			    "schemas": {
			      "Input": { "type": "object", "properties": { "enabled": { "type": "boolean" } } },
			      "Cat": { "type": "object", "properties": { "lives": { "type": "integer" } } },
			      "Dog": { "type": "object", "properties": { "barks": { "type": "boolean" } } },
			      "Holder": {
			        "type": "object",
			        "properties": {
			          "pet": {
			            "allOf": [
			              {
			                "type": "object",
			                "properties": { "id": { "type": "string" } },
			                "additionalProperties": { "$ref": "#/components/schemas/Input" }
			              },
			              { "oneOf": [ { "$ref": "#/components/schemas/Cat" }, { "$ref": "#/components/schemas/Dog" } ] }
			            ]
			          }
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
		)!.Items.Single(p => p.Name == "pet").Children.Variants!.Variants;

		variants.Select(v => v.DisplayName).Should().Equal("Cat", "Dog");
		var cat = variants[0].Properties!.Items;
		cat.Select(p => p.Name).Should().Equal("id", "lives", "<string>");
		cat.Single(p => p.Name == "<string>").Children.Properties!.Items.Select(p => p.Name).Should().Equal("enabled");
	}

	[Test]
	public async Task BuildPropertyList_AllOfUnionWithAMapOnlyBase_StaysAUnionAndKeepsTheMap()
	{
		var json =
			"""
			{
			  "openapi": "3.0.3",
			  "info": { "title": "t", "version": "1" },
			  "paths": {},
			  "components": {
			    "schemas": {
			      "Input": { "type": "object", "properties": { "enabled": { "type": "boolean" } } },
			      "Cat": { "type": "object", "properties": { "lives": { "type": "integer" } } },
			      "Dog": { "type": "object", "properties": { "barks": { "type": "boolean" } } },
			      "Pet": {
			        "allOf": [
			          { "type": "object", "additionalProperties": { "$ref": "#/components/schemas/Input" } },
			          { "oneOf": [ { "$ref": "#/components/schemas/Cat" }, { "$ref": "#/components/schemas/Dog" } ] }
			        ]
			      },
			      "Holder": { "type": "object", "properties": { "pet": { "$ref": "#/components/schemas/Pet" } } }
			    }
			  }
			}
			""";
		var document = await LoadSpecAsync(json);
		var builder = BuilderFor(document);

		var pet = builder.BuildPropertyList(document.Components!.Schemas!["Holder"], new PropertyTreeScope { Prefix = "" })!.Items.Single();
		pet.Children.Kind.Should().Be(ChildKind.UnionVariants);
		var cat = pet.Children.Variants!.Variants[0];
		cat.DisplayName.Should().Be("Cat");
		cat.Properties!.Items.Select(p => p.Name).Should().Equal("lives", "<string>");

		var (properties, variants) = ApiBodyContent.Build(
			document.Components!.Schemas!["Pet"],
			new PropertyTreeScope { Prefix = "req", IsRequest = true },
			new SchemaAnalyzer(document),
			builder
		);
		properties.Should().BeNull();
		variants!.Variants.Select(v => v.DisplayName).Should().Equal("Cat", "Dog");
	}

	[Test]
	public async Task GetTypeInfo_TwoAllOfUnionsReferringToEachOther_GivesTheSameResultInAnyOrder()
	{
		var json =
			"""
			{
			  "openapi": "3.0.3",
			  "info": { "title": "t", "version": "1" },
			  "paths": {},
			  "components": {
			    "schemas": {
			      "Leaf": { "type": "object", "properties": { "value": { "type": "string" } } },
			      "A": { "allOf": [
			        { "type": "object", "properties": { "a": { "type": "string" } } },
			        { "oneOf": [ { "$ref": "#/components/schemas/Leaf" }, { "$ref": "#/components/schemas/B" } ] } ] },
			      "B": { "allOf": [
			        { "type": "object", "properties": { "b": { "type": "string" } } },
			        { "oneOf": [ { "$ref": "#/components/schemas/Leaf" }, { "$ref": "#/components/schemas/A" } ] } ] }
			    }
			  }
			}
			""";
		var document = await LoadSpecAsync(json);

		static string Describe(Model.TypeInfo info) =>
			$"{info.TypeName}:{info.UnionKeyword}:{string.Join(",", info.UnionOptions!.Select(o => $"{o.Name}/{o.Ref}/{o.IsObject}"))}";

		var forward = new SchemaAnalyzer(document);
		var aFirst = Describe(forward.GetTypeInfo(new OpenApiSchemaReference("A", document)));
		var bAfter = Describe(forward.GetTypeInfo(new OpenApiSchemaReference("B", document)));
		var aAgain = Describe(forward.GetTypeInfo(new OpenApiSchemaReference("A", document)));

		var backward = new SchemaAnalyzer(document);
		var bFirst = Describe(backward.GetTypeInfo(new OpenApiSchemaReference("B", document)));
		var aAfter = Describe(backward.GetTypeInfo(new OpenApiSchemaReference("A", document)));

		aAgain.Should().Be(aFirst, "the cycle guard is empty again once a classification returns");
		aAfter.Should().Be(aFirst);
		bFirst.Should().Be(bAfter);
		aFirst.Should().Contain("B/B/True");
	}

	private const string InheritanceSpec =
		"""
		{
		  "openapi": "3.1.0",
		  "info": { "title": "t", "version": "1" },
		  "paths": {},
		  "components": {
		    "schemas": {
		      "Base": {
		        "type": "object",
		        "required": ["id"],
		        "properties": { "id": { "type": "string" }, "note": { "type": "string" } }
		      },
		      "Derived": {
		        "allOf": [
		          { "$ref": "#/components/schemas/Base" },
		          { "type": "object", "required": ["size"], "properties": { "size": { "type": "integer" } } }
		        ]
		      },
		      "Holder": {
		        "type": "object",
		        "properties": { "base": { "allOf": [ { "$ref": "#/components/schemas/Base" } ], "description": "Wrapped." } }
		      },
		      "Mixed": {
		        "type": "object",
		        "required": ["own"],
		        "properties": { "own": { "type": "string" } },
		        "allOf": [ { "$ref": "#/components/schemas/Base" } ]
		      }
		    }
		  }
		}
		""";

	[Test]
	public async Task BuildPropertyList_AllOfOverABase_KeepsRequiredFromEveryMember()
	{
		var document = await LoadSpecAsync(InheritanceSpec);

		var list = BuilderFor(document).BuildPropertyList(document.Components!.Schemas!["Derived"], new PropertyTreeScope { Prefix = "" });

		list!.Items.Where(static p => p.IsRequired).Select(static p => p.Name).Should().BeEquivalentTo("id", "size");
	}

	[Test]
	public async Task BuildPropertyList_PropertyWrappingARefInAllOf_KeepsRequiredOfTheReferencedSchema()
	{
		var document = await LoadSpecAsync(InheritanceSpec);

		var list = BuilderFor(document).BuildPropertyList(document.Components!.Schemas!["Holder"], new PropertyTreeScope { Prefix = "" });

		var children = list!.Items.Single().Children.Properties!.Items;
		children.Single(static p => p.Name == "id").IsRequired.Should().BeTrue();
		children.Single(static p => p.Name == "note").IsRequired.Should().BeFalse();
	}

	[Test]
	public async Task Flatten_SchemaWithPropertiesAndAllOf_CombinesBothOwnFirst()
	{
		var document = await LoadSpecAsync(InheritanceSpec);

		var effective = new SchemaAnalyzer(document).Flatten(document.Components!.Schemas!["Mixed"]);

		effective.Properties.Keys.Should().Equal("own", "id", "note");
		effective.Required.Should().BeEquivalentTo("own", "id");
	}
}
