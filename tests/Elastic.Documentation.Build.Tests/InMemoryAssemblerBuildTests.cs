// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.RegularExpressions;
using AwesomeAssertions;
using Elastic.Documentation.Testing;
using Nullean.Snapshots;
using Nullean.Snapshots.Html;

namespace Elastic.Documentation.Build.Tests;

/// <summary>
/// An assembled site of four fake repositories, built in memory through the real assembler pieces and snapshotted: the
/// output tree, the non-HTML outputs and the content section of a handful of pages. Locks today's output ahead of the
/// domain model refactor. The sitemap stamps every URL with the build time, which the sitemap snapshot scrubs.
/// </summary>
public partial class InMemoryAssemblerBuildTests
{
	private static readonly Lazy<Task<InMemoryAssemblerSiteResult>> Site = new(
		() => InMemoryAssemblerSite.BuildAsync(CancellationToken.None)
	);

	private static Task<InMemoryAssemblerSiteResult> Built => Site.Value;

	[Test]
	public async Task Output_Tree_MatchesSnapshot()
	{
		var site = await Built;

		// the hashed static assets change with every frontend build, so they are left out
		var files = string.Join('\n', site.Files.Where(f => !f.StartsWith("docs/_static/", StringComparison.Ordinal)));
		files.Should().MatchInlineSnapshot(
			"""
			config/assembler.yml
			config/legacy-url-mappings.yml
			config/navigation.yml
			config/products.yml
			config/versions.yml
			docs/get-started.md
			docs/get-started/concepts.md
			docs/get-started/concepts/index.html
			docs/get-started/index.html
			docs/get-started/install.md
			docs/get-started/install/index.html
			docs/index.html
			docs/internal.md
			docs/internal/index.html
			docs/links.json
			docs/llm.zip
			docs/llms.txt
			docs/reference/apm/agents/java.md
			docs/reference/apm/agents/java/index.html
			docs/reference/apm/agents/java/setup.md
			docs/reference/apm/agents/java/setup/index.html
			docs/reference/elasticsearch.md
			docs/reference/elasticsearch/index.html
			docs/reference/elasticsearch/query-dsl.md
			docs/reference/elasticsearch/query-dsl/index.html
			docs/reference/elasticsearch/query-dsl/term-queries.md
			docs/reference/elasticsearch/query-dsl/term-queries/index.html
			docs/reference/elasticsearch/search-apis.md
			docs/reference/elasticsearch/search-apis/index.html
			docs/reference/kibana.md
			docs/reference/kibana/dashboards.md
			docs/reference/kibana/dashboards/index.html
			docs/reference/kibana/experimental.md
			docs/reference/kibana/experimental/index.html
			docs/reference/kibana/index.html
			docs/sitemap-docs.xml
			docs/sitemap.xml
			redirects.json
			"""
		);
	}

	[Test]
	public async Task Diagnostics_OfAGoodBuild_AreEmpty() => (await Built).Diagnostics.ToSnapshotText().Should().MatchInlineSnapshot("");

	[Test]
	public async Task LinksJson_IsTheLastRepositoryOfTheBuild() => (await Built).Read("links.json").Should().MatchSnapshot("json");

	[Test]
	public async Task Sitemap_WithTheBuildTimeScrubbed_MatchesSnapshot() =>
		LastModified().Replace((await Built).Read("sitemap-docs.xml"), "<lastmod>{build time}</lastmod>").Should().MatchSnapshot("xml");

	[Test]
	public async Task SitemapIndex_ListsTheNarrativeSitemapOnly_WhenTheBuildRendersNoApis() =>
		(await Built)
			.Read("sitemap.xml")
			.Should()
			.MatchInlineSnapshot(
				"""
			<?xml version="1.0" encoding="utf-8" standalone="yes"?>
			<sitemapindex xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">
			  <sitemap>
			    <loc>https://www.elastic.co/docs/sitemap-docs.xml</loc>
			  </sitemap>
			</sitemapindex>
			"""
			);

	[Test]
	public async Task Redirects_MatchSnapshot() =>
		(await Built)
			.FileSystem
			.File
			.ReadAllText("/output/redirects.json")
			.Replace("\r\n", "\n")
			.Should()
			.MatchInlineSnapshot(
				"""
			{
			  "/docs/old-start": "/docs/get-started",
			  "/docs/old-search": "/docs/reference/elasticsearch/search-apis",
			  "/docs/reference/elasticsearch/legacy-search": "/docs/reference/elasticsearch/search-apis"
			}
			"""
			);

	[Test]
	public async Task LlmsTxt_NavigationSections_MatchSnapshot()
	{
		var llms = (await Built).Read("llms.txt");

		llms.Should().StartWith("# Elastic Documentation");
		// everything before the appended navigation sections is boilerplate the build embeds
		llms[llms.IndexOf("\n## Get started", StringComparison.Ordinal)..].Should().MatchSnapshot();
	}

	[Test]
	public async Task Navigation_Tree_MatchesSnapshot() => NavigationDump.ToText((await Built).Navigation).Should().MatchSnapshot();

	[Test]
	public async Task MarkdownExport_OfAPage_MatchesSnapshot() =>
		(await Built)
			.Read("get-started.md")
			.Should()
			.MatchInlineSnapshot(
				"""
		---
		title: Get started
		description: Read Install and Concepts. 
		url: https://www.elastic.co/docs/get-started
		---

		# Get started
		Read [Install](https://www.elastic.co/docs/get-started/install) and [Concepts](https://www.elastic.co/docs/get-started/concepts).
		"""
			);

	[Test]
	public async Task Page_Home_LinksAcrossRepositories() => Section((await Built).Read("index.html")).Should().MatchHtmlSnapshot(Options);

	[Test]
	public async Task Page_AppliesToFrontmatter_AndCrossLinks() =>
		Section((await Built).Read("get-started/install/index.html")).Should().MatchHtmlSnapshot(Options);

	[Test]
	public async Task Page_AdmonitionTableAndSectionAppliesTo() =>
		Section((await Built).Read("get-started/concepts/index.html")).Should().MatchHtmlSnapshot(Options);

	[Test]
	public async Task Page_ReferenceRepositoryMountedAtAPathPrefix() =>
		Section((await Built).Read("reference/elasticsearch/search-apis/index.html")).Should().MatchHtmlSnapshot(Options);

	[Test]
	public async Task Page_HiddenFromTheNavigation_StillRenders() =>
		Section((await Built).Read("reference/kibana/experimental/index.html")).Should().MatchHtmlSnapshot(Options);

	private static HtmlSnapshotOptions Options => DocsHtml.Body().Select("section#elastic-docs-v3");

	private static string Section(string html) => html;

	[GeneratedRegex("<lastmod>[^<]*</lastmod>", RegexOptions.None, RegexTimeouts.MatchTimeoutMilliseconds)]
	private static partial Regex LastModified();
}
