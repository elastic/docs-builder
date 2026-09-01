// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.Builder;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Documentation.Navigation;
using Elastic.Documentation.Navigation.Tests.Isolation;
using Elastic.Documentation.Site;
using Elastic.Documentation.Site.FileProviders;
using Elastic.Documentation.Versions;
using Elastic.Markdown;
using Elastic.Markdown.Layout;
using RazorSlices;

namespace Elastic.Documentation.Navigation.Tests.Rendering;

public class TableOfContentsRenderingTests(ITestOutputHelper output) : DocumentationSetNavigationTestBase(output)
{
	[Fact]
	public async Task Assembler_FlagOff_RendersVersionDropdown()
	{
		var html = await Render(BuildType.Assembler, showVersionDropdown: true, navigationPreviewEnabled: false);

		html.Should().Contain("<version-dropdown");
		html.Should().Contain("data-testid=\"docs-version-dropdown\"");
		html.Should().Contain("<div class=\"mt-4\">");
		html.Should().NotContain("hidden md:block");
	}

	[Fact]
	public async Task Assembler_FlagOn_OmitsVersionDropdown()
	{
		var html = await Render(BuildType.Assembler, showVersionDropdown: true, navigationPreviewEnabled: true);

		html.Should().NotContain("<version-dropdown");
		html.Should().NotContain("data-testid=\"docs-version-dropdown\"");
	}

	[Fact]
	public async Task Isolated_OmitsVersionDropdown()
	{
		var html = await Render(BuildType.Isolated, showVersionDropdown: false, navigationPreviewEnabled: false);

		html.Should().NotContain("<version-dropdown");
		html.Should().NotContain("data-testid=\"docs-version-dropdown\"");
	}

	private async Task<string> Render(BuildType buildType, bool showVersionDropdown, bool navigationPreviewEnabled)
	{
		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs");
		var context = CreateContext(fileSystem);
		var currentNavItem = new StubNavigationItem("/docs/");

		var model = new MarkdownLayoutViewModel
		{
			DocsBuilderVersion = "test",
			DocSetName = "test",
			Description = "",
			CurrentNavigationItem = currentNavItem,
			Previous = null,
			Next = null,
			NavigationHtml = "",
			UrlPathPrefix = "/docs",
			CanonicalBaseUrl = null,
			AllowIndexing = false,
			Features = navigationPreviewEnabled
				? new FeatureFlags(new Dictionary<string, bool> { ["navigation-preview"] = true })
				: new FeatureFlags([]),
			GoogleTagManager = new GoogleTagManagerConfiguration(),
			Optimizely = new OptimizelyConfiguration(),
			StaticFileContentHashProvider = new StaticFileContentHashProvider(new EmbeddedOrPhysicalFileProvider(context)),
			BuildType = buildType,
			ShowVersionDropdown = showVersionDropdown,
			AllVersionsUrl = "/docs/versions/",
			CurrentVersion = "8.19",
			VersionDropdownSerializedModel = "[]",
			GithubEditUrl = null,
			MarkdownUrl = "/docs/page.md",
			HideEditThisPage = true,
			ReportIssueUrl = null,
			Breadcrumbs = [],
			PageTocItems = [],
			Layout = null,
			VersioningSystem = new VersioningSystem
			{
				Id = VersioningSystemId.Stack,
				Base = new SemVersion(8, 0, 0),
				Current = new SemVersion(8, 19, 0)
			},
			Cta = Cta.Default
		};

		return await _TableOfContents.Create(model).RenderAsync(cancellationToken: TestContext.Current.CancellationToken);
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
