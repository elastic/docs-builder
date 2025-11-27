// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using System.Diagnostics;
using Elastic.Documentation.Api.Core;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Api.Infrastructure.Caching;

/// <summary>
/// Multi-layer cache decorator implementing L1 (in-memory) + L2 (distributed) caching strategy.
/// Optimizes Lambda warm-start performance while maintaining cross-container cache sharing.
/// Design Pattern: Decorator - Adds L1 layer to existing cache implementation.
/// Caching Strategy: Cache-aside (read-through) and write-through patterns.
/// </summary>
public sealed class MultiLayerCache(IDistributedCache l2Cache, ILogger<MultiLayerCache> logger) : IDistributedCache
{
	private static readonly ActivitySource ActivitySource = new(TelemetryConstants.CacheSourceName);

	// L1 Cache: Static in-memory cache survives across Lambda warm starts
	// Thread-safe for concurrent requests within same Lambda container
	private static readonly ConcurrentDictionary<string, L1CacheEntry> L1Cache = new();

	// L2 Cache: DynamoDB for cross-container sharing
	private readonly IDistributedCache _l2Cache = l2Cache;
	private readonly ILogger<MultiLayerCache> _logger = logger;

	/// <summary>
	/// Immutable L1 cache entry with value and expiration timestamp.
	/// Clean Code: Record type ensures immutability, intention-revealing name.
	/// </summary>
	private sealed record L1CacheEntry(string Value, DateTimeOffset ExpiresAt);

	public async Task<string?> GetAsync(CacheKey key, Cancel ct = default)
	{
		var hashedKey = key.Value;
		using var activity = ActivitySource.StartActivity("get cache", ActivityKind.Internal);
		_ = (activity?.SetTag("cache.key", hashedKey));
		_ = (activity?.SetTag("cache.backend", "multilayer"));

		// L1: Check in-memory cache first (fastest)
		if (TryGetFromL1(hashedKey, out var value))
		{
			_ = (activity?.SetTag("cache.l1.hit", true));
			_ = (activity?.SetTag("cache.l2.hit", false));
			_logger.LogDebug("L1 cache hit for key: {CacheKey}", hashedKey);
			return value;
		}

		_ = (activity?.SetTag("cache.l1.hit", false));

		// L2: Check distributed cache (DynamoDB)
		var l2Value = await _l2Cache.GetAsync(key, ct);

		if (l2Value != null)
		{
			_ = (activity?.SetTag("cache.l2.hit", true));
			_logger.LogDebug("L2 cache hit for key: {CacheKey}, populating L1", hashedKey);

			// Populate L1 cache for future requests in this container
			// Use a reasonable TTL for L1 (1 hour) to match ID token lifetime
			PopulateL1(hashedKey, l2Value, TimeSpan.FromHours(1));
		}
		else
		{
			_ = (activity?.SetTag("cache.l2.hit", false));
			_logger.LogDebug("Cache miss (L1 and L2) for key: {CacheKey}", hashedKey);
		}

		return l2Value;
	}

	public async Task SetAsync(CacheKey key, string value, TimeSpan ttl, Cancel ct = default)
	{
		var hashedKey = key.Value;
		using var activity = ActivitySource.StartActivity("set cache", ActivityKind.Internal);
		_ = (activity?.SetTag("cache.key", hashedKey));
		_ = (activity?.SetTag("cache.backend", "multilayer"));
		_ = (activity?.SetTag("cache.ttl", ttl.TotalSeconds));

		// Write-through: Update both L1 and L2
		PopulateL1(hashedKey, value, ttl);
		_logger.LogDebug("Writing to L1 and L2 cache for key: {CacheKey}, TTL: {TTL}s", hashedKey, ttl.TotalSeconds);

		await _l2Cache.SetAsync(key, value, ttl, ct);
	}

	/// <summary>
	/// Attempts to retrieve value from L1 cache.
	/// Clean Code: Single Responsibility - only handles L1 retrieval logic.
	/// </summary>
	private static bool TryGetFromL1(string key, out string? value)
	{
		if (L1Cache.TryGetValue(key, out var entry))
		{
			if (IsExpired(entry))
			{
				// Remove expired entry from L1
				_ = L1Cache.TryRemove(key, out _);
				value = null;
				return false;
			}

			value = entry.Value;
			return true;
		}

		value = null;
		return false;
	}

	/// <summary>
	/// Populates L1 cache with value and expiration.
	/// Clean Code: Single Responsibility - only handles L1 population logic.
	/// </summary>
	private static void PopulateL1(string key, string value, TimeSpan ttl)
	{
		var expiresAt = DateTimeOffset.UtcNow.Add(ttl);
		var entry = new L1CacheEntry(value, expiresAt);
		_ = L1Cache.AddOrUpdate(key, entry, (_, _) => entry);
	}

	/// <summary>
	/// Checks if L1 cache entry has expired.
	/// Clean Code: Single-purpose helper with intention-revealing name.
	/// </summary>
	private static bool IsExpired(L1CacheEntry entry) =>
		entry.ExpiresAt <= DateTimeOffset.UtcNow;
}
