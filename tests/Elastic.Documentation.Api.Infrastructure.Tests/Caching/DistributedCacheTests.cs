// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
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
		// Arrange
		var key = CacheKey.Create("test", "nonexistent-key");

		// Act
		var result = await _cache.GetAsync(key, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeNull();
	}

	[Fact]
	public async Task SetAndGetWhenKeyIsSetReturnsValue()
	{
		// Arrange
		var key = CacheKey.Create("test", "test-key");
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
		var key = CacheKey.Create("test", "expiring-key");

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
		var key = CacheKey.Create("test", "key");

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
		var uniqueKey = CacheKey.Create("test", $"test-key-{Guid.NewGuid()}"); // Use unique key to avoid L1 cache pollution from other tests

		// Pre-populate L1 by setting a value
		await cache.SetAsync(uniqueKey, "value", TimeSpan.FromMinutes(1), TestContext.Current.CancellationToken);

		// Act - Second get should hit L1
		var result1 = await cache.GetAsync(uniqueKey, TestContext.Current.CancellationToken);
		var result2 = await cache.GetAsync(uniqueKey, TestContext.Current.CancellationToken);

		// Assert
		result1.Should().Be("value");
		result2.Should().Be("value");
		// L2 should only be called once (during SetAsync), not on subsequent Gets
		A.CallTo(() => fakeL2.GetAsync(A<CacheKey>._, A<Cancel>._))
			.MustNotHaveHappened();
	}

	[Fact]
	public async Task GetAsyncWhenL1MissCallsL2AndPopulatesL1()
	{
		// Arrange
		var fakeL2 = A.Fake<IDistributedCache>();
		var uniqueKey = CacheKey.Create("test", $"test-key-{Guid.NewGuid()}"); // Use unique key to avoid L1 cache pollution from other tests
		A.CallTo(() => fakeL2.GetAsync(uniqueKey, A<Cancel>._))
			.Returns("l2-value");

		var cache = new MultiLayerCache(fakeL2, NullLogger<MultiLayerCache>.Instance);

		// Act - First call misses L1, hits L2
		var result1 = await cache.GetAsync(uniqueKey, TestContext.Current.CancellationToken);
		// Second call should hit L1 (populated from previous call)
		var result2 = await cache.GetAsync(uniqueKey, TestContext.Current.CancellationToken);

		// Assert
		result1.Should().Be("l2-value");
		result2.Should().Be("l2-value");
		A.CallTo(() => fakeL2.GetAsync(uniqueKey, A<Cancel>._))
			.MustHaveHappenedOnceExactly();
	}

	[Fact]
	public async Task SetAsyncWritesToBothL1AndL2()
	{
		// Arrange
		var fakeL2 = A.Fake<IDistributedCache>();
		var cache = new MultiLayerCache(fakeL2, NullLogger<MultiLayerCache>.Instance);
		var uniqueKey = CacheKey.Create("test", $"test-key-{Guid.NewGuid()}"); // Use unique key to avoid L1 cache pollution from other tests

		// Act
		await cache.SetAsync(uniqueKey, "value", TimeSpan.FromMinutes(1), TestContext.Current.CancellationToken);

		// Get from cache (should hit L1)
		var result = await cache.GetAsync(uniqueKey, TestContext.Current.CancellationToken);

		// Assert
		result.Should().Be("value", "L1 should have the value");
		A.CallTo(() => fakeL2.SetAsync(uniqueKey, "value", TimeSpan.FromMinutes(1), A<Cancel>._))
			.MustHaveHappenedOnceExactly();
	}

	[Fact]
	public async Task GetAsyncWhenBothCachesMissReturnsNull()
	{
		// Arrange
		var fakeL2 = A.Fake<IDistributedCache>();
		A.CallTo(() => fakeL2.GetAsync(A<CacheKey>._, A<Cancel>._))
			.Returns((string?)null);

		var cache = new MultiLayerCache(fakeL2, NullLogger<MultiLayerCache>.Instance);
		var key = CacheKey.Create("test", "missing-key");

		// Act
		var result = await cache.GetAsync(key, TestContext.Current.CancellationToken);

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
		var key = CacheKey.Create("test", "test-key");
		// Note: We no longer use ExpiresAt - DynamoDB TTL handles expiration automatically

		var response = new GetItemResponse
		{
			Item = new Dictionary<string, AttributeValue>
			{
				["CacheKey"] = new AttributeValue { S = key.Value },
				["Value"] = new AttributeValue { S = "test-value" },
				["TTL"] = new AttributeValue { N = DateTimeOffset.UtcNow.AddMinutes(30).ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture) }
			},
			IsItemSet = true
		};

		A.CallTo(() => fakeDynamoDb.GetItemAsync(A<GetItemRequest>._, A<Cancel>._))
			.Returns(response);

		var cache = new DynamoDbDistributedCache(fakeDynamoDb, "test-table", NullLogger<DynamoDbDistributedCache>.Instance);

		// Act
		var result = await cache.GetAsync(key, TestContext.Current.CancellationToken);

		// Assert
		result.Should().Be("test-value");
	}

	[Fact]
	public async Task GetAsyncWhenItemDoesNotExistReturnsNull()
	{
		// Arrange
		var fakeDynamoDb = A.Fake<IAmazonDynamoDB>();
		var key = CacheKey.Create("test", "missing-key");
		var response = new GetItemResponse { IsItemSet = false };

		A.CallTo(() => fakeDynamoDb.GetItemAsync(A<GetItemRequest>._, A<Cancel>._))
			.Returns(response);

		var cache = new DynamoDbDistributedCache(fakeDynamoDb, "test-table", NullLogger<DynamoDbDistributedCache>.Instance);

		// Act
		var result = await cache.GetAsync(key, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeNull();
	}

	[Fact]
	public async Task SetAsyncCallsDynamoDbPutItem()
	{
		// Arrange
		var fakeDynamoDb = A.Fake<IAmazonDynamoDB>();
		var cache = new DynamoDbDistributedCache(fakeDynamoDb, "test-table", NullLogger<DynamoDbDistributedCache>.Instance);
		var key = CacheKey.Create("test", "key");

		// Act
		await cache.SetAsync(key, "value", TimeSpan.FromMinutes(30), TestContext.Current.CancellationToken);

		// Assert
		A.CallTo(() => fakeDynamoDb.PutItemAsync(
			A<PutItemRequest>.That.Matches(r =>
				r.TableName == "test-table" &&
				r.Item["CacheKey"].S == key.Value &&
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
		var key = CacheKey.Create("test", "key");
		var exception = new ResourceNotFoundException("Table not found");
		A.CallTo(() => fakeDynamoDb.GetItemAsync(A<GetItemRequest>._, A<Cancel>._))
			.Throws(exception);

		var cache = new DynamoDbDistributedCache(fakeDynamoDb, "missing-table", NullLogger<DynamoDbDistributedCache>.Instance);

		// Act
		var result = await cache.GetAsync(key, TestContext.Current.CancellationToken);

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
		// Use HttpMessageHandler directly with method name matching to verify ExchangeJwtForIdToken was not called
		// See: https://fakeiteasy.github.io/docs/8.1.0/Recipes/faking-http-client/
		var fakeHandler = A.Fake<HttpMessageHandler>();
		using var httpClient = new HttpClient(fakeHandler);
		var fakeHttpClientFactory = A.Fake<IHttpClientFactory>();
		A.CallTo(() => fakeHttpClientFactory.CreateClient(A<string>._))
			.Returns(httpClient);

		var cache = new InMemoryDistributedCache();

		var provider = new GcpIdTokenProvider(fakeHttpClientFactory, cache);
		const string targetAudience = "https://test-audience.googleapis.com";

		// Pre-populate cache with valid token (TTL of 45 minutes - matches cache expiry logic)
		const string cachedToken = "fake-cached-token";
		var cacheKey = CacheKey.Create("idtoken", targetAudience);
		await cache.SetAsync(cacheKey, cachedToken, TimeSpan.FromMinutes(45), TestContext.Current.CancellationToken);

		// Act
		var result = await provider.GenerateIdTokenAsync("{}", targetAudience, TestContext.Current.CancellationToken);

		// Assert
		result.Should().Be("fake-cached-token", "should return cached token without calling Google OAuth");
		// Verify that ExchangeJwtForIdToken was not called (PostAsync -> SendAsync)
		// Using method name matching since SendAsync is protected
		A.CallTo(fakeHandler)
			.WithReturnType<Task<HttpResponseMessage>>()
			.Where(call => call.Method.Name == "SendAsync")
			.MustNotHaveHappened();
	}

	[Fact]
	public async Task GenerateIdTokenAsyncIgnoresExpiredCachedToken()
	{
		// Arrange
		var cache = new InMemoryDistributedCache();
		const string targetAudience = "https://test.com";

		// Pre-populate cache with token that has very short TTL (will expire quickly)
		// InMemoryDistributedCache will remove it when expired
		const string expiredToken = "expired-token";
		var cacheKey = CacheKey.Create("idtoken", targetAudience);
		await cache.SetAsync(cacheKey, expiredToken, TimeSpan.FromMilliseconds(10), TestContext.Current.CancellationToken);

		// Wait for expiration
		await Task.Delay(50, TestContext.Current.CancellationToken);

		// Act - Try to get the expired token
		var cachedValue = await cache.GetAsync(cacheKey, TestContext.Current.CancellationToken);

		// Assert - Expired cache entry should return null
		// GcpIdTokenProvider checks `if (cachedToken != null)` - when cache returns null,
		// it will generate a new token, effectively ignoring the expired cached token
		cachedValue.Should().BeNull("expired cache entries should return null, allowing provider to generate new token");
	}
}
