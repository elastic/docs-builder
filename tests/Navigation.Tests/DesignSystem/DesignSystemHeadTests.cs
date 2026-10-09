// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.Builder;
using Elastic.Documentation.Navigation.Tests.Isolation;
using Elastic.Documentation.Site;
using Elastic.Documentation.Site.FileProviders;
using Elastic.Documentation.Site.Layout;
using RazorSlices;

namespace Elastic.Documentation.Navigation.Tests.DesignSystem;

/// <summary>The design-system bundle is additive and gated by NAVIGATION_PREVIEW: flag off must match the previous head exactly.</summary>
public class DesignSystemHeadTests : DocumentationSetNavigationTestBase
{
	public static IEnumerable<BuildType> BuildTypes() => [BuildType.Isolated, BuildType.Assembler, BuildType.Codex];

	[Test]
	[MethodDataSource(nameof(BuildTypes))]
	public async Task Head_FlagOff_EmitsNoDesignSystemAssets(BuildType buildType)
	{
		var html = await RenderHead(new FeatureFlags([]), buildType);

		html.Should().NotContain("ds.css").And.NotContain("ds.js").And.NotContain("docs-theme");
		html.Should().Contain("styles.css").And.Contain("main.js");
	}

	[Test]
	[MethodDataSource(nameof(BuildTypes))]
	public async Task Head_FlagOn_AddsDesignSystemNextToLegacyBundle(BuildType buildType)
	{
		var html = await RenderHead(new FeatureFlags(new Dictionary<string, bool> { ["navigation-preview"] = true }), buildType);

		html.Should().Contain("ds.css").And.Contain("ds.js").And.Contain("docs-theme");
		html.Should().Contain("styles.css").And.Contain("main.js");
	}

	[Test]
	public async Task Head_FlagOn_LoadsStylesBeforeScripts()
	{
		var html = await RenderHead(new FeatureFlags(new Dictionary<string, bool> { ["navigation-preview"] = true }), BuildType.Isolated);

		html.IndexOf("ds.css", StringComparison.Ordinal).Should().BeLessThan(html.IndexOf("ds.js", StringComparison.Ordinal));
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
