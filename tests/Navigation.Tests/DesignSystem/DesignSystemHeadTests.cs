// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using System.Text.RegularExpressions;
using AwesomeAssertions;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.Builder;
using Elastic.Documentation.Navigation.Tests.Isolation;
using Elastic.Documentation.Site;
using Elastic.Documentation.Site.FileProviders;
using Elastic.Documentation.Site.Layout;
using RazorSlices;

namespace Elastic.Documentation.Navigation.Tests.DesignSystem;

/// <summary>The design-system bundle replaces the legacy one behind NAVIGATION_PREVIEW: flag off must match the previous head exactly.</summary>
public class DesignSystemHeadTests : DocumentationSetNavigationTestBase
{
	public static IEnumerable<BuildType> BuildTypes() => [BuildType.Isolated, BuildType.Assembler, BuildType.Codex];

	[Test]
	[MethodDataSource(nameof(BuildTypes))]
	public async Task Head_FlagOff_EmitsNoDesignSystemAssets(BuildType buildType)
	{
		var html = await RenderHead(new FeatureFlags([]), buildType);

		html.Should().NotContain("ds.css").And.NotContain("docs-theme").And.NotContain("docs-motion");
		EntryIndex(html, "ds").Should().Be(-1);
		html.Should().Contain("styles.css");
		EntryIndex(html, "main").Should().BeGreaterThanOrEqualTo(0);
	}

	[Test]
	[MethodDataSource(nameof(BuildTypes))]
	public async Task Head_FlagOn_LoadsOnlyTheDesignSystemBundle(BuildType buildType)
	{
		var html = await RenderHead(new FeatureFlags(new Dictionary<string, bool> { ["navigation-preview"] = true }), buildType);

		html.Should().Contain("ds.css").And.Contain("docs-theme");
		EntryIndex(html, "ds").Should().BeGreaterThanOrEqualTo(0);
		html.Should().Contain("docs-motion", "the head sets data-motion before first paint");
		html.Should().NotContain("styles.css");
		EntryIndex(html, "main").Should().Be(-1);
		html.Should().NotContain("assembler.css").And.NotContain("isolated.css").And.NotContain("codex.css");
	}

	[Test]
	public async Task Head_FlagOn_LoadsTheLayerOrderBeforeTheBundle()
	{
		var html = await RenderHead(new FeatureFlags(new Dictionary<string, bool> { ["navigation-preview"] = true }), BuildType.Isolated);

		var layers = html.IndexOf("ds-layers.css", StringComparison.Ordinal);
		layers.Should().BeGreaterThanOrEqualTo(0);
		layers.Should().BeLessThan(
			html.IndexOf("ds.css", StringComparison.Ordinal),
			"the order must be declared before the layers it names"
		);
	}

	[Test]
	public async Task Head_FlagOn_LoadsStylesBeforeScripts()
	{
		var html = await RenderHead(new FeatureFlags(new Dictionary<string, bool> { ["navigation-preview"] = true }), BuildType.Isolated);

		html.IndexOf("ds.css", StringComparison.Ordinal).Should().BeLessThan(EntryIndex(html, "ds"));
	}

	[Test]
	public async Task StateMeta_NoFlags_ListsNone()
	{
		var html = await RenderHead(new FeatureFlags([]), BuildType.Isolated);

		StateMeta(html)
			.Should()
			.MatchInlineSnapshot(
				"""
				<meta name="docs:features">
				<meta name="docs:build-type" content="isolated">
				"""
			);
	}

	[Test]
	public async Task StateMeta_Flags_AreListedSortedAndOnlyWhenEnabled()
	{
		var flags = new Dictionary<string, bool>
		{
			["page-feedback"] = true,
			["assembler-api-explorer"] = true,
			["navigation-preview"] = true,
			["guide-nav"] = false
		};

		var html = await RenderHead(new FeatureFlags(flags), BuildType.Assembler);

		StateMeta(html)
			.Should()
			.MatchInlineSnapshot(
				"""
			<meta name="docs:features" content="assembler-api-explorer navigation-preview page-feedback">
			<meta name="docs:build-type" content="assembler">
			"""
			);
	}

