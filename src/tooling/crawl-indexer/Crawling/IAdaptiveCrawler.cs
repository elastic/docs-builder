// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using CrawlIndexer.Caching;

namespace CrawlIndexer.Crawling;

/// <summary>
/// Adaptive HTTP crawler that adjusts rate based on server response times.
/// </summary>
public interface IAdaptiveCrawler
{
	/// <summary>
	/// Crawl the given URLs and yield results as they complete.
	/// </summary>
	IAsyncEnumerable<CrawlResult> CrawlAsync(IEnumerable<SitemapEntry> urls, Cancel ctx = default);

	/// <summary>
	/// Crawl URLs based on crawl decisions, using conditional HTTP requests when cached data is available.
	/// </summary>
	IAsyncEnumerable<CrawlResult> CrawlAsync(IEnumerable<CrawlDecision> decisions, Cancel ctx = default);
}
