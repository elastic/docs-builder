// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Elastic.Transport;
using Elastic.Transport.Products.Elasticsearch;
using Microsoft.Extensions.Logging;

namespace Elastic.Markdown.Exporters.Elasticsearch.Enrichment;

/// <summary>
/// Elasticsearch-backed enrichment cache for use with the enrich processor.
/// Stores AI-generated enrichment fields directly (not as JSON string) for efficient lookups.
/// Only entries with the current prompt hash are considered valid - stale entries are treated as non-existent.
/// </summary>
public sealed class ElasticsearchEnrichmentCache(
	ITransport transport,
	ILogger<ElasticsearchEnrichmentCache> logger,
	ElasticsearchOperations? operations = null,
	string indexName = "docs-ai-enriched-fields-cache") : IEnrichmentCache
{
	private readonly ITransport _transport = transport;
	private readonly ILogger _logger = logger;
	private readonly ElasticsearchOperations? _operations = operations;

	// Only contains entries with current prompt hash - stale entries are excluded
	private readonly ConcurrentDictionary<string, byte> _validEntries = new();

	public string IndexName { get; } = indexName;

	// language=json
	// Note: No settings block - Serverless doesn't allow number_of_shards/replicas
	private const string IndexMapping = """
		{
			"mappings": {
				"properties": {
				"enrichment_key": { "type": "keyword" },
				"url": { "type": "keyword" },
				"ai_rag_optimized_summary": { "type": "text" },
				"ai_short_summary": { "type": "text" },
				"ai_search_query": { "type": "text" },
				"ai_questions": { "type": "text" },
				"ai_use_cases": { "type": "text" },
				"created_at": { "type": "date" },
				"prompt_hash": { "type": "keyword" }
			}
			}
		}
		""";

	/// <summary>
	/// Number of valid cache entries (with current prompt hash).
	/// </summary>
	public int Count => _validEntries.Count;

	/// <summary>
	/// Number of stale entries found during initialization (for logging only).
	/// </summary>
	public int StaleCount { get; private set; }

	public async Task InitializeAsync(CancellationToken ct)
	{
		await EnsureIndexExistsAsync(ct);
		await LoadExistingHashesAsync(ct);
	}

	/// <summary>
	/// Checks if a valid enrichment exists in the cache (with current prompt hash).
	/// Stale entries are treated as non-existent and will be regenerated.
	/// </summary>
	public bool Exists(string enrichmentKey) => _validEntries.ContainsKey(enrichmentKey);

	public async Task StoreAsync(string enrichmentKey, string url, EnrichmentData data, CancellationToken ct)
	{
		var promptHash = ElasticsearchLlmClient.PromptHash;
		var cacheEntry = new CacheIndexEntry
		{
			EnrichmentKey = enrichmentKey,
			Url = url,
			AiRagOptimizedSummary = data.RagOptimizedSummary,
			AiShortSummary = data.ShortSummary,
			AiSearchQuery = data.SearchQuery,
			AiQuestions = data.Questions,
			AiUseCases = data.UseCases,
			CreatedAt = DateTimeOffset.UtcNow,
			PromptHash = promptHash
		};

		var body = JsonSerializer.Serialize(cacheEntry, EnrichmentSerializerContext.Default.CacheIndexEntry);
		var response = await _transport.PutAsync<StringResponse>(
			$"{IndexName}/_doc/{enrichmentKey}",
			PostData.String(body),
			ct);

		if (response.ApiCallDetails.HasSuccessfulStatusCode)
		{
			_ = _validEntries.TryAdd(enrichmentKey, 0);

			// Clean up any older entries for the same URL (but different enrichment_key)
			await DeleteOldEntriesForUrlAsync(enrichmentKey, url, ct);
		}
		else
		{
			_logger.LogWarning("Failed to store enrichment: {StatusCode}", response.ApiCallDetails.HttpStatusCode);
		}
	}

	/// <summary>
	/// Deletes older cache entries for the same URL, keeping only the current one.
	/// This cleans up stale entries left behind when documents are re-enriched.
	/// Uses wait_for_completion=false for async execution (fire-and-forget).
	/// </summary>
	private async Task DeleteOldEntriesForUrlAsync(string currentEnrichmentKey, string url, CancellationToken ct)
	{
		// Delete all entries with this URL except the current one
		var deleteQuery = PostData.String($$"""
			{
				"query": {
					"bool": {
						"must": { "term": { "url": "{{url}}" } },
						"must_not": { "term": { "_id": "{{currentEnrichmentKey}}" } }
					}
				}
			}
			""");

		if (_operations is not null)
		{
			// Use shared operations for fire-and-forget delete
			var taskId = await _operations.DeleteByQueryFireAndForgetAsync(IndexName, deleteQuery, ct);
			if (taskId is not null)
				_logger.LogDebug("Started cleanup task {TaskId} for URL {Url}", taskId, url);
		}
		else
		{
			// Fallback for when operations not provided
			var response = await _transport.PostAsync<StringResponse>(
				$"{IndexName}/_delete_by_query?wait_for_completion=false",
				deleteQuery,
				ct);

			if (response.ApiCallDetails.HasSuccessfulStatusCode)
			{
				using var doc = JsonDocument.Parse(response.Body ?? "{}");
				if (doc.RootElement.TryGetProperty("task", out var taskProp))
					_logger.LogDebug("Started cleanup task {TaskId} for URL {Url}", taskProp.GetString(), url);
			}
		}
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
		var currentPromptHash = ElasticsearchLlmClient.PromptHash;
		var staleCount = 0;
		var totalCount = 0;

		// Fetch _id and prompt_hash to determine validity
		var scrollQuery = /*lang=json,strict*/ """{"size": 1000, "_source": ["prompt_hash"], "query": {"match_all": {}}}""";

		var searchResponse = await _transport.PostAsync<StringResponse>(
			$"{IndexName}/_search?scroll=1m",
			PostData.String(scrollQuery),
			ct);

		if (!searchResponse.ApiCallDetails.HasSuccessfulStatusCode)
		{
			_logger.LogWarning("Failed to load existing hashes: {StatusCode}", searchResponse.ApiCallDetails.HttpStatusCode);
			return;
		}

		var (batchTotal, batchStale, scrollId) = ProcessHashHits(searchResponse.Body, currentPromptHash);
		totalCount += batchTotal;
		staleCount += batchStale;

		while (scrollId is not null && batchTotal > 0)
		{
			var scrollBody = $$"""{"scroll": "1m", "scroll_id": "{{scrollId}}"}""";
			var scrollResponse = await _transport.PostAsync<StringResponse>(
				"_search/scroll",
				PostData.String(scrollBody),
				ct);

			if (!scrollResponse.ApiCallDetails.HasSuccessfulStatusCode)
				break;

			(batchTotal, batchStale, scrollId) = ProcessHashHits(scrollResponse.Body, currentPromptHash);
			totalCount += batchTotal;
			staleCount += batchStale;
		}

		StaleCount = staleCount;
		_logger.LogInformation(
			"Loaded {Total} enrichment cache entries: {Valid} valid (current prompt), {Stale} stale (will be refreshed) in {ElapsedMs}ms",
			totalCount, _validEntries.Count, staleCount, sw.ElapsedMilliseconds);
	}

	private (int total, int stale, string? scrollId) ProcessHashHits(string? responseBody, string currentPromptHash)
	{
		if (string.IsNullOrEmpty(responseBody))
			return (0, 0, null);

		using var doc = JsonDocument.Parse(responseBody);

		var scrollId = doc.RootElement.TryGetProperty("_scroll_id", out var scrollIdProp)
			? scrollIdProp.GetString()
			: null;

		if (!doc.RootElement.TryGetProperty("hits", out var hitsObj) ||
			!hitsObj.TryGetProperty("hits", out var hitsArray))
			return (0, 0, scrollId);

		var total = 0;
		var stale = 0;
		foreach (var entry in hitsArray
			.EnumerateArray()
			.Select(hit => new
			{
				Hit = hit,
				Id = hit.TryGetProperty("_id", out var idProp) ? idProp.GetString() : null
			})
			.Where(e => e.Id is not null))
		{
			var hit = entry.Hit;
			var id = entry.Id!;
			total++;

			// Only add entries with current prompt hash - stale entries are ignored
			if (hit.TryGetProperty("_source", out var source) &&
				source.TryGetProperty("prompt_hash", out var promptHashProp) &&
				promptHashProp.GetString() == currentPromptHash)
			{
				_ = _validEntries.TryAdd(id, 0);
			}
			else
			{
				stale++;
			}
		}
		return (total, stale, scrollId);
	}
}

/// <summary>
/// Document structure for the enrichment cache index.
/// Fields are stored directly for use with the enrich processor.
/// </summary>
public sealed record CacheIndexEntry
{
	[JsonPropertyName("enrichment_key")]
	public required string EnrichmentKey { get; init; }

	/// <summary>
	/// Document URL for debugging - helps identify which document this cache entry belongs to.
	/// </summary>
	[JsonPropertyName("url")]
	public string? Url { get; init; }

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

	[JsonPropertyName("prompt_hash")]
	public required string PromptHash { get; init; }
}
