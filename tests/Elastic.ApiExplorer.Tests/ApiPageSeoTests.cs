// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using AwesomeAssertions;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Landing;
using Elastic.ApiExplorer.Operations;
using Elastic.ApiExplorer.Types;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Navigation;
using Elastic.Documentation.Site;
using Elastic.Documentation.Site.FileProviders;
using FakeItEasy;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Tests;

[ClassDataSource<ApiExplorerFixture>(Shared = SharedType.PerClass)]
public class ApiPageSeoTests(ApiExplorerFixture fixture)
{
	[Test]
	public void OperationLayout_UsesSummaryInTitleAndExcerptedDescription()
	{
		var item = fixture.Walk().OfType<OperationNavigationItem>().First(n => n.Model.Operation.OperationId == "search");
		var context = RenderContext(item);
		var viewModel = new OperationViewModel(context) { Operation = item.Model, Page = OperationPageModel.Create(item.Model, context) };

		var layout = viewModel.CreateGlobalLayoutModel();

		layout.Title.Should().Be("Run a search | Fixture API documentation");
		layout.Description.Should().Be("Returns hits that match the query defined in the request.");
		layout.Breadcrumbs.Take(2).Select(b => b.NavigationTitle).Should().Equal("Docs", "APIs");
		layout.Breadcrumbs[1].Url.Should().Be("/api");
		layout.Breadcrumbs.Should().Contain(b => b.NavigationTitle == "Fixture API");
		layout.Breadcrumbs.Should().NotContain(b => b.NavigationTitle == "Api Overview");
	}

	[Test]
	public void SchemaLayout_UsesDisplayNameInTitleAndExcerptedDescription()
	{
		var item = fixture.Walk().OfType<SchemaNavigationItem>().Should().ContainSingle(n => n.Model.DisplayName == "Query").Subject;
		var context = RenderContext(item);
		var viewModel = new SchemaViewModel(context) { Schema = item.Model, Page = SchemaPageModel.Create(item.Model, context) };

		var layout = viewModel.CreateGlobalLayoutModel();

		layout.Title.Should().Be("Query | Fixture API documentation");
		layout.Description.Should().StartWith("A container for a single query variant");
	}

	[Test]
	[Arguments("v8", "Run a search | Fixture API documentation (v8)")]
	[Arguments("v9", "Run a search | Fixture API documentation (v9)")]
	[Arguments("latest", "Run a search | Fixture API documentation")]
	public void OperationLayout_Version_TitleSuffix(string version, string expected)
	{
		var item = fixture.Walk().OfType<OperationNavigationItem>().First(n => n.Model.Operation.OperationId == "search");
		var context = Versioned(RenderContext(item), version, "Fixture");
		var viewModel = new OperationViewModel(context) { Operation = item.Model, Page = OperationPageModel.Create(item.Model, context) };

		viewModel.CreateGlobalLayoutModel().Title.Should().Be(expected);
	}

	[Test]
	[Arguments("v8", "Elasticsearch API documentation (v8)")]
	[Arguments("v9", "Elasticsearch API documentation (v9)")]
	public void VersionLanding_Title_UsesProductNameNotSpecTitle(string version, string expected)
	{
		var document = new OpenApiDocument
		{
			Info = new OpenApiInfo { Title = "Elasticsearch Request & Response Specification", Version = version }
		};
		var context = Versioned(
			new ApiRenderContext(
				fixture.Context,
				document,
				new StaticFileContentHashProvider(new EmbeddedOrPhysicalFileProvider(fixture.Context))
			)
			{
				NavigationHtml = string.Empty,
				CurrentNavigation = new LandingNavigationItem($"/api/doc/elasticsearch/{version}/").Index,
				MarkdownRenderer = PassthroughMarkdownRenderer.Instance
			},
			version
		);
		var viewModel = new LandingViewModel(context) { Landing = new ApiLanding(), ApiInfo = document.Info, OverviewRows = [] };

		viewModel.CreateGlobalLayoutModel().Title.Should().Be(expected);
	}

	[Test]
	[Arguments("Elastic Cloud Hosted", "Elastic Cloud Billing API", "Elastic Cloud Billing")]
	[Arguments("Elastic Cloud Hosted", "Elastic Cloud Connected API", "Elastic Cloud Connected")]
	[Arguments("Elastic Cloud Hosted", "Elastic Cloud Hosted API", "Elastic Cloud Hosted")]
	[Arguments("Elasticsearch", "Elasticsearch Request & Response Specification", "Elasticsearch")]
	[Arguments(null, "Kibana APIs", "Kibana")]
	public void ProductName_SpecTitleNamingAnotherApi_KeepsSpecTitle(string? displayName, string specTitle, string expected) =>
		ApiPageTitle.ProductName(displayName, specTitle).Should().Be(expected);

	[Test]
	public void CatalogLayout_TitleContainsElastic()
	{
		var catalog = new ApiCatalogNavigationItem("/api/", []);
		var viewModel = ApiCatalogViewModel.FromEntries(RenderContext(catalog.Index), []);

		viewModel.CreateGlobalLayoutModel().Title.Should().Be("Elastic APIs");
	}

	private static ApiRenderContext Versioned(ApiRenderContext context, string version, string displayName = "Elasticsearch") =>
		context with
		{
			Product = new Product { Id = "elasticsearch", DisplayName = displayName },
			VersionSwitcherItems = ApiVersionSwitcher.Build(
				"/api/doc",
				"elasticsearch",
				["main", "9", "8"],
				version == "latest" ? "main" : version.TrimStart('v')
			)
		};

	[Test]
	public void Excerpt_StripsLinksAndEmphasis()
	{
		var excerpt = ApiSeoDescription.Excerpt("See the [search](https://example.com/search) docs for *queries* and **filters**.");

		excerpt.Should().Be("See the search docs for queries and filters.");
	}

	[Test]
	public void Excerpt_TruncatesOnWordBoundary()
	{
		var words = string.Join(' ', Enumerable.Range(0, 40).Select(i => $"word{i}"));

		var excerpt = ApiSeoDescription.Excerpt(words) ?? "";

		excerpt.Length.Should().BeGreaterThan(ApiSeoDescription.MaxLength);
		excerpt.Should().EndWith("...");
		excerpt.Should().NotContain("word39");
	}

	[Test]
	public void Excerpt_EmptyMarkdown_ReturnsNull() => ApiSeoDescription.Excerpt("   ").Should().BeNull();

	[Test]
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
