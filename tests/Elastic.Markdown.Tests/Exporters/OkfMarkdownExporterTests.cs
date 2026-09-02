// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Navigation;
using Elastic.Markdown;
using Elastic.Markdown.Exporters;
using YamlDotNet.Serialization;

namespace Elastic.Markdown.Tests.Exporters;

public class OkfMarkdownExporterTests
{
	[Fact]
	public void ComputeBundlePath_RootUrl_ReturnsOverviewMd()
	{
		var bundlePath = OkfMarkdownExporter.ComputeBundlePath("/", urlPathPrefix: "");

		bundlePath.Should().Be("overview.md");
	}

	[Fact]
	public void ComputeBundlePath_FolderLandingUrl_ReturnsSiblingFolderMd()
	{
		var bundlePath = OkfMarkdownExporter.ComputeBundlePath("/reference/foo", urlPathPrefix: "");

		bundlePath.Should().Be("reference/foo.md");
	}

	[Fact]
	public void ComputeBundlePath_LeafPageUrl_ReturnsPathWithMdExtension()
	{
		var bundlePath = OkfMarkdownExporter.ComputeBundlePath("/reference/foo/bar", urlPathPrefix: "");

		bundlePath.Should().Be("reference/foo/bar.md");
	}

	[Fact]
	public void ComputeBundlePath_UrlPathPrefixConfigured_IsStripped()
	{
		var bundlePath = OkfMarkdownExporter.ComputeBundlePath("/docs/reference/foo", urlPathPrefix: "/docs");

		bundlePath.Should().Be("reference/foo.md");
	}

	[Fact]
	public void DeriveType_UrlWithPrefixAndSection_ReturnsFirstSegmentAfterPrefix()
	{
		var type = OkfMarkdownExporter.DeriveType("/docs/reference/query-languages/eql", "/docs");

		type.Should().Be("reference");
	}

	[Fact]
	public void DeriveType_NoPrefixConfigured_ReturnsFirstSegment()
	{
		var type = OkfMarkdownExporter.DeriveType("/solutions/search", urlPathPrefix: "");

		type.Should().Be("solutions");
	}

	[Fact]
	public void DeriveType_RootUrl_ReturnsDocumentationFallback()
	{
		var type = OkfMarkdownExporter.DeriveType("/", urlPathPrefix: "");

		type.Should().Be("documentation");
	}

	[Fact]
	public void RewriteLinkUrl_InternalLinkWithAnchor_ReturnsBundleRelativePathWithAnchor()
	{
		var rewritten = OkfMarkdownExporter.RewriteLinkUrl("/reference/foo/bar#section", urlPathPrefix: "", canonicalBaseUrl: null);

		rewritten.Should().Be("/reference/foo/bar.md#section");
	}

	[Fact]
	public void RewriteLinkUrl_ExternalAbsoluteUrl_ReturnsUnchanged()
	{
		var rewritten = OkfMarkdownExporter.RewriteLinkUrl(
			"https://example.com/page",
			urlPathPrefix: "",
			canonicalBaseUrl: new Uri("https://www.elastic.co")
		);

		rewritten.Should().Be("https://example.com/page");
	}

	[Fact]
	public void RewriteLinkUrl_UrlPathPrefixConfigured_IsStripped()
	{
		var rewritten = OkfMarkdownExporter.RewriteLinkUrl("/docs/reference/foo", urlPathPrefix: "/docs", canonicalBaseUrl: null);

		rewritten.Should().Be("/reference/foo.md");
	}

	[Fact]
	public void RewriteLinkUrl_NullOrEmpty_ReturnsInputUnchanged()
	{
		OkfMarkdownExporter.RewriteLinkUrl(null, urlPathPrefix: "", canonicalBaseUrl: null).Should().BeNull();
		OkfMarkdownExporter.RewriteLinkUrl(string.Empty, urlPathPrefix: "", canonicalBaseUrl: null).Should().Be(string.Empty);
	}

	[Fact]
	public void RewriteLinkUrl_RootLink_ReturnsOverviewMd()
	{
		var rewritten = OkfMarkdownExporter.RewriteLinkUrl("/", urlPathPrefix: "", canonicalBaseUrl: null);

		rewritten.Should().Be("/overview.md");
	}

	[Fact]
	public void RewriteLinkUrl_SelfReferencingAbsoluteUrlMatchingCanonicalBase_UnwrapsToBundleRelativePath()
	{
		// The assembler always sets CanonicalBaseUrl to the production URL, which can leak into rendered
		// link text for otherwise-internal links (e.g. via cross-link resolution or image URL absolutization
		// paths that bypass the rewriter). These must still resolve to bundle-relative paths, not pass through.
		var rewritten = OkfMarkdownExporter.RewriteLinkUrl(
			"https://www.elastic.co/docs/deploy-manage/deploy#about-orchestration",
			urlPathPrefix: "/docs",
			canonicalBaseUrl: new Uri("https://www.elastic.co")
		);

		rewritten.Should().Be("/deploy-manage/deploy.md#about-orchestration");
	}

