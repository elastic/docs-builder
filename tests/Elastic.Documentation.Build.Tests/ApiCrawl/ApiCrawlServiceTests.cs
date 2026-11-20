// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using System.Net;
using System.Text;
using AwesomeAssertions;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.Documentation.Assembler.ApiCrawl;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Links;
using Elastic.Documentation.Services;
using Elastic.Documentation.Testing;
using Microsoft.Extensions.Logging.Abstractions;
using Nullean.Snapshots;

namespace Elastic.Documentation.Build.Tests.ApiCrawl;

/// <summary>The crawl over a two-API fixture site served by a stub handler; the network is never touched.</summary>
public class ApiCrawlServiceTests
{
	private const string LinksUrl = "https://links.test/links.json";

	private sealed class CapturingCollector() : DiagnosticsCollector([])
	{
		private readonly List<Diagnostic> _diagnostics = [];

		public IReadOnlyCollection<Diagnostic> Diagnostics => _diagnostics;

		public override void Write(Diagnostic diagnostic)
		{
			IncrementSeverityCount(diagnostic);
			_diagnostics.Add(diagnostic);
		}
	}

	private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
	{
		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
			Task.FromResult(responder(request));
	}

	/// <summary>A site that serves every page as the table says. <paramref name="seed"/> may answer a path first.</summary>
	private sealed class FixtureSite(Func<string, HttpResponseMessage?>? seed = null)
	{
		private static readonly ApiUrlModel Model = ApiCrawlFixture.Model;

		public string SitemapIndex { get; init; } =
			"""
			<?xml version="1.0" encoding="utf-8"?>
			<sitemapindex xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">
			  <sitemap><loc>https://site.test/docs/sitemap-docs.xml</loc></sitemap>
			  <sitemap><loc>https://site.test/docs/sitemap-api.xml</loc></sitemap>
			</sitemapindex>
			""";

		public IEnumerable<string> SitemapPaths { get; init; } = Model
			.Pages
			.Where(p => ApiPageTypes.Indexing(p.Type).InSitemap)
			.Select(p => p.Path);

		public HttpResponseMessage Respond(HttpRequestMessage request)
		{
			// Like the edge WAF, refuse a request without a User-Agent.
			if (request.Headers.UserAgent.Count == 0)
				return new HttpResponseMessage(HttpStatusCode.Forbidden);
			var uri = request.RequestUri!;
			if (uri.AbsoluteUri == LinksUrl)
				return Text(ApiCrawlFixture.LinksJson);
			var path = uri.AbsolutePath;
			if (seed?.Invoke(path) is { } seeded)
				return seeded;
			return path switch
			{
				"/docs/sitemap.xml" => Text(SitemapIndex),
				"/docs/sitemap-api.xml" => Text(Sitemap(SitemapPaths)),
				_ when ApiLegacyRedirects.IsLegacy(path) => Redirect(HttpStatusCode.Found, ApiLegacyRedirects.Resolve(path)),
				_ => Page(Model.Pages.FirstOrDefault(p => p.Path == path)?.CanonicalUrl ?? "https://www.elastic.co" + path)
			};
		}

		private static string Sitemap(IEnumerable<string> paths) =>
			"<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">"
				+ string.Concat(paths.Select(p => $"<url><loc>https://www.elastic.co{p}</loc></url>"))
				+ "</urlset>";
	}

