// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Components.PropertyTree;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Types;
using Elastic.Documentation.Navigation;
using Elastic.Documentation.Site;
using Elastic.Documentation.Site.FileProviders;
using static Elastic.ApiExplorer.Tests.TestSpecs;

namespace Elastic.ApiExplorer.Tests;

/// <summary>Schema type pages list a union's variants by the same rules as a request or response body.</summary>
[ClassDataSource<ApiExplorerFixture>(Shared = SharedType.PerClass)]
public class SchemaPageModelTests(ApiExplorerFixture fixture)
{
	[Test]
	public void Create_OneOfSchema_ListsTheVariantsUnderTheKeywordHeading()
	{
		var item = fixture.Walk().OfType<SchemaNavigationItem>().Single(n => n.Model.DisplayName == "Aggregate");
		var context = RenderContext(item);

		var page = SchemaPageModel.Create(item.Model, context);

		page.UnionKeyword.Should().Be(UnionKeyword.OneOf);
		page.UnionVariants!.Variants.Select(v => v.DisplayName).Should().BeEquivalentTo(["TermsAggregate", "MaxAggregate"]);
		page.UnionVariants.Variants.Should().OnlyContain(v => v.AnchorId.StartsWith("oneof-variant-"), "links to a variant keep working");
		page.UnionVariants.Label.Should().BeNull("the section heading already names the keyword");
		page.Requires.Should().BeNull();
		SchemaCommonMark.Write(item.Model, page, context).Should().Contain("## Union Types (oneOf)");
	}

	[Test]
	public async Task UnionIntroFor_ArrayOfUnion_SaysEachItemIsAVariant()
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
			      "Pet": { "anyOf": [ { "$ref": "#/components/schemas/Cat" }, { "$ref": "#/components/schemas/Dog" } ] }
			    }
			  }
			}
			""";
		var document = await LoadSpecAsync(json);
		var analyzer = new SchemaAnalyzer(document);
		var builder = BuilderFor(document);

		string Intro(string id)
		{
			var schema = document.Components!.Schemas![id];
			var variants = ApiBodyContent.BuildUnionVariants(schema, new PropertyTreeScope { Prefix = "oneof" }, analyzer, builder);
			return SchemaPageModel.UnionIntroFor(analyzer.GetTypeInfo(schema), variants);
		}

		Intro("Pets").Should().Be("An array; each item is one of:");
		Intro("Pet").Should().Be("This type can be any of the following:");
	}

	private ApiRenderContext RenderContext(INavigationItem current) =>
		new(fixture.Context, fixture.Document, new StaticFileContentHashProvider(new EmbeddedOrPhysicalFileProvider(fixture.Context)))
		{
			NavigationHtml = string.Empty,
			CurrentNavigation = current,
			MarkdownRenderer = PassthroughMarkdownRenderer.Instance
		};
}
