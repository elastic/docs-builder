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
using Elastic.Documentation.Site;
using Elastic.Documentation.Site.FileProviders;
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
		layout.HeaderTitle.Should().Be("Fixture API");
		layout.Description.Should().Be("Returns hits that match the query defined in the request.");
	}

	[Test]
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

		layout.Title.Should().Be("QueryContainer | Fixture API documentation");
		layout.Description.Should().StartWith("A container for a single query variant");
	}

	[Test]
	[Arguments("v8")]
	[Arguments("v9")]
	public void OperationLayout_ReleasedMajor_TitleIncludesVersion(string version)
	{
		var item = fixture.Walk().OfType<OperationNavigationItem>().First(n => n.Model.Operation.OperationId == "search");
		var context = Versioned(RenderContext(item), version);
		var viewModel = new OperationViewModel(context) { Operation = item.Model, Page = OperationPageModel.Create(item.Model, context) };

		var layout = viewModel.CreateGlobalLayoutModel();

		layout.Title.Should().Be($"Run a search | Elasticsearch API documentation ({version})");
	}

	[Test]
	public void OperationLayout_Latest_OmitsVersion()
	{
		var item = fixture.Walk().OfType<OperationNavigationItem>().First(n => n.Model.Operation.OperationId == "search");
		var context = Versioned(RenderContext(item), "latest");
		var viewModel = new OperationViewModel(context) { Operation = item.Model, Page = OperationPageModel.Create(item.Model, context) };

		viewModel.CreateGlobalLayoutModel().Title.Should().Be("Run a search | Elasticsearch API documentation");
	}

	[Test]
	public void OperationLayout_SharedPathPreamble_UsesEachOperationsOwnText()
	{
		var indices = OperationLayout(PathPreamble("Get high-level information about indices in a cluster."), "Get index information");
		var delete = OperationLayout(PathPreamble("Delete an index and its documents."), "Delete index");

		indices.Description.Should().Be("Get high-level information about indices in a cluster.");
		delete.Description.Should().Be("Delete an index and its documents.");
		indices.Description.Should().NotBe(delete.Description);
		indices.Title.Should().Be("Get index information | Elasticsearch API documentation");
	}

	[Test]
	[Arguments("v8")]
	[Arguments("v9")]
	public void VersionLanding_EmptyDescription_GetsOne(string version)
	{
		var document = new OpenApiDocument
		{
			Info = new OpenApiInfo { Title = "Elasticsearch Request & Response Specification", Version = version }
		};
		var nav = new LandingNavigationItem($"/api/doc/elasticsearch/{version}/").Index;
		var context = Versioned(
			new ApiRenderContext(
				fixture.Context,
				document,
				new StaticFileContentHashProvider(new EmbeddedOrPhysicalFileProvider(fixture.Context))
			)
			{ NavigationHtml = string.Empty, CurrentNavigation = nav, MarkdownRenderer = PassthroughMarkdownRenderer.Instance },
			version
		);
		var viewModel = new LandingViewModel(context) { Landing = new ApiLanding(), ApiInfo = document.Info, OverviewRows = [] };

		var layout = viewModel.CreateGlobalLayoutModel();

		layout.Title.Should().Be($"Elasticsearch API documentation ({version})");
		layout.Description.Should().Be($"Elasticsearch API documentation ({version}).");
	}

	[Test]
	public void CatalogLayout_TitleContainsElastic()
	{
		var catalog = new ApiCatalogNavigationItem("/api/", []);
		var viewModel = ApiCatalogViewModel.FromEntries(RenderContext(catalog.Index), []);

		viewModel.CreateGlobalLayoutModel().Title.Should().Be("Elastic APIs");
	}

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
		ApiBreadcrumb[] crumbs =
		[
			new("Elasticsearch API", "/docs/api/elasticsearch"),
			new("Search", "/docs/api/elasticsearch/group/search"),
			new("Run a search", null)
		];

		var json = ApiBreadcrumbBuilder.ToJsonLd(crumbs, new Uri("https://www.elastic.co"));
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

	private static string PathPreamble(string sentence) =>
		$"""
		**All methods and paths for this operation:**

		<div><span class="operation-verb get">GET</span> <span class="operation-path">/_cat/indices</span></div>

		{sentence}
		""";

	private GlobalLayoutViewModel OperationLayout(string description, string summary)
	{
		var root = fixture.Navigation;
		var apiOperation = new ApiOperation(
			HttpMethod.Get,
			new OpenApiOperation { OperationId = summary.Replace(' ', '-'), Summary = summary, Description = description },
			"/_cat/indices",
			new OpenApiPathItem(),
			summary
		);
		var nav = new OperationNavigationItem(null, "elasticsearch", apiOperation, root, root);
		var context = Versioned(RenderContext(nav), "latest");
		var page = OperationPageModel.Create(apiOperation, context);
		return new OperationViewModel(context) { Operation = apiOperation, Page = page }.CreateGlobalLayoutModel();
	}

	private ApiRenderContext Versioned(ApiRenderContext context, string version) =>
		context with
		{
			Product = new Product { Id = "elasticsearch", DisplayName = "Elasticsearch" },
			VersionSwitcherItems =
			[
				new("latest", "/api/doc/elasticsearch/", version == "latest"),
				new("v9", "/api/doc/elasticsearch/v9/", version == "v9"),
				new("v8", "/api/doc/elasticsearch/v8/", version == "v8")
			]
		};

	private ApiRenderContext RenderContext(Elastic.Documentation.Navigation.INavigationItem current) =>
		new(fixture.Context, fixture.Document, new StaticFileContentHashProvider(new EmbeddedOrPhysicalFileProvider(fixture.Context)))
		{
			NavigationHtml = string.Empty,
			CurrentNavigation = current,
			MarkdownRenderer = PassthroughMarkdownRenderer.Instance
		};
}
