// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using Elastic.Transport;
using Elastic.Transport.Products.Elasticsearch;
using Microsoft.Extensions.Logging;

namespace Elastic.Markdown.Exporters.Elasticsearch.Enrichment;

/// <summary>
/// Elasticsearch-backed enrichment cache for use with the enrich processor.
/// Stores AI-generated enrichment fields directly (not as JSON string) for efficient lookups.
/// </summary>
public sealed class ElasticsearchEnrichmentCache(
	DistributedTransport transport,
	ILogger<ElasticsearchEnrichmentCache> logger,
	string indexName = "docs-ai-enriched-fields-cache") : IEnrichmentCache
{
	private readonly DistributedTransport _transport = transport;
	private readonly ILogger _logger = logger;
	private readonly ConcurrentDictionary<string, byte> _existingHashes = new();

	public string IndexName { get; } = indexName;

	// language=json
	// Note: No settings block - Serverless doesn't allow number_of_shards/replicas
	private const string IndexMapping = """
		{
			"mappings": {
				"properties": {
					"content_hash": { "type": "keyword" },
					"ai_rag_optimized_summary": { "type": "text" },
					"ai_short_summary": { "type": "text" },
					"ai_search_query": { "type": "text" },
					"ai_questions": { "type": "text" },
					"ai_use_cases": { "type": "text" },
					"created_at": { "type": "date" },
					"prompt_version": { "type": "integer" }
				}
			}
		}
		""";

	public int Count => _existingHashes.Count;

	public async Task InitializeAsync(CancellationToken ct)
	{
		await EnsureIndexExistsAsync(ct);
		await LoadExistingHashesAsync(ct);
	}

	public bool Exists(string contentHash) => _existingHashes.ContainsKey(contentHash);

	public Task<EnrichmentData?> GetAsync(string contentHash, CancellationToken ct) =>
		Task.FromResult<EnrichmentData?>(null); // Not used - enrich processor handles cache hits

	public async Task StoreAsync(string contentHash, EnrichmentData data, int promptVersion, CancellationToken ct)
	{
		var cacheEntry = new CacheIndexEntry
		{
			ContentHash = contentHash,
			AiRagOptimizedSummary = data.RagOptimizedSummary,
			AiShortSummary = data.ShortSummary,
			AiSearchQuery = data.SearchQuery,
			AiQuestions = data.Questions,
			AiUseCases = data.UseCases,
			CreatedAt = DateTimeOffset.UtcNow,
			PromptVersion = promptVersion
		};

		var body = JsonSerializer.Serialize(cacheEntry, EnrichmentSerializerContext.Default.CacheIndexEntry);
		var response = await _transport.PutAsync<StringResponse>(
			$"{IndexName}/_doc/{contentHash}",
			PostData.String(body),
			ct);

		if (response.ApiCallDetails.HasSuccessfulStatusCode)
			_ = _existingHashes.TryAdd(contentHash, 0);
		else
			_logger.LogWarning("Failed to store enrichment: {StatusCode}", response.ApiCallDetails.HttpStatusCode);
	}

	private async Task EnsureIndexExistsAsync(CancellationToken ct)
	{
		var existsResponse = await _transport.HeadAsync(IndexName, ct);
		if (existsResponse.ApiCallDetails.HasSuccessfulStatusCode)
		{
			_logger.LogDebug("Enrichment cache index {IndexName} already exists", IndexName);
			return;
		}

		_logger.LogInformation("Creating enrichment cache index {IndexName}...", IndexName);
		var createResponse = await _transport.PutAsync<StringResponse>(
			IndexName,
			PostData.String(IndexMapping),
			ct);

		if (createResponse.ApiCallDetails.HasSuccessfulStatusCode)
			_logger.LogInformation("Created enrichment cache index {IndexName}", IndexName);
		else if (createResponse.ApiCallDetails.HttpStatusCode == 400 &&
				 createResponse.Body?.Contains("resource_already_exists_exception") == true)
			_logger.LogDebug("Enrichment cache index {IndexName} already exists (race condition)", IndexName);
		else
			_logger.LogError("Failed to create cache index: {StatusCode} - {Response}",
				createResponse.ApiCallDetails.HttpStatusCode, createResponse.Body);
	}

	private async Task LoadExistingHashesAsync(CancellationToken ct)
	{
		var sw = System.Diagnostics.Stopwatch.StartNew();

		// Only fetch _id to minimize memory - we use _id as the hash
		var scrollQuery = /*lang=json,strict*/ """{"size": 1000, "_source": false, "query": {"match_all": {}}}""";

		var searchResponse = await _transport.PostAsync<StringResponse>(
			$"{IndexName}/_search?scroll=1m",
			PostData.String(scrollQuery),
			ct);

		if (!searchResponse.ApiCallDetails.HasSuccessfulStatusCode)
		{
			_logger.LogWarning("Failed to load existing hashes: {StatusCode}", searchResponse.ApiCallDetails.HttpStatusCode);
			return;
		}

		var (count, scrollId) = ProcessHashHits(searchResponse.Body);

		while (scrollId is not null && count > 0)
		{
			var scrollBody = $$"""{"scroll": "1m", "scroll_id": "{{scrollId}}"}""";
			var scrollResponse = await _transport.PostAsync<StringResponse>(
				"_search/scroll",
				PostData.String(scrollBody),
				ct);

			if (!scrollResponse.ApiCallDetails.HasSuccessfulStatusCode)
				break;

			(count, scrollId) = ProcessHashHits(scrollResponse.Body);
		}

		_logger.LogInformation("Loaded {Count} existing enrichment hashes in {ElapsedMs}ms",
			_existingHashes.Count, sw.ElapsedMilliseconds);
	}

	private (int count, string? scrollId) ProcessHashHits(string? responseBody)
	{
		if (string.IsNullOrEmpty(responseBody))
			return (0, null);

		using var doc = JsonDocument.Parse(responseBody);

		var scrollId = doc.RootElement.TryGetProperty("_scroll_id", out var scrollIdProp)
			? scrollIdProp.GetString()
			: null;

		if (!doc.RootElement.TryGetProperty("hits", out var hitsObj) ||
			!hitsObj.TryGetProperty("hits", out var hitsArray))
			return (0, scrollId);

		var count = 0;
		foreach (var hit in hitsArray.EnumerateArray())
		{
			// Use _id as the hash (we store content_hash as both _id and field)
			if (hit.TryGetProperty("_id", out var idProp) && idProp.GetString() is { } id)
			{
				_ = _existingHashes.TryAdd(id, 0);
				count++;
			}
		}
		return (count, scrollId);
	}
}

/// <summary>
/// Document structure for the enrichment cache index.
/// Fields are stored directly for use with the enrich processor.
/// </summary>
public sealed record CacheIndexEntry
{
	[JsonPropertyName("content_hash")]
	public required string ContentHash { get; init; }

	[JsonPropertyName("ai_rag_optimized_summary")]
	public string? AiRagOptimizedSummary { get; init; }

	[JsonPropertyName("ai_short_summary")]
	public string? AiShortSummary { get; init; }

	[JsonPropertyName("ai_search_query")]
	public string? AiSearchQuery { get; init; }

	[JsonPropertyName("ai_questions")]
	public string[]? AiQuestions { get; init; }

	[JsonPropertyName("ai_use_cases")]
	public string[]? AiUseCases { get; init; }

	[JsonPropertyName("created_at")]
	public required DateTimeOffset CreatedAt { get; init; }

	[JsonPropertyName("prompt_version")]
	public required int PromptVersion { get; init; }
}
