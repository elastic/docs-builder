// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using System.Text.Json.Serialization;
using Elastic.Transport;
using Elastic.Transport.Products.Elasticsearch;
using Microsoft.Extensions.Logging;

namespace Elastic.Markdown.Exporters.Elasticsearch.Enrichment;

/// <summary>
/// Elasticsearch-backed implementation of <see cref="IEnrichmentCache"/>.
/// Pre-loads all entries into memory at startup for fast lookups.
/// </summary>
public sealed class ElasticsearchEnrichmentCache(
	DistributedTransport transport,
	ILogger<ElasticsearchEnrichmentCache> logger,
	string indexName = "docs-ai-enriched-fields-cache") : IEnrichmentCache
{
	private readonly DistributedTransport _transport = transport;
	private readonly ILogger _logger = logger;
	private readonly string _indexName = indexName;
	private readonly Dictionary<string, CachedEnrichmentEntry> _cache = [];

	private const int ScrollBatchSize = 1000;
	private const string ScrollTimeout = "1m";

	// language=json
	private const string IndexMapping = """
		{
			"settings": {
				"number_of_shards": 1,
				"number_of_replicas": 1
			},
			"mappings": {
				"properties": {
					"response_json": { "type": "text", "index": false },
					"created_at": { "type": "date" },
					"prompt_version": { "type": "integer" }
				}
			}
		}
		""";

	public int Count => _cache.Count;

	public async Task InitializeAsync(CancellationToken ct)
	{
		await EnsureIndexExistsAsync(ct);
		await PreloadCacheAsync(ct);
	}

	public CachedEnrichmentEntry? TryGet(string key) =>
		_cache.TryGetValue(key, out var entry) ? entry : null;

	public async Task StoreAsync(string key, EnrichmentData data, int promptVersion, CancellationToken ct)
	{
		_cache[key] = new CachedEnrichmentEntry(data, promptVersion);
		await PersistToElasticsearchAsync(key, data, promptVersion, ct);
	}

	private async Task PersistToElasticsearchAsync(string key, EnrichmentData data, int promptVersion, CancellationToken ct)
	{
		try
		{
			var responseJson = JsonSerializer.Serialize(data, EnrichmentSerializerContext.Default.EnrichmentData);
			var cacheItem = new CachedEnrichment
			{
				ResponseJson = responseJson,
				CreatedAt = DateTimeOffset.UtcNow,
				PromptVersion = promptVersion
			};

			var body = JsonSerializer.Serialize(cacheItem, EnrichmentSerializerContext.Default.CachedEnrichment);
			var response = await _transport.PutAsync<StringResponse>(
				$"{_indexName}/_doc/{key}",
				PostData.String(body),
				ct);

			if (!response.ApiCallDetails.HasSuccessfulStatusCode)
				_logger.LogWarning("Failed to persist cache entry: {StatusCode}", response.ApiCallDetails.HttpStatusCode);
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Failed to persist cache entry for key {Key}", key);
		}
	}

	private async Task EnsureIndexExistsAsync(CancellationToken ct)
	{
		var existsResponse = await _transport.HeadAsync(_indexName, ct);
		if (existsResponse.ApiCallDetails.HasSuccessfulStatusCode)
			return;

		var createResponse = await _transport.PutAsync<StringResponse>(
			_indexName,
			PostData.String(IndexMapping),
			ct);

		if (createResponse.ApiCallDetails.HasSuccessfulStatusCode)
			_logger.LogInformation("Created cache index {IndexName}", _indexName);
		else if (createResponse.ApiCallDetails.HttpStatusCode != 400) // 400 = already exists
			_logger.LogWarning("Failed to create cache index: {StatusCode}", createResponse.ApiCallDetails.HttpStatusCode);
	}

	private async Task PreloadCacheAsync(CancellationToken ct)
	{
		var sw = System.Diagnostics.Stopwatch.StartNew();

		if (!await HasDocumentsAsync(ct))
		{
			_logger.LogInformation("Cache index is empty, skipping preload ({ElapsedMs}ms)", sw.ElapsedMilliseconds);
			return;
		}

		await ScrollAllDocumentsAsync(ct);

		sw.Stop();
		_logger.LogInformation("Pre-loaded {Count} cache entries in {ElapsedMs}ms", _cache.Count, sw.ElapsedMilliseconds);
	}

	private async Task<bool> HasDocumentsAsync(CancellationToken ct)
	{
		var countResponse = await _transport.GetAsync<DynamicResponse>($"{_indexName}/_count", ct);
		if (!countResponse.ApiCallDetails.HasSuccessfulStatusCode)
		{
			_logger.LogWarning("Cache count failed: {StatusCode}", countResponse.ApiCallDetails.HttpStatusCode);
			return true; // Assume there might be documents
		}

		var docCount = countResponse.Body.Get<long?>("count") ?? 0;
		_logger.LogDebug("Cache index has {Count} documents", docCount);
		return docCount > 0;
	}

	private async Task ScrollAllDocumentsAsync(CancellationToken ct)
	{
		var scrollQuery = "{\"size\": " + ScrollBatchSize + ", \"query\": {\"match_all\": {}}}";

		var searchResponse = await _transport.PostAsync<DynamicResponse>(
			$"{_indexName}/_search?scroll={ScrollTimeout}",
			PostData.String(scrollQuery),
			ct);

		if (!searchResponse.ApiCallDetails.HasSuccessfulStatusCode)
		{
			_logger.LogWarning("Failed to start cache scroll: {StatusCode}", searchResponse.ApiCallDetails.HttpStatusCode);
			return;
		}

		_ = ProcessHits(searchResponse);
		var scrollId = searchResponse.Body.Get<string>("_scroll_id");

		while (scrollId is not null)
		{
			var scrollBody = $$"""{"scroll": "{{ScrollTimeout}}", "scroll_id": "{{scrollId}}"}""";
			var scrollResponse = await _transport.PostAsync<DynamicResponse>(
				"_search/scroll",
				PostData.String(scrollBody),
				ct);

			if (!scrollResponse.ApiCallDetails.HasSuccessfulStatusCode)
				break;

			var hitsCount = ProcessHits(scrollResponse);
			if (hitsCount == 0)
				break;

			scrollId = scrollResponse.Body.Get<string>("_scroll_id");
		}
	}

	private int ProcessHits(DynamicResponse response)
	{
		if (response.ApiCallDetails.ResponseBodyInBytes is not { } responseBytes)
			return 0;

		using var doc = JsonDocument.Parse(responseBytes);
		if (!doc.RootElement.TryGetProperty("hits", out var hitsObj) ||
			!hitsObj.TryGetProperty("hits", out var hitsArray))
			return 0;

		var count = 0;
		foreach (var hit in hitsArray.EnumerateArray())
		{
			if (TryParseHit(hit, out var id, out var entry))
			{
				_cache[id] = entry;
				count++;
			}
		}
		return count;
	}

	private static bool TryParseHit(JsonElement hit, out string id, out CachedEnrichmentEntry entry)
	{
		id = string.Empty;
		entry = default!;

		if (!hit.TryGetProperty("_id", out var idProp) || idProp.GetString() is not { } docId)
			return false;

		if (!hit.TryGetProperty("_source", out var source))
			return false;

		if (!source.TryGetProperty("response_json", out var jsonProp) ||
			jsonProp.GetString() is not { Length: > 0 } responseJson)
			return false;

		var promptVersion = source.TryGetProperty("prompt_version", out var versionProp)
			? versionProp.GetInt32()
			: 0;

		var data = JsonSerializer.Deserialize(responseJson, EnrichmentSerializerContext.Default.EnrichmentData);
		if (data is not { HasData: true })
			return false;

		id = docId;
		entry = new CachedEnrichmentEntry(data, promptVersion);
		return true;
	}
}

/// <summary>
/// Wrapper for storing enrichment data in the cache index.
/// </summary>
public sealed record CachedEnrichment
{
	[JsonPropertyName("response_json")]
	public required string ResponseJson { get; init; }

	[JsonPropertyName("created_at")]
	public required DateTimeOffset CreatedAt { get; init; }

	[JsonPropertyName("prompt_version")]
	public required int PromptVersion { get; init; }
}
