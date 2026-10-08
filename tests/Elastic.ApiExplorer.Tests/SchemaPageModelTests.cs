// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Types;
using Elastic.Documentation.Navigation;
using Elastic.Documentation.Site;
using Elastic.Documentation.Site.FileProviders;

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

	private ApiRenderContext RenderContext(INavigationItem current) =>
		new(fixture.Context, fixture.Document, new StaticFileContentHashProvider(new EmbeddedOrPhysicalFileProvider(fixture.Context)))
		{
			NavigationHtml = string.Empty,
			CurrentNavigation = current,
			MarkdownRenderer = PassthroughMarkdownRenderer.Instance
		};
}
