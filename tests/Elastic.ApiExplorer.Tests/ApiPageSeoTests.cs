// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using AwesomeAssertions;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Operations;
using Elastic.ApiExplorer.Types;
using Elastic.Documentation.Navigation;
using Elastic.Documentation.Site;
using Elastic.Documentation.Site.FileProviders;
using FakeItEasy;

namespace Elastic.ApiExplorer.Tests;

public class ApiPageSeoTests(ApiExplorerFixture fixture) : IClassFixture<ApiExplorerFixture>
{
	[Fact]
	public void OperationLayout_UsesSummaryInTitleAndExcerptedDescription()
	{
		var item = fixture.Walk().OfType<OperationNavigationItem>().First(n => n.Model.Operation.OperationId == "search");
		var context = RenderContext(item);
		var viewModel = new OperationViewModel(context) { Operation = item.Model, Page = OperationPageModel.Create(item.Model, context) };

		var layout = viewModel.CreateGlobalLayoutModel();

		layout.Title.Should().Be("Run a search | Fixture API");
		layout.Description.Should().Be("Returns hits that match the query defined in the request.");
	}

	[Fact]
	public void SchemaLayout_UsesDisplayNameInTitleAndExcerptedDescription()
	{
		var item = fixture
			.Walk()
			.OfType<SchemaNavigationItem>()
			.Should()
			.ContainSingle(n => n.Model.DisplayName == "QueryContainer")
			.Subject;
		var context = RenderContext(item);
		var viewModel = new SchemaViewModel(context) { Schema = item.Model, Page = SchemaPageModel.Create(item.Model, context) };

		var layout = viewModel.CreateGlobalLayoutModel();

		layout.Title.Should().Be("QueryContainer | Fixture API");
		layout.Description.Should().StartWith("A container for a single query variant");
	}

	[Fact]
	public void Excerpt_StripsLinksAndEmphasis()
	{
		var excerpt = ApiSeoDescription.Excerpt("See the [search](https://example.com/search) docs for *queries* and **filters**.");

		excerpt.Should().Be("See the search docs for queries and filters.");
	}

	[Fact]
	public void Excerpt_TruncatesOnWordBoundary()
	{
		var words = string.Join(' ', Enumerable.Range(0, 40).Select(i => $"word{i}"));

		var excerpt = ApiSeoDescription.Excerpt(words) ?? "";

		excerpt.Length.Should().BeGreaterThan(ApiSeoDescription.MaxLength);
		excerpt.Should().EndWith("...");
		excerpt.Should().NotContain("word39");
	}

	[Fact]
	public void Excerpt_EmptyMarkdown_ReturnsNull() => ApiSeoDescription.Excerpt("   ").Should().BeNull();

	[Fact]
	public void ToJsonLd_AbsolutizesParentsAndOmitsCurrentItem()
	{
		INavigationItem[] parents =
		[
			Crumb("/docs/api/elasticsearch", "Elasticsearch API"),
			Crumb("/docs/api/elasticsearch/group/search", "Search")
		];

		var json = BreadcrumbJson.Serialize(parents, "Run a search", new Uri("https://www.elastic.co"));
		var list = JsonSerializer.Deserialize(json, BreadcrumbsContext.Default.BreadcrumbsList);

		list.Should().NotBeNull();
		list.ItemListElement.Should().HaveCount(3);
		list.ItemListElement[0].Position.Should().Be(1);
		list.ItemListElement[0].Name.Should().Be("Elasticsearch API");
		list.ItemListElement[0].Item.Should().Be("https://www.elastic.co/docs/api/elasticsearch");
		list.ItemListElement[1].Position.Should().Be(2);
		list.ItemListElement[1].Item.Should().Be("https://www.elastic.co/docs/api/elasticsearch/group/search");
		list.ItemListElement[2].Position.Should().Be(3);
		list.ItemListElement[2].Name.Should().Be("Run a search");
		list.ItemListElement[2].Item.Should().BeNull();
	}

	private static INavigationItem Crumb(string url, string title)
	{
		var item = A.Fake<INavigationItem>();
		A.CallTo(() => item.Url).Returns(url);
		A.CallTo(() => item.NavigationTitle).Returns(title);
		return item;
	}

	private ApiRenderContext RenderContext(INavigationItem current) =>
		new(fixture.Context, fixture.Document, new StaticFileContentHashProvider(new EmbeddedOrPhysicalFileProvider(fixture.Context)))
		{
			NavigationHtml = string.Empty,
			CurrentNavigation = current,
			MarkdownRenderer = PassthroughMarkdownRenderer.Instance
		};
}
