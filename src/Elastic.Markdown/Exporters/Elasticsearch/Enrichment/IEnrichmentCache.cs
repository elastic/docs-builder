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
/// Enables swapping implementations (Elasticsearch, Redis, in-memory) and testing.
/// </summary>
public interface IEnrichmentCache
{
	/// <summary>
	/// Initializes the cache, including any index bootstrapping and preloading.
	/// </summary>
	Task InitializeAsync(CancellationToken ct);

	/// <summary>
	/// Attempts to retrieve enrichment data from the cache.
	/// </summary>
	/// <param name="key">The content-addressable cache key.</param>
	/// <returns>The cached entry if found, null otherwise.</returns>
	CachedEnrichmentEntry? TryGet(string key);

	/// <summary>
	/// Stores enrichment data in the cache.
	/// </summary>
	/// <param name="key">The content-addressable cache key.</param>
	/// <param name="data">The enrichment data to store.</param>
	/// <param name="promptVersion">The prompt version used to generate this data.</param>
	/// <param name="ct">Cancellation token.</param>
	Task StoreAsync(string key, EnrichmentData data, int promptVersion, CancellationToken ct);

	/// <summary>
	/// Gets the number of entries currently in the cache.
	/// </summary>
	int Count { get; }
}
