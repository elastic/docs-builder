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
using Elastic.Documentation.Site.Navigation;
using Elastic.Documentation.Site.StyleGuide;
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

	private const string HubHtml =
		"""
		<section class="hub-hero"><div class="hub-hero-inner">
			<div class="hub-hero-eyebrow"><a class="hub-hero-eyebrow-link" href="#"><span>Browse all products</span><svg class="hub-arrow" viewBox="0 0 24 24" fill="none" stroke-width="1.5" stroke="currentColor" aria-hidden="true"><path stroke-linecap="round" stroke-linejoin="round" d="M17.25 8.25 21 12m0 0-3.75 3.75M21 12H3"/></svg></a></div>
			<div class="hub-hero-top"><span class="hub-hero-icon" aria-hidden="true">LI</span><h1>Lorem ipsum</h1></div>
			<p class="hub-hero-desc">Dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.</p>
			<div class="hub-hero-actions doc-button-group">
				<span class="doc-button-item doc-button-neutral"><a class="hub-hero-action" href="#">Get started</a></span>
				<span class="doc-button-item doc-button-neutral"><a class="hub-hero-action" href="#">Syntax guide</a></span>
				<span class="doc-button-item doc-button-neutral"><a class="hub-hero-action" href="#">GitHub</a></span>
			</div>
		</div></section>
		<section class="get-started" id="get-started">
			<header class="get-started__head"><h2 class="get-started__heading">Get started</h2><p class="get-started__intro">Consectetur adipiscing elit sed do eiusmod tempor.</p></header>
			<ol class="get-started__steps">
				<li class="get-started__step">
					<div class="get-started__lead"><span class="get-started__num" aria-hidden="true">1</span><div class="get-started__text"><p class="get-started__eyebrow">Step 01</p><h3 class="get-started__title">Lorem ipsum dolor</h3><p class="get-started__desc">Ut enim ad minim veniam, quis nostrud.</p></div></div>
					<div class="get-started__options">
						<div class="get-started__option get-started__option--lead"><h4 class="get-started__option-title">Dolor sit</h4><p class="get-started__option-desc">Amet consectetur adipiscing elit.</p><a class="btn btn--primary btn--sm" href="#">Start</a></div>
						<span class="get-started__or" aria-hidden="true">Or</span>
						<div class="get-started__option"><h4 class="get-started__option-title">Tempor</h4><p class="get-started__option-desc">Incididunt ut labore.</p><div class="get-started__code"><span class="get-started__prompt" aria-hidden="true">$</span><pre><code class="language-shell">lorem ipsum --dolor</code></pre></div></div>
					</div>
				</li>
				<li class="get-started__step">
					<div class="get-started__lead"><span class="get-started__num" aria-hidden="true">2</span><div class="get-started__text"><p class="get-started__eyebrow">Step 02</p><h3 class="get-started__title">Sed do eiusmod</h3><p class="get-started__desc">Duis aute irure dolor in reprehenderit.</p></div></div>
					<div class="get-started__action"><a class="btn btn--secondary btn--sm" href="#">Read more</a></div>
				</li>
			</ol>
		</section>
		<section class="hub-whats-new" id="whats-new">
			<header class="hub-wn-header"><div class="hub-wn-heading"><h2 class="hub-wn-title">What's new</h2><p class="hub-wn-intro">Excepteur sint occaecat cupidatat non proident.</p></div></header>
			<ul class="hub-wn-grid">
				<li class="hub-wn-card hub-wn-card-featured"><a class="hub-wn-card-link" href="#"><div class="hub-wn-card-meta"><span class="hub-wn-card-meta-left"><span class="hub-wn-card-date">Aug 2026</span></span><span class="hub-wn-card-tag">Release</span></div><h3 class="hub-wn-card-title">Lorem ipsum directive</h3><p class="hub-wn-card-desc">Numbered steps with connectors.</p><span class="hub-wn-card-more"><span>Read more</span></span></a></li>
				<li class="hub-wn-card"><a class="hub-wn-card-link" href="#"><div class="hub-wn-card-meta"><span class="hub-wn-card-meta-left"><span class="hub-wn-card-date">Jul 2026</span></span></div><h3 class="hub-wn-card-title">Dolor sit amet</h3><p class="hub-wn-card-desc">Consectetur adipiscing elit.</p><span class="hub-wn-card-more"><span>Read more</span></span></a></li>
			</ul>
		</section>
		<div class="hub-zone"><h2 class="hub-zone-title">Explore</h2><p class="hub-zone-intro">Sunt in culpa qui officia deserunt mollit.</p></div>
		<div class="hub-explore">
			<details class="hub-accordion" open><summary class="hub-accordion-summary"><h3 class="hub-accordion-title">Guides</h3><svg class="hub-accordion-icon" width="16" height="16" viewBox="0 0 16 16" fill="none" aria-hidden="true"><path d="M3 8h10" stroke="currentColor" stroke-width="1.6" stroke-linecap="round"/><path class="hub-accordion-icon-v" d="M8 3v10" stroke="currentColor" stroke-width="1.6" stroke-linecap="round"/></svg></summary>
				<div class="hub-accordion-body"><ul class="hub-explore-cols">
					<li class="hub-col"><h4 class="hub-col-title"><a href="#">Lorem</a></h4><ul class="hub-col-links"><li><a href="#">Ipsum</a></li><li><a href="#">Dolor</a></li><li><a href="#">Sit amet</a></li></ul></li>
					<li class="hub-col"><h4 class="hub-col-title"><a href="#">Consectetur</a></h4><ul class="hub-col-links"><li><a href="#">Adipiscing</a></li><li><a href="#">Elit sed</a></li></ul></li>
				</ul></div></details>
			<details class="hub-accordion"><summary class="hub-accordion-summary"><h3 class="hub-accordion-title">Reference</h3><svg class="hub-accordion-icon" width="16" height="16" viewBox="0 0 16 16" fill="none" aria-hidden="true"><path d="M3 8h10" stroke="currentColor" stroke-width="1.6" stroke-linecap="round"/><path class="hub-accordion-icon-v" d="M8 3v10" stroke="currentColor" stroke-width="1.6" stroke-linecap="round"/></svg></summary>
				<div class="hub-accordion-body"><ul class="hub-explore-cols"><li class="hub-col"><h4 class="hub-col-title"><a href="#">Tempor</a></h4></li></ul></div></details>
		</div>
		""";

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
		var navigation = await NavHtmlAsync(NavSideExamples.Navigation, ctx);
		var topLevel = await NavHtmlAsync(NavSideExamples.TopLevel, ctx);
		var body = id == "isolated-directives" ? await DirectivesBodyAsync(ctx) : ArticleHtml;
		var model = id switch
		{
			"isolated-article" => Page(hashes, topLevel, BuildType.Isolated, null),
			"isolated-directives" => Page(hashes, topLevel, BuildType.Isolated, null, bodyHtml: body),
			"isolated-branded" => Page(hashes, topLevel, BuildType.Isolated, null, branded: true),
			"assembler-article" => Page(hashes, topLevel, BuildType.Assembler, null),
			"assembler-article-island" => Page(hashes, navigation, BuildType.Assembler, null),
			"assembler-product" => Page(hashes, navigation, BuildType.Assembler, MarkdownPageLayout.Hub, bodyHtml: HubHtml),
			"assembler-landing" => Page(hashes, navigation, BuildType.Assembler, MarkdownPageLayout.LandingPage),
			"assembler-archive" => Page(hashes, navigation, BuildType.Assembler, MarkdownPageLayout.Archive),
			"assembler-not-found" => Page(hashes, navigation, BuildType.Assembler, MarkdownPageLayout.NotFound),
			_ => null
		};
		return model is null ? null : await PreviewPage.Create(model).RenderAsync(cancellationToken: ctx);
	}

	public static IReadOnlyList<string> Ids { get; } =
	[
		"isolated-article",
		"isolated-directives",
		"isolated-branded",
		"assembler-article",
		"assembler-article-island",
		"assembler-product",
		"assembler-landing",
		"assembler-archive",
		"assembler-not-found"
	];

	/// <summary>
	/// A documentation page of a Codex site, for the Codex previews: the Codex layout around the same placeholder article.
	/// A page in a group has the group in its trail; one outside a group goes straight from the home page to its set.
	/// </summary>
	public static async Task<PreviewPageModel> CodexDocsPageAsync(
		EmbeddedOrPhysicalFileProvider staticFiles,
		bool inGroup,
		Cancel ctx = default
	)
	{
		var navigation = await NavHtmlAsync(NavSideExamples.TopLevel, ctx);
		var page = Page(new StaticFileContentHashProvider(staticFiles), navigation, BuildType.Codex, null);
		var trail = inGroup
			? new[] { new CodexBreadcrumb("Home", "/"), new CodexBreadcrumb("Search", "#"), new CodexBreadcrumb("Lorem Ipsum", "#") }
			: [new CodexBreadcrumb("Home", "/"), new CodexBreadcrumb("Lorem Ipsum", "#")];
		return page with
		{
			Layout = page.Layout with
			{
				DocSetName = "Lorem Ipsum",
				CodexBreadcrumbs = trail,
				GitBranch = null,
				GitCommitShort = null,
				GitRepository = null,
				GitHubDocsUrl = null,
				GitHubRef = null
			}
		};
	}

	private static async Task<string> NavHtmlAsync(NavigationRenderModel navigation, Cancel ctx) =>
		await _NavSide.Create(new NavSideModel { Navigation = navigation }).RenderAsync(cancellationToken: ctx);

	/// <summary>
	/// A page that uses every directive and role: the design-system components that render them, one after another.
	/// They are the same components the Markdown renderer writes, so the page cannot drift from them.
	/// </summary>
	private static async Task<string> DirectivesBodyAsync(Cancel ctx)
	{
		var html = new System.Text.StringBuilder("<section id=\"directives\"><h1>Every directive and role</h1><p>")
			.Append(Lorem)
			.Append("</p>");
		foreach (var page in new[] { "content", "badges" })
		{
			foreach (var section in StyleGuideCatalog.ComponentPages[page])
			{
				_ = html
					.Append("<h2 id=\"")
					.Append(section.ClassName)
					.Append("\">")
					.Append(System.Net.WebUtility.HtmlEncode(section.Title))
					.Append("</h2><p>")
					.Append(System.Net.WebUtility.HtmlEncode(section.Intro))
					.Append("</p>");
				foreach (var example in section.Examples)
					_ = html.Append(await example.RenderAsync(ctx));
			}
		}
		return html.Append("</section>").ToString();
	}

	private static PreviewPageModel Page(
		StaticFileContentHashProvider hashes,
		string navigation,
		BuildType buildType,
		MarkdownPageLayout? layout,
		bool branded = false,
		string? bodyHtml = null
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
		return new PreviewPageModel(layoutModel, layout is null or MarkdownPageLayout.Hub ? bodyHtml ?? ArticleHtml : string.Empty);
	}
}
