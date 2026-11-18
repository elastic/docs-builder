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

		layout.Title.Should().Be("Run a search | Fixture API docs");
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

		layout.Title.Should().Be("Query | Fixture API docs");
		layout.Description.Should().StartWith("A container for a single query variant");
	}

	[Test]
	[Arguments("v8", "Run a search | Fixture API docs (v8)")]
	[Arguments("v9", "Run a search | Fixture API docs (v9)")]
	[Arguments("latest", "Run a search | Fixture API docs")]
	public void OperationLayout_Version_TitleSuffix(string version, string expected)
	{
		var item = fixture.Walk().OfType<OperationNavigationItem>().First(n => n.Model.Operation.OperationId == "search");
		var context = Versioned(RenderContext(item), version, "Fixture");
		var viewModel = new OperationViewModel(context) { Operation = item.Model, Page = OperationPageModel.Create(item.Model, context) };

		viewModel.CreateGlobalLayoutModel().Title.Should().Be(expected);
	}

	[Test]
	[Arguments("v8", "Elasticsearch API (v8)")]
	[Arguments("v9", "Elasticsearch API (v9)")]
	[Arguments("latest", "Elasticsearch API")]
	public void VersionLanding_Heading_CarriesReleasedVersion(string version, string expected)
	{
		var document = new OpenApiDocument { Info = new OpenApiInfo { Title = "Elasticsearch API", Version = version } };
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

		viewModel.Heading.Should().Be(expected);
		LandingCommonMark
			.Product(document.Info, [], "/api/doc/elasticsearch", ApiPageTitle.ReleasedVersionLabel(context.VersionSwitcherItems))
			.Should()
			.StartWith($"# {expected}");
	}

	[Test]
	[Arguments("v8", "Elasticsearch API docs (v8)")]
	[Arguments("v9", "Elasticsearch API docs (v9)")]
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

	private const string EsPreamble =
		"**All methods and paths for this operation:**\n\n<div>\n  <span class=\"operation-verb get\">GET</span>\n  <span class=\"operation-path\">/_cat/indices</span>\n</div>\n<div>\n  <span class=\"operation-verb get\">GET</span>\n  <span class=\"operation-path\">/_cat/indices/{index}</span>\n</div>\n\n";

	private const string KibanaPreamble =
		"**Spaces method and path for this operation:**\n\n<div><span class=\"operation-verb post\">post</span>&nbsp;<span class=\"operation-path\">/s/{space_id}/api/agent_builder/mcp</span></div>\n\nRefer to [Spaces](https://www.elastic.co/docs/deploy-manage/manage-spaces) for more information.\n\n";

	[Test]
	[Arguments(EsPreamble
		+ "Get high-level information about indices in a cluster.\n\nSecond paragraph.", "Get high-level information about indices in a cluster.")]
	[Arguments(KibanaPreamble + "Communicate with the MCP server.", "Communicate with the MCP server.")]
	[Arguments("Reindex documents.\n\nCopies documents from a source to a destination.", "Reindex documents.")]
	public void OperationText_SkipsThePathPreamble(string description, string expected) =>
		ApiSeoDescription.Excerpt(ApiSeoDescription.OperationText(description, "Summary")).Should().Be(expected);

	[Test]
	[Arguments(EsPreamble, "Get index information")]
	[Arguments(KibanaPreamble, "Get index information")]
	[Arguments(null, "Get index information")]
	[Arguments("  ", "Get index information")]
	public void OperationText_NothingAfterThePreamble_UsesTheSummary(string? description, string expected) =>
		ApiSeoDescription.OperationText(description, "Get index information").Should().Be(expected);

	[Test]
	public void OperationText_NoDescriptionNoSummary_ReturnsNull() => ApiSeoDescription.OperationText(EsPreamble, null).Should().BeNull();

	[Test]
	public void OperationText_ReferLineWithoutAListing_IsKept() =>
		ApiSeoDescription
			.OperationText("Refer to the docs for more information.", "Summary")
			.Should()
			.Be("Refer to the docs for more information.");

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
