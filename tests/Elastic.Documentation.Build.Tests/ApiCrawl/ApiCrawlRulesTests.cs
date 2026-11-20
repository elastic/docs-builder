// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.Documentation.Assembler.ApiCrawl;
using Nullean.Snapshots;

namespace Elastic.Documentation.Build.Tests.ApiCrawl;

public class ApiCrawlRulesTests
{
	private static readonly string[] ClassifiedPaths =
	[
		"/docs/api",
		"/docs/api/elasticsearch/operation/operation-search",
		"/docs/api/elasticsearch/v9/operation/operation-search",
		"/docs/api/elasticsearch/v8",
		"/docs/api/elasticsearch/next/operation/operation-new",
		"/docs/api/elasticsearch/servers",
		"/docs/api/cloud",
		"/docs/api/unknown",
		"/docs/reference",
	];

	private static ApiUrlModel Model => ApiCrawlFixture.Model;

	private static ExpectedApiPage Page(string path) => Model.Pages.Single(p => p.Path == path);

	private static ObservedPage Served(ExpectedApiPage page, string robots = "noindex, nofollow") =>
		new(page.Path, 200, [], page.CanonicalUrl, robots);

	private static string Text(IEnumerable<CrawlDifference> differences) => string.Join('\n', differences);

	[Test]
	public void Create_FixtureLinks_ListsEveryPageWithItsTypeAndCanonical() =>
		string
			.Join('\n', Model.Pages.Select(p => $"{p.Path} {p.Type} {p.CanonicalUrl}"))
			.Should()
			.MatchInlineSnapshot(
				"""
			/docs/api Versionless https://www.elastic.co/docs/api
			/docs/api/cloud Versionless https://www.elastic.co/docs/api/cloud
			/docs/api/cloud/operation/operation-list Versionless https://www.elastic.co/docs/api/cloud/operation/operation-list
			/docs/api/elasticsearch/v8 OlderMajor https://www.elastic.co/docs/api/elasticsearch/v8
			/docs/api/elasticsearch/v8/operation/operation-search OlderMajor https://www.elastic.co/docs/api/elasticsearch/v8/operation/operation-search
			/docs/api/elasticsearch Versionless https://www.elastic.co/docs/api/elasticsearch
			/docs/api/elasticsearch/operation/operation-search Versionless https://www.elastic.co/docs/api/elasticsearch/operation/operation-search
			/docs/api/elasticsearch/v9 NewestMajorCopy https://www.elastic.co/docs/api/elasticsearch
			/docs/api/elasticsearch/v9/operation/operation-search NewestMajorCopy https://www.elastic.co/docs/api/elasticsearch/operation/operation-search
			/docs/api/elasticsearch/next Unreleased https://www.elastic.co/docs/api/elasticsearch/next
			/docs/api/elasticsearch/next/operation/operation-search Unreleased https://www.elastic.co/docs/api/elasticsearch/next/operation/operation-search
			/docs/api/elasticsearch/next/operation/operation-new Unreleased https://www.elastic.co/docs/api/elasticsearch/next/operation/operation-new
			"""
			);

	[Test]
	public void TryClassify_Paths_MatchTheLongestRoot() =>
		string
			.Join(
				'\n',
				ClassifiedPaths.Select(
					path => Model.TryClassify(path, out var tree) ? $"{path} {tree.Type} {tree.Root}" : $"{path} unknown"
				)
			)
			.Should()
			.MatchInlineSnapshot(
				"""
				/docs/api Versionless /docs/api
				/docs/api/elasticsearch/operation/operation-search Versionless /docs/api/elasticsearch
				/docs/api/elasticsearch/v9/operation/operation-search NewestMajorCopy /docs/api/elasticsearch/v9
				/docs/api/elasticsearch/v8 OlderMajor /docs/api/elasticsearch/v8
				/docs/api/elasticsearch/next/operation/operation-new Unreleased /docs/api/elasticsearch/next
				/docs/api/elasticsearch/servers Versionless /docs/api/elasticsearch
				/docs/api/cloud Versionless /docs/api/cloud
				/docs/api/unknown unknown
				/docs/reference unknown
				"""
			);

	[Test]
	public void Compare_EveryPageServedAsTheTableSays_HasNoDifferences()
	{
		var differences = Model.Pages.SelectMany(
			page => ApiCrawlRules.Compare(
				page,
				Served(page, ApiPageTypes.Robots(page.Type, buildAllowsIndexing: true)),
				inSitemap: ApiPageTypes.Indexing(page.Type).InSitemap,
				buildAllowsIndexing: true
			)
		);

		differences.Should().BeEmpty();
	}

