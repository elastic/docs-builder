// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using AwesomeAssertions;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Landing;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.FileSystems;
using Elastic.Documentation.Site.FileProviders;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Tests;

public class ApiLayoutVersionDropdownTests
{
	[Test]
	public void CreateGlobalLayoutModel_BringsItsOwnBarAndTrail()
	{
		var layout = CreateLayout();

		layout.ShowSecondaryNav.Should().BeFalse();
		layout.Breadcrumbs.Select(b => b.NavigationTitle).Should().Equal("Docs", "API");
		layout.Breadcrumbs.Select(b => b.Url).Should().Equal("/", "/api");
		layout.StructuredBreadcrumbsJson.Should().Contain("Elasticsearch API");
		layout.StructuredBreadcrumbsJson.Should().NotContain("Api Overview");
		layout.ApiContext!.Spec.JsonUrl.Should().Be("/api/doc/elasticsearch.json");
		layout.ApiContext.Spec.YamlUrl.Should().Be("/api/doc/elasticsearch.yaml");
	}

	[Test]
	public void CreateGlobalLayoutModel_HasNoVersionsToPickWhenThereIsOne()
	{
		var layout = CreateLayout(items: []);

		layout.ApiContext!.Versions.Should().BeEmpty();
	}

	[Test]
	public void CreateGlobalLayoutModel_DescribesTheCurrentApiInTheContextBar()
	{
		var layout = CreateLayout(buildType: BuildType.Assembler, catalogEntries: Catalog);

		layout.ApiContext.Should().NotBeNull();
		layout.ApiContext!.Title.Should().Be("Elasticsearch");
		layout.ApiContext.HubUrl.Should().Be("/docs/api");
		layout.ApiContext.Products.Select(p => p.Label).Should().Equal("Elasticsearch", "Kibana");
		layout.ApiContext.Products.Should().ContainSingle(p => p.Selected && p.Label == "Elasticsearch");
		layout.ApiContext.Products.Should().OnlyContain(p => p.IconSvg != null);
		layout.ApiContext.Versions.Should().HaveCount(3);
		layout.Breadcrumbs.Select(b => b.NavigationTitle).Should().Equal("Docs", "API");
		layout.Breadcrumbs.Select(b => b.Url).Should().Equal("/docs", "/docs/api");
	}

	[Test]
	public void CreateGlobalLayoutModel_BuildsTheContextBarInIsolatedToo()
	{
		var layout = CreateLayout(catalogEntries: Catalog);

		layout.ApiContext.Should().NotBeNull();
		layout.ApiContext!.Products.Should().HaveCount(2);
	}

	private static readonly ApiCatalogEntry[] Catalog =
	[
		new("elasticsearch", "Elasticsearch", "/docs/api/doc/elasticsearch/", "elasticsearch"),
		new("kibana", "Kibana", "/docs/api/doc/kibana/", "kibana")
	];

	private static ApiLayoutViewModel CreateLayout(
		IReadOnlyList<ApiVersionSwitcherItem>? items = null,
		BuildType buildType = BuildType.Isolated,
		IReadOnlyList<ApiCatalogEntry>? catalogEntries = null
	)
	{
		var fs = new FileSystem();
		var context = new BuildContext(
			new DiagnosticsCollector([]),
			DocumentationFileSystem.Resolve(Paths.WorkingDirectoryRoot.FullName),
			TestHelpers.CreateConfigurationContext(fs)
		)
		{ BuildType = buildType, UrlPathPrefix = buildType == BuildType.Assembler ? "/docs" : null };
		var stack = TestHelpers.CreateStackVersionsConfiguration(currentMajor: 9);
		var product = TestHelpers.CreateProduct("elasticsearch", stack.GetVersioningSystem(VersioningSystemId.Stack));
		var switcherItems = items ?? ApiVersionSwitcher.Build("", "elasticsearch", ["main", "9", "8"], "main");
		var renderContext = new ApiRenderContext(
			context,
			new OpenApiDocument { Info = new OpenApiInfo { Title = "Elasticsearch API", Version = "9.0.0" } },
			new StaticFileContentHashProvider(new EmbeddedOrPhysicalFileProvider(context))
		)
		{
			NavigationHtml = string.Empty,
			CurrentNavigation = new LandingNavigationItem("/api/doc/elasticsearch/").Index,
			MarkdownRenderer = PassthroughMarkdownRenderer.Instance,
			VersionSwitcherItems = switcherItems,
			Product = product,
			CatalogEntries = catalogEntries ?? [],
			CurrentApiKey = catalogEntries is { Count: > 0 } ? "elasticsearch" : null
		};

		return new TestApiViewModel(renderContext).CreateGlobalLayoutModel();
	}

	private sealed class TestApiViewModel(ApiRenderContext context) : ApiViewModel(context);
}
