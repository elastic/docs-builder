// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Markdown.Exporters.Elasticsearch.Enrichment;

/// <summary>
/// Abstraction for enrichment cache operations.
/// With the enrich processor pattern, the cache stores enrichment data that
/// gets joined to documents at index time via an Elasticsearch enrich processor.
/// </summary>
public interface IEnrichmentCache
{
	/// <summary>
	/// The name of the cache index.
	/// </summary>
	string IndexName { get; }

	/// <summary>
	/// Initializes the cache, including index creation and loading existing hashes.
	/// </summary>
	Task InitializeAsync(CancellationToken ct);

	/// <summary>
	/// Checks if an enrichment exists for the given enrichment key.
	/// </summary>
	bool Exists(string enrichmentKey);

	/// <summary>
	/// Stores enrichment data in the cache.
	/// </summary>
	/// <param name="enrichmentKey">The enrichment key (content hash).</param>
	/// <param name="url">The document URL for debugging.</param>
	/// <param name="data">The enrichment data to store.</param>
	/// <param name="ct">Cancellation token.</param>
	Task StoreAsync(string enrichmentKey, string url, EnrichmentData data, CancellationToken ct);

	/// <summary>
	/// Gets the number of entries currently in the cache.
	/// </summary>
	int Count { get; }
}
