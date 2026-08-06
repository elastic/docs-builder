// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Search.Contract;
using Microsoft.Extensions.Logging;

namespace Elastic.SiteSearch.Cli.LabsCrawl;

/// <summary>Labs-only sitemap URLs and crawl planning (vendored from site crawler logic).</summary>
public static class LabsSiteCrawlPlanner
{
	public static readonly string[] LabsSitemapUrls =
	[
		"https://www.elastic.co/search-labs/sitemap.xml",
		"https://www.elastic.co/security-labs/sitemap.xml",
		"https://www.elastic.co/observability-labs/sitemap.xml"
	];

	/// <summary>All unique URLs plus per-sitemap fetch counts (before cross-sitemap deduplication).</summary>
	public sealed record LabsSitemapDiscoveryResult(
		IReadOnlyList<SitemapEntry> Urls,
		IReadOnlyList<(string SitemapUrl, int RawUrlCount)> PerSitemap);

	public static readonly string[] DefaultExcludePaths =
	[
		"/guide/",
		"/downloads/past-releases/"
	];

	private static readonly string[] LangPrefixes = ["/de/", "/fr/", "/jp/", "/kr/", "/cn/", "/es/", "/pt/"];

	private static readonly (string Pattern, string Label)[] PathEntries =
	[
		("/search-labs/", "/search-labs/"),
		("/security-labs/", "/security-labs/"),
		("/observability-labs/", "/observability-labs/")
	];

	public static async Task<LabsSitemapDiscoveryResult> DiscoverUrlsAsync(
		ISitemapParser sitemapParser,
		IReadOnlyList<string> sitemaps,
		IProgress<(int completed, string currentSitemap)> progress,
		CancellationToken ct)
	{
		var allUrlsList = new List<SitemapEntry>();
		var perSitemap = new List<(string SitemapUrl, int RawUrlCount)>();
		var completed = 0;
		foreach (var currentSitemap in sitemaps)
		{
			var urls = await sitemapParser.ParseAsync(new Uri(currentSitemap), null, ct);
			allUrlsList.AddRange(urls);
			perSitemap.Add((currentSitemap, urls.Count));
			completed++;
			progress.Report((completed, currentSitemap));
		}

		var deduped = allUrlsList
			.GroupBy(u => u.Location)
			.Select(g => g.First())
			.ToList();

		return new LabsSitemapDiscoveryResult(deduped, perSitemap);
	}

	/// <summary>Short label for CLI output, e.g. <c>search-labs</c>.</summary>
	public static string SitemapDisplayLabel(string sitemapUrl)
	{
		try
		{
			var path = new Uri(sitemapUrl).AbsolutePath.Trim('/');
			var first = path.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
			return string.IsNullOrEmpty(first) ? sitemapUrl : first;
		}
		catch (UriFormatException)
		{
			return sitemapUrl;
		}
	}

	public static List<SitemapEntry> FilterUrls(
		IReadOnlyList<SitemapEntry> urls,
		IReadOnlyList<string> exclusions,
		HashSet<string>? languageFilter) => urls
			.Where(u =>
			{
				var uri = new Uri(u.Location);

				if (!uri.Host.Equals("www.elastic.co", StringComparison.OrdinalIgnoreCase) &&
					!uri.Host.Equals("elastic.co", StringComparison.OrdinalIgnoreCase))
					return false;

				foreach (var exclusion in exclusions)
				{
					if (uri.AbsolutePath.StartsWith(exclusion, StringComparison.OrdinalIgnoreCase))
						return false;
				}

				if (languageFilter is { Count: > 0 })
				{
					var lang = GetLanguageFromUrl(u.Location);
					if (!languageFilter.Contains(lang))
						return false;
				}

				return true;
			})
			.ToList();

	public static string GetCategory(string url)
	{
		var uri = new Uri(url);
		var path = uri.AbsolutePath;

		foreach (var prefix in LangPrefixes)
		{
			if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
			{
				path = $"/{path[prefix.Length..]}";
				break;
			}
		}

		foreach (var (pattern, label) in PathEntries)
		{
			if (path.StartsWith(pattern, StringComparison.OrdinalIgnoreCase))
				return label;
		}

		var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
		return segments.Length > 0 ? $"/{segments[0]}/" : "/";
	}