	[Fact]
	public void RewriteLinkUrl_ApiReferencePath_ReturnsLiveSiteUrlUnchanged()
	{
		// /api/* pages are genuine third-party (OpenAPI-generated) endpoints with no backing markdown file
		// in this export — see the TODO(api-explorer) in RewriteLinkUrl.
		var rewritten = OkfMarkdownExporter.RewriteLinkUrl(
			"https://www.elastic.co/docs/api/some-endpoint",
			urlPathPrefix: "/docs",
			canonicalBaseUrl: new Uri("https://www.elastic.co")
		);

		rewritten.Should().Be("https://www.elastic.co/docs/api/some-endpoint");
	}

	[Fact]
	public void RewriteLinkUrl_RelativeApiReferencePath_ReturnsAbsoluteLiveSiteUrl()
	{
		var rewritten = OkfMarkdownExporter.RewriteLinkUrl(
			"/docs/api/some-endpoint#section",
			urlPathPrefix: "/docs",
			canonicalBaseUrl: new Uri("https://www.elastic.co")
		);

		rewritten.Should().Be("https://www.elastic.co/docs/api/some-endpoint#section");
	}

	[Fact]
	public void IsApiReferencePath_ApiSegmentAfterPrefix_ReturnsTrue()
	{
		OkfMarkdownExporter.IsApiReferencePath("/docs/api/some-endpoint", urlPathPrefix: "/docs").Should().BeTrue();
		OkfMarkdownExporter.IsApiReferencePath("/docs/api", urlPathPrefix: "/docs").Should().BeTrue();
	}

	[Fact]
	public void IsApiReferencePath_NonApiSegment_ReturnsFalse()
	{
		OkfMarkdownExporter.IsApiReferencePath("/docs/reference/foo", urlPathPrefix: "/docs").Should().BeFalse();
		// "apic" shouldn't fuzzy-match "api"
		OkfMarkdownExporter.IsApiReferencePath("/docs/apiconfig", urlPathPrefix: "/docs").Should().BeFalse();
	}

	[Fact]
	public void RewriteLinkUrl_AbsoluteUrlWithDifferentHost_ReturnsUnchangedEvenWithCanonicalBaseSet()
	{
		var rewritten = OkfMarkdownExporter.RewriteLinkUrl(
			"https://github.com/elastic/docs-builder",
			urlPathPrefix: "/docs",
			canonicalBaseUrl: new Uri("https://www.elastic.co")
		);

		rewritten.Should().Be("https://github.com/elastic/docs-builder");
	}

	[Fact]
	public void IsUtilityPage_NotFoundArchiveOrFullSearch_ReturnsTrue()
	{
		OkfMarkdownExporter.IsUtilityPage(MarkdownPageLayout.NotFound).Should().BeTrue();
		OkfMarkdownExporter.IsUtilityPage(MarkdownPageLayout.Archive).Should().BeTrue();
		OkfMarkdownExporter.IsUtilityPage(MarkdownPageLayout.FullSearch).Should().BeTrue();
	}

	[Fact]
	public void IsUtilityPage_LandingPageOrNull_ReturnsFalse()
	{
		OkfMarkdownExporter.IsUtilityPage(MarkdownPageLayout.LandingPage).Should().BeFalse();
		OkfMarkdownExporter.IsUtilityPage(null).Should().BeFalse();
	}

	[Fact]
	public void GetDirectory_NestedPath_ReturnsParentDirectory() =>
		OkfMarkdownExporter.GetDirectory("reference/foo/bar.md").Should().Be("reference/foo");

	[Fact]
	public void GetDirectory_TopLevelFile_ReturnsEmptyString() => OkfMarkdownExporter.GetDirectory("overview.md").Should().Be(string.Empty);

	[Fact]
	public void RenderIndexContent_RootDirectory_DeclaresOkfVersionAndNoOtherFrontmatter()
	{
		var content = OkfMarkdownExporter.RenderIndexContent(directory: "", concepts: [], subdirectories: []);

		content.Should().StartWith("---\nokf_version: \"0.1\"\n---");
	}

	[Fact]
	public void RenderIndexContent_NonRootDirectory_HasNoFrontmatter()
	{
		var content = OkfMarkdownExporter.RenderIndexContent(directory: "reference", concepts: [], subdirectories: []);

		content.Should().NotContain("---");
		content.Should().NotContain("okf_version");
	}