	[Test]
	[Arguments(BuildType.Isolated, "isolated")]
	[Arguments(BuildType.Assembler, "assembler")]
	[Arguments(BuildType.Codex, "codex")]
	public async Task StateMeta_BuildType_MatchesTheBuild(BuildType buildType, string expected)
	{
		var html = await RenderHead(new FeatureFlags([]), buildType);

		html.Should().Contain($"""<meta name="docs:build-type" content="{expected}">""");
	}

	[Test]
	public async Task StateMeta_AirGappedAssembler_IsStillAnAssemblerBuild()
	{
		var html = await RenderHead(new FeatureFlags(new Dictionary<string, bool> { ["air-gapped"] = true }), BuildType.Assembler);

		StateMeta(html)
			.Should()
			.MatchInlineSnapshot(
				"""
			<meta name="docs:features" content="air-gapped">
			<meta name="docs:build-type" content="assembler">
			"""
			);
	}

	private const string WafIntegrationUrl = "https://waf.example/jsapi.js";

	[Test]
	public async Task WafSdk_AssemblerWithUrl_LoadsTheScriptAsync()
	{
		var html = await RenderHead(new FeatureFlags([]) { WafIntegrationUrl = WafIntegrationUrl }, BuildType.Assembler);

		html.Should().Contain($"""<script src="{WafIntegrationUrl}" async></script>""");
	}

	[Test]
	[Arguments(BuildType.Isolated)]
	[Arguments(BuildType.Codex)]
	public async Task WafSdk_OtherBuildTypes_LoadNoScript(BuildType buildType)
	{
		var html = await RenderHead(new FeatureFlags([]) { WafIntegrationUrl = WafIntegrationUrl }, buildType);

		html.Should().NotContain(WafIntegrationUrl);
	}

	[Test]
	public async Task WafSdk_AirGappedAssembler_LoadsNoScript()
	{
		var features = new FeatureFlags(new Dictionary<string, bool> { ["air-gapped"] = true }) { WafIntegrationUrl = WafIntegrationUrl };

		var html = await RenderHead(features, BuildType.Assembler);

		html.Should().NotContain(WafIntegrationUrl);
	}

	[Test]
	public async Task WafSdk_AssemblerWithoutUrl_LoadsNoScript()
	{
		var html = await RenderHead(new FeatureFlags([]), BuildType.Assembler);

		html.Should().NotContain("awswaf").And.NotContain("waf.example");
	}

	private static string StateMeta(string html) =>
		string.Join('\n', Regex.Matches(html, @"<meta name=""docs:(features|build-type)""[^>]*>").Select(m => m.Value));

	// Vite writes an entry bundle as <name>-[hash].js and the head references it by that name, so match the name with or without a hash.
	private static int EntryIndex(string html, string name)
	{
		var match = Regex.Match(html, $@"/{name}(-[\w-]+)?\.js");
		return match.Success ? match.Index : -1;
	}

	private async Task<string> RenderHead(FeatureFlags features, BuildType buildType)
	{
		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs");
		var context = CreateContext(fileSystem);

		var model = new GlobalLayoutViewModel
		{
			DocSetName = "test",
			Description = "",
			CurrentNavigationItem = new StubNavigationItem("/docs/"),
			Previous = null,
			Next = null,
			NavigationHtml = "",
			UrlPathPrefix = "/docs",
			CanonicalBaseUrl = null,
			AllowIndexing = false,
			Features = features,
			GoogleTagManager = new GoogleTagManagerConfiguration(),
			Optimizely = new OptimizelyConfiguration(),
			StaticFileContentHashProvider = new StaticFileContentHashProvider(new EmbeddedOrPhysicalFileProvider(context)),
			BuildType = buildType
		};

		return await _Head.Create(model).RenderAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);
	}

	private sealed record StubNavigationItem(string Url) : INavigationItem
	{
		public string NavigationTitle => "stub";
		public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot => null!;
		public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; }
		public bool Hidden => false;
		public int NavigationIndex { get; set; }
	}
}
