// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using System.Globalization;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Elastic.Documentation.Api.Core;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Api.Infrastructure.Caching;

/// <summary>
/// DynamoDB implementation of <see cref="IDistributedCache"/> for Lambda environments.
/// Provides distributed caching across all Lambda containers using DynamoDB as backing store.
/// Clean Code: Constructor injection (Dependency Inversion), small focused methods.
/// </summary>
public sealed class DynamoDbDistributedCache(IAmazonDynamoDB dynamoDb, string tableName, ILogger<DynamoDbDistributedCache> logger) : IDistributedCache
{
	private static readonly ActivitySource ActivitySource = new(TelemetryConstants.CacheSourceName);

	private readonly IAmazonDynamoDB _dynamoDb = dynamoDb;
	private readonly string _tableName = tableName;
	private readonly ILogger<DynamoDbDistributedCache> _logger = logger;

	// DynamoDB attribute names
	private const string AttributeCacheKey = "CacheKey";
	private const string AttributeValue = "Value";
	private const string AttributeExpiresAt = "ExpiresAt";
	private const string AttributeTtl = "TTL";

	public async Task<string?> GetAsync(CacheKey key, Cancel ct = default)
	{
		var hashedKey = key.Value;
		using var activity = ActivitySource.StartActivity("get cache", ActivityKind.Client);
		_ = (activity?.SetTag("cache.key", hashedKey));
		_ = (activity?.SetTag("cache.table", _tableName));
		_ = (activity?.SetTag("cache.backend", "dynamodb"));

		try
		{
			var response = await _dynamoDb.GetItemAsync(new GetItemRequest
			{
				TableName = _tableName,
				Key = new Dictionary<string, AttributeValue>
				{
					[AttributeCacheKey] = new AttributeValue { S = hashedKey }
				}
			}, ct);

			if (!response.IsItemSet)
			{
				_ = (activity?.SetTag("cache.hit", false));
				_logger.LogDebug("Cache miss for key: {CacheKey}", hashedKey);
				return null;
			}

			// Check if expired (application-level check, DynamoDB TTL is for cleanup)
			if (IsExpired(response.Item))
			{
				_ = (activity?.SetTag("cache.hit", false));
				_ = (activity?.SetTag("cache.expired", true));
				_logger.LogDebug("Cache expired for key: {CacheKey}", hashedKey);
				return null;
			}

			var value = response.Item.TryGetValue(AttributeValue, out var valueAttr)
				? valueAttr.S
				: null;

			_ = (activity?.SetTag("cache.hit", value != null));
			if (value != null)
			{
				_logger.LogDebug("Cache hit for key: {CacheKey}", hashedKey);
			}

			return value;
		}
		catch (ResourceNotFoundException ex)
		{
			// Table doesn't exist - return null gracefully
			// Infrastructure should create table, but don't fail hard in dev
			_ = (activity?.SetTag("cache.error", "table_not_found"));
			_logger.LogWarning(ex, "DynamoDB table {TableName} not found. Cache operations will fail gracefully.", _tableName);
			return null;
		}
		catch (ProvisionedThroughputExceededException ex)
		{
			_ = (activity?.SetTag("cache.error", "provisioned_throughput_exceeded"));
			_logger.LogWarning(ex, "Provisioned throughput exceeded for DynamoDB cache table {TableName}.", _tableName);
			return null;
		}
		catch (InternalServerErrorException ex)
		{
			_ = (activity?.SetTag("cache.error", "internal_server_error"));
			_logger.LogError(ex, "Internal server error retrieving cache key {CacheKey} from DynamoDB", hashedKey);
			return null;
		}
		catch (Exception ex) when (ex is not OperationCanceledException && ex is not TaskCanceledException)
		{
			_ = (activity?.SetStatus(ActivityStatusCode.Error, ex.Message));
			_logger.LogError(ex, "Error retrieving cache key {CacheKey} from DynamoDB", hashedKey);
			return null; // Fail gracefully
		}
		// Allow cancellation exceptions to propagate to respect request lifetimes
	}

	public async Task SetAsync(CacheKey key, string value, TimeSpan ttl, Cancel ct = default)
	{
		var hashedKey = key.Value;
		using var activity = ActivitySource.StartActivity("set cache", ActivityKind.Client);
		_ = (activity?.SetTag("cache.key", hashedKey));
		_ = (activity?.SetTag("cache.table", _tableName));
		_ = (activity?.SetTag("cache.backend", "dynamodb"));
		_ = (activity?.SetTag("cache.ttl", ttl.TotalSeconds));

		try
		{
			var expiresAt = DateTimeOffset.UtcNow.Add(ttl);
			var ttlTimestamp = expiresAt.ToUnixTimeSeconds();

			_ = await _dynamoDb.PutItemAsync(new PutItemRequest
			{
				TableName = _tableName,
				Item = new Dictionary<string, AttributeValue>
				{
					[AttributeCacheKey] = new AttributeValue { S = hashedKey },
					[AttributeValue] = new AttributeValue { S = value },
					[AttributeExpiresAt] = new AttributeValue { N = ttlTimestamp.ToString(CultureInfo.InvariantCulture) },
					[AttributeTtl] = new AttributeValue { N = ttlTimestamp.ToString(CultureInfo.InvariantCulture) }
				}
			}, ct);

			_logger.LogDebug("Cache set for key: {CacheKey}, TTL: {TTL}s", hashedKey, ttl.TotalSeconds);
		}
		catch (ResourceNotFoundException ex)
		{
			// Table doesn't exist - fail silently in dev, log in production
			// Infrastructure should create table before deployment
			_ = (activity?.SetTag("cache.error", "table_not_found"));
			_logger.LogWarning(ex, "DynamoDB table {TableName} not found. Unable to cache key {CacheKey}.", _tableName, hashedKey);
		}
		catch (ProvisionedThroughputExceededException ex)
			_ = (activity?.SetTag("cache.error", "provisioned_throughput_exceeded"));
			_logger.LogWarning(ex, "Provisioned throughput exceeded for DynamoDB cache table {TableName}. Unable to cache key {CacheKey}.", _tableName, hashedKey);
		}
		catch (InternalServerErrorException ex)
		{
			_ = (activity?.SetTag("cache.error", "internal_server_error"));
			_logger.LogError(ex, "Internal server error setting cache key {CacheKey} in DynamoDB", hashedKey);
		}
		catch (Exception ex) when (ex is not OperationCanceledException && ex is not TaskCanceledException)
		{
		// Allow cancellation exceptions to propagate to respect request lifetimes
		{
			_ = (activity?.SetStatus(ActivityStatusCode.Error, ex.Message));
			_logger.LogError(ex, "Error setting cache key {CacheKey} in DynamoDB", hashedKey);
			// Fail gracefully - don't throw
		}
	}

	/// <summary>
	/// Checks if a DynamoDB item has expired based on ExpiresAt attribute.
	/// Clean Code: Single-purpose helper method with intention-revealing name.
	/// </summary>
	private static bool IsExpired(Dictionary<string, AttributeValue> item)
	{
		if (!item.TryGetValue(AttributeExpiresAt, out var expiresAtAttr))
			return true; // No expiration timestamp = treat as expired

		if (!long.TryParse(expiresAtAttr.N, out var expiresAtUnix))
			return true; // Invalid timestamp = treat as expired

		var expiresAt = DateTimeOffset.FromUnixTimeSeconds(expiresAtUnix);
		return expiresAt <= DateTimeOffset.UtcNow;
	}
}
