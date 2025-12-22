// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Markdown.Exporters.Elasticsearch.Enrichment;

/// <summary>
/// Represents cached enrichment data with its version metadata.
/// </summary>
public sealed record CachedEnrichmentEntry(EnrichmentData Data, int PromptVersion);

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
	/// Checks if an enrichment exists for the given content hash.
	/// </summary>
	bool Exists(string contentHash);

	/// <summary>
	/// Fetches enrichment data from the cache by content hash.
	/// Returns null if not found.
	/// </summary>
	Task<EnrichmentData?> GetAsync(string contentHash, CancellationToken ct);

	/// <summary>
	/// Stores enrichment data in the cache.
	/// </summary>
	Task StoreAsync(string contentHash, EnrichmentData data, int promptVersion, CancellationToken ct);

	/// <summary>
	/// Gets the number of entries currently in the cache.
	/// </summary>
	int Count { get; }
}
