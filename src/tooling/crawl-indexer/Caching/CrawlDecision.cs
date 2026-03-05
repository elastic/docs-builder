// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using CrawlIndexer.Crawling;

namespace CrawlIndexer.Caching;

/// <summary>
/// Reason for the crawl decision.
/// </summary>
public enum CrawlReason
{
	/// <summary>URL not in cache - must crawl.</summary>
	New,

	/// <summary>Sitemap lastmod indicates unchanged - skip crawl.</summary>
	Unchanged,

	/// <summary>Sitemap changed or no lastmod - verify via HTTP conditional request.</summary>
	PossiblyChanged
}

/// <summary>
/// Decision about whether to crawl a URL and why.
/// </summary>
public record CrawlDecision(
	SitemapEntry Entry,
	CrawlReason Reason,
	CachedDocInfo? Cached = null
);
