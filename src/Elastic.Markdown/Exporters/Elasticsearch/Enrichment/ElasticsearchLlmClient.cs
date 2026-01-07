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
/// </summary>
public sealed class ElasticsearchLlmClient(
	DistributedTransport transport,
	ILogger<ElasticsearchLlmClient> logger,
	int maxConcurrency = 10,
	int maxRetries = 5,
	string inferenceEndpointId = ".gp-llm-v2-completion") : ILlmClient, IDisposable
{
	private readonly DistributedTransport _transport = transport;
	private readonly ILogger _logger = logger;
	private readonly SemaphoreSlim _throttle = new(maxConcurrency);
	private readonly string _inferenceEndpointId = inferenceEndpointId;
	private readonly int _maxRetries = maxRetries;

	private static readonly Lazy<string> PromptHashLazy = new(() =>
	{
		var prompt = BuildPrompt("", "");
		var hash = SHA256.HashData(Encoding.UTF8.GetBytes(prompt));
		return Convert.ToHexString(hash).ToLowerInvariant();
	});

	/// <summary>
	/// Hash of the prompt template. Changes when the prompt changes, triggering cache invalidation.
	/// </summary>
	public static string PromptHash => PromptHashLazy.Value;

	public async Task<EnrichmentData?> EnrichAsync(string title, string body, CancellationToken ct)
	{
		await _throttle.WaitAsync(ct);
		try
		{
			return await CallInferenceApiWithRetryAsync(title, body, ct);
		}
		finally
		{
			_ = _throttle.Release();
		}
	}

	public void Dispose() => _throttle.Dispose();

	private async Task<EnrichmentData?> CallInferenceApiWithRetryAsync(string title, string body, CancellationToken ct)
	{
		var prompt = BuildPrompt(title, body);
		var request = new InferenceRequest { Input = prompt };
		var requestBody = JsonSerializer.Serialize(request, EnrichmentSerializerContext.Default.InferenceRequest);

		for (var attempt = 0; attempt <= _maxRetries; attempt++)
		{
			var response = await _transport.PostAsync<StringResponse>(
				$"_inference/completion/{_inferenceEndpointId}",
				PostData.String(requestBody),
				ct);

			if (response.ApiCallDetails.HasSuccessfulStatusCode)
				return ParseResponse(response.Body);

			if (response.ApiCallDetails.HttpStatusCode == 429 && attempt < _maxRetries)
			{
				var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt)); // 1s, 2s, 4s, 8s, 16s
				_logger.LogDebug("Rate limited (429), retrying in {Delay}s (attempt {Attempt}/{MaxRetries})",
					delay.TotalSeconds, attempt + 1, _maxRetries);
				await Task.Delay(delay, ct);
				continue;
			}

			_logger.LogWarning("LLM inference failed: {StatusCode} - {Body}",
				response.ApiCallDetails.HttpStatusCode, response.Body);
			return null;
		}

		return null;
	}

	private EnrichmentData? ParseResponse(string? responseBody)
	{
		if (string.IsNullOrEmpty(responseBody))
		{
			_logger.LogWarning("No response body from LLM");
			return null;
		}

		string? responseText = null;
		try
		{
			var completionResponse = JsonSerializer.Deserialize(responseBody, EnrichmentSerializerContext.Default.CompletionResponse);
			responseText = completionResponse?.Completion?.FirstOrDefault()?.Result;

			if (string.IsNullOrEmpty(responseText))
			{
				_logger.LogWarning("Empty LLM response");
				return null;
			}

			responseText = CleanLlmResponse(responseText);
			var result = JsonSerializer.Deserialize(responseText, EnrichmentSerializerContext.Default.EnrichmentData);

			if (result is null || !result.HasData)
			{
				_logger.LogWarning("LLM response parsed but has no data: {Response}",
					responseText.Length > 500 ? responseText[..500] + "..." : responseText);
				return null;
			}

			return result;
		}
		catch (JsonException ex)
		{
			_logger.LogWarning("Failed to parse LLM response. Error: {Error}. Response: {Response}",
				ex.Message, responseText);
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

	private static string BuildPrompt(string title, string body) =>
		$$"""
		ROLE: Expert technical writer creating search metadata for Elastic documentation (Elasticsearch, Kibana, Beats, Logstash). Audience: developers, DevOps, data engineers.

		TASK: Return a single valid JSON object. No markdown, no extra text, no trailing characters.

		JSON SCHEMA:
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

		RULES:
		- Extract ONLY from provided content. Never hallucinate APIs or features not mentioned.
		- Be specific: 'configure index lifecycle policy' not 'manage data'.
		- Avoid generic phrases: no 'comprehensive guide', 'powerful feature', 'easy to use'.
		- Output exactly ONE opening brace and ONE closing brace.

		EXAMPLE:
		{"ai_rag_optimized_summary":"The Bulk API executes multiple index, create, delete, and update operations in a single NDJSON request. Endpoint: POST _bulk or POST /{index}/_bulk. Each action requires metadata line (index, create, update, delete) followed by optional document source. Supports parameters: routing, pipeline, refresh, require_alias. Returns per-operation results with _id, _version, result status, and error details for partial failures.","ai_short_summary":"Execute batch document operations in single request","ai_search_query":"elasticsearch bulk api batch index update delete","ai_questions":["How do I bulk index documents?","What format does the bulk API use?","How do I handle bulk operation errors?"],"ai_use_cases":["bulk index documents","batch update data","delete many docs"]}

		DOCUMENT:
		Title: {{title}}
		Content: {{body}}
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
