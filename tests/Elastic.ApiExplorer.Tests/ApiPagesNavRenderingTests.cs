// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Text.RegularExpressions;
using AwesomeAssertions;
using Elastic.ApiExplorer._Partials.Layout;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Landing;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.Builder;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.FileSystems;
using Elastic.Documentation.Site;
using Elastic.Documentation.Site.FileProviders;
using RazorSlices;

namespace Elastic.ApiExplorer.Tests;

public partial class ApiPagesNavRenderingTests
{
	[Fact]
	public async Task Render_HostsTheVersionSwitcherAheadOfTheTree()
	{
		var model = CreateLayoutModel(
			"/api/doc/elasticsearch/v9/",
			"/api/doc/elasticsearch/v9.md",
			versionSwitcherItems: [
				new("latest", "/api/doc/elasticsearch/", Selected: false),
				new("v9", "/api/doc/elasticsearch/v9/", Selected: true),
				new("v8", "/api/doc/elasticsearch/v8/", Selected: false),
			]
		);

		var html = await _ApiPagesNav.Create(model).RenderAsync(cancellationToken: TestContext.Current.CancellationToken);

		html.Should().Contain("id=\"api-version-switcher\"");
		html.Should().Contain("aria-selected=\"true\"");
		html.Should().Contain("aria-selected=\"false\"");
		html.Should().NotContain("selected=\"False\"");
		html.Should().NotContain("selected=\"True\"");
		html.Should().NotContain("<select");
		html.Should().NotContain("<option");
		html
			.IndexOf("id=\"api-version-switcher\"", StringComparison.Ordinal)
			.Should()
			.BeLessThan(html.IndexOf("<nav>tree</nav>", StringComparison.Ordinal));
	}

	[Fact]
	public async Task Render_MarksOnlyCurrentHubProductSelected()
	{
		var model = CreateLayoutModel(
			"/api/doc/elasticsearch/",
			"/api/doc/elasticsearch.md",
			hubSwitcherItems: [
				new("Back to hub", "/api/", Selected: false),
				new("Elasticsearch", "/api/doc/elasticsearch/", Selected: true),
				new("Kibana", "/api/doc/kibana/", Selected: false),
			]
		);

		var html = await _ApiPagesNav.Create(model).RenderAsync(cancellationToken: TestContext.Current.CancellationToken);

		html.Should().Contain("id=\"api-hub-switcher\"");
		html.Should().NotContain("api-version-switcher");
		html.Should().NotContain("selected=\"False\"");
		html.Should().NotContain("selected=\"True\"");
		html.Should().Contain("<option value=\"/api/\">Back to hub</option>");
		html.Should().Contain("<option value=\"/api/doc/elasticsearch/\" selected>Elasticsearch</option>");
		html.Should().Contain("<option value=\"/api/doc/kibana/\">Kibana</option>");
		CountSelectedOptions(html).Should().Be(1);
	}

	[Fact]
	public async Task Render_Assembler_OmitsTheHubSwitcher()
	{
		var model = CreateLayoutModel(
			"/api/doc/elasticsearch/",
			"/api/doc/elasticsearch.md",
			hubSwitcherItems: [
				new("Back to hub", "/api/", Selected: false),
				new("Elasticsearch", "/api/doc/elasticsearch/", Selected: true),
			],
			buildType: BuildType.Assembler
		);

		var html = await _ApiPagesNav.Create(model).RenderAsync(cancellationToken: TestContext.Current.CancellationToken);

		html.Should().NotContain("api-hub-switcher");
		html.Should().NotContain("<select");
		html.Should().NotContain("<option");
	}

	[Fact]
	public async Task Render_PreservesTheNavAcrossHtmxSwapsWhenPreviewEnabled()
	{
		var model = CreateLayoutModel(
			"/api/doc/elasticsearch/",
			"/api/doc/elasticsearch.md",
			features: new FeatureFlags(new Dictionary<string, bool> { ["navigation-preview"] = true })
		);

		var html = await _ApiPagesNav.Create(model).RenderAsync(cancellationToken: TestContext.Current.CancellationToken);

		html.Should().Contain("id=\"pages-nav\"");
		html.Should().Contain("hx-preserve");
	}

