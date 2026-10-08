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

/// <summary>How <c>oneOf</c>/<c>anyOf</c> properties list their variants: labels, discriminator values, nested and recursive unions.</summary>
public class UnionVariantTests
{
	[Test]
	public async Task BuildPropertyList_UnionWithOwnProperties_ListsTheSharedPropertiesThenTheVariants()
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
			      "Holder": {
			        "type": "object",
			        "properties": {
			          "pet": {
			            "type": "object",
			            "properties": { "kind": { "type": "string" } },
			            "oneOf": [ { "$ref": "#/components/schemas/Cat" }, { "$ref": "#/components/schemas/Dog" } ]
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
		pet.Children.Kind.Should().Be(ChildKind.PropertyList);
		pet.Children.Properties!.Items.Select(p => p.Name).Should().Equal("kind");
		pet.Children.Variants!.Variants.Select(v => v.DisplayName).Should().Equal("Cat", "Dog");
	}

	[Test]
	public async Task BuildPropertyList_AnyOfRequiredOnlyMembers_SaysWhichFieldsAreRequired()
	{
		var json =
			"""
			{
			  "openapi": "3.0.3",
			  "info": { "title": "t", "version": "1" },
			  "paths": {},
			  "components": {
			    "schemas": {
			      "WindowRequired": { "required": ["window"] },
			      "Holder": {
			        "type": "object",
			        "properties": {
			          "incident": {
			            "type": "object",
			            "anyOf": [ { "required": ["correlation_id"] }, { "required": ["externalId"] } ],
			            "properties": { "correlation_id": { "type": "string" }, "externalId": { "type": "string" } }
			          },
			          "range": {
			            "type": "object",
			            "oneOf": [ { "type": "object", "required": ["from", "to"] }, { "$ref": "#/components/schemas/WindowRequired" } ],
			            "properties": { "from": { "type": "string" }, "to": { "type": "string" }, "window": { "type": "string" } }
			          }
			        }
			      }
			    }
			  }
			}
			""";
		var document = await LoadSpecAsync(json);

		var list = BuilderFor(document).BuildPropertyList(document.Components!.Schemas!["Holder"], new PropertyTreeScope { Prefix = "" })!;

		var incident = list.Items.Single(p => p.Name == "incident");
		incident.Type.Text.Should().Be("object", "required-only members offer no shapes to choose between");
		incident.Union.Should().BeNull();
		incident.Requires.Should().BeEquivalentTo(new RequiredAlternatives("Requires at least one of:", ["correlation_id", "externalId"]));
		incident.Children.Properties!.Items.Select(p => p.Name).Should().Equal("correlation_id", "externalId");
		list.Items.Single(p => p.Name == "range").Requires!.Options.Should().Equal("from + to", "window");
		var markdown = new System.Text.StringBuilder();
		ApiPropertyMarkdown.WriteList(markdown, list, "/api/doc/fixture");
		markdown
			.ToString()
			.Should()
			.Contain("Requires at least one of: `correlation_id` or `externalId`")
			.And
			.Contain("Requires exactly one of: `from + to` or `window`");
	}

	[Test]
	public async Task BuildPropertyList_DiscriminatedOneOf_LabelsVariantsWithTheirDiscriminatorValue()
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
			      "Cat": { "type": "object", "properties": { "kind": { "type": "string", "enum": ["cat"] }, "lives": { "type": "integer" } } },
			      "Dog": { "type": "object", "properties": { "kind": { "type": "string", "enum": ["dog"] }, "barks": { "type": "boolean" } } },
			      "Fish": { "type": "object", "properties": { "kind": { "type": "string" }, "fins": { "type": "integer" } } },
			      "Pet": {
			        "oneOf": [ { "$ref": "#/components/schemas/Cat" }, { "$ref": "#/components/schemas/Dog" } ],
			        "discriminator": { "propertyName": "kind", "mapping": { "tomcat": "#/components/schemas/Cat" } }
			      },
			      "Holder": {
			        "type": "object",
			        "properties": {
			          "mapped": {
			            "oneOf": [ { "$ref": "#/components/schemas/Cat" }, { "$ref": "#/components/schemas/Dog" } ],
			            "discriminator": {
			              "propertyName": "kind",
			              "mapping": { "feline": "#/components/schemas/Cat", "canine": "#/components/schemas/Dog" }
			            }
			          },
			          "implicit": {
			            "oneOf": [ { "$ref": "#/components/schemas/Cat" }, { "$ref": "#/components/schemas/Fish" } ],
			            "discriminator": { "propertyName": "kind" }
			          },
			          "composed": {
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
			          "plain": {
			            "oneOf": [ { "$ref": "#/components/schemas/Cat" }, { "$ref": "#/components/schemas/Dog" } ]
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

		List<string?> Labels(string name) =>
			list!.Items.Single(p => p.Name == name).Children.Variants!.Variants.Select(v => v.DiscriminatorLabel).ToList();

		Labels("mapped").Should().Equal("kind: feline", "kind: canine");
		Labels("implicit").Should().Equal("kind: cat", "kind: Fish");
		Labels("plain").Should().Equal(null, null);

		Labels("composed").Should().Equal("kind: feline", "kind: canine");
		list!.Items.Single(p => p.Name == "composed").Union!.DiscriminatorProperty.Should().Be("kind");

		var topLevel = ApiBodyContent.BuildUnionVariants(
			document.Components!.Schemas!["Pet"],
			new PropertyTreeScope { Prefix = "oneof" },
			new SchemaAnalyzer(document),
			builder
		);
		topLevel!.Variants.Select(v => v.DiscriminatorLabel).Should().Equal("kind: tomcat", "kind: dog");
	}

	[Test]
	public async Task BuildPropertyList_AnyOfUnion_LabelsTheRowAnyOfAndOneOfUnionsOneOf()
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
			      "Flexible": { "anyOf": [ { "$ref": "#/components/schemas/Cat" }, { "$ref": "#/components/schemas/Dog" } ] },
			      "Holder": {
			        "type": "object",
			        "properties": {
			          "inlineAny": { "anyOf": [ { "$ref": "#/components/schemas/Cat" }, { "$ref": "#/components/schemas/Dog" } ] },
			          "inlineOne": { "oneOf": [ { "$ref": "#/components/schemas/Cat" }, { "$ref": "#/components/schemas/Dog" } ] },
			          "refAny": { "$ref": "#/components/schemas/Flexible" }
			        }
			      }
			    }
			  }
			}
			""";
		var document = await LoadSpecAsync(json);
		var builder = BuilderFor(document);

		var list = builder.BuildPropertyList(document.Components!.Schemas!["Holder"], new PropertyTreeScope { Prefix = "" });

		string? Label(string name) => list!.Items.Single(p => p.Name == name).Union?.Label;

		Label("inlineAny").Should().Be("Any of:");
		Label("inlineOne").Should().Be("One of:");
		Label("refAny").Should().Be("Any of:");
	}

	[Test]
	public async Task BuildPropertyList_UnionMemberThatIsAMap_ListsTheValuePropertiesUnderAKeyRow()
	{
		var json =
			"""
			{
			  "openapi": "3.0.3",
			  "info": { "title": "t", "version": "1" },
			  "paths": {},
			  "components": {
			    "schemas": {
			      "Input": { "type": "object", "properties": { "enabled": { "type": "boolean" }, "vars": { "type": "object" } } },
			      "Holder": {
			        "type": "object",
			        "properties": {
			          "inputs": {
			            "anyOf": [
			              { "type": "array", "items": { "$ref": "#/components/schemas/Input" } },
			              { "type": "object", "additionalProperties": { "$ref": "#/components/schemas/Input" } }
			            ]
			          },
			          "query": {
			            "anyOf": [ { "type": "string" }, { "type": "object", "additionalProperties": {} } ]
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

		var inputs = list!.Items.Single(p => p.Name == "inputs");
		inputs.Children.Kind.Should().Be(ChildKind.UnionVariants);
		var map = inputs.Children.Variants!.Variants.Single(v => v.Properties?.Items.Any(p => p.Name == "<string>") == true);
		var keyRow = map.Properties!.Items.Single();
		keyRow.AnchorId.Should().EndWith("-string", "the id stays free of angle brackets");
		keyRow.Children.Properties!.Items.Select(p => p.Name).Should().Equal("enabled", "vars");

		list.Items.Single(p => p.Name == "query").Children.Kind.Should().Be(ChildKind.None, "a map of anything has no properties to list");
	}

	[Test]
	public async Task BuildPropertyList_UnionOfArrays_ListsThePropertiesOfTheObjectItems()
	{
		var json =
			"""
			{
			  "openapi": "3.0.3",
			  "info": { "title": "t", "version": "1" },
			  "paths": {},
			  "components": {
			    "schemas": {
			      "Policy": { "type": "object", "properties": { "name": { "type": "string" }, "inputs": { "type": "object" } } },
			      "Holder": {
			        "type": "object",
			        "properties": {
			          "policies": {
			            "anyOf": [
			              { "type": "array", "items": { "type": "string" } },
			              { "type": "array", "items": { "$ref": "#/components/schemas/Policy" } }
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

		var variants = list!.Items.Single(p => p.Name == "policies").Children.Variants!.Variants;
		var policy = variants.Single(v => v.DisplayName == "Policy");
		policy.IsArrayVariant.Should().BeTrue();
		policy.ShowProperties.Should().BeTrue("an array-only variant lists the properties of its items");
		policy.Properties!.Items.Select(p => p.Name).Should().Equal("name", "inputs");
		variants.Single(v => v.DisplayName == "string").Properties.Should().BeNull();
	}

	[Test]
	public async Task BuildPropertyList_UnionWithInlineObjectMembers_ExpandsEachOneSeparately()
	{
		var json =
			"""
			{
			  "openapi": "3.0.3",
			  "info": { "title": "t", "version": "1" },
			  "paths": {},
			  "components": {
			    "schemas": {
			      "Named": {
			        "oneOf": [
			          { "type": "string" },
			          { "type": "object", "title": "ByQuery", "properties": { "query": { "type": "string" } } },
			          { "type": "object", "properties": { "ids": { "type": "array", "items": { "type": "string" } } } },
			          { "type": "object", "properties": { "all": { "type": "boolean" } } }
			        ]
			      },
			      "Holder": {
			        "type": "object",
			        "properties": { "selector": { "$ref": "#/components/schemas/Named" } }
			      }
			    }
			  }
			}
			""";
		var document = await LoadSpecAsync(json);
		var builder = BuilderFor(document);

		var list = builder.BuildPropertyList(document.Components!.Schemas!["Holder"], new PropertyTreeScope { Prefix = "" });

		var selector = list!.Items.Single(p => p.Name == "selector");
		selector.Children.Kind.Should().Be(ChildKind.UnionVariants);
		var variants = selector.Children.Variants!.Variants;
		variants.Select(v => v.DisplayName).Should().Equal("string", "ByQuery", "{ ids }", "{ all }");
		variants
			.Select(v => v.AnchorId)
			.Should()
			.Equal("selector-variant-string", "selector-variant-byquery", "selector-variant-object", "selector-variant-object-2");
		variants.Select(v => v.Properties?.Items.Single().Name).Should().Equal(null, "query", "ids", "all");
		variants.Select(v => v.AnchorId).Should().OnlyHaveUniqueItems();
	}

	[Test]
	public async Task BuildPropertyList_DiscriminatorWithoutMappingOrEnum_LabelsVariantsWithTheirSchemaName()
	{
		var json =
			"""
			{
			  "openapi": "3.0.3",
			  "info": { "title": "t", "version": "1" },
			  "paths": {},
			  "components": {
			    "schemas": {
			      "Cat": { "type": "object", "properties": { "kind": { "type": "string" }, "lives": { "type": "integer" } } },
			      "Dog": { "type": "object", "properties": { "kind": { "type": "string" }, "barks": { "type": "boolean" } } },
			      "Pup": { "type": "object", "properties": { "kind": { "type": "string", "enum": ["dog", "puppy"] } } },
			      "Holder": {
			        "type": "object",
			        "properties": {
			          "enumerated": {
			            "oneOf": [ { "$ref": "#/components/schemas/Cat" }, { "$ref": "#/components/schemas/Pup" } ],
			            "discriminator": { "propertyName": "kind" }
			          },
			          "implicit": {
			            "oneOf": [ { "$ref": "#/components/schemas/Cat" }, { "$ref": "#/components/schemas/Dog" } ],
			            "discriminator": { "propertyName": "kind" }
			          },
			          "partial": {
			            "oneOf": [ { "$ref": "#/components/schemas/Cat" }, { "$ref": "#/components/schemas/Dog" } ],
			            "discriminator": { "propertyName": "kind", "mapping": { "feline": "#/components/schemas/Cat" } }
			          }
			        }
			      }
			    }
			  }
			}
			""";
		var document = await LoadSpecAsync(json);

		var list = BuilderFor(document).BuildPropertyList(document.Components!.Schemas!["Holder"], new PropertyTreeScope { Prefix = "" });

		List<string?> Labels(string name) =>
			list!.Items.Single(p => p.Name == name).Children.Variants!.Variants.Select(v => v.DiscriminatorLabel).ToList();

		Labels("implicit").Should().Equal("kind: Cat", "kind: Dog");
		Labels("partial").Should().Equal("kind: feline", "kind: Dog");
		Labels("enumerated").Should().Equal("kind: Cat", "kind: dog | puppy");
	}

	[Test]
	public async Task BuildPropertyList_ArrayOfUnion_ExpandsTheItemVariants()
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
			          "and": {
			            "type": "array",
			            "maxItems": 50,
			            "items": {
			              "anyOf": [
			                { "anyOf": [
			                  { "type": "object", "required": ["field"], "properties": { "field": { "type": "string" }, "eq": { "type": "string" } } },
			                  { "type": "object", "properties": { "field": { "type": "string" }, "exists": { "type": "boolean" } } }
			                ] },
			                { "type": "object", "properties": { "always": { "type": "object" } } }
			              ]
			            }
			          }
			        }
			      }
			    }
			  }
			}
			""";
		var document = await LoadSpecAsync(json);

		var and = BuilderFor(document).BuildPropertyList(
			document.Components!.Schemas!["Holder"],
			new PropertyTreeScope { Prefix = "" }
		)!.Items.Single();

		and.Type.Text.Should().Contain("anyOf");
		and.Children.Kind.Should().Be(ChildKind.UnionVariants);
		and.Children.Variants!
			.Variants
			.Select(v => v.Properties!.Items.Select(p => p.Name).First())
			.Should()
			.Equal("field", "field", "always");
	}

	[Test]
	public async Task BuildPropertyList_UnionOfLiteralsAndAnObject_ListsTheLiteralsAsOptionsBesideTheVariant()
	{
		var json =
			"""
			{
			  "openapi": "3.0.3",
			  "info": { "title": "t", "version": "1" },
			  "paths": {},
			  "components": {
			    "schemas": {
			      "SortObject": { "type": "object", "properties": { "order": { "type": "string" }, "missing": { "type": "string" } } },
			      "Holder": {
			        "type": "object",
			        "properties": {
			          "sort": { "oneOf": [ { "type": "string", "enum": ["asc", "desc"] }, { "$ref": "#/components/schemas/SortObject" } ] },
			          "order": { "oneOf": [ { "type": "string", "enum": ["asc", "desc"] }, { "type": "integer" } ] },
			          "rank": {
			            "oneOf": [
			              { "type": "string", "enum": ["asc", "desc"] },
			              { "type": "integer" },
			              { "$ref": "#/components/schemas/SortObject" }
			            ]
			          }
			        }
			      }
			    }
			  }
			}
			""";
		var document = await LoadSpecAsync(json);

		var list = BuilderFor(document).BuildPropertyList(document.Components!.Schemas!["Holder"], new PropertyTreeScope { Prefix = "" })!;

		var sort = list.Items.Single(p => p.Name == "sort");
		sort.EnumValues.Should().BeEmpty("the literals move into the One of row");
		sort.Union!.Badges.Select(b => b.Text).Should().Equal("asc", "desc");
		sort.Union.MoreOptions.Should().Be("or an object listed below");
		sort.Children.Variants!.Variants.Select(v => v.DisplayName).Should().Equal("SortObject");
		var markdown = new System.Text.StringBuilder();
		ApiPropertyMarkdown.WriteList(markdown, new ApiPropertyList([sort]), "/api/doc/fixture");
		markdown.ToString().Should().Contain("One of: `asc` or `desc` or an object listed below").And.NotContain("Values:");

		var order = list.Items.Single(p => p.Name == "order");
		order.EnumValues.Should().Equal(["asc", "desc"], "with no object variant the literals keep their Values row");
		order.Union.Should().BeNull();

		var rank = list.Items.Single(p => p.Name == "rank");
		rank.Union!.MoreOptions.Should().Be("or a type listed below", "the list below holds integer as well as the object");
		rank.Children.Variants!.Variants.Select(v => v.DisplayName).Should().Equal("integer", "SortObject");
	}

	[Test]
	public async Task BuildPropertyList_ArrayUnionOfAUnion_LabelsTheVariantsWithTheInnerType()
	{
		var json =
			"""
			{
			  "openapi": "3.0.3",
			  "info": { "title": "t", "version": "1" },
			  "paths": {},
			  "components": {
			    "schemas": {
			      "LikeDocument": { "type": "object", "properties": { "kind": { "type": "string" }, "_id": { "type": "string" } } },
			      "LikeText": { "type": "object", "properties": { "kind": { "type": "string" }, "text": { "type": "string" } } },
			      "Like": {
			        "anyOf": [ { "$ref": "#/components/schemas/LikeDocument" }, { "$ref": "#/components/schemas/LikeText" } ],
			        "discriminator": { "propertyName": "kind", "mapping": { "doc": "#/components/schemas/LikeDocument" } }
			      },
			      "Holder": {
			        "type": "object",
			        "properties": {
			          "like": { "oneOf": [ { "$ref": "#/components/schemas/Like" }, { "type": "array", "items": { "$ref": "#/components/schemas/Like" } } ] }
			        }
			      }
			    }
			  }
			}
			""";
		var document = await LoadSpecAsync(json);

		var like = BuilderFor(document).BuildPropertyList(
			document.Components!.Schemas!["Holder"],
			new PropertyTreeScope { Prefix = "" }
		)!.Items.Single();

		like.Union.Should().BeNull("the type already reads Like | Like[]");
		like.Children.Variants!.Label.Should().Be("Like or Like[]; each Like is any of:");
		like.Children.Variants.Variants.Select(v => v.DiscriminatorLabel).Should().Equal("kind: doc", "kind: LikeText");
		var markdown = new System.Text.StringBuilder();
		ApiPropertyMarkdown.WriteList(markdown, new ApiPropertyList([like]), "/api/doc/fixture");
		markdown.ToString().Should().Contain("Like or Like[]; each Like is any of:");
	}

	[Test]
	public async Task BuildPropertyList_SameNameInAnotherNamespace_IsNotARecursion()
	{
		var json =
			"""
			{
			  "openapi": "3.0.3",
			  "info": { "title": "t", "version": "1" },
			  "paths": {},
			  "components": {
			    "schemas": {
			      "ml.Settings": { "type": "object", "properties": { "inner": { "$ref": "#/components/schemas/indices.Settings" } } },
			      "indices.Settings": { "type": "object", "properties": { "shards": { "type": "integer" } } },
			      "Node": {
			        "type": "object",
			        "properties": {
			          "child": { "oneOf": [ { "$ref": "#/components/schemas/Node" }, { "type": "string" } ] },
			          "children": { "type": "array", "items": { "$ref": "#/components/schemas/Node" } }
			        }
			      }
			    }
			  }
			}
			""";
		var document = await LoadSpecAsync(json);
		var builder = BuilderFor(document);

		var ml =
			builder.BuildPropertyList(
				document.Components!.Schemas!["ml.Settings"],
				new PropertyTreeScope { Prefix = "", AncestorRefs = new HashSet<string> { "ml.Settings" } }
			)!;
		var inner = ml.Items.Single();
		inner.IsRecursive.Should().BeFalse("indices.Settings only shares the short name Settings with its parent");
		inner.Children.Properties!.Items.Select(p => p.Name).Should().Equal("shards");

		var node =
			builder.BuildPropertyList(
				document.Components!.Schemas!["Node"],
				new PropertyTreeScope { Prefix = "", AncestorRefs = new HashSet<string> { "Node" } }
			)!;
		node.Items.Should().OnlyContain(p => p.IsRecursive, "a union option and an array item that refer to Node both point back up");
	}

	[Test]
	public async Task BuildPropertyList_InlineRecursiveUnion_ExpandsOnceAndPointsBackToTheParent()
	{
		var condition = """{ "type": "object", "properties": { "field": { "type": "string" }, "eq": { "type": "string" } } }""";
		var always = """{ "type": "object", "properties": { "always": { "type": "object" } } }""";
		var leaf = $$"""{ "anyOf": [ { "anyOf": [ {{condition}} ] }, {{always}} ] }""";
		var level2 =
			$$"""{ "anyOf": [ { "anyOf": [ {{condition}} ] }, { "type": "object", "properties": { "and": { "type": "array", "items": {{leaf}} } } }, {{always}} ] }""";
		var json =
			$$"""
			{
			  "openapi": "3.1.0",
			  "info": { "title": "t", "version": "1" },
			  "paths": {},
			  "components": {
			    "schemas": {
			      "Holder": {
			        "type": "object",
			        "properties": {
			          "where": {
			            "anyOf": [
			              { "anyOf": [ {{condition}} ] },
			              { "type": "object", "properties": { "and": { "type": "array", "items": {{level2}} } } },
			              {{always}}
			            ]
			          }
			        }
			      }
			    }
			  }
			}
			""";
		var document = await LoadSpecAsync(json);

		var where = BuilderFor(document).BuildPropertyList(
			document.Components!.Schemas!["Holder"],
			new PropertyTreeScope { Prefix = "" }
		)!.Items.Single();

		where.Children.Kind.Should().Be(ChildKind.UnionVariants);
		var variants = where.Children.Variants!.Variants;
		variants[0].Properties!.Items.Select(p => p.Name).Should().Equal("field", "eq");
		var and = variants[1].Properties!.Items.Single();
		and.Repeats.Should().Be(new RepeatedShape("where", "where", IsUnion: true));
		and.Children.Kind.Should().Be(ChildKind.None);
		var markdown = new System.Text.StringBuilder();
		ApiPropertyMarkdown.WriteList(markdown, variants[1].Properties, "/api/doc/fixture");
		markdown
			.ToString()
			.Should()
			.Contain("Same options as `where`")
			.And
			.NotContain("Any of:", "the repeat line replaces the empty \"Any of:\" row");
	}

	[Test]
	public async Task BuildPropertyList_ArrayOfUnionWithALinkedType_LinksTheVariantInsteadOfExpandingIt()
	{
		var json =
			"""
			{
			  "openapi": "3.0.3",
			  "info": { "title": "t", "version": "1" },
			  "paths": {},
			  "components": {
			    "schemas": {
			      "_types.query_dsl.QueryContainer": { "type": "object", "properties": { "match": { "type": "object" }, "term": { "type": "object" } } },
			      "security._types.RoleTemplateQuery": { "type": "object", "properties": { "template": { "type": "object" } } },
			      "security._types.IndicesPrivilegesQuery": {
			        "oneOf": [
			          { "type": "string" },
			          { "$ref": "#/components/schemas/_types.query_dsl.QueryContainer" },
			          { "$ref": "#/components/schemas/security._types.RoleTemplateQuery" }
			        ]
			      },
			      "Holder": {
			        "type": "object",
			        "properties": {
			          "query": { "type": "array", "items": { "$ref": "#/components/schemas/security._types.IndicesPrivilegesQuery" } }
			        }
			      }
			    }
			  }
			}
			""";
		var document = await LoadSpecAsync(json);

		var query = BuilderFor(document).BuildPropertyList(
			document.Components!.Schemas!["Holder"],
			new PropertyTreeScope { Prefix = "" }
		)!.Items.Single();

		var variants = query.Children.Variants!.Variants;
		var container = variants.Single(v => v.DisplayName == "QueryContainer");
		container.PageUrl.Should().NotBeNullOrEmpty();
		container.Properties.Should().BeNull("QueryContainer has its own page");
		variants.Single(v => v.DisplayName == "RoleTemplateQuery").Properties!.Items.Select(p => p.Name).Should().Equal("template");
	}

	[Test]
	public async Task BuildPropertyList_MapVariantWithARealStringProperty_KeepsDistinctAnchors()
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
			          "inputs": { "anyOf": [
			            { "type": "string" },
			            { "type": "object", "properties": { "string": { "type": "string" } }, "additionalProperties": { "$ref": "#/components/schemas/Input" } } ] }
			        }
			      }
			    }
			  }
			}
			""";
		var document = await LoadSpecAsync(json);

		var map = BuilderFor(document).BuildPropertyList(document.Components!.Schemas!["Holder"], new PropertyTreeScope { Prefix = "" })!
			.Items
			.Single()
			.Children
			.Variants!.Variants.Single(v => v.Properties is not null);

		var anchors = map.Properties!.Items.Select(p => p.AnchorId).ToList();
		anchors.Should().OnlyHaveUniqueItems();
		map.Properties.Items.Single(p => p.Name == "<string>").AnchorId.Should().EndWith("-string-map");
	}
}
