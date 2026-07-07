// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Microsoft.Extensions.Logging;

namespace Elastic.SiteSearch.Cli.LabsCrawl;

public class CrawlDecisionMaker(ILogger<CrawlDecisionMaker> logger)
{
	public IEnumerable<CrawlDecision> MakeDecisions(
		IEnumerable<SitemapEntry> sitemapUrls,
		IReadOnlyDictionary<string, CachedDocInfo> cache)
	{
		foreach (var entry in sitemapUrls)
		{
			if (!cache.TryGetValue(entry.Location, out var cached))
			{
				yield return new CrawlDecision(entry, CrawlReason.New);
				continue;
			}

			if (entry.LastModified.HasValue && cached.LastUpdated >= entry.LastModified.Value)
			{
				logger.LogDebug("Unchanged (sitemap): {Url}", entry.Location);
				yield return new CrawlDecision(entry, CrawlReason.Unchanged, cached);
				continue;
			}

			logger.LogDebug("Possibly changed: {Url}", entry.Location);
			yield return new CrawlDecision(entry, CrawlReason.PossiblyChanged, cached);
		}
	}

	public IEnumerable<string> FindStaleUrls(
		IReadOnlyDictionary<string, CachedDocInfo> cache,
		IReadOnlySet<string> sitemapUrls)
	{
		foreach (var url in cache.Keys.Where(url => !sitemapUrls.Contains(url)))
		{
			logger.LogDebug("Stale URL (not in sitemap): {Url}", url);
			yield return url;
		}
	}

	public static CrawlDecisionStats GetStats(IReadOnlyList<CrawlDecision> decisions)
	{
		var newUrls = 0;
		var unchanged = 0;
		var possiblyChanged = 0;
		foreach (var d in decisions)
		{
			switch (d.Reason)
			{
				case CrawlReason.New:
					newUrls++;
					break;
				case CrawlReason.Unchanged:
					unchanged++;
					break;
				case CrawlReason.PossiblyChanged:
					possiblyChanged++;
					break;
			}
		}
		return new(newUrls, unchanged, possiblyChanged);
	}
}

public record CrawlDecisionStats(int NewUrls, int UnchangedUrls, int PossiblyChangedUrls)
{
	public int TotalUrls => NewUrls + UnchangedUrls + PossiblyChangedUrls;
	public int UrlsToCrawl => NewUrls + PossiblyChangedUrls;
}
