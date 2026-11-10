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

/// <summary>The social card is "Documentation" on assembler builds and "Codex" on Codex, labelled outside production, and always an absolute URL.</summary>
public partial class SocialImageHeadTests : DocumentationSetNavigationTestBase
{
	private static readonly Uri ProductionOrigin = new("https://www.elastic.co");

	[Test]
	public async Task OgImage_AssemblerProduction_UsesThePlainDocumentationCard()
	{
		var html = await RenderHead(new HeadOptions(BuildType.Assembler, "prod", ProductionOrigin, "/docs"));

		ImageMeta(html)
			.Should()
			.MatchInlineSnapshot(
				"""
			<meta property="og:image" content="https://www.elastic.co/docs/_static/og-default.png?v=HASH"/>
			<meta name="twitter:image" content="https://www.elastic.co/docs/_static/og-default.png?v=HASH"/>
			"""
			);
	}

	[Test]
	public async Task OgImage_AssemblerPreview_ResolvesAgainstTheServingOriginAndPathPrefix()
	{
		var options = new HeadOptions(
			BuildType.Assembler,
			"preview",
			new Uri("https://docs-v3-preview.elastic.dev"),
			"/elastic/docs-builder/docs/4794"
		)
		{ CanonicalBaseUrl = ProductionOrigin };

		var html = await RenderHead(options);

		ImageMeta(html)
			.Should()
			.MatchInlineSnapshot(
				"""
			<meta property="og:image" content="https://docs-v3-preview.elastic.dev/elastic/docs-builder/docs/4794/_static/og-preview.png?v=HASH"/>
			<meta name="twitter:image" content="https://docs-v3-preview.elastic.dev/elastic/docs-builder/docs/4794/_static/og-preview.png?v=HASH"/>
			"""
			);
	}

	[Test]
	public async Task OgImage_AssemblerWithoutServingOrigin_FallsBackToTheAbsoluteElasticImage()
	{
		var html = await RenderHead(new HeadOptions(BuildType.Assembler, "edge", null, "/docs"));

		ImageMeta(html)
			.Should()
			.MatchInlineSnapshot(
				"""
			<meta property="og:image" content="https://www.elastic.co/static-res/images/social_media_default.png"/>
			<meta name="twitter:image" content="https://www.elastic.co/static-res/images/social_media_default.png"/>
			"""
			);
	}

	[Test]
	public async Task OgImage_Isolated_KeepsTheAbsoluteElasticImage()
	{
		var html = await RenderHead(new HeadOptions(BuildType.Isolated, null, null, ""));

		ImageMeta(html)
			.Should()
			.MatchInlineSnapshot(
				"""
			<meta property="og:image" content="https://www.elastic.co/static-res/images/social_media_default.png"/>
			<meta name="twitter:image" content="https://www.elastic.co/static-res/images/social_media_default.png"/>
			"""
			);
	}

	[Test]
	public async Task OgImage_CodexWithCanonicalUrl_UsesTheCodexCard()
	{
		var options = new HeadOptions(BuildType.Codex, null, null, "/internal-docs/r/repo")
		{
			CanonicalBaseUrl = new Uri("https://codex.example.com")
		};

		var html = await RenderHead(options);

		ImageMeta(html)
			.Should()
			.MatchInlineSnapshot(
				"""
			<meta property="og:image" content="https://codex.example.com/internal-docs/_static/og-codex.png?v=HASH"/>
			<meta name="twitter:image" content="https://codex.example.com/internal-docs/_static/og-codex.png?v=HASH"/>
			"""
			);
	}

	[Test]
	[Arguments("/internal-docs")]
	[Arguments("/internal-docs/r/repo")]
	public async Task OgImage_CodexWithSitePrefix_KeepsThePrefixOnLandingAndRepositoryPages(string urlPathPrefix)
	{
		var options = new HeadOptions(BuildType.Codex, null, null, urlPathPrefix)
		{
			CanonicalBaseUrl = new Uri("https://codex.example.com")
		};

		var html = await RenderHead(options);

		ImageMeta(html)
			.Should()
			.MatchInlineSnapshot(
				"""
			<meta property="og:image" content="https://codex.example.com/internal-docs/_static/og-codex.png?v=HASH"/>
			<meta name="twitter:image" content="https://codex.example.com/internal-docs/_static/og-codex.png?v=HASH"/>
			"""
			);
	}

	[Test]
	public async Task OgImage_CodexWithoutCanonicalUrl_FallsBackToTheAbsoluteElasticImage()
	{
		var html = await RenderHead(new HeadOptions(BuildType.Codex, null, null, "/internal-docs/r/repo"));

		ImageMeta(html)
			.Should()
			.MatchInlineSnapshot(
				"""
			<meta property="og:image" content="https://www.elastic.co/static-res/images/social_media_default.png"/>
			<meta name="twitter:image" content="https://www.elastic.co/static-res/images/social_media_default.png"/>
			"""
			);
	}

	// The content hash after ?v= changes with the image bytes, which is not what these tests are about.
	private static string ImageMeta(string html) =>
		string.Join('\n', ImageMetaRegex().Matches(html).Select(m => ContentHashRegex().Replace(m.Value, "?v=HASH")));

	[GeneratedRegex(@"<meta (property=""og:image""|name=""twitter:image"")[^>]*>")]
	private static partial Regex ImageMetaRegex();

	[GeneratedRegex(@"\?v=[^""]*")]
	private static partial Regex ContentHashRegex();

	private async Task<string> RenderHead(HeadOptions options)
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
			UrlPathPrefix = options.UrlPathPrefix,
			CanonicalBaseUrl = options.CanonicalBaseUrl,
			PublicBaseUrl = options.PublicBaseUrl,
			EnvironmentName = options.EnvironmentName,
			AllowIndexing = false,
			Features = new FeatureFlags([]),
			GoogleTagManager = new GoogleTagManagerConfiguration(),
			Optimizely = new OptimizelyConfiguration(),
			StaticFileContentHashProvider = new StaticFileContentHashProvider(new EmbeddedOrPhysicalFileProvider(context)),
			BuildType = options.BuildType
		};

		return await _Head.Create(model).RenderAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);
	}

	private sealed record HeadOptions(BuildType BuildType, string? EnvironmentName, Uri? PublicBaseUrl, string UrlPathPrefix)
	{
		public Uri? CanonicalBaseUrl { get; init; }
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