	[Test]
	public void Compare_UnreleasedPageInSitemap_ReportsSitemap() =>
		Text(
			ApiCrawlRules.Compare(
				Page("/docs/api/elasticsearch/next"),
				Served(Page("/docs/api/elasticsearch/next")),
				inSitemap: true,
				buildAllowsIndexing: false
			)
		).Should().MatchInlineSnapshot("/docs/api/elasticsearch/next [Unreleased] Sitemap: expected not listed, got listed");

	[Test]
	public void Compare_NewestMajorCopyWithSelfCanonical_ReportsCanonical()
	{
		var page = Page("/docs/api/elasticsearch/v9/operation/operation-search");
		var observed = Served(page) with { Canonical = "https://www.elastic.co" + page.Path };

		Text(ApiCrawlRules.Compare(page, observed, inSitemap: false, buildAllowsIndexing: false))
			.Should()
			.MatchInlineSnapshot(
				"/docs/api/elasticsearch/v9/operation/operation-search [NewestMajorCopy] Canonical: expected https://www.elastic.co/docs/api/elasticsearch/operation/operation-search, got https://www.elastic.co/docs/api/elasticsearch/v9/operation/operation-search"
			);
	}

	[Test]
	public void Compare_RobotsMismatchOnIndexingBuild_ReportsRobots()
	{
		var page = Page("/docs/api/elasticsearch/next/operation/operation-search");

		Text(ApiCrawlRules.Compare(page, Served(page, "index, follow"), inSitemap: false, buildAllowsIndexing: true))
			.Should()
			.MatchInlineSnapshot(
				"/docs/api/elasticsearch/next/operation/operation-search [Unreleased] Robots: expected noindex, got index, follow"
			);
	}

	[Test]
	public void Compare_PageThatRedirects_ReportsStatusOnly()
	{
		var page = Page("/docs/api/cloud");
		var observed = new ObservedPage(page.Path, 200, [new RedirectHop(301, "/docs/api/cloud/")], null, null);

		Text(ApiCrawlRules.Compare(page, observed, inSitemap: true, buildAllowsIndexing: false))
			.Should()
			.MatchInlineSnapshot("/docs/api/cloud [Versionless] Status: expected 200 with no redirect, got 301 /docs/api/cloud/ -> 200");
	}

	[Test]
	public void CompareSitemap_IndexWithoutApiSitemapAndUnknownUrl_ReportsBoth() =>
		Text(
			ApiCrawlRules.CompareSitemap(
				"/docs/sitemap.xml",
				["https://docs-edge.elastic.dev/docs/sitemap-docs.xml"],
				["/docs/api/cloud", "/docs/api/unknown/operation/x"],
				Model
			)
		)
			.Should()
			.MatchInlineSnapshot(
				"""
			/docs/sitemap.xml SitemapIndex: expected a sitemap-api.xml entry, got sitemap-docs.xml
			/docs/api/unknown/operation/x UnknownSitemapUrl: expected a URL in an API tree, got no API tree serves it
			"""
			);

	[Test]
	public void CompareSitemap_IndexListsApiSitemap_HasNoDifferences() =>
		ApiCrawlRules
			.CompareSitemap("/docs/sitemap.xml", ["https://www.elastic.co/docs/sitemap-api.xml"], ["/docs/api/cloud"], Model)
			.Should()
			.BeEmpty();

	[Test]
	public void CompareRedirect_OneHopToTheTarget_HasNoDifferences()
	{
		var check = new RedirectCheck("/docs/api/doc/cloud", "/docs/api/cloud");
		var observed = new ObservedPage(check.OldPath, 200, [new RedirectHop(302, "/docs/api/cloud")], null, null);

		ApiCrawlRules.CompareRedirect(check, observed, expectedStatus: 302).Should().BeEmpty();
	}

	[Test]
	public void CompareRedirect_TwoHops_ReportsHops()
	{
		var check = new RedirectCheck("/docs/api/doc/cloud", "/docs/api/cloud");
		var observed = new ObservedPage(
			check.OldPath,
			200,
			[new RedirectHop(302, "/docs/api/cloud/"), new RedirectHop(301, "/docs/api/cloud")],
			null,
			null
		);

		Text(ApiCrawlRules.CompareRedirect(check, observed, expectedStatus: 302))
			.Should()
			.MatchInlineSnapshot(
				"""
			/docs/api/doc/cloud Hops: expected 1 redirect, got 302 /docs/api/cloud/ -> 301 /docs/api/cloud -> 200
			/docs/api/doc/cloud RedirectTarget: expected /docs/api/cloud, got /docs/api/cloud/
			"""
			);
	}