	[Fact]
	public async Task Render_ShowsJumpToPageOnAssemblerBuilds()
	{
		var model = CreateLayoutModel(
			"/api/doc/elasticsearch/",
			"/api/doc/elasticsearch.md",
			versionSwitcherItems: [
				new("latest", "/api/doc/elasticsearch/", Selected: true),
				new("v8", "/api/doc/elasticsearch/v8/", Selected: false),
			],
			buildType: BuildType.Assembler
		);

		var html = await _ApiPagesNav.Create(model).RenderAsync(cancellationToken: TestContext.Current.CancellationToken);

		html.Should().Contain("<navigation-search type=\"api\" placeholder=\"Jump to API\"></navigation-search>");
		html
			.IndexOf("id=\"api-version-switcher\"", StringComparison.Ordinal)
			.Should()
			.BeLessThan(html.IndexOf("navigation-search", StringComparison.Ordinal));
	}

	[Fact]
	public async Task Render_ShowsJumpToPageWhenNavigationPreviewIsOn()
	{
		var model = CreateLayoutModel(
			"/api/doc/elasticsearch/",
			"/api/doc/elasticsearch.md",
			features: new FeatureFlags(new Dictionary<string, bool> { ["navigation-preview"] = true }),
			buildType: BuildType.Assembler
		);

		var html = await _ApiPagesNav.Create(model).RenderAsync(cancellationToken: TestContext.Current.CancellationToken);

		html.Should().Contain("<navigation-search type=\"api\" placeholder=\"Jump to API\"></navigation-search>");
		html.Should().Contain("hx-preserve");
	}

	[Fact]
	public async Task Render_OmitsJumpToPageOnIsolatedBuilds()
	{
		var model = CreateLayoutModel("/api/doc/elasticsearch/", "/api/doc/elasticsearch.md", buildType: BuildType.Isolated);

		var html = await _ApiPagesNav.Create(model).RenderAsync(cancellationToken: TestContext.Current.CancellationToken);

		html.Should().NotContain("navigation-search");
	}

	[Fact]
	public async Task Render_OmitsJumpToPageWhenAirGapped()
	{
		var model = CreateLayoutModel(
			"/api/doc/elasticsearch/",
			"/api/doc/elasticsearch.md",
			features: new FeatureFlags(new Dictionary<string, bool> { ["air-gapped"] = true }),
			buildType: BuildType.Assembler
		);

		var html = await _ApiPagesNav.Create(model).RenderAsync(cancellationToken: TestContext.Current.CancellationToken);

		html.Should().NotContain("navigation-search");
	}

	private static ApiLayoutViewModel CreateLayoutModel(
		string navigationUrl,
		string markdownUrl,
		IReadOnlyList<ApiVersionSwitcherItem>? versionSwitcherItems = null,
		IReadOnlyList<ApiVersionSwitcherItem>? hubSwitcherItems = null,
		FeatureFlags? features = null,
		BuildType buildType = BuildType.Isolated
	)
	{
		var fs = new FileSystem();
		var context = new BuildContext(
			new DiagnosticsCollector([]),
			DocumentationFileSystem.Resolve(Paths.WorkingDirectoryRoot.FullName),
			TestHelpers.CreateConfigurationContext(fs)
		);
		return new()
		{
			DocSetName = "Api Explorer",
			Description = string.Empty,
			CurrentNavigationItem = new LandingNavigationItem(navigationUrl).Index,
			Previous = null,
			Next = null,
			NavigationHtml = "<nav>tree</nav>",
			UrlPathPrefix = string.Empty,
			AllowIndexing = false,
			CanonicalBaseUrl = null,
			GoogleTagManager = new GoogleTagManagerConfiguration(),
			Optimizely = new OptimizelyConfiguration(),
			Features = features ?? new FeatureFlags([]),
			BuildType = buildType,
			StaticFileContentHashProvider = new StaticFileContentHashProvider(new EmbeddedOrPhysicalFileProvider(context)),
			TocItems = [],
			MarkdownUrl = markdownUrl,
			Breadcrumbs = [],
			VersionSwitcherItems = versionSwitcherItems ?? [],
			HubSwitcherItems = hubSwitcherItems ?? [],
		};
	}

	private static int CountSelectedOptions(string html) =>
		OptionTag().Matches(html).Count(m => m.Value.Contains(" selected", StringComparison.Ordinal));

	[GeneratedRegex("<option[^>]*>", RegexOptions.IgnoreCase)]
	private static partial Regex OptionTag();
}
