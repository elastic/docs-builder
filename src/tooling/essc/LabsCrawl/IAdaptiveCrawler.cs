// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.SiteSearch.Cli.LabsCrawl;

public interface IAdaptiveCrawler
{
	IAsyncEnumerable<CrawlResult> CrawlAsync(IEnumerable<SitemapEntry> urls, CancellationToken ctx = default);
	IAsyncEnumerable<CrawlResult> CrawlAsync(IEnumerable<CrawlDecision> decisions, CancellationToken ctx = default);
}