	[Test]
	public void CompareRedirect_WrongStatus_ReportsRedirectStatus()
	{
		var check = new RedirectCheck("/docs/api/doc/cloud", "/docs/api/cloud");
		var observed = new ObservedPage(check.OldPath, 200, [new RedirectHop(301, "/docs/api/cloud")], null, null);

		Text(ApiCrawlRules.CompareRedirect(check, observed, expectedStatus: 302))
			.Should()
			.MatchInlineSnapshot("/docs/api/doc/cloud RedirectStatus: expected 302, got 301");
	}

	[Test]
	public void CompareRedirect_NoRedirect_ReportsHops()
	{
		var check = new RedirectCheck("/docs/api/doc/cloud", "/docs/api/cloud");
		var observed = new ObservedPage(check.OldPath, 404, [], null, null);

		Text(ApiCrawlRules.CompareRedirect(check, observed, expectedStatus: 302))
			.Should()
			.MatchInlineSnapshot("/docs/api/doc/cloud Hops: expected 1 redirect, got 404");
	}

	[Test]
	public void RedirectSample_Fixture_ListsTableRedirectsRootsAndSampledPages() =>
		string
			.Join('\n', Model.RedirectSample(pageSample: 2).Select(c => $"{c.OldPath} -> {c.Target}"))
			.Should()
			.MatchInlineSnapshot(
				"""
			/docs/api/doc/cloud-billing/group/endpoint-budgets -> /docs/api/cloud-billing/group/endpoint-billing-budgets
			/docs/api/doc/cloud-billing/topic/topic-authentication -> /docs/api/cloud-billing/authentication
			/docs/api/doc/cloud-enterprise/servers -> /docs/api/cloud-enterprise
			/docs/api/doc/cloud-enterprise/v3/servers -> /docs/api/cloud-enterprise/v3
			/docs/api/doc/cloud/topic/topic-authentication -> /docs/api/cloud/authentication
			/docs/api/doc/elastic-cloud-serverless -> /docs/api/cloud-serverless
			/docs/api/doc/elasticsearch-serverless/servers -> /docs/api/elasticsearch-serverless
			/docs/api/doc/elasticsearch/servers -> /docs/api/elasticsearch
			/docs/api/doc/elasticsearch/v8/servers -> /docs/api/elasticsearch/v8
			/docs/api/doc/elasticsearch/v9/servers -> /docs/api/elasticsearch/v9
			/docs/api/doc/kibana/topic/topic-kibana-spaces -> /docs/api/kibana/kibana-api-overview
			/docs/api/doc/kibana/v8/topic/topic-kibana-spaces -> /docs/api/kibana/v8/kibana-api-overview
			/docs/api/doc/kibana/v9/group/endpoint-significant_events -> /docs/api/kibana/v9/group/endpoint-streams
			/docs/api/doc/kibana/v9/operation/operation-delete-streams-name-queries-queryid -> /docs/api/kibana/v9/group/endpoint-streams
			/docs/api/doc/kibana/v9/operation/operation-get-streams-name-queries -> /docs/api/kibana/v9/group/endpoint-streams
			/docs/api/doc/kibana/v9/operation/operation-get-streams-name-significant-events -> /docs/api/kibana/v9/group/endpoint-streams
			/docs/api/doc/kibana/v9/operation/operation-post-streams-name-queries-bulk -> /docs/api/kibana/v9/group/endpoint-streams
			/docs/api/doc/kibana/v9/operation/operation-put-streams-name-queries-queryid -> /docs/api/kibana/v9/group/endpoint-streams
			/docs/api/doc/kibana/v9/topic/topic-kibana-spaces -> /docs/api/kibana/v9/kibana-api-overview
			/docs/api/doc/serverless -> /docs/api/kibana-serverless
			/docs/api/doc/cloud -> /docs/api/cloud
			/docs/api/doc/elasticsearch -> /docs/api/elasticsearch
			/docs/api/doc/elasticsearch/next -> /docs/api/elasticsearch/next
			/docs/api/doc/elasticsearch/v8 -> /docs/api/elasticsearch/v8
			/docs/api/doc/elasticsearch/v9 -> /docs/api/elasticsearch/v9
			/docs/api/doc/cloud/operation/operation-list -> /docs/api/cloud/operation/operation-list
			/docs/api/doc/elasticsearch/v9/operation/operation-search -> /docs/api/elasticsearch/v9/operation/operation-search
			"""
			);

	[Test]
	public void Parse_EdgeHead_ReadsCanonicalAndRobots() =>
		ApiPageHead
			.Parse(
				"""
				<head><meta charset="utf-8">
				<link rel="canonical" href="https://www.elastic.co/docs/api/elasticsearch?a=1&amp;b=2" />
				<meta name="robots" content="noindex, nofollow"></head>
				"""
			)
			.Should()
			.Be(new ApiPageHead("https://www.elastic.co/docs/api/elasticsearch?a=1&b=2", "noindex, nofollow"));
}