	private static HttpResponseMessage Text(string body) => new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8) };

	private static HttpResponseMessage Page(string canonical, string robots = "noindex, nofollow") =>
		Text(
			$"""<html><head><link rel="canonical" href="{canonical}" /><meta name="robots" content="{robots}"></head><body></body></html>"""
		);

	private static HttpResponseMessage Redirect(HttpStatusCode status, string location) =>
		new(status) { Headers = { Location = new Uri(location, UriKind.Relative) } };

	private static async Task<(int ExitCode, string Diagnostics, string Summary)> Crawl(FixtureSite site)
	{
		var collector = new CapturingCollector();
		using var handler = new StubHandler(site.Respond);
		await using var output = new StringWriter();
		var service = new ApiCrawlService(NullLoggerFactory.Instance, new MockFileSystem(), output, handler);
		var options = new ApiCrawlOptions(new Uri("https://site.test"), "docs", LinksUrl, AllowIndexing: false)
		{
			RedirectSample = 2,
			Concurrency = 4
		};

		int exitCode;
		await using (var invoker = new ServiceInvoker(collector))
		{
			invoker.AddCommand(service, options, static async (s, c, o, ctx) => await s.CrawlAsync(c, o, ctx));
			exitCode = await invoker.InvokeAsync(CancellationToken.None);
		}

		return (exitCode, collector.Diagnostics.ToSnapshotText(), output.ToString());
	}

	[Test]
	public async Task CrawlAsync_SiteServedAsTheTableSays_ExitsZero()
	{
		var (exitCode, diagnostics, summary) = await Crawl(new FixtureSite());

		diagnostics.Should().MatchInlineSnapshot("");
		summary.Should().MatchInlineSnapshot(
			"""
			Check             Checked  Differences
			Versionless             5            0
			NewestMajorCopy         2            0
			OlderMajor              2            0
			Unreleased              3            0
			Redirects              27            0
			Sitemap                 7            0
			"""
		);
		exitCode.Should().Be(0);
	}

	[Test]
	public async Task CrawlAsync_SeededDifferences_ReportsEachAndExitsOne()
	{
		var site = new FixtureSite(
			path =>
				path switch
				{
					"/docs/api/elasticsearch/v9/operation/operation-search" => Page("https://www.elastic.co" + path),
					"/docs/api/cloud/operation/operation-list" => Page("https://www.elastic.co" + path, "index, follow"),
					"/docs/api/doc/cloud" => Redirect(HttpStatusCode.MovedPermanently, "/docs/api/cloud"),
					"/docs/api/doc/elasticsearch" => Redirect(HttpStatusCode.Found, "/docs/api/elasticsearch/"),
					"/docs/api/elasticsearch/" => Redirect(HttpStatusCode.MovedPermanently, "/docs/api/elasticsearch"),
					_ => null
				}
		)
		{
			SitemapIndex =
				"""<sitemapindex xmlns="http://www.sitemaps.org/schemas/sitemap/0.9"><sitemap><loc>https://site.test/docs/sitemap-docs.xml</loc></sitemap></sitemapindex>""",
			SitemapPaths = ApiCrawlFixture
				.Model
				.Pages
				.Where(p => ApiPageTypes.Indexing(p.Type).InSitemap)
				.Select(p => p.Path)
				.Append("/docs/api/elasticsearch/next")
				.Append("/docs/api/elasticsearch/servers")
		};

		var (exitCode, diagnostics, summary) = await Crawl(site);

		diagnostics.Should().MatchInlineSnapshot(
			"""
			error  /docs/api/cloud/operation/operation-list [Versionless] Robots: expected noindex, nofollow, got index, follow
			error  /docs/api/elasticsearch/v9/operation/operation-search [NewestMajorCopy] Canonical: expected https://www.elastic.co/docs/api/elasticsearch/operation/operation-search, got https://www.elastic.co/docs/api/elasticsearch/v9/operation/operation-search
			error  /docs/api/elasticsearch/next [Unreleased] Sitemap: expected not listed, got listed
			error  /docs/api/doc/cloud RedirectStatus: expected 302, got 301
			error  /docs/api/doc/elasticsearch Hops: expected 1 redirect, got 302 /docs/api/elasticsearch/ -> 301 /docs/api/elasticsearch -> 200
			error  /docs/api/doc/elasticsearch RedirectTarget: expected /docs/api/elasticsearch, got /docs/api/elasticsearch/
			error  /docs/sitemap.xml SitemapIndex: expected a sitemap-api.xml entry, got sitemap-docs.xml
			"""
		);
		summary.Should().MatchInlineSnapshot(
			"""
			Check             Checked  Differences
			Versionless             6            1
			NewestMajorCopy         2            1
			OlderMajor              2            0
			Unreleased              3            1
			Redirects              27            3
			Sitemap                 9            1
			"""
		);
		exitCode.Should().Be(1);
	}
}
