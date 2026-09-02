// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.SiteSearch.Cli.LabsCrawl;

public interface IDocumentExtractor<TDocument>
{
	Task<TDocument?> ExtractAsync(CrawlResult result, CancellationToken ct);
}
