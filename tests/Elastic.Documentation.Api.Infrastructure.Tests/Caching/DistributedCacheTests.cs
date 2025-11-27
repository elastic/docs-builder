// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Elastic.Documentation.Api.Infrastructure.Caching;
using Elastic.Documentation.Api.Infrastructure.Gcp;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elastic.Documentation.Api.Infrastructure.Tests.Caching;

public class InMemoryDistributedCacheTests
{
	private readonly InMemoryDistributedCache _cache = new();

	[Fact]
	public async Task GetAsyncWhenKeyDoesNotExistReturnsNull()
	{
		// Act
		var result = await _cache.GetAsync("nonexistent-key", TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeNull();
	}

	[Fact]
	public async Task SetAndGetWhenKeyIsSetReturnsValue()
	{
		// Arrange
		const string key = "test-key";
		const string value = "test-value";

		// Act
		await _cache.SetAsync(key, value, TimeSpan.FromMinutes(1), TestContext.Current.CancellationToken);
		var result = await _cache.GetAsync(key, TestContext.Current.CancellationToken);

		// Assert
		result.Should().Be(value);
	}

	[Fact]
	public async Task GetAsyncWhenEntryExpiredReturnsNull()
	{
		// Arrange
		const string key = "expiring-key";

		// Act
		await _cache.SetAsync(key, "value", TimeSpan.FromMilliseconds(10), TestContext.Current.CancellationToken);
		await Task.Delay(50, TestContext.Current.CancellationToken);
		var result = await _cache.GetAsync(key, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeNull("expired entries should be removed");
	}

	[Fact]
	public async Task SetAsyncOverwritesExistingValue()
	{
		// Arrange
		const string key = "key";

		// Act
		await _cache.SetAsync(key, "first", TimeSpan.FromMinutes(1), TestContext.Current.CancellationToken);
		await _cache.SetAsync(key, "second", TimeSpan.FromMinutes(1), TestContext.Current.CancellationToken);
		var result = await _cache.GetAsync(key, TestContext.Current.CancellationToken);

		// Assert
		result.Should().Be("second");
	}
}

public class MultiLayerCacheTests
{
	[Fact]
	public async Task GetAsyncWhenL1HitDoesNotCallL2Again()
	{
		// Arrange
		var fakeL2 = A.Fake<IDistributedCache>();
		var cache = new MultiLayerCache(fakeL2, NullLogger<MultiLayerCache>.Instance);

		// Pre-populate L1 by setting a value
		await cache.SetAsync("key", "value", TimeSpan.FromMinutes(1), TestContext.Current.CancellationToken);

		// Act - Second get should hit L1
		var result1 = await cache.GetAsync("key", TestContext.Current.CancellationToken);
		var result2 = await cache.GetAsync("key", TestContext.Current.CancellationToken);

		// Assert
		result1.Should().Be("value");
		result2.Should().Be("value");
		// L2 should only be called once (during SetAsync), not on subsequent Gets
		A.CallTo(() => fakeL2.GetAsync(A<string>._, A<Cancel>._))
			.MustNotHaveHappened();
	}

	[Fact]
	public async Task GetAsyncWhenL1MissCallsL2AndPopulatesL1()
	{
		// Arrange
		var fakeL2 = A.Fake<IDistributedCache>();
		A.CallTo(() => fakeL2.GetAsync("key", A<Cancel>._))
			.Returns("l2-value");

		var cache = new MultiLayerCache(fakeL2, NullLogger<MultiLayerCache>.Instance);

		// Act - First call misses L1, hits L2
		var result1 = await cache.GetAsync("key", TestContext.Current.CancellationToken);
		// Second call should hit L1 (populated from previous call)
		var result2 = await cache.GetAsync("key", TestContext.Current.CancellationToken);

		// Assert
		result1.Should().Be("l2-value");
		result2.Should().Be("l2-value");
		A.CallTo(() => fakeL2.GetAsync("key", A<Cancel>._))
			.MustHaveHappenedOnceExactly();
	}

	[Fact]
	public async Task SetAsyncWritesToBothL1AndL2()
	{
		// Arrange
		var fakeL2 = A.Fake<IDistributedCache>();
		var cache = new MultiLayerCache(fakeL2, NullLogger<MultiLayerCache>.Instance);

		// Act
		await cache.SetAsync("key", "value", TimeSpan.FromMinutes(1), TestContext.Current.CancellationToken);

		// Get from cache (should hit L1)
		var result = await cache.GetAsync("key", TestContext.Current.CancellationToken);

		// Assert
		result.Should().Be("value", "L1 should have the value");
		A.CallTo(() => fakeL2.SetAsync("key", "value", TimeSpan.FromMinutes(1), A<Cancel>._))
			.MustHaveHappenedOnceExactly();
	}

	[Fact]
	public async Task GetAsyncWhenBothCachesMissReturnsNull()
	{
		// Arrange
		var fakeL2 = A.Fake<IDistributedCache>();
		A.CallTo(() => fakeL2.GetAsync(A<string>._, A<Cancel>._))
			.Returns((string?)null);

		var cache = new MultiLayerCache(fakeL2, NullLogger<MultiLayerCache>.Instance);

		// Act
		var result = await cache.GetAsync("missing-key", TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeNull();
	}
}

public class DynamoDbDistributedCacheTests
{
	[Fact]
	public async Task GetAsyncWhenItemExistsReturnsValue()
	{
		// Arrange
		var fakeDynamoDb = A.Fake<IAmazonDynamoDB>();
		var expiresAt = DateTimeOffset.UtcNow.AddMinutes(30).ToUnixTimeSeconds();

		var response = new GetItemResponse
		{
			Item = new Dictionary<string, AttributeValue>
			{
				["CacheKey"] = new AttributeValue { S = "test-key" },
				["Value"] = new AttributeValue { S = "test-value" },
				["ExpiresAt"] = new AttributeValue { N = expiresAt.ToString() }
			},
			IsItemSet = true
		};

		A.CallTo(() => fakeDynamoDb.GetItemAsync(A<GetItemRequest>._, A<Cancel>._))
			.Returns(response);

		var cache = new DynamoDbDistributedCache(fakeDynamoDb, "test-table", NullLogger<DynamoDbDistributedCache>.Instance);

		// Act
		var result = await cache.GetAsync("test-key", TestContext.Current.CancellationToken);

		// Assert
		result.Should().Be("test-value");
	}

	[Fact]
	public async Task GetAsyncWhenItemExpiredReturnsNull()
	{
		// Arrange
		var fakeDynamoDb = A.Fake<IAmazonDynamoDB>();
		var expiresAt = DateTimeOffset.UtcNow.AddMinutes(-5).ToUnixTimeSeconds(); // Expired 5 min ago

		var response = new GetItemResponse
		{
			Item = new Dictionary<string, AttributeValue>
			{
				["CacheKey"] = new AttributeValue { S = "test-key" },
				["Value"] = new AttributeValue { S = "test-value" },
				["ExpiresAt"] = new AttributeValue { N = expiresAt.ToString() }
			},
			IsItemSet = true
		};

		A.CallTo(() => fakeDynamoDb.GetItemAsync(A<GetItemRequest>._, A<Cancel>._))
			.Returns(response);

		var cache = new DynamoDbDistributedCache(fakeDynamoDb, "test-table", NullLogger<DynamoDbDistributedCache>.Instance);

		// Act
		var result = await cache.GetAsync("test-key", TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeNull("expired items should not be returned");
	}

	[Fact]
	public async Task GetAsyncWhenItemDoesNotExistReturnsNull()
	{
		// Arrange
		var fakeDynamoDb = A.Fake<IAmazonDynamoDB>();
		var response = new GetItemResponse { IsItemSet = false };

		A.CallTo(() => fakeDynamoDb.GetItemAsync(A<GetItemRequest>._, A<Cancel>._))
			.Returns(response);

		var cache = new DynamoDbDistributedCache(fakeDynamoDb, "test-table", NullLogger<DynamoDbDistributedCache>.Instance);

		// Act
		var result = await cache.GetAsync("missing-key", TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeNull();
	}

	[Fact]
	public async Task SetAsyncCallsDynamoDbPutItem()
	{
		// Arrange
		var fakeDynamoDb = A.Fake<IAmazonDynamoDB>();
		var cache = new DynamoDbDistributedCache(fakeDynamoDb, "test-table", NullLogger<DynamoDbDistributedCache>.Instance);

		// Act
		await cache.SetAsync("key", "value", TimeSpan.FromMinutes(30), TestContext.Current.CancellationToken);

		// Assert
		A.CallTo(() => fakeDynamoDb.PutItemAsync(
			A<PutItemRequest>.That.Matches(r =>
				r.TableName == "test-table" &&
				r.Item["CacheKey"].S == "key" &&
				r.Item["Value"].S == "value"
			),
			A<Cancel>._
		)).MustHaveHappenedOnceExactly();
	}

	[Fact]
	public async Task GetAsyncWhenTableNotFoundReturnsNullGracefully()
	{
		// Arrange
		var fakeDynamoDb = A.Fake<IAmazonDynamoDB>();
		var exception = new ResourceNotFoundException("Table not found");
		A.CallTo(() => fakeDynamoDb.GetItemAsync(A<GetItemRequest>._, A<Cancel>._))
			.Throws(exception);

		var cache = new DynamoDbDistributedCache(fakeDynamoDb, "missing-table", NullLogger<DynamoDbDistributedCache>.Instance);

		// Act
		var result = await cache.GetAsync("key", TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeNull("should handle missing table gracefully");
	}
}

public class GcpIdTokenProviderCachingIntegrationTests
{
	[Fact]
	public async Task GenerateIdTokenAsyncUsesCachedTokenWhenValid()
	{
		// Arrange
		var fakeHttpClientFactory = A.Fake<IHttpClientFactory>();
		var cache = new InMemoryDistributedCache();

		var provider = new GcpIdTokenProvider(fakeHttpClientFactory, cache);
		const string targetAudience = "https://test-audience.googleapis.com";

		// Pre-populate cache with valid token (expires in 50 minutes)
		var cachedToken = new
		{
			token = "fake-cached-token",
			expiresAtUnix = DateTimeOffset.UtcNow.AddMinutes(50).ToUnixTimeSeconds()
		};
		var cacheJson = JsonSerializer.Serialize(cachedToken);
		await cache.SetAsync($"idtoken:{targetAudience}", cacheJson, TimeSpan.FromHours(1), TestContext.Current.CancellationToken);

		// Act
		var result = await provider.GenerateIdTokenAsync("{}", targetAudience, TestContext.Current.CancellationToken);

		// Assert
		result.Should().Be("fake-cached-token", "should return cached token without calling Google OAuth");
		A.CallTo(() => fakeHttpClientFactory.CreateClient()).MustNotHaveHappened();
	}

	[Fact]
	public async Task GenerateIdTokenAsyncIgnoresExpiredCachedToken()
	{
		// Arrange
		var cache = new InMemoryDistributedCache();

		// Pre-populate cache with expired token (already past 45-minute threshold)
		var expiredToken = new
		{
			token = "expired-token",
			expiresAtUnix = DateTimeOffset.UtcNow.AddSeconds(30).ToUnixTimeSeconds() // Only 30s left
		};
		var cacheJson = JsonSerializer.Serialize(expiredToken);
		await cache.SetAsync("idtoken:https://test.com", cacheJson, TimeSpan.FromHours(1), TestContext.Current.CancellationToken);

		// Act - Try to get the expired token
		var cachedValue = await cache.GetAsync("idtoken:https://test.com", TestContext.Current.CancellationToken);
		var parsedToken = JsonSerializer.Deserialize<JsonElement>(cachedValue!);
		var expiresAt = DateTimeOffset.FromUnixTimeSeconds(parsedToken.GetProperty("expiresAtUnix").GetInt64());

		// Assert - Token should be considered expired (less than 1 minute buffer)
		(expiresAt <= DateTimeOffset.UtcNow.AddMinutes(1)).Should().BeTrue(
			"tokens with less than 1 minute remaining should be refreshed");
	}
}
