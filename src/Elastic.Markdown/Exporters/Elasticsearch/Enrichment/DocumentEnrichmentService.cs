// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Elastic.Documentation.Search;
using Microsoft.Extensions.Logging;

namespace Elastic.Markdown.Exporters.Elasticsearch.Enrichment;

/// <summary>
/// Orchestrates document enrichment using an LLM client and cache.
/// </summary>
public sealed partial class DocumentEnrichmentService(
	IEnrichmentCache cache,
	ILlmClient llm,
	EnrichmentOptions options,
	ILogger<DocumentEnrichmentService> logger) : IDisposable
{
	private readonly IEnrichmentCache _cache = cache;
	private readonly ILlmClient _llm = llm;
	private readonly EnrichmentOptions _options = options;
	private readonly ILogger _logger = logger;

	private int _cacheHitCount;
	private int _staleRefreshCount;
	private int _newEnrichmentCount;
	private int _skippedCount;

	public Task InitializeAsync(CancellationToken ct) =>
		_options.Enabled ? _cache.InitializeAsync(ct) : Task.CompletedTask;

	public async Task<bool> TryEnrichAsync(DocumentationDocument doc, CancellationToken ct)
	{
		if (!_options.Enabled)
			return false;

		if (string.IsNullOrWhiteSpace(doc.StrippedBody))
			return false;

		var cacheKey = GenerateCacheKey(doc.Title, doc.StrippedBody);

		if (TryApplyCachedEnrichment(doc, cacheKey))
		{
			await TryRefreshStaleCacheAsync(doc, cacheKey, ct);
			return true;
		}

		return await TryEnrichNewDocumentAsync(doc, cacheKey, ct);
	}

	public void LogProgress()
	{
		if (!_options.Enabled)
		{
			_logger.LogInformation("AI enrichment is disabled (use --enable-ai-enrichment to enable)");
			return;
		}

		_logger.LogInformation(
			"Enrichment summary: {CacheHits} cache hits ({StaleRefreshed} stale refreshed), {NewEnrichments} new, {Skipped} skipped (limit: {Limit})",
			_cacheHitCount, _staleRefreshCount, _newEnrichmentCount, _skippedCount, _options.MaxNewEnrichmentsPerRun);

		if (_skippedCount > 0)
		{
			_logger.LogInformation(
				"Enrichment progress: {Skipped} documents pending, will complete over subsequent runs",
				_skippedCount);
		}
	}

	public void Dispose() => (_llm as IDisposable)?.Dispose();

	private bool TryApplyCachedEnrichment(DocumentationDocument doc, string cacheKey)
	{
		var cached = _cache.TryGet(cacheKey);
		if (cached is null)
			return false;

		// Defensive check: if cached data is invalid, treat as miss and let it re-enrich
		if (!cached.Data.HasData)
		{
			_logger.LogDebug("Cached entry for {Url} has no valid data, will re-enrich", doc.Url);
			return false;
		}

		ApplyEnrichment(doc, cached.Data);
		_ = Interlocked.Increment(ref _cacheHitCount);
		return true;
	}

	private async Task TryRefreshStaleCacheAsync(DocumentationDocument doc, string cacheKey, CancellationToken ct)
	{
		var cached = _cache.TryGet(cacheKey);
		// If cache is current version or newer, no refresh needed
		if (cached is not null && cached.PromptVersion >= _options.PromptVersion)
			return;

		if (!TryClaimEnrichmentSlot())
			return;

		_ = Interlocked.Increment(ref _staleRefreshCount);

		try
		{
			var fresh = await _llm.EnrichAsync(doc.Title, doc.StrippedBody ?? string.Empty, ct);
			if (fresh is not null)
			{
				await _cache.StoreAsync(cacheKey, fresh, _options.PromptVersion, ct);
				ApplyEnrichment(doc, fresh);
			}
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			_logger.LogDebug(ex, "Failed to refresh stale cache for {Url}", doc.Url);
		}
	}

	private async Task<bool> TryEnrichNewDocumentAsync(DocumentationDocument doc, string cacheKey, CancellationToken ct)
	{
		if (!TryClaimEnrichmentSlot())
		{
			_logger.LogDebug("Skipping enrichment for {Url} - limit reached", doc.Url);
			_ = Interlocked.Increment(ref _skippedCount);
			return false;
		}

		try
		{
			var enrichment = await _llm.EnrichAsync(doc.Title, doc.StrippedBody ?? string.Empty, ct);
			if (enrichment is not null)
			{
				await _cache.StoreAsync(cacheKey, enrichment, _options.PromptVersion, ct);
				ApplyEnrichment(doc, enrichment);
				return true;
			}
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			_logger.LogWarning(ex, "Failed to enrich document {Url}", doc.Url);
		}

		return false;
	}

	/// <summary>
	/// Tries to get permission to make a new LLM call.
	/// 
	/// Why: We have ~12k documents but only allow 100 new LLM calls per deployment.
	/// This keeps each deployment fast. Documents not enriched this run will be
	/// enriched in the next deployment. Cache hits are free and don't count.
	/// 
	/// How: Add 1 to counter. If counter is too high, subtract 1 and return false.
	/// This is safe when multiple documents run at the same time.
	/// </summary>
	private bool TryClaimEnrichmentSlot()
	{
		var current = Interlocked.Increment(ref _newEnrichmentCount);
		if (current <= _options.MaxNewEnrichmentsPerRun)
			return true;

		_ = Interlocked.Decrement(ref _newEnrichmentCount);
		return false;
	}

	private static void ApplyEnrichment(DocumentationDocument doc, EnrichmentData data)
	{
		doc.AiRagOptimizedSummary = data.RagOptimizedSummary;
		doc.AiShortSummary = data.ShortSummary;
		doc.AiSearchQuery = data.SearchQuery;
		doc.AiQuestions = data.Questions;
		doc.AiUseCases = data.UseCases;
	}

	private static string GenerateCacheKey(string title, string body)
	{
		var normalized = NormalizeContent(title + body);
		var hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
		return Convert.ToHexString(hash).ToLowerInvariant();
	}

	private static string NormalizeContent(string input) =>
		NormalizeRegex().Replace(input, "").ToLowerInvariant();

	[GeneratedRegex("[^a-zA-Z0-9]")]
	private static partial Regex NormalizeRegex();
}

/// <summary>
/// LLM-generated enrichment data for documentation documents.
/// </summary>
public sealed record EnrichmentData
{
	[JsonPropertyName("ai_rag_optimized_summary")]
	public string? RagOptimizedSummary { get; init; }

	[JsonPropertyName("ai_short_summary")]
	public string? ShortSummary { get; init; }

	[JsonPropertyName("ai_search_query")]
	public string? SearchQuery { get; init; }

	[JsonPropertyName("ai_questions")]
	public string[]? Questions { get; init; }

	[JsonPropertyName("ai_use_cases")]
	public string[]? UseCases { get; init; }

	[JsonIgnore]
	public bool HasData =>
		!string.IsNullOrEmpty(RagOptimizedSummary) ||
		!string.IsNullOrEmpty(ShortSummary) ||
		!string.IsNullOrEmpty(SearchQuery) ||
		Questions is { Length: > 0 } ||
		UseCases is { Length: > 0 };
}

[JsonSerializable(typeof(EnrichmentData))]
[JsonSerializable(typeof(CachedEnrichment))]
[JsonSerializable(typeof(CompletionResponse))]
[JsonSerializable(typeof(InferenceRequest))]
internal sealed partial class EnrichmentSerializerContext : JsonSerializerContext;
