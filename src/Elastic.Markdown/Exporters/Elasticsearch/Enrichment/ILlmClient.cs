// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;

namespace Elastic.Markdown.Exporters.Elasticsearch.Enrichment;

/// <summary>
/// Abstraction for LLM inference operations.
/// Enables swapping implementations and testing.
/// </summary>
public interface ILlmClient : IDisposable
{
	/// <summary>
	/// Generates enrichment data for the given document content.
	/// </summary>
	/// <param name="title">The document title.</param>
	/// <param name="body">The document body content.</param>
	/// <param name="ct">Cancellation token.</param>
	/// <returns>The enrichment data if successful, null otherwise.</returns>
	Task<EnrichmentData?> EnrichAsync(string title, string body, CancellationToken ct);
}

/// <summary>
/// AI-generated enrichment fields for a document.
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

	public bool HasData =>
		!string.IsNullOrEmpty(RagOptimizedSummary) ||
		!string.IsNullOrEmpty(ShortSummary) ||
		!string.IsNullOrEmpty(SearchQuery) ||
		Questions is { Length: > 0 } ||
		UseCases is { Length: > 0 };
}

[JsonSerializable(typeof(EnrichmentData))]
[JsonSerializable(typeof(InferenceRequest))]
[JsonSerializable(typeof(CompletionResponse))]
[JsonSerializable(typeof(CacheIndexEntry))]
internal sealed partial class EnrichmentSerializerContext : JsonSerializerContext;
