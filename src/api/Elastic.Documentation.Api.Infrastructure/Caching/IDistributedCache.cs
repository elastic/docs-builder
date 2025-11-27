// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Api.Infrastructure.Caching;

/// <summary>
/// Abstraction for distributed caching across Lambda invocations.
/// Infrastructure concern: Used by other Infrastructure adapters for caching.
/// </summary>
public interface IDistributedCache
{
	/// <summary>
	/// Retrieves a cached value by key.
	/// </summary>
	/// <param name="key">Cache key following pattern: {category}:{identifier} (e.g., "idtoken:https://example.com")</param>
	/// <param name="ct">Cancellation token</param>
	/// <returns>Cached value as string, or null if not found or expired</returns>
	Task<string?> GetAsync(string key, Cancel ct = default);

	/// <summary>
	/// Stores a value in the cache with a time-to-live.
	/// </summary>
	/// <param name="key">Cache key following pattern: {category}:{identifier}</param>
	/// <param name="value">Value to cache (typically JSON-serialized data)</param>
	/// <param name="ttl">Time-to-live duration</param>
	/// <param name="ct">Cancellation token</param>
	Task SetAsync(string key, string value, TimeSpan ttl, Cancel ct = default);
}
