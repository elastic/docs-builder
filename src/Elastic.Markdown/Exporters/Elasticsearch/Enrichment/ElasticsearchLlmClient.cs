// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Elastic.Transport;
using Elastic.Transport.Products.Elasticsearch;
using Microsoft.Extensions.Logging;

namespace Elastic.Markdown.Exporters.Elasticsearch.Enrichment;

/// <summary>
/// Elasticsearch inference API-backed implementation of <see cref="ILlmClient"/>.
/// Uses a semaphore to throttle concurrent LLM calls with exponential backoff on 429.
/// For large documents, uses hierarchical summarization (map-reduce) to handle content
/// that exceeds LLM context windows.
/// </summary>
public sealed class ElasticsearchLlmClient(
	ITransport transport,
	ILogger<ElasticsearchLlmClient> logger,
	ElasticsearchOperations? operations = null,
	int maxConcurrency = 10,
	string inferenceEndpointId = ".gp-llm-v2-completion") : ILlmClient, IDisposable
{
	private readonly ITransport _transport = transport;
	private readonly ILogger _logger = logger;
	private readonly ElasticsearchOperations? _operations = operations;
	private readonly SemaphoreSlim _throttle = new(maxConcurrency);
	private readonly string _inferenceEndpointId = inferenceEndpointId;

	/// <summary>
	/// Maximum body length in characters for direct enrichment.
	/// Documents larger than this use hierarchical summarization.
	/// Based on analysis: only ~2 docs exceed 400K stripped chars.
	/// EIS uses Claude Sonnet models with 200K token (~800K char) context windows.
	/// See: https://www.elastic.co/docs/explore-analyze/elastic-inference/eis
	/// </summary>
	private const int MaxBodyLength = 400_000;

	/// <summary>
	/// Maximum chunk size for hierarchical summarization.
	/// Actual chunk size is calculated dynamically for even distribution.
	/// With prompt overhead (~10K), total is ~210K chars (~52K tokens), well under 200K token limit.
	/// See: https://www.elastic.co/docs/explore-analyze/elastic-inference/eis
	/// </summary>
	private const int MaxChunkSize = 200_000;

	private static readonly Lazy<string> PromptHashLazy = new(() =>
	{
		// Include both prompts (with and without context) in hash so cache invalidates if any changes
		var combinedPrompts = BuildEnrichmentPrompt("", "") +
			BuildChunkSummaryPrompt("", "", 0, 0, null) +
			BuildChunkSummaryPrompt("", "", 0, 0, "prev");
		var hash = SHA256.HashData(Encoding.UTF8.GetBytes(combinedPrompts));
		return Convert.ToHexString(hash).ToLowerInvariant();
	});

	/// <summary>
	/// Hash of the prompt templates. Changes when any prompt changes, triggering cache invalidation.
	/// </summary>
	public static string PromptHash => PromptHashLazy.Value;

	public async Task<EnrichmentData?> EnrichAsync(string title, string body, CancellationToken ct)
	{
		await _throttle.WaitAsync(ct);
		try
		{
			// For small documents, enrich directly
			if (body.Length <= MaxBodyLength)
				return await CallEnrichmentAsync(title, body, ct);

			// For large documents, use hierarchical summarization
			return await EnrichLargeDocumentAsync(title, body, ct);
		}
		finally
		{
			_ = _throttle.Release();
		}
	}

	public void Dispose() => _throttle.Dispose();

	/// <summary>
	/// Hierarchical summarization for large documents (map-reduce):
	/// 1. Split into chunks
	/// 2. Summarize each chunk (map phase)
	/// 3. Combine summaries and generate enrichment (reduce phase)
	/// </summary>
	private async Task<EnrichmentData?> EnrichLargeDocumentAsync(string title, string body, CancellationToken ct)
	{
		var chunks = SplitIntoChunks(body);
		_logger.LogInformation(
			"Using hierarchical summarization for large document ({Length} chars, {ChunkCount} chunks): {Title}",
			body.Length, chunks.Count, title);

		// Map phase: summarize each chunk, passing previous summary as context
		var chunkSummaries = new List<string>();
		string? previousSummary = null;

		for (var i = 0; i < chunks.Count; i++)
		{
			var summary = await SummarizeChunkAsync(title, chunks[i], i + 1, chunks.Count, previousSummary, ct);

			// All-or-nothing: if any chunk fails, skip the entire document
			if (string.IsNullOrWhiteSpace(summary))
			{
				_logger.LogWarning(
					"Chunk {ChunkNum}/{TotalCount} failed - skipping enrichment for: {Title}",
					i + 1, chunks.Count, title);
				return null;
			}

			chunkSummaries.Add(summary);
			previousSummary = summary;
		}

		// Reduce phase: combine summaries and generate enrichment
		var combinedSummary = string.Join("\n\n---\n\n", chunkSummaries);
		_logger.LogDebug(
			"Combined {SummaryCount} chunk summaries ({CombinedLength} chars) for enrichment: {Title}",
			chunkSummaries.Count, combinedSummary.Length, title);

		return await CallEnrichmentAsync(title, combinedSummary, ct);
	}

	/// <summary>
	/// Splits document into chunks at paragraph boundaries.
	/// Uses dynamic chunk sizing for even distribution while respecting MaxChunkSize.
	/// </summary>
	private static List<string> SplitIntoChunks(string body)
	{
		// Calculate dynamic chunk size for even distribution
		var numChunks = (int)Math.Ceiling((double)body.Length / MaxChunkSize);
		var targetSize = (int)Math.Ceiling((double)body.Length / numChunks);

		var paragraphs = body.Split(["\n\n", "\r\n\r\n"], StringSplitOptions.RemoveEmptyEntries);
		var chunks = new List<string>();
		var currentParagraphs = new List<string>();
		var currentSize = 0;

		void FlushCurrentChunk()
		{
			if (currentParagraphs.Count == 0)
				return;

			chunks.Add(string.Join("\n\n", currentParagraphs));
			currentParagraphs.Clear();
			currentSize = 0;
		}

		foreach (var paragraph in paragraphs)
		{
			var wouldExceedTarget = currentSize + paragraph.Length > targetSize;

			// Start new chunk if adding this paragraph would exceed target
			if (currentParagraphs.Count > 0 && wouldExceedTarget)
				FlushCurrentChunk();

			currentParagraphs.Add(paragraph);
			currentSize += paragraph.Length;

			// Immediately flush oversized paragraphs
			if (currentSize > targetSize)
				FlushCurrentChunk();
		}

		FlushCurrentChunk();

		return chunks;
	}

	/// <summary>
	/// Summarizes a single chunk with a lightweight prompt.
	/// Includes the previous chunk's summary as context for continuity.
	/// </summary>
	private async Task<string?> SummarizeChunkAsync(
		string title,
		string chunk,
		int chunkNum,
		int totalChunks,
		string? previousSummary,
		CancellationToken ct)
	{
		var prompt = BuildChunkSummaryPrompt(title, chunk, chunkNum, totalChunks, previousSummary);
		var response = await CallInferenceAsync(prompt, ct);

		if (string.IsNullOrEmpty(response))
		{
			_logger.LogWarning("Failed to summarize chunk {ChunkNum}/{TotalChunks} for: {Title}",
				chunkNum, totalChunks, title);
			return null;
		}

		return response.Trim();
	}

	/// <summary>
	/// Generates enrichment data from the document content.
	/// </summary>
	private async Task<EnrichmentData?> CallEnrichmentAsync(string title, string body, CancellationToken ct)
	{
		var prompt = BuildEnrichmentPrompt(title, body);

		_logger.LogDebug(
			"Calling LLM for enrichment: {Title} (body: {BodyLength} chars, prompt: {PromptLength} chars)",
			title, body.Length, prompt.Length);

		var response = await CallInferenceAsync(prompt, ct);
		return ParseEnrichmentResponse(title, response);
	}

	/// <summary>
	/// Calls the inference API with retry logic and returns the raw text response.
	/// </summary>
	private async Task<string?> CallInferenceAsync(string prompt, CancellationToken ct)
	{
		var request = new InferenceRequest { Input = prompt };
		var requestBody = JsonSerializer.Serialize(request, EnrichmentSerializerContext.Default.InferenceRequest);
		var url = $"_inference/completion/{_inferenceEndpointId}";
		var postData = PostData.String(requestBody);

		var response = _operations is not null
			? await _operations.WithRetryAsync(
				() => _transport.PostAsync<StringResponse>(url, postData, ct),
				"inference",
				ct)
			: await _transport.PostAsync<StringResponse>(url, postData, ct);

		if (response.ApiCallDetails.HasSuccessfulStatusCode)
			return ExtractCompletionText(response.Body);

		var responsePreview = response.Body?.Length > 1000
			? response.Body[..1000] + "..."
			: response.Body;

		_logger.LogWarning(
			"LLM inference failed: HTTP {StatusCode}. Prompt length: {PromptLength} chars. Response: {Response}",
			response.ApiCallDetails.HttpStatusCode, prompt.Length, responsePreview);
		return null;
	}

	/// <summary>
	/// Extracts the completion text from the inference API response.
	/// </summary>
	private string? ExtractCompletionText(string? responseBody)
	{
		if (string.IsNullOrEmpty(responseBody))
			return null;

		try
		{
			var completionResponse = JsonSerializer.Deserialize(responseBody, EnrichmentSerializerContext.Default.CompletionResponse);
			return completionResponse?.Completion?.FirstOrDefault()?.Result;
		}
		catch (JsonException ex)
		{
			_logger.LogWarning("Failed to parse inference response: {Error}", ex.Message);
			return null;
		}
	}

	/// <summary>
	/// Parses the enrichment JSON from the LLM response.
	/// </summary>
	private EnrichmentData? ParseEnrichmentResponse(string title, string? responseText)
	{
		if (string.IsNullOrEmpty(responseText))
		{
			_logger.LogWarning("Empty LLM response for enrichment: {Title}", title);
			return null;
		}

		try
		{
			var cleaned = CleanLlmResponse(responseText);
			var result = JsonSerializer.Deserialize(cleaned, EnrichmentSerializerContext.Default.EnrichmentData);

			if (result is null || !result.HasData)
			{
				var responsePreview = cleaned.Length > 500
					? cleaned[..500] + "..."
					: cleaned;
				_logger.LogWarning(
					"LLM response parsed but has no data for {Title}. Response: {Response}",
					title, responsePreview);
				return null;
			}

			_logger.LogDebug("Successfully enriched {Title}", title);
			return result;
		}
		catch (JsonException ex)
		{
			var responsePreview = responseText.Length > 500
				? responseText[..500] + "..."
				: responseText;
			_logger.LogWarning(
				"Failed to parse LLM response for {Title}. Error: {Error}. Response: {Response}",
				title, ex.Message, responsePreview);
			return null;
		}
	}

	private static string CleanLlmResponse(string response)
	{
		var cleaned = response.Replace("```json", "").Replace("```", "").Trim();

		// Fix common LLM issue: extra closing brace
		if (cleaned.EndsWith("}}") && !cleaned.Contains("{{"))
			cleaned = cleaned[..^1];

		// Fix common LLM issue: trailing backticks from incomplete code block syntax
		cleaned = cleaned.TrimEnd('`');

		return cleaned;
	}

	/// <summary>
	/// Builds a lightweight prompt for summarizing a single chunk.
	/// Includes previous summary for context continuity.
	/// Output is plain text (not JSON) to minimize token usage.
	/// </summary>
	private static string BuildChunkSummaryPrompt(
		string title,
		string chunk,
		int chunkNum,
		int totalChunks,
		string? previousSummary)
	{
		var contextSection = string.IsNullOrEmpty(previousSummary)
			? ""
			: $"""

			<previous-summary>
			{previousSummary}
			</previous-summary>

			<context-guidance>
			Build on this context. Avoid repeating information already covered. Focus on new concepts introduced in this section.
			</context-guidance>
			""";

		return $$"""
			<task>
			Summarize this section of a technical document for Elastic documentation.
			Focus on: API endpoints, methods, parameters, configuration options, and key technical concepts.
			</task>

			<rules>
			- Output plain text only, no JSON, no markdown formatting
			- Maximum 750 words
			- Be concise but preserve all technical details
			</rules>

			<document-info>
			Title: {{title}}
			Section: {{chunkNum}} of {{totalChunks}}
			</document-info>
			{{contextSection}}
			<section-content>
			{{chunk}}
			</section-content>
			""";
	}

	/// <summary>
	/// Builds the main enrichment prompt that generates the JSON metadata.
	/// </summary>
	private static string BuildEnrichmentPrompt(string title, string body) =>
		$$"""
		<role>
		Expert technical writer creating search metadata for Elastic documentation (Elasticsearch, Kibana, Beats, Logstash). Audience: developers, DevOps, data engineers.
		</role>

		<task>
		Return a single valid JSON object. No markdown, no extra text, no trailing characters.
		</task>

		<json-schema>
		{
		  "$schema": "http://json-schema.org/draft-07/schema#",
		  "type": "object",
		  "required": ["ai_rag_optimized_summary", "ai_short_summary", "ai_search_query", "ai_questions", "ai_use_cases"],
		  "additionalProperties": false,
		  "properties": {
		    "ai_rag_optimized_summary": {
		      "type": "string",
		      "description": "3-5 sentences densely packed with technical entities for semantic vector matching. Include: API endpoint names, method names, parameter names, configuration options, data types, and core functionality. Write for RAG retrieval - someone asking 'how do I configure X' should match this text."
		    },
		    "ai_short_summary": {
		      "type": "string",
		      "description": "Exactly 5-10 words for UI tooltip or search snippet. Action-oriented, starts with a verb. Example: 'Configure index lifecycle policies for data retention'"
		    },
		    "ai_search_query": {
		      "type": "string",
		      "description": "3-8 keywords representing a realistic search query a developer would type. Include product name and key technical terms. Example: 'elasticsearch bulk api batch indexing'"
		    },
		    "ai_questions": {
		      "type": "array",
		      "items": { "type": "string" },
		      "minItems": 3,
		      "maxItems": 5,
		      "description": "Natural questions a dev would ask (6-15 words). Not too short, not too verbose. Examples: 'How do I bulk index documents?', 'What format does the bulk API use?', 'Why is my bulk request failing?'"
		    },
		    "ai_use_cases": {
		      "type": "array",
		      "items": { "type": "string" },
		      "minItems": 2,
		      "maxItems": 4,
		      "description": "Simple 2-4 word tasks a dev wants to do. Examples: 'index documents', 'check cluster health', 'enable TLS', 'fix slow queries', 'backup data'"
		    }
		  }
		}
		</json-schema>

		<rules>
		- Extract ONLY from provided content. Never hallucinate APIs or features not mentioned.
		- Be specific: 'configure index lifecycle policy' not 'manage data'.
		- Avoid generic phrases: no 'comprehensive guide', 'powerful feature', 'easy to use'.
		- Output exactly ONE opening brace and ONE closing brace.
		</rules>

		<example>
		{"ai_rag_optimized_summary":"The Bulk API executes multiple index, create, delete, and update operations in a single NDJSON request. Endpoint: POST _bulk or POST /{index}/_bulk. Each action requires metadata line (index, create, update, delete) followed by optional document source. Supports parameters: routing, pipeline, refresh, require_alias. Returns per-operation results with _id, _version, result status, and error details for partial failures.","ai_short_summary":"Execute batch document operations in single request","ai_search_query":"elasticsearch bulk api batch index update delete","ai_questions":["How do I bulk index documents?","What format does the bulk API use?","How do I handle bulk operation errors?"],"ai_use_cases":["bulk index documents","batch update data","delete many docs"]}
		</example>

		<document>
		<title>{{title}}</title>
		<content>
		{{body}}
		</content>
		</document>
		""";
}

public sealed record InferenceRequest
{
	[JsonPropertyName("input")]
	public required string Input { get; init; }
}

public sealed record CompletionResponse
{
	[JsonPropertyName("completion")]
	public CompletionResult[]? Completion { get; init; }
}

public sealed record CompletionResult
{
	[JsonPropertyName("result")]
	public string? Result { get; init; }
}
