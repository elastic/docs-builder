// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using System.IO.Abstractions;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Links;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Assembler.ApiCrawl;

/// <param name="Site">The scheme and host to crawl, such as <c>https://docs-edge.elastic.dev</c>.</param>
/// <param name="SitePrefix">The path the docs live under on <see cref="Site"/>, such as <c>/docs</c>.</param>
/// <param name="LinksJson">A path or URL of the <c>links.json</c> that lists the API pages.</param>
/// <param name="AllowIndexing">Whether the build lets search engines index its pages, which sets the expected robots value.</param>
public sealed record ApiCrawlOptions(Uri Site, string SitePrefix, string LinksJson, bool AllowIndexing)
{
	/// <summary>The links.json of docs-content main in the public link index.</summary>
	public const string DefaultLinksJson =
		"https://elastic-docs-link-index.s3.us-east-2.amazonaws.com/elastic/docs-content/main/links.json";

	public int RedirectStatus { get; init; } = 302;
	public int RedirectSample { get; init; } = 50;
	public int Concurrency { get; init; } = 16;
}

/// <summary>
/// Crawls every API URL of a deployed site and compares each answer with the API page-type table. Each difference is a
/// collector error and a summary per page type goes to <paramref name="output"/>.
/// </summary>
public sealed class ApiCrawlService(
	ILoggerFactory logFactory,
	IFileSystem fileSystem,
	TextWriter output,
	HttpMessageHandler? handler = null
) : IService
{
	private const string SitemapRow = "Sitemap";
	private const string RedirectRow = "Redirects";

	private static readonly string[] RowNames = [.. Enum.GetNames<ApiPageType>(), RedirectRow, SitemapRow];

	private readonly ILogger _logger = logFactory.CreateLogger<ApiCrawlService>();

	public async Task<bool> CrawlAsync(IDiagnosticsCollector collector, ApiCrawlOptions options, Cancel ctx)
	{
		using var client = new ApiCrawlClient(options.Site, handler);
		var prefix = "/" + options.SitePrefix.Trim('/');

		var links = await LoadLinksAsync(client, options.LinksJson, ctx).ConfigureAwait(false);
		RepositoryLinks repositoryLinks;
		switch (links)
		{
			case LinksLoaded loaded:
				repositoryLinks = loaded.Links;
				break;
			case LinksFailed failed:
				collector.EmitGlobalError($"Could not read links.json from {options.LinksJson}: {failed.Reason}");
				return false;
			default:
				return false;
		}

		var model = ApiUrlModel.Create(repositoryLinks, prefix);
		var differences = new ConcurrentBag<CrawlDifference>();

		var sitemap = await ReadSitemapsAsync(client, options.Site, prefix, model, ctx).ConfigureAwait(false);
		foreach (var difference in sitemap.Differences)
			differences.Add(difference);

		var known = model.Pages.Select(p => p.Path).ToHashSet(StringComparer.Ordinal);
		var pages = model
			.Pages
			.Concat(sitemap
				.ApiPaths
				.Where(path => !known.Contains(path) && model.TryClassify(path, out _))
				.Distinct()
				.Select(path =>
				{
					_ = model.TryClassify(path, out var tree);
					return ApiUrlModel.Expect(path, tree);
				}))
			.ToArray();

		_logger.LogInformation("Crawling {Pages} API pages on {Site}", pages.Length, options.Site);
		var parallel = new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, options.Concurrency), CancellationToken = ctx };
		await Parallel.ForEachAsync(pages, parallel, async (page, c) =>
		{
			var observed = await client.ObserveAsync(page.Path, c).ConfigureAwait(false);
			var inSitemap = sitemap.ApiPaths.Contains(page.Path);
			foreach (var difference in ApiCrawlRules.Compare(page, observed, inSitemap, buildAllowsIndexing: options.AllowIndexing))
				differences.Add(difference);
		}).ConfigureAwait(false);

		var redirects = model.RedirectSample(options.RedirectSample);
		_logger.LogInformation("Checking {Redirects} old /docs/api/doc/ URLs", redirects.Count);
		await Parallel.ForEachAsync(redirects, parallel, async (check, c) =>
		{
			var observed = await client.ObserveAsync(check.OldPath, c).ConfigureAwait(false);
			foreach (var difference in ApiCrawlRules.CompareRedirect(check, observed, options.RedirectStatus))
				differences.Add(difference);
		}).ConfigureAwait(false);

		var sorted = differences
			.OrderBy(d => Array.IndexOf(RowNames, Row(d)))
			.ThenBy(d => d.Path, StringComparer.Ordinal)
			.ThenBy(d => d.Field)
			.ToArray();
		foreach (var difference in sorted)
			collector.EmitGlobalError(difference.ToString());

		await WriteSummaryAsync(pages, redirects.Count, sitemap.ApiPaths.Count, sorted).ConfigureAwait(false);
		return true;
	}

	private async Task WriteSummaryAsync(
		IReadOnlyList<ExpectedApiPage> pages,
		int redirects,
		int sitemapUrls,
		IReadOnlyList<CrawlDifference> differences
	)
	{
		var rows = Enum
			.GetValues<ApiPageType>()
			.Select(type => (Name: type.ToString(), Checked: pages.Count(p => p.Type == type)))
			.Append((RedirectRow, redirects))
			.Append((SitemapRow, sitemapUrls));

		await output.WriteLineAsync($"{"Check",-16} {"Checked",8} {"Differences",12}").ConfigureAwait(false);
		foreach (var (name, count) in rows)
			await output.WriteLineAsync($"{name,-16} {count,8} {differences.Count(d => Row(d) == name),12}").ConfigureAwait(false);
	}

	private static string Row(CrawlDifference difference) => difference switch
	{
		{ Type: { } type } => type.ToString(),
		{ Field: CrawlField.SitemapIndex or CrawlField.UnknownSitemapUrl } => SitemapRow,
		_ => RedirectRow
	};

	private sealed record SitemapRead(IReadOnlySet<string> ApiPaths, IReadOnlyList<CrawlDifference> Differences);

	private static async Task<SitemapRead> ReadSitemapsAsync(ApiCrawlClient client, Uri site, string prefix, ApiUrlModel model, Cancel ctx)
	{
		var indexPath = $"{prefix}/sitemap.xml";
		var apiPath = $"{prefix}/{ApiCrawlRules.ApiSitemapFileName}";
		var index = await client.GetTextAsync(new Uri(site, indexPath), ctx).ConfigureAwait(false);
		var api = await client.GetTextAsync(new Uri(site, apiPath), ctx).ConfigureAwait(false);

		var differences = new List<CrawlDifference>();
		if (!index.IsOk)
			differences.Add(new(indexPath, null, CrawlField.SitemapIndex, "HTTP 200", index.Describe()));
		if (!api.IsOk)
			differences.Add(new(apiPath, null, CrawlField.SitemapIndex, "HTTP 200", api.Describe()));

		var indexLocations = index.IsOk ? Locations(index.Body) : [];
		// The API sitemap names the canonical host; the path is what the crawled host serves.
		var apiPaths = (api.IsOk ? Locations(api.Body) : []).Select(
			loc => Uri.TryCreate(loc, UriKind.Absolute, out var uri) ? uri.AbsolutePath : loc
		).ToHashSet(StringComparer.Ordinal);

		if (index.IsOk)
			differences.AddRange(ApiCrawlRules.CompareSitemap(indexPath, indexLocations, api.IsOk ? apiPaths : [], model));
		return new SitemapRead(apiPaths, differences);
	}

	private static string[] Locations(string xml)
	{
		try
		{
			return [.. XDocument.Parse(xml).Descendants().Where(e => e.Name.LocalName == "loc").Select(e => e.Value.Trim())];
		}
		catch (XmlException)
		{
			return [];
		}
	}

	private async Task<LinksResult> LoadLinksAsync(ApiCrawlClient client, string source, Cancel ctx)
	{
		try
		{
			if (Uri.TryCreate(source, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https")
			{
				var fetched = await client.GetTextAsync(uri, ctx).ConfigureAwait(false);
				return fetched.IsOk ? new LinksLoaded(RepositoryLinks.Deserialize(fetched.Body)) : new LinksFailed(fetched.Describe());
			}

			var json = await fileSystem.File.ReadAllTextAsync(source, ctx).ConfigureAwait(false);
			return new LinksLoaded(RepositoryLinks.Deserialize(json));
		}
		catch (Exception ex) when (ex is JsonException or IOException)
		{
			return new LinksFailed(ex.Message);
		}
	}

	private abstract record LinksResult;

	private sealed record LinksLoaded(RepositoryLinks Links) : LinksResult;

	private sealed record LinksFailed(string Reason) : LinksResult;
}