	[Fact]
	public void RenderIndexContent_WithConceptsAndSubdirectories_GroupsThemUnderSeparateHeadings()
	{
		// "reference/foo.md" is the sibling landing page for the "reference/foo" subdirectory.
		var concepts = new List<OkfMarkdownExporter.ConceptEntry>
		{
			new("reference/bar.md", "Bar", "Bar description"),
			new("reference/foo.md", "Foo", "Foo description"),
		};

		var content = OkfMarkdownExporter.RenderIndexContent(directory: "reference", concepts: concepts, subdirectories: ["reference/foo"]);

		content.Should().Contain("# Documents");
		content.Should().Contain("* [Bar](bar.md) - Bar description");
		content.Should().Contain("* [Foo](foo.md) - Foo description");
		content.Should().Contain("# Subdirectories");
		content.Should().Contain("* [foo](foo/) - Foo description");
	}

	[Fact]
	public void RenderIndexContent_SubdirectoryWithoutSiblingLandingPage_OmitsDescriptionSuffix()
	{
		var content = OkfMarkdownExporter.RenderIndexContent(directory: "reference", concepts: [], subdirectories: ["reference/foo"]);

		content.Should().Contain("* [foo](foo/)");
		content.Should().NotContain("* [foo](foo/) -");
	}

	[Theory]
	// The reproduction from https://github.com/elastic/docs-builder/issues/3999 — a `": "` in prose.
	[InlineData("What each entry point exports. Types: PrimitiveDefinition, PrimitiveNode.")]
	[InlineData("Last updated: May 3, 2026")]
	[InlineData("A description with \"double quotes\" in it")]
	[InlineData(@"A Windows path C:\Users\foo and a trailing backslash \")]
	[InlineData("A description\nspanning two lines")]
	[InlineData("#leading indicator characters *&!|>%@`")]
	[InlineData("true")]
	[InlineData("{not: a, flow: mapping}")]
	[InlineData("")]
	public void RenderFrontMatter_ArbitraryProseDescription_RoundTripsVerbatim(string description)
	{
		var rendered = OkfMarkdownExporter.RenderFrontMatter(FrontMatter(description: description));

		ParseFrontMatter(rendered)["description"].Should().Be(description);
	}

	[Fact]
	public void RenderFrontMatter_AppliesToTags_RoundTripAsStringsNotMappings()
	{
		// GetAppliesToItems formats every tag as "{displayName}: {availability}" — unquoted that is valid
		// YAML, which makes this the quieter half of the bug: the sequence entry parses as a mapping.
		string[] tags = ["Elastic Stack: Available", "Serverless: Planned, GA"];

		var rendered = OkfMarkdownExporter.RenderFrontMatter(FrontMatter(tags: tags));

		ParseFrontMatter(rendered)["tags"].Should().BeEquivalentTo(tags);
	}

	[Fact]
	public void RenderFrontMatter_TitleContainingColon_RoundTripsVerbatim()
	{
		var rendered = OkfMarkdownExporter.RenderFrontMatter(FrontMatter(title: "Kibana: getting started"));

		var parsed = ParseFrontMatter(rendered);
		parsed["title"].Should().Be("Kibana: getting started");
		parsed["resource"].Should().Be("https://www.elastic.co/docs/reference/foo");
	}

	[Fact]
	public void RenderFrontMatter_NavigationTitleEmpty_KeyIsOmitted()
	{
		var rendered = OkfMarkdownExporter.RenderFrontMatter(FrontMatter());

		ParseFrontMatter(rendered).Should().NotContainKey("navigation_title");
		ParseFrontMatter(OkfMarkdownExporter.RenderFrontMatter(FrontMatter(navigationTitle: "Foo: short")))["navigation_title"]
			.Should()
			.Be("Foo: short");
	}

	[Theory]
	[InlineData("First line.\nSecond line: with a colon.", "First line. Second line: with a colon.")]
	// A `description: |` block with a blank line would otherwise terminate the index list it is rendered into.
	[InlineData("Para one.\n\nPara two.", "Para one. Para two.")]
	// DescriptionGenerator pads each block it appends with a trailing space.
	[InlineData("A generated description. ", "A generated description.")]
	[InlineData("  padded\tand\r\nragged  ", "padded and ragged")]
	[InlineData("", "")]
	public void NormalizeDescription_MultiLineOrPaddedProse_CollapsesToASingleLine(string description, string expected) =>
		OkfMarkdownExporter.NormalizeDescription(description).Should().Be(expected);

	private static OkfMarkdownExporter.ConceptFrontMatter FrontMatter(
		string title = "Foo",
		string? navigationTitle = null,
		string description = "A description",
		IReadOnlyCollection<string>? tags = null
	) => new("reference", title, navigationTitle, description, "https://www.elastic.co/docs/reference/foo", tags ?? []);

	/// <summary>Parses the emitted block as YAML, the only assertion that proves arbitrary prose survives escaping.</summary>
	private static Dictionary<string, object> ParseFrontMatter(string rendered)
	{
		var lines = rendered.Split('\n');
		lines.First().Should().Be("---");
		var body = string.Join('\n', lines.Skip(1).TakeWhile(l => l != "---"));
		return new DeserializerBuilder().Build().Deserialize<Dictionary<string, object>>(body);
	}
}
