// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace CrawlIndexer.Crawling;

/// <summary>
/// Parses sitemap.xml files, including sitemap indexes that reference multiple sitemaps.
/// </summary>
public interface ISitemapParser
{
	/// <summary>
	/// Parse a sitemap URL and return all discovered URL entries.
	/// Handles both sitemap index files and regular sitemaps.
	/// </summary>
	/// <param name="sitemapUrl">The sitemap URL to parse</param>
	/// <param name="onProgress">Optional callback for progress updates (current, total, currentUrl)</param>
	/// <param name="ctx">Cancellation token</param>
	Task<IReadOnlyList<SitemapEntry>> ParseAsync(
		Uri sitemapUrl,
		Action<int, int, string>? onProgress = null,
		Cancel ctx = default
	);
}
