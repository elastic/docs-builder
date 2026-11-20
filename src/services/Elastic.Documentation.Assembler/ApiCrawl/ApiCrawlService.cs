// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using System.IO.Abstractions;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using Amazon.Runtime;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.Documentation.Assembler.Building;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.LinkIndex;
using Elastic.Documentation.Links;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Assembler.ApiCrawl;

/// <param name="Site">The scheme and host to crawl, such as <c>https://docs-edge.elastic.dev</c>.</param>
/// <param name="SitePrefix">The path the docs live under on <see cref="Site"/>, such as <c>/docs</c>.</param>
/// <param name="LinksJson">A local <c>links.json</c> that lists the API pages, or null for docs-content <c>main</c> in the link index.</param>
/// <param name="AllowIndexing">Whether the build lets search engines index its pages, which sets the expected robots value.</param>
public sealed record ApiCrawlOptions(Uri Site, string SitePrefix, string? LinksJson, bool AllowIndexing)
{
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
	private const string DefaultLinksKey = "elastic/docs-content/main/links.json";

	// ApiLegacyRedirects describes the old URLs of a site under /docs only.
	private const string LegacyRedirectPrefix = "/docs";

	private static readonly string[] RowNames = [.. Enum.GetNames<ApiPageType>(), RedirectRow, SitemapRow];

	private readonly ILogger _logger = logFactory.CreateLogger<ApiCrawlService>();

	public async Task<bool> CrawlAsync(IDiagnosticsCollector collector, ApiCrawlOptions options, Cancel ctx)
	{
		using var client = new ApiCrawlClient(options.Site, handler);
		var prefix = "/" + options.SitePrefix.Trim('/');
		var indexPath = $"{prefix}/{SitemapBuilder.IndexFileName}";
		var apiPath = $"{prefix}/{SitemapBuilder.ApiFileName}";

		var linksTask = LoadLinksAsync(options.LinksJson, ctx);
		var indexTask = client.GetTextAsync(new Uri(options.Site, indexPath), ctx);
		var apiTask = client.GetTextAsync(new Uri(options.Site, apiPath), ctx);
		await Task.WhenAll(linksTask, indexTask, apiTask).ConfigureAwait(false);

		if ((await linksTask.ConfigureAwait(false)).EmitTo(collector) is not { } repositoryLinks)
			return false;

		var model = ApiUrlModel.Create(repositoryLinks, prefix);
		var differences = new ConcurrentBag<CrawlDifference>();

		var sitemap = ReadSitemaps(
			(indexPath, await indexTask.ConfigureAwait(false)),
			(apiPath, await apiTask.ConfigureAwait(false)),
			model
		);
		foreach (var difference in sitemap.Differences)
			differences.Add(difference);

		// A sitemap URL that links.json does not list is still crawled as the page type of its tree.
		var known = model.Pages.Select(p => p.Path).ToHashSet(StringComparer.Ordinal);
		var unlisted = sitemap
			.ApiPaths
			.Where(path => !known.Contains(path))
			.Select(path => model.TryClassify(path, out var tree) ? ApiUrlModel.Expect(path, tree) : null)
			.OfType<ExpectedApiPage>();
		var pages = model.Pages.Concat(unlisted).ToArray();

		_logger.LogInformation("Crawling {Pages} API pages on {Site}", pages.Length, options.Site);
		var parallel = new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, options.Concurrency), CancellationToken = ctx };
		await Parallel.ForEachAsync(pages, parallel, async (page, c) =>
		{
			var observed = await client.ObserveAsync(page.Path, c).ConfigureAwait(false);
			var inSitemap = sitemap.ApiPaths.Contains(page.Path);
			foreach (var difference in ApiCrawlRules.Compare(page, observed, inSitemap, buildAllowsIndexing: options.AllowIndexing))
				differences.Add(difference);
		}).ConfigureAwait(false);

		var redirects = RedirectSample(model, prefix, options.RedirectSample);
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

		// The collector prints errors as it drains them; the summary goes after the last one.
		await collector.WaitForDrain().ConfigureAwait(false);
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

	private IReadOnlyList<RedirectCheck> RedirectSample(ApiUrlModel model, string prefix, int pageSample)
	{
		if (prefix != LegacyRedirectPrefix)
		{
			_logger.LogInformation(
				"Skipping the redirect check: the old API URL table only describes {Legacy}, and this site lives under {Prefix}",
				LegacyRedirectPrefix,
				prefix
			);
			return [];
		}

		var redirects = model.RedirectSample(pageSample);
		_logger.LogInformation("Checking {Redirects} old /docs/api/doc/ URLs", redirects.Count);
		return redirects;
	}

	private sealed record SitemapRead(IReadOnlySet<string> ApiPaths, IReadOnlyList<CrawlDifference> Differences);

	private static SitemapRead ReadSitemaps((string Path, FetchedText Text) index, (string Path, FetchedText Text) api, ApiUrlModel model)
	{
		var differences = new List<CrawlDifference>();
		foreach (var (path, text) in new[] { index, api })
		{
			if (!text.IsOk)
				differences.Add(new(path, null, CrawlField.SitemapIndex, "HTTP 200", text.Describe()));
		}

		// The API sitemap names the canonical host; the path is what the crawled host serves.
		var apiPaths = Locations(api.Text)
			.Select(loc => Uri.TryCreate(loc, UriKind.Absolute, out var uri) ? uri.AbsolutePath : loc)
			.ToHashSet(StringComparer.Ordinal);

		if (index.Text.IsOk)
			differences.AddRange(ApiCrawlRules.CompareSitemap(index.Path, Locations(index.Text), apiPaths, model));
		return new SitemapRead(apiPaths, differences);
	}

	private static string[] Locations(FetchedText text)
	{
		if (!text.IsOk)
			return [];
		try
		{
			return [.. XDocument.Parse(text.Body).Descendants().Where(e => e.Name.LocalName == "loc").Select(e => e.Value.Trim())];
		}
		catch (XmlException)
		{
			return [];
		}
	}

	/// <summary>Reads <paramref name="path"/>, or docs-content <c>main</c> from the link index when it is null.</summary>
	private async Task<LoadResult<RepositoryLinks>> LoadLinksAsync(string? path, Cancel ctx)
	{
		try
		{
			if (path is null)
			{
				var links = await Aws3LinkIndexReader.CreateAnonymous().GetRepositoryLinks(DefaultLinksKey, ctx).ConfigureAwait(false);
				return new Loaded<RepositoryLinks>(links, []);
			}

			var stream = fileSystem.File.OpenRead(path);
			await using (stream.ConfigureAwait(false))
				return new Loaded<RepositoryLinks>(RepositoryLinks.Deserialize(stream), []);
		}
		catch (Exception ex) when (ex is JsonException or IOException or HttpRequestException or AmazonServiceException or AmazonClientException)
		{
			return new Unreadable<RepositoryLinks>([
				new Diagnostic
				{
					Severity = Severity.Error,
					File = path ?? DefaultLinksKey,
					Message = $"Could not read links.json: {ex.Message}"
				}
			]);
		}
	}
}
