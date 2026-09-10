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
using RazorSlices;

namespace Elastic.Documentation.Navigation.Tests.Rendering;

public class LandingLayoutRenderingTests(ITestOutputHelper output) : DocumentationSetNavigationTestBase(output)
{
	[Fact]
	public async Task LandingPage_OmitsEmptyMobileHamburger()
	{
		var html = await RenderLanding();

		html.Should().Contain("Elastic Docs");
		html.Should().NotContain("pages-nav-hamburger");
		html.Should().NotContain("id=\"pages-nav\"");
	}

	private async Task<string> RenderLanding()
	{
		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs");
		var context = CreateContext(fileSystem);

		var model = new MarkdownLayoutViewModel
		{
			DocsBuilderVersion = "test",
			DocSetName = "test",
			Description = "",
			CurrentNavigationItem = new StubNavigationItem("/docs/"),
			Previous = null,
			Next = null,
			NavigationHtml = "",
			UrlPathPrefix = "/docs",
			CanonicalBaseUrl = null,
			AllowIndexing = false,
			Features = new FeatureFlags([]),
			GoogleTagManager = new GoogleTagManagerConfiguration(),
			Optimizely = new OptimizelyConfiguration(),
			StaticFileContentHashProvider = new StaticFileContentHashProvider(new EmbeddedOrPhysicalFileProvider(context)),
			BuildType = BuildType.Assembler,
			ShowVersionDropdown = true,
			AllVersionsUrl = "/docs/release-versioning/all-versions",
			CurrentVersion = "9.5.3",
			VersionDropdownSerializedModel = "[]",
			GithubEditUrl = null,
			MarkdownUrl = "/docs/index.md",
			HideEditThisPage = true,
			ReportIssueUrl = null,
			Breadcrumbs = [],
			PageTocItems = [],
			Layout = MarkdownPageLayout.LandingPage,
			VersioningSystem = new VersioningSystem
			{
				Id = VersioningSystemId.Stack,
				Base = new SemVersion(9, 0, 0),
				Current = new SemVersion(9, 5, 3)
			},
			Cta = Cta.Default
		};

		return await _Layout.Create(model).RenderAsync(cancellationToken: TestContext.Current.CancellationToken);
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
