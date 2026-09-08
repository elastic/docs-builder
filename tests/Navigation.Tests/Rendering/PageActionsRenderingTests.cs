// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.Builder;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Documentation.Navigation.Tests.Isolation;
using Elastic.Documentation.Site;
using Elastic.Documentation.Site.FileProviders;
using Elastic.Documentation.Versions;
using Elastic.Markdown;
using Elastic.Markdown.Layout;
using RazorSlices;

namespace Elastic.Documentation.Navigation.Tests.Rendering;

public class PageActionsRenderingTests(ITestOutputHelper output) : DocumentationSetNavigationTestBase(output)
{
	[Fact]
	public async Task PrimaryNavOn_RendersBothCtas()
	{
		var html = await Render(new PageActionsScenario
		{
			PrimaryNavEnabled = true,
			GithubEditUrl = "https://github.com/elastic/docs/edit/main/page.md"
		});

		html.Should().Contain("class=\"page-actions\"");
		html.Should().Contain("Edit page");
		html.Should().Contain("Report issue");
		html.Should().Contain("href=\"https://github.com/elastic/docs/edit/main/page.md\"");
		html.Should().NotContain("page-actions__action hidden");
		html
			.IndexOf("Edit page", StringComparison.Ordinal)
			.Should()
			.BeLessThan(html.IndexOf("Report issue", StringComparison.Ordinal), "the edit button comes first");
	}

	[Fact]
	public async Task PrimaryNavOff_OmitsReportIssueButKeepsEditPage()
	{
		var html = await Render(new PageActionsScenario { GithubEditUrl = "https://github.com/elastic/docs/edit/main/page.md" });

		html.Should().NotContain("Report issue");
		html.Should().Contain("Edit page");
	}

	[Fact]
	public async Task Codex_OmitsReportIssue()
	{
		var html = await Render(new PageActionsScenario
		{
			BuildType = BuildType.Codex,
			PrimaryNavEnabled = true,
			GithubEditUrl = "https://github.com/elastic/docs/edit/main/page.md"
		});

		html.Should().NotContain("Report issue");
		html.Should().Contain("Edit page");
	}

	[Fact]
	public async Task NoGithubEditUrl_OmitsEditPage()
	{
		var html = await Render(new PageActionsScenario { PrimaryNavEnabled = true });

		html.Should().Contain("Report issue");
		html.Should().NotContain("Edit page");
	}

	[Fact]
	public async Task HideEditThisPage_RendersEditPageHidden()
	{
		var html = await Render(new PageActionsScenario
		{
			PrimaryNavEnabled = true,
			GithubEditUrl = "https://github.com/elastic/docs/edit/main/page.md",
			HideEditThisPage = true
		});

		// main.ts reveals every ".edit-this-page.hidden" when ?edit is present, so the class pair matters.
		html.Should().Contain("edit-this-page page-actions__action hidden");
	}

	[Fact]
	public async Task NoCtas_RendersNothing()
	{
		var html = await Render(new PageActionsScenario());

		html.Should().NotContain("page-actions");
		html.Trim().Should().BeEmpty();
	}

	private sealed record PageActionsScenario
	{
		public BuildType BuildType { get; init; } = BuildType.Assembler;
		public bool PrimaryNavEnabled { get; init; }
		public string? GithubEditUrl { get; init; }
		public bool HideEditThisPage { get; init; }
	}

	private async Task<string> Render(PageActionsScenario scenario)
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
			Features = new FeatureFlags(new Dictionary<string, bool> { ["primary-nav"] = scenario.PrimaryNavEnabled }),
			GoogleTagManager = new GoogleTagManagerConfiguration(),
			Optimizely = new OptimizelyConfiguration(),
			StaticFileContentHashProvider = new StaticFileContentHashProvider(new EmbeddedOrPhysicalFileProvider(context)),
			BuildType = scenario.BuildType,
			ShowVersionDropdown = false,
			AllVersionsUrl = "/docs/versions/",
			CurrentVersion = "8.19",
			VersionDropdownSerializedModel = "[]",
			GithubEditUrl = scenario.GithubEditUrl,
			MarkdownUrl = "/docs/page.md",
			HideEditThisPage = scenario.HideEditThisPage,
			ReportIssueUrl = "https://github.com/elastic/docs/issues/new",
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

		return await _PageActions.Create(model).RenderAsync(cancellationToken: TestContext.Current.CancellationToken);
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