	public static CrawlPlan BuildCrawlPlan(
		List<SitemapEntry> filteredUrls,
		Dictionary<string, CachedDocInfo> cache,
		bool unchanged,
		bool fair,
		int maxPages,
		ILoggerFactory loggerFactory)
	{
		var decisionMaker = new CrawlDecisionMaker(loggerFactory.CreateLogger<CrawlDecisionMaker>());
		var allDecisions = decisionMaker.MakeDecisions(filteredUrls, cache).ToList();

		if (fair && maxPages > 0)
		{
			var needsCrawling = allDecisions.Where(d => d.Reason != CrawlReason.Unchanged).ToList();
			if (needsCrawling.Count > maxPages)
			{
				var fairSample = ApplyCategoryFairness(needsCrawling, maxPages);
				var cachedDecisions = allDecisions.Where(d => d.Reason == CrawlReason.Unchanged).ToList();
				allDecisions = [.. cachedDecisions, .. fairSample];
			}
		}

		var stats = CrawlDecisionMaker.GetStats(allDecisions);

		var allKnownUrls = filteredUrls
			.Select(u => u.Location)
			.ToHashSet(StringComparer.OrdinalIgnoreCase);
		var staleUrls = decisionMaker.FindStaleUrls(cache, allKnownUrls).ToList();

		var urlsToCrawl = allDecisions
			.Where(d => unchanged || d.Reason != CrawlReason.Unchanged)
			.ToList();

		if (!fair && maxPages > 0 && urlsToCrawl.Count > maxPages)
			urlsToCrawl = urlsToCrawl.Take(maxPages).ToList();

		return new CrawlPlan(urlsToCrawl, staleUrls, stats);
	}

	private static List<CrawlDecision> ApplyCategoryFairness(List<CrawlDecision> decisions, int maxPages)
	{
		var byCategory = decisions
			.GroupBy(d => GetCategory(d.Entry.Location))
			.ToDictionary(g => g.Key, g => g.ToList());

		var categoryCount = byCategory.Count;
		if (categoryCount == 0)
			return [];

		var result = new List<CrawlDecision>();
		var remaining = maxPages;

		var sortedCategories = byCategory
			.OrderByDescending(kvp => kvp.Value.Count)
			.ToList();

		var taken = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

		var fairShare = maxPages / categoryCount;
		var extraSlots = maxPages % categoryCount;

		foreach (var (category, categoryDecisions) in sortedCategories)
		{
			var quota = fairShare + (extraSlots > 0 ? 1 : 0);
			if (extraSlots > 0)
				extraSlots--;

			var toTake = Math.Min(quota, categoryDecisions.Count);
			result.AddRange(categoryDecisions.Take(toTake));
			taken[category] = toTake;
			remaining -= toTake;
		}

		if (remaining > 0)
		{
			foreach (var (category, categoryDecisions) in sortedCategories)
			{
				if (remaining <= 0)
					break;

				var alreadyTaken = taken[category];
				var available = categoryDecisions.Count - alreadyTaken;
				if (available > 0)
				{
					var toTake = Math.Min(remaining, available);
					result.AddRange(categoryDecisions.Skip(alreadyTaken).Take(toTake));
					remaining -= toTake;
				}
			}
		}

		return result;
	}

	private static string GetLanguageFromUrl(string url)
	{
		var uri = new Uri(url);
		var path = uri.AbsolutePath;

		if (path.StartsWith("/de/", StringComparison.OrdinalIgnoreCase))
			return "de";
		if (path.StartsWith("/fr/", StringComparison.OrdinalIgnoreCase))
			return "fr";
		if (path.StartsWith("/jp/", StringComparison.OrdinalIgnoreCase))
			return "ja";
		if (path.StartsWith("/kr/", StringComparison.OrdinalIgnoreCase))
			return "ko";
		if (path.StartsWith("/cn/", StringComparison.OrdinalIgnoreCase))
			return "zh";
		if (path.StartsWith("/es/", StringComparison.OrdinalIgnoreCase))
			return "es";
		if (path.StartsWith("/pt/", StringComparison.OrdinalIgnoreCase))
			return "pt";

		return "en";
	}

	/// <summary>Lexical read alias for incremental cache (same index as contentstack sync).</summary>
	public static string ResolveLexicalReadAlias(string buildType, string environment) =>
		LabsMappingContext.LabsDocument
			.CreateContext(type: buildType, env: environment)
			.ResolveReadTarget();
}

public sealed record CrawlPlan(
	IReadOnlyList<CrawlDecision> UrlsToCrawl,
	IReadOnlyList<string> StaleUrls,
	CrawlDecisionStats Stats);
