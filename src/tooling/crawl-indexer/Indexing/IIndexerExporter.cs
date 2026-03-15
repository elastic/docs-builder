// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Ingest.Elasticsearch;
using Elastic.Ingest.Elasticsearch.Enrichment;

namespace CrawlIndexer.Indexing;

/// <summary>
/// Common contract for site and guide indexer exporters.
/// </summary>
public interface IIndexerExporter : IDisposable
{
	bool AiEnrichmentEnabled { get; }

	IngestSyncStrategy Strategy { get; }

	/// <summary>Fired during finalization for reindex/delete/cleanup progress updates.</summary>
	Action<SyncProgressInfo>? OnSyncProgress { get; set; }

	ValueTask StartAsync(Cancel ctx = default);

	ValueTask FinalizeAsync(Cancel ctx = default);

	IAsyncEnumerable<AiEnrichmentProgress> RunAiEnrichmentAsync(int maxDocs = 0, Cancel ctx = default);
}
