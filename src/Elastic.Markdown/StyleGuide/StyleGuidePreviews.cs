// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.Builder;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Documentation.Site;
using Elastic.Documentation.Site.Components.NavSide;
using Elastic.Documentation.Site.FileProviders;
using Elastic.Documentation.Versions;
using RazorSlices;

namespace Elastic.Markdown.StyleGuide;

/// <summary>
/// Whole pages of the isolated and assembler builds, rendered with the real layouts around placeholder content,
/// so the style guide can show how the pieces come together without a clone of any repository.
/// </summary>
public static class StyleGuidePreviews
{
	private const string Lorem =
		"Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat.";

	private static readonly string ArticleHtml = """
		<section id="lorem-ipsum">
		<h1>Lorem ipsum dolor sit amet</h1>
		<p>__LOREM__</p>
		<h2 id="consectetur-adipiscing">Consectetur adipiscing</h2>
		<p>__LOREM__ <a href="#">Duis aute irure</a> dolor in reprehenderit in voluptate velit esse cillum dolore.</p>
		<ul><li>Sed ut perspiciatis unde omnis iste natus</li><li>Nemo enim ipsam voluptatem quia voluptas</li><li>Neque porro quisquam est qui dolorem</li></ul>
		<h2 id="sed-do-eiusmod">Sed do eiusmod</h2>
		<pre><code>GET /lorem/_search
		{ "query": { "match": { "ipsum": "dolor" } } }</code></pre>
		<h3 id="excepteur-sint">Excepteur sint</h3>
		<p>__LOREM__</p>
		<table><thead><tr><th>Setting</th><th>Default</th></tr></thead><tbody><tr><td>lorem_ipsum</td><td>1</td></tr><tr><td>dolor_sit</td><td>amet</td></tr></tbody></table>
		</section>
		""".Replace(
		"__LOREM__",
		Lorem,
		StringComparison.Ordinal
	);

	private static readonly PageTocItem[] Toc =
	[
		new() { Heading = "Consectetur adipiscing", Slug = "consectetur-adipiscing", Level = 2 },
		new() { Heading = "Sed do eiusmod", Slug = "sed-do-eiusmod", Level = 2 },
		new() { Heading = "Excepteur sint", Slug = "excepteur-sint", Level = 3 }
	];

	/// <summary>Renders a preview as a complete HTML document, or <c>null</c> when the id is unknown.</summary>
	public static async Task<string?> RenderAsync(string id, EmbeddedOrPhysicalFileProvider staticFiles, Cancel ctx = default)
	{
		var hashes = new StaticFileContentHashProvider(staticFiles);
		var navigation = await _NavSide.Create(new NavSideModel { Navigation = NavSideExamples.Navigation }).RenderAsync(
			cancellationToken: ctx
		);

		var model = id switch
		{
			"isolated-article" => Page(hashes, navigation, BuildType.Isolated, null),
			"isolated-branded" => Page(hashes, navigation, BuildType.Isolated, null, branded: true),
			"assembler-article" => Page(hashes, navigation, BuildType.Assembler, null),
			"assembler-landing" => Page(hashes, navigation, BuildType.Assembler, MarkdownPageLayout.LandingPage),
			"assembler-archive" => Page(hashes, navigation, BuildType.Assembler, MarkdownPageLayout.Archive),
			"assembler-search" => Page(hashes, navigation, BuildType.Assembler, MarkdownPageLayout.FullSearch),
			"assembler-not-found" => Page(hashes, navigation, BuildType.Assembler, MarkdownPageLayout.NotFound),
			_ => null
		};
		return model is null ? null : await PreviewPage.Create(model).RenderAsync(cancellationToken: ctx);
	}

	public static IReadOnlyList<string> Ids { get; } =
	[
		"isolated-article",
		"isolated-branded",
		"assembler-article",
		"assembler-landing",
		"assembler-archive",
		"assembler-search",
		"assembler-not-found"
	];

	private static PreviewPageModel Page(
		StaticFileContentHashProvider hashes,
		string navigation,
		BuildType buildType,
		MarkdownPageLayout? layout,
		bool branded = false
	)
	{
		var assembler = buildType == BuildType.Assembler;
		var current = PreviewNavigation.Page("Lorem ipsum dolor sit amet", "/docs/lorem-ipsum/", assembler);
		var layoutModel = new MarkdownLayoutViewModel
		{
			DocSetName = assembler ? "Elastic Docs" : "Lorem Ipsum Docs",
			Title = "Lorem ipsum dolor sit amet | Style guide preview",
			Description = Lorem,
			CurrentNavigationItem = current,
			Previous = PreviewNavigation.Crumb("Sed ut perspiciatis", "/docs/previous/"),
			Next = PreviewNavigation.Crumb("Nemo enim ipsam", "/docs/next/"),
			NavigationHtml = navigation,
			UrlPathPrefix = null,
			CanonicalBaseUrl = null,
			AllowIndexing = false,
			Features = new FeatureFlags(new Dictionary<string, bool> { ["navigation-preview"] = true }),
			GoogleTagManager = new GoogleTagManagerConfiguration(),
			Optimizely = new OptimizelyConfiguration(),
			StaticFileContentHashProvider = hashes,
			BuildType = buildType,
			ShowVersionDropdown = assembler,
			AllVersionsUrl = "/docs/release-versioning/all-versions",
			CurrentVersion = "9.5.3",
			VersionDropdownSerializedModel =
				"""[{"name":"9.0","href":"/docs/9.0","disabled":false},{"name":"8.19","href":"/docs/8.19","disabled":false}]""",
			GithubEditUrl = "https://github.com/acme/lorem-ipsum/edit/main/docs/index.md",
			MarkdownUrl = "/docs/lorem-ipsum.md",
			HideEditThisPage = false,
			ReportIssueUrl = "https://github.com/acme/lorem-ipsum/issues/new",
			Breadcrumbs =
			[
				PreviewNavigation.Crumb("Docs", "/docs/"),
				PreviewNavigation.Crumb("Guides", "/docs/guides/"),
				PreviewNavigation.Crumb("Lorem ipsum dolor sit amet", "/docs/lorem-ipsum/")
			],
			PageTocItems = Toc,
			Layout = layout,
			VersioningSystem = new VersioningSystem
			{
				Id = VersioningSystemId.Stack,
				Base = new SemVersion(9, 0, 0),
				Current = new SemVersion(9, 5, 3)
			},
			Cta = Cta.Default,
			HeaderTitle = "Lorem Ipsum Docs",
			GitBranch = assembler ? null : "main",
			GitCommitShort = assembler ? null : "45954b6",
			GitRepository = assembler ? null : "acme/lorem-ipsum",
			GitHubDocsUrl = assembler ? null : "https://github.com/acme/lorem-ipsum",
			GitHubRef = assembler ? null : "refs/pull/42/merge",
			Branding = branded ? new BrandingConfiguration { Copyright = "© 2026 Lorem Ipsum Inc. All rights reserved." } : null
		};
		return new PreviewPageModel(layoutModel, layout is null ? ArticleHtml : string.Empty);
	}
}
