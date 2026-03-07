// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace CrawlIndexer.Caching;

/// <summary>
/// Interface for loading cached document metadata from Elasticsearch.
/// </summary>
public interface ICrawlCache
{
	/// <summary>
	/// Loads all cached documents from the index using PIT API for consistency.
	/// </summary>
	Task<Dictionary<string, CachedDocInfo>> LoadCacheAsync(
		string indexAlias,
		IProgress<(int loaded, string? currentUrl)>? progress = null,
		CancellationToken ct = default
	);

	/// <summary>
	/// Checks if the index exists (first run detection).
	/// </summary>
	Task<bool> IndexExistsAsync(string indexAlias, CancellationToken ct = default);
}
