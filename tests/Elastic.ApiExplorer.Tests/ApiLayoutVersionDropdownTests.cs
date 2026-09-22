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
	public void CreateGlobalLayoutModel_KeepsVersionOffTheTopBar()
	{
		var layout = CreateLayout();

		layout.ShowVersionDropdown.Should().BeFalse();
		layout.ShowLegacyBarVersionDropdown.Should().BeFalse();
		layout.CurrentVersion.Should().Be("9.0+");
		layout.VersionDropdownSerializedModel.Should().Contain("\"name\":\"latest\"");
		layout.VersionDropdownSerializedModel.Should().Contain("\"name\":\"v9\"");
		layout.ApiCatalogUrl.Should().Be("/api/");
		layout.SpecJsonUrl.Should().Be("/api/doc/elasticsearch.json");
		layout.SpecYamlUrl.Should().Be("/api/doc/elasticsearch.yaml");
	}

	[Test]
	public void CreateGlobalLayoutModel_HidesDropdownWhenOnlyOneVersion()
	{
		var layout = CreateLayout(items: []);

		layout.ShowVersionDropdown.Should().BeFalse();
		layout.ShowLegacyBarVersionDropdown.Should().BeFalse();
		layout.VersionDropdownSerializedModel.Should().Be("[]");
	}

	[Test]
	public void CreateGlobalLayoutModel_WiresProductSwitcherOnAssemblerGreyBar()
	{
		var layout = CreateLayout(buildType: BuildType.Assembler, catalogEntries: Catalog);

		layout.LegacyBarProductSwitcher.Should().HaveCount(3);
		layout.LegacyBarProductSwitcher[0].Label.Should().Be("Back to hub");
		layout.LegacyBarProductSwitcher[0].Value.Should().Be("/docs/api/");
		layout.LegacyBarProductSwitcher.Should().ContainSingle(i => i.Selected && i.Label == "Elasticsearch");
		layout.HubSwitcherItems.Should().HaveCount(3);
	}

	[Test]
	public void CreateGlobalLayoutModel_KeepsProductSwitcherOffTheGreyBarInIsolated()
	{
		var layout = CreateLayout(catalogEntries: Catalog);

		layout.LegacyBarProductSwitcher.Should().BeEmpty();
		layout.HubSwitcherItems.Should().HaveCount(3);
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
