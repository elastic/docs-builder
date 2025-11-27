// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;

namespace Elastic.Documentation.Api.Infrastructure.Caching;

/// <summary>
/// In-memory implementation of <see cref="IDistributedCache"/> for local development.
/// Uses ConcurrentDictionary for thread-safe storage with TTL-based expiration.
/// Clean Code: Sealed class (not meant for inheritance), single responsibility.
/// </summary>
public sealed class InMemoryDistributedCache : IDistributedCache
{
	private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();

	/// <summary>
	/// Immutable cache entry with value and expiration timestamp.
	/// Clean Code: Record type ensures immutability.
	/// </summary>
	private sealed record CacheEntry(string Value, DateTimeOffset ExpiresAt);

	public Task<string?> GetAsync(string key, Cancel ct = default)
	{
		if (_cache.TryGetValue(key, out var entry))
		{
			if (IsExpired(entry))
			{
				// Remove expired entry
				_ = _cache.TryRemove(key, out _);
				return Task.FromResult<string?>(null);
			}

			return Task.FromResult<string?>(entry.Value);
		}

		return Task.FromResult<string?>(null);
	}

	public Task SetAsync(string key, string value, TimeSpan ttl, Cancel ct = default)
	{
		var expiresAt = DateTimeOffset.UtcNow.Add(ttl);
		var entry = new CacheEntry(value, expiresAt);
		_ = _cache.AddOrUpdate(key, entry, (_, _) => entry);
		return Task.CompletedTask;
	}

	/// <summary>
	/// Checks if a cache entry has expired.
	/// Clean Code: Single-purpose helper method with intention-revealing name.
	/// </summary>
	private static bool IsExpired(CacheEntry entry) =>
		entry.ExpiresAt <= DateTimeOffset.UtcNow;
}
