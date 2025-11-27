// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Api.Infrastructure.Caching;

/// <summary>
/// Abstraction for distributed caching across Lambda invocations.
/// Infrastructure concern: Used by other Infrastructure adapters for caching.
/// </summary>
/// <remarks>
/// <para>
/// Cache keys should be created using <see cref="CacheKey.Create"/> to automatically hash
/// sensitive identifiers and prevent exposing sensitive data in cache keys (CodeQL security requirement).
/// </para>
/// <para>
/// Key format: {category}:{hashed-identifier} (e.g., "idtoken:{hash}" where hash is SHA256 of the identifier)
/// </para>
/// </remarks>
public interface IDistributedCache
{
	/// <summary>
	/// Retrieves a cached value by key.
	/// </summary>
	/// <param name="key">Cache key created using <see cref="CacheKey.Create"/> (format: {category}:{hashed-identifier})</param>
	/// <param name="ct">Cancellation token</param>
	/// <returns>Cached value as string, or null if not found or expired</returns>
	Task<string?> GetAsync(CacheKey key, Cancel ct = default);

	/// <summary>
	/// Stores a value in the cache with a time-to-live.
	/// </summary>
	/// <param name="key">Cache key created using <see cref="CacheKey.Create"/> (format: {category}:{hashed-identifier})</param>
	/// <param name="value">Value to cache (typically JSON-serialized data)</param>
	/// <param name="ttl">Time-to-live duration</param>
	/// <param name="ct">Cancellation token</param>
	Task SetAsync(CacheKey key, string value, TimeSpan ttl, Cancel ct = default);
}
