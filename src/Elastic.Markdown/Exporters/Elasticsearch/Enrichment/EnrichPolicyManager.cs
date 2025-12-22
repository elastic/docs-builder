// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Transport;
using Elastic.Transport.Products.Elasticsearch;
using Microsoft.Extensions.Logging;

namespace Elastic.Markdown.Exporters.Elasticsearch.Enrichment;

/// <summary>
/// Manages the Elasticsearch enrich policy and ingest pipeline for AI document enrichment.
/// </summary>
public sealed class EnrichPolicyManager(
	DistributedTransport transport,
	ILogger<EnrichPolicyManager> logger,
	string cacheIndexName = "docs-ai-enriched-fields-cache")
{
	private readonly DistributedTransport _transport = transport;
	private readonly ILogger _logger = logger;
	private readonly string _cacheIndexName = cacheIndexName;

	public const string PolicyName = "ai-enrichment-policy";
	public const string PipelineName = "ai-enrichment-pipeline";


	// language=json
	private const string IngestPipelineBody = """
		{
			"description": "Enriches documents with AI-generated fields from the enrichment cache",
			"processors": [
				{
					"enrich": {
						"policy_name": "ai-enrichment-policy",
						"field": "content_hash",
						"target_field": "ai_enrichment",
						"max_matches": 1,
						"ignore_missing": true
					}
				},
				{
					"script": {
						"description": "Flatten ai_enrichment fields to document root",
						"if": "ctx.ai_enrichment != null",
						"source": "if (ctx.ai_enrichment.ai_rag_optimized_summary != null) ctx.ai_rag_optimized_summary = ctx.ai_enrichment.ai_rag_optimized_summary; if (ctx.ai_enrichment.ai_short_summary != null) ctx.ai_short_summary = ctx.ai_enrichment.ai_short_summary; if (ctx.ai_enrichment.ai_search_query != null) ctx.ai_search_query = ctx.ai_enrichment.ai_search_query; if (ctx.ai_enrichment.ai_questions != null) ctx.ai_questions = ctx.ai_enrichment.ai_questions; if (ctx.ai_enrichment.ai_use_cases != null) ctx.ai_use_cases = ctx.ai_enrichment.ai_use_cases; ctx.remove('ai_enrichment');"
					}
				}
			]
		}
		""";

	/// <summary>
	/// Ensures the enrich policy exists and creates it if not.
	/// </summary>
	public async Task EnsurePolicyExistsAsync(CancellationToken ct)
	{
		// Check if policy exists - GET returns 200 with policies array, or 404 if not found
		var existsResponse = await _transport.GetAsync<StringResponse>($"_enrich/policy/{PolicyName}", ct);

		if (existsResponse.ApiCallDetails.HasSuccessfulStatusCode &&
			existsResponse.Body?.Contains(PolicyName) == true)
		{
			_logger.LogInformation("Enrich policy {PolicyName} already exists", PolicyName);
			return;
		}

		_logger.LogInformation("Creating enrich policy {PolicyName} for index {CacheIndex}...", PolicyName, _cacheIndexName);

		// language=json
		var policyBody = $$"""
			{
				"match": {
					"indices": "{{_cacheIndexName}}",
					"match_field": "content_hash",
					"enrich_fields": ["ai_rag_optimized_summary", "ai_short_summary", "ai_search_query", "ai_questions", "ai_use_cases"]
				}
			}
			""";

		var createResponse = await _transport.PutAsync<StringResponse>(
			$"_enrich/policy/{PolicyName}",
			PostData.String(policyBody),
			ct);

		_logger.LogInformation("Policy creation response: {StatusCode} - {Response}",
			createResponse.ApiCallDetails.HttpStatusCode, createResponse.Body);

		if (createResponse.ApiCallDetails.HasSuccessfulStatusCode)
			_logger.LogInformation("Created enrich policy {PolicyName}", PolicyName);
		else
			_logger.LogError("Failed to create enrich policy: {StatusCode} - {Response}",
				createResponse.ApiCallDetails.HttpStatusCode, createResponse.Body);
	}

	/// <summary>
	/// Executes the enrich policy to rebuild the enrich index with latest data.
	/// Call this after adding new entries to the cache index.
	/// </summary>
	public async Task ExecutePolicyAsync(CancellationToken ct)
	{
		// Verify policy exists before executing
		var checkResponse = await _transport.GetAsync<StringResponse>($"_enrich/policy/{PolicyName}", ct);
		_logger.LogDebug("Pre-execute policy check: {StatusCode} - {Body}",
			checkResponse.ApiCallDetails.HttpStatusCode, checkResponse.Body);

		if (!checkResponse.ApiCallDetails.HasSuccessfulStatusCode ||
			checkResponse.Body?.Contains(PolicyName) != true)
		{
			_logger.LogInformation("Policy {PolicyName} not found, creating...", PolicyName);
			await EnsurePolicyExistsAsync(ct);
			// Small delay for Serverless propagation
			await Task.Delay(2000, ct);
		}

		_logger.LogInformation("Executing enrich policy {PolicyName}...", PolicyName);

		var response = await _transport.PostAsync<StringResponse>(
			$"_enrich/policy/{PolicyName}/_execute",
			PostData.Empty,
			ct);

		if (response.ApiCallDetails.HasSuccessfulStatusCode)
			_logger.LogInformation("Enrich policy executed successfully");
		else
			_logger.LogWarning("Enrich policy execution failed (may be empty): {StatusCode} - {Response}",
				response.ApiCallDetails.HttpStatusCode, response.Body);
	}

	/// <summary>
	/// Ensures the ingest pipeline exists and creates it if not.
	/// </summary>
	public async Task EnsurePipelineExistsAsync(CancellationToken ct)
	{
		var existsResponse = await _transport.GetAsync<StringResponse>($"_ingest/pipeline/{PipelineName}", ct);

		if (existsResponse.ApiCallDetails.HasSuccessfulStatusCode)
		{
			_logger.LogInformation("Ingest pipeline {PipelineName} already exists", PipelineName);
			return;
		}

		_logger.LogInformation("Creating ingest pipeline {PipelineName}...", PipelineName);
		var createResponse = await _transport.PutAsync<StringResponse>(
			$"_ingest/pipeline/{PipelineName}",
			PostData.String(IngestPipelineBody),
			ct);

		if (createResponse.ApiCallDetails.HasSuccessfulStatusCode)
			_logger.LogInformation("Created ingest pipeline {PipelineName}", PipelineName);
		else
			_logger.LogError("Failed to create ingest pipeline: {StatusCode} - {Response}",
				createResponse.ApiCallDetails.HttpStatusCode, createResponse.Body);
	}

	/// <summary>
	/// Ensures the enrich policy exists. Pipeline is created separately in StartAsync.
	/// </summary>
	public async Task InitializeAsync(CancellationToken ct) => await EnsurePolicyExistsAsync(ct);
}
