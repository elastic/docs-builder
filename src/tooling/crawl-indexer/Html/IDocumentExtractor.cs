// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using CrawlIndexer.Crawling;

namespace CrawlIndexer.Html;

/// <summary>
/// Extracts a typed document from a crawl result.
/// </summary>
public interface IDocumentExtractor<TDocument>
{
	/// <summary>
	/// Extract a document from the crawl result. Returns null if extraction fails.
	/// </summary>
	Task<TDocument?> ExtractAsync(CrawlResult result, CancellationToken ct);
}
