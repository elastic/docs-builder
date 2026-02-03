// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using System.Text.Json.Serialization;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Search;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Documentation.Diagnostics;
using Elastic.Ingest.Elasticsearch;
using Elastic.Ingest.Elasticsearch.Indices;
using Elastic.Markdown.Exporters.Elasticsearch.Enrichment;
using Elastic.Transport;
using Microsoft.Extensions.Logging;
using NetEscapades.EnumGenerators;

namespace Elastic.Markdown.Exporters.Elasticsearch;

[EnumExtensions]
public enum IngestStrategy { Reindex, Multiplex }

public partial class ElasticsearchMarkdownExporter : IMarkdownExporter, IDisposable
{
	private readonly IDiagnosticsCollector _collector;
	private readonly IDocumentationConfigurationContext _context;
	private readonly ILogger _logger;
	private readonly ElasticsearchLexicalIngestChannel _lexicalChannel;
	private readonly ElasticsearchSemanticIngestChannel _semanticChannel;

	private readonly ElasticsearchEndpoint _endpoint;

	private readonly DateTimeOffset _batchIndexDate = DateTimeOffset.UtcNow;
	private readonly DistributedTransport _transport;
	private IngestStrategy _indexStrategy;
	private readonly string _indexNamespace;
	private string _currentLexicalHash = string.Empty;
	private string _currentSemanticHash = string.Empty;

	private readonly IReadOnlyDictionary<string, string[]> _synonyms;
	private readonly IReadOnlyCollection<QueryRule> _rules;
	private readonly VersionsConfiguration _versionsConfiguration;
	private readonly string _fixedSynonymsHash;

	// AI Enrichment - hybrid approach: cache hits use enrich processor, misses are applied inline
	private readonly ElasticsearchEnrichmentCache? _enrichmentCache;
	private readonly ElasticsearchLlmClient? _llmClient;
	private readonly EnrichPolicyManager? _enrichPolicyManager;
	private readonly EnrichmentOptions _enrichmentOptions = new();
	private int _enrichmentCount;
	private int _cacheHitCount;

	public ElasticsearchMarkdownExporter(
		ILoggerFactory logFactory,
		IDiagnosticsCollector collector,
		DocumentationEndpoints endpoints,
		string indexNamespace,
		IDocumentationConfigurationContext context
	)
	{
		_collector = collector;
		_context = context;
		_logger = logFactory.CreateLogger<ElasticsearchMarkdownExporter>();
		_endpoint = endpoints.Elasticsearch;
		_indexStrategy = IngestStrategy.Reindex;
		_indexNamespace = indexNamespace;
		_versionsConfiguration = context.VersionsConfiguration;
		_synonyms = context.SearchConfiguration.Synonyms;
		_rules = context.SearchConfiguration.Rules;
		var es = endpoints.Elasticsearch;

		_transport = ElasticsearchTransportFactory.Create(es);

		string[] fixedSynonyms = ["esql", "data-stream", "data-streams", "machine-learning"];
		var indexTimeSynonyms = _synonyms.Aggregate(new List<SynonymRule>(), (acc, synonym) =>
		{
			var id = synonym.Key;
			acc.Add(new SynonymRule { Id = id, Synonyms = string.Join(", ", synonym.Value) });
			return acc;
		}).Where(r => fixedSynonyms.Contains(r.Id)).Select(r => r.Synonyms).ToArray();
		_fixedSynonymsHash = HashedBulkUpdate.CreateHash(string.Join(",", indexTimeSynonyms));

		// Use AI enrichment pipeline if enabled - hybrid approach:
		// - Cache hits: enrich processor applies fields at index time
		// - Cache misses: apply fields inline before indexing
		var aiPipeline = es.EnableAiEnrichment ? EnrichPolicyManager.PipelineName : null;
		_lexicalChannel = new ElasticsearchLexicalIngestChannel(logFactory, collector, es, indexNamespace, _transport, indexTimeSynonyms, aiPipeline);
		_semanticChannel = new ElasticsearchSemanticIngestChannel(logFactory, collector, es, indexNamespace, _transport, indexTimeSynonyms, aiPipeline);

		// Initialize AI enrichment services if enabled
		if (es.EnableAiEnrichment)
		{
			_enrichmentCache = new ElasticsearchEnrichmentCache(_transport, logFactory.CreateLogger<ElasticsearchEnrichmentCache>());
			_llmClient = new ElasticsearchLlmClient(_transport, logFactory.CreateLogger<ElasticsearchLlmClient>());
			_enrichPolicyManager = new EnrichPolicyManager(_transport, logFactory.CreateLogger<EnrichPolicyManager>(), _enrichmentCache.IndexName);
		}
	}

	private const int MaxRetries = 5;

	/// <summary>
	/// Executes an Elasticsearch API call with exponential backoff retry on 429 (rate limit) errors.
	/// </summary>
	private async Task<TResponse> WithRetryAsync<TResponse>(
		Func<Task<TResponse>> apiCall,
		string operationName,
		Cancel ctx) where TResponse : TransportResponse
	{
		for (var attempt = 0; attempt <= MaxRetries; attempt++)
		{
			var response = await apiCall();

			if (response.ApiCallDetails.HasSuccessfulStatusCode)
				return response;

			if (response.ApiCallDetails.HttpStatusCode == 429 && attempt < MaxRetries)
			{
				var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt)); // 1s, 2s, 4s, 8s, 16s
				_logger.LogWarning(
					"Rate limited (429) on {Operation}, retrying in {Delay}s (attempt {Attempt}/{MaxRetries})",
					operationName, delay.TotalSeconds, attempt + 1, MaxRetries);
				await Task.Delay(delay, ctx);
				continue;
			}

			// Not a 429 or exhausted retries - return the response for caller to handle
			return response;
		}

		// Should never reach here, but satisfy compiler
		return await apiCall();
	}

	/// <inheritdoc />
	public async ValueTask StartAsync(Cancel ctx = default)
	{
		// Initialize AI enrichment cache (pre-loads existing hashes into memory)
		if (_enrichmentCache is not null && _enrichPolicyManager is not null)
		{
			_logger.LogInformation("Initializing AI enrichment cache...");
			await _enrichmentCache.InitializeAsync(ctx);
			_logger.LogInformation("AI enrichment cache ready with {Count} existing entries", _enrichmentCache.Count);

			// The enrich pipeline must exist before indexing (used as default_pipeline).
			// The pipeline's enrich processor requires the .enrich-* index to exist,
			// which is created by executing the policy. We execute even with an empty
			// cache index - it just creates an empty enrich index that returns no matches.
			_logger.LogInformation("Setting up enrich policy and pipeline...");
			await _enrichPolicyManager.ExecutePolicyAsync(ctx);
			await _enrichPolicyManager.EnsurePipelineExistsAsync(ctx);
		}

		_currentLexicalHash = await _lexicalChannel.Channel.GetIndexTemplateHashAsync(ctx) ?? string.Empty;
		_currentSemanticHash = await _semanticChannel.Channel.GetIndexTemplateHashAsync(ctx) ?? string.Empty;

		await PublishSynonymsAsync(ctx);
		await PublishQueryRulesAsync(ctx);
		_ = await _lexicalChannel.Channel.BootstrapElasticsearchAsync(BootstrapMethod.Failure, null, ctx);

		// if the previous hash does not match the current hash, we know already we want to multiplex to a new index
		if (_currentLexicalHash != _lexicalChannel.Channel.ChannelHash)
			_indexStrategy = IngestStrategy.Multiplex;

		if (!_endpoint.NoSemantic)
		{
			var semanticWriteAlias = string.Format(_semanticChannel.Channel.Options.IndexFormat, "latest");
			var semanticIndexAvailable = await _transport.HeadAsync(semanticWriteAlias, ctx);
			if (!semanticIndexAvailable.ApiCallDetails.HasSuccessfulStatusCode && _endpoint is { ForceReindex: false, NoSemantic: false })
			{
				_indexStrategy = IngestStrategy.Multiplex;
				_logger.LogInformation("Index strategy set to multiplex because {SemanticIndex} does not exist, pass --force-reindex to always use reindex", semanticWriteAlias);
			}

			//try re-use index if we are re-indexing. Multiplex should always go to a new index
			_semanticChannel.Channel.Options.TryReuseIndex = _indexStrategy == IngestStrategy.Reindex;
			_ = await _semanticChannel.Channel.BootstrapElasticsearchAsync(BootstrapMethod.Failure, null, ctx);
		}

		var lexicalIndexExists = await IndexExists(_lexicalChannel.Channel.IndexName) ? "existing" : "new";
		var semanticIndexExists = await IndexExists(_semanticChannel.Channel.IndexName) ? "existing" : "new";
		if (_currentLexicalHash != _lexicalChannel.Channel.ChannelHash)
		{
			_indexStrategy = IngestStrategy.Multiplex;
			_logger.LogInformation("Multiplexing lexical new index: '{Index}' since current hash on server '{HashCurrent}' does not match new '{HashNew}'",
				_lexicalChannel.Channel.IndexName, _currentLexicalHash, _lexicalChannel.Channel.ChannelHash);
		}
		else
			_logger.LogInformation("Targeting {State} lexical: '{Index}'", lexicalIndexExists, _lexicalChannel.Channel.IndexName);

		if (!_endpoint.NoSemantic && _currentSemanticHash != _semanticChannel.Channel.ChannelHash)
		{
			_indexStrategy = IngestStrategy.Multiplex;
			_logger.LogInformation("Multiplexing new index '{Index}' since current hash on server '{HashCurrent}' does not match new '{HashNew}'",
				_semanticChannel.Channel.IndexName, _currentSemanticHash, _semanticChannel.Channel.ChannelHash);
		}
		else if (!_endpoint.NoSemantic)
			_logger.LogInformation("Targeting {State} semantical: '{Index}'", semanticIndexExists, _semanticChannel.Channel.IndexName);

		_logger.LogInformation("Using {IndexStrategy} to sync lexical index to semantic index", _indexStrategy.ToStringFast(true));

		async ValueTask<bool> IndexExists(string name) => (await _transport.HeadAsync(name, ctx)).ApiCallDetails.HasSuccessfulStatusCode;
	}

	private async Task PublishSynonymsAsync(Cancel ctx)
	{
		var setName = $"docs-{_indexNamespace}";
		_logger.LogInformation("Publishing synonym set '{SetName}' to Elasticsearch", setName);

		var synonymRules = _synonyms.Aggregate(new List<SynonymRule>(), (acc, synonym) =>
		{
			var id = synonym.Key;
			acc.Add(new SynonymRule { Id = id, Synonyms = string.Join(", ", synonym.Value) });
			return acc;
		});

		var synonymsSet = new SynonymsSet { Synonyms = synonymRules };
		await PutSynonyms(synonymsSet, setName, ctx);
	}

	private async Task PutSynonyms(SynonymsSet synonymsSet, string setName, Cancel ctx)
	{
		var json = JsonSerializer.Serialize(synonymsSet, SynonymSerializerContext.Default.SynonymsSet);

		var response = await WithRetryAsync(
			() => _transport.PutAsync<StringResponse>($"_synonyms/{setName}", PostData.String(json), ctx),
			$"PUT _synonyms/{setName}",
			ctx);

		if (!response.ApiCallDetails.HasSuccessfulStatusCode)
			_collector.EmitGlobalError($"Failed to publish synonym set '{setName}'. Reason: {response.ApiCallDetails.OriginalException?.Message ?? response.ToString()}");
		else
			_logger.LogInformation("Successfully published synonym set '{SetName}'.", setName);
	}

	private async Task PublishQueryRulesAsync(Cancel ctx)
	{
		if (_rules.Count == 0)
		{
			_logger.LogInformation("No query rules to publish");
			return;
		}

		var rulesetName = $"docs-ruleset-{_indexNamespace}";
		_logger.LogInformation("Publishing query ruleset '{RulesetName}' with {Count} rules to Elasticsearch", rulesetName, _rules.Count);

		var rulesetRules = _rules.Select(r => new QueryRulesetRule
		{
			RuleId = r.RuleId,
			Type = r.Type.ToString().ToLowerInvariant(),
			Criteria = r.Criteria.Select(c => new QueryRulesetCriteria
			{
				Type = c.Type.ToString().ToLowerInvariant(),
				Metadata = c.Metadata,
				Values = c.Values.ToList()
			}).ToList(),
			Actions = new QueryRulesetActions { Ids = r.Actions.Ids.ToList() }
		}).ToList();

		var ruleset = new QueryRuleset { Rules = rulesetRules };
		await PutQueryRuleset(ruleset, rulesetName, ctx);
	}

	private async Task PutQueryRuleset(QueryRuleset ruleset, string rulesetName, Cancel ctx)
	{
		var json = JsonSerializer.Serialize(ruleset, QueryRulesetSerializerContext.Default.QueryRuleset);

		var response = await WithRetryAsync(
			() => _transport.PutAsync<StringResponse>($"_query_rules/{rulesetName}", PostData.String(json), ctx),
			$"PUT _query_rules/{rulesetName}",
			ctx);

		if (!response.ApiCallDetails.HasSuccessfulStatusCode)
			_collector.EmitGlobalError($"Failed to publish query ruleset '{rulesetName}'. Reason: {response.ApiCallDetails.OriginalException?.Message ?? response.ToString()}");
		else
			_logger.LogInformation("Successfully published query ruleset '{RulesetName}'.", rulesetName);
	}

	private async ValueTask<long> CountAsync(string index, string body, Cancel ctx = default)
	{
		var countResponse = await WithRetryAsync(
			() => _transport.PostAsync<DynamicResponse>($"/{index}/_count", PostData.String(body), ctx),
			$"POST {index}/_count",
			ctx);
		return countResponse.Body.Get<long>("count");
	}

	/// <inheritdoc />
	public async ValueTask StopAsync(Cancel ctx = default)
	{
		var semanticWriteAlias = string.Format(_semanticChannel.Channel.Options.IndexFormat, "latest");
		var lexicalWriteAlias = string.Format(_lexicalChannel.Channel.Options.IndexFormat, "latest");

		var stopped = await _lexicalChannel.StopAsync(ctx);
		if (!stopped)
			throw new Exception($"Failed to stop {_lexicalChannel.GetType().Name}");

		await QueryIngestStatistics(lexicalWriteAlias, ctx);

		if (_indexStrategy == IngestStrategy.Multiplex)
		{
			if (!_endpoint.NoSemantic)
				_ = await _semanticChannel.StopAsync(ctx);

			// cleanup lexical index of old data
			await DoDeleteByQuery(lexicalWriteAlias, ctx);
			// need to refresh the lexical index to ensure that the delete by query is available
			_ = await _lexicalChannel.RefreshAsync(ctx);
			await QueryDocumentCounts(ctx);
			// ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
			if (_endpoint.NoSemantic)
				_logger.LogInformation("Finish indexing {IndexStrategy} strategy", _indexStrategy.ToStringFast(true));
			else
				_logger.LogInformation("Finish syncing to semantic in {IndexStrategy} strategy", _indexStrategy.ToStringFast(true));
			return;
		}

		if (_endpoint.NoSemantic)
		{
			_logger.LogInformation("--no-semantic was specified so exiting early before reindexing to {Index}", lexicalWriteAlias);
			return;
		}

		var semanticIndex = _semanticChannel.Channel.IndexName;
		// check if the alias exists
		var semanticIndexHead = await _transport.HeadAsync(semanticWriteAlias, ctx);
		if (!semanticIndexHead.ApiCallDetails.HasSuccessfulStatusCode)
		{
			_logger.LogInformation("No semantic index exists yet, creating index {Index} for semantic search", semanticIndex);
			_ = await _semanticChannel.Channel.BootstrapElasticsearchAsync(BootstrapMethod.Failure, null, ctx);
			var semanticIndexPut = await _transport.PutAsync<StringResponse>(semanticIndex, PostData.String("{}"), ctx);
			if (!semanticIndexPut.ApiCallDetails.HasSuccessfulStatusCode)
				throw new Exception($"Failed to create index {semanticIndex}: {semanticIndexPut}");
		}
		var destinationIndex = _semanticChannel.Channel.IndexName;

		_logger.LogInformation("_reindex updates: '{SourceIndex}' => '{DestinationIndex}'", lexicalWriteAlias, destinationIndex);
		var request = PostData.String(@"
		{
			""dest"": {
				""index"": """ + destinationIndex + @"""
			},
			""source"": {
				""index"": """ + lexicalWriteAlias + @""",
				""size"": 100,
				""query"": {
					""range"": {
						""last_updated"": {
							""gte"": """ + _batchIndexDate.ToString("o") + @"""
						}
					}
				}
			}
		}");
		await DoReindex(request, lexicalWriteAlias, destinationIndex, "updates", ctx);

		_logger.LogInformation("_reindex deletions: '{SourceIndex}' => '{DestinationIndex}'", lexicalWriteAlias, destinationIndex);
		request = PostData.String(@"
		{
			""dest"": {
				""index"": """ + destinationIndex + @"""
			},
			""script"": {
				""source"": ""ctx.op = \""delete\""""
			},
			""source"": {
				""index"": """ + lexicalWriteAlias + @""",
				""size"": 100,
				""query"": {
					""range"": {
						""batch_index_date"": {
							""lt"": """ + _batchIndexDate.ToString("o") + @"""
						}
					}
				}
			}
		}");
		await DoReindex(request, lexicalWriteAlias, destinationIndex, "deletions", ctx);

		await DoDeleteByQuery(lexicalWriteAlias, ctx);

		_ = await _lexicalChannel.Channel.ApplyLatestAliasAsync(ctx);
		_ = await _semanticChannel.Channel.ApplyAliasesAsync(ctx);

		_ = await _lexicalChannel.RefreshAsync(ctx);
		_ = await _semanticChannel.RefreshAsync(ctx);

		_logger.LogInformation("Finish sync to semantic index using {IndexStrategy} strategy", _indexStrategy.ToStringFast(true));
		await QueryDocumentCounts(ctx);

		// Execute enrich policy so new cache entries are available for next run
		await ExecuteEnrichPolicyIfNeededAsync(ctx);
	}

	private async ValueTask ExecuteEnrichPolicyIfNeededAsync(Cancel ctx)
	{
		if (_enrichmentCache is null || _enrichPolicyManager is null)
			return;

		_logger.LogInformation(
			"AI enrichment complete: {CacheHits} cache hits, {Enrichments} enrichments generated (limit: {Limit})",
			_cacheHitCount, _enrichmentCount, _enrichmentOptions.MaxNewEnrichmentsPerRun);

		if (_enrichmentCache.Count > 0)
		{
			_logger.LogInformation("Executing enrich policy to update internal index with {Count} total entries...", _enrichmentCache.Count);
			await _enrichPolicyManager.ExecutePolicyAsync(ctx);

			// Backfill: Apply AI fields to documents that were skipped by hash-based upsert
			await BackfillMissingAiFieldsAsync(ctx);
		}
	}

	private async ValueTask BackfillMissingAiFieldsAsync(Cancel ctx)
	{
		// Why backfill is needed:
		// The exporter uses hash-based upsert - unchanged documents are skipped during indexing.
		// These skipped documents never pass through the ingest pipeline, so they miss AI fields.
		// This backfill runs _update_by_query with the AI pipeline to enrich those documents.
		//
		// Only backfill the semantic index - it's what the search API uses.
		// The lexical index is just an intermediate step for reindexing.
		if (_endpoint.NoSemantic || _enrichmentCache is null)
			return;

		var semanticAlias = _semanticChannel.Channel.Options.ActiveSearchAlias;

		_logger.LogInformation(
			"Starting AI backfill for documents missing AI fields (cache has {CacheCount} entries)",
			_enrichmentCache.Count);

		// Find documents with enrichment_key but missing AI fields - these need the pipeline applied
		var query = /*lang=json,strict*/ """
			{
				"query": {
					"bool": {
						"must": { "exists": { "field": "enrichment_key" } },
						"must_not": { "exists": { "field": "ai_questions" } }
					}
				}
			}
			""";

		await RunBackfillQuery(semanticAlias, query, ctx);
	}

	private async ValueTask RunBackfillQuery(string indexAlias, string query, Cancel ctx)
	{
		var pipeline = EnrichPolicyManager.PipelineName;
		var url = $"/{indexAlias}/_update_by_query?pipeline={pipeline}&wait_for_completion=false";

		var response = await WithRetryAsync(
			() => _transport.PostAsync<DynamicResponse>(url, PostData.String(query), ctx),
			$"POST {indexAlias}/_update_by_query",
			ctx);

		var taskId = response.Body.Get<string>("task");
		if (string.IsNullOrWhiteSpace(taskId))
		{
			_logger.LogWarning("AI backfill failed for {Index}: {Response}", indexAlias, response);
			return;
		}

		_logger.LogInformation("AI backfill task id: {TaskId}", taskId);
		await PollTaskUntilComplete(taskId, "_update_by_query (AI backfill)", indexAlias, null, ctx);
	}

	private async ValueTask QueryIngestStatistics(string lexicalWriteAlias, Cancel ctx)
	{
		var lexicalSearchAlias = _lexicalChannel.Channel.Options.ActiveSearchAlias;
		var updated = await CountAsync(lexicalSearchAlias, $$""" { "query": { "range": { "last_updated": { "gte": "{{_batchIndexDate:o}}" } } } }""", ctx);
		var total = await CountAsync(lexicalSearchAlias, $$""" { "query": { "range": { "batch_index_date": { "gte": "{{_batchIndexDate:o}}" } } } }""", ctx);
		var deleted = await CountAsync(lexicalSearchAlias, $$""" { "query": { "range": { "batch_index_date": { "lt": "{{_batchIndexDate:o}}" } } } }""", ctx);

		// TODO emit these as metrics
		_logger.LogInformation("Exported {Total}, Updated {Updated}, Deleted, {Deleted} documents to {LexicalIndex}", total, updated, deleted, lexicalWriteAlias);
		_logger.LogInformation("Syncing to semantic index using {IndexStrategy} strategy", _indexStrategy.ToStringFast(true));
	}

	private async ValueTask QueryDocumentCounts(Cancel ctx)
	{
		var semanticWriteAlias = string.Format(_semanticChannel.Channel.Options.IndexFormat, "latest");
		var lexicalWriteAlias = string.Format(_lexicalChannel.Channel.Options.IndexFormat, "latest");
		var totalLexical = await CountAsync(lexicalWriteAlias, "{}", ctx);
		var totalSemantic = await CountAsync(semanticWriteAlias, "{}", ctx);

		// TODO emit these as metrics
		_logger.LogInformation("Document counts -> Semantic Index: {TotalSemantic}, Lexical Index: {TotalLexical}", totalSemantic, totalLexical);
	}

	private async ValueTask DoDeleteByQuery(string lexicalWriteAlias, Cancel ctx)
	{
		// delete all documents with batch_index_date < _batchIndexDate
		// they weren't part of the current export
		_logger.LogInformation("Delete data in '{SourceIndex}' not part of batch date: {Date}", lexicalWriteAlias, _batchIndexDate.ToString("o"));
		var request = PostData.String(@"
		{
			""query"": {
				""range"": {
					""batch_index_date"": {
						""lt"": """ + _batchIndexDate.ToString("o") + @"""
					}
				}
			}
		}");
		var reindexUrl = $"/{lexicalWriteAlias}/_delete_by_query?wait_for_completion=false";
		var deleteOldLexicalDocs = await WithRetryAsync(
			() => _transport.PostAsync<DynamicResponse>(reindexUrl, request, ctx),
			$"POST {lexicalWriteAlias}/_delete_by_query",
			ctx);
		var taskId = deleteOldLexicalDocs.Body.Get<string>("task");
		if (string.IsNullOrWhiteSpace(taskId))
		{
			_collector.EmitGlobalError($"Failed to delete data in '{lexicalWriteAlias}' not part of batch date: {_batchIndexDate:o}");
			_logger.LogError("Failed to delete data to '{LexicalWriteAlias}' {Response}", lexicalWriteAlias, deleteOldLexicalDocs);
			return;
		}
		_logger.LogInformation("_delete_by_query task id: {TaskId}", taskId);
		await PollTaskUntilComplete(taskId, "_delete_by_query", lexicalWriteAlias, null, ctx);
	}

	private async ValueTask PollTaskUntilComplete(string taskId, string operation, string sourceIndex, string? destIndex, Cancel ctx)
	{
		bool completed;
		do
		{
			var reindexTask = await WithRetryAsync(
				() => _transport.GetAsync<DynamicResponse>($"/_tasks/{taskId}", ctx),
				$"GET _tasks/{taskId}",
				ctx);
			completed = reindexTask.Body.Get<bool>("completed");
			var total = reindexTask.Body.Get<int>("task.status.total");
			var updated = reindexTask.Body.Get<int>("task.status.updated");
			var created = reindexTask.Body.Get<int>("task.status.created");
			var deleted = reindexTask.Body.Get<int>("task.status.deleted");
			var batches = reindexTask.Body.Get<int>("task.status.batches");
			var runningTimeInNanos = reindexTask.Body.Get<long>("task.running_time_in_nanos");
			var time = TimeSpan.FromMicroseconds(runningTimeInNanos / 1000);

			if (destIndex is not null)
			{
				_logger.LogInformation("{Operation}: {Time} '{SourceIndex}' => '{DestIndex}'. Documents {Total}: {Updated} updated, {Created} created, {Deleted} deleted, {Batches} batches",
					operation, time.ToString(@"hh\:mm\:ss"), sourceIndex, destIndex, total, updated, created, deleted, batches);
			}
			else
			{
				_logger.LogInformation("{Operation} '{SourceIndex}': {Time} Documents {Total}: {Updated} updated, {Created} created, {Deleted} deleted, {Batches} batches",
					operation, sourceIndex, time.ToString(@"hh\:mm\:ss"), total, updated, created, deleted, batches);
			}

			if (!completed)
				await Task.Delay(TimeSpan.FromSeconds(5), ctx);

		} while (!completed);
	}

	private async ValueTask DoReindex(PostData request, string lexicalWriteAlias, string semanticWriteAlias, string typeOfSync, Cancel ctx)
	{
		var reindexUrl = "/_reindex?wait_for_completion=false&scroll=10m";
		var reindexNewChanges = await WithRetryAsync(
			() => _transport.PostAsync<DynamicResponse>(reindexUrl, request, ctx),
			$"POST _reindex ({typeOfSync})",
			ctx);
		var taskId = reindexNewChanges.Body.Get<string>("task");
		if (string.IsNullOrWhiteSpace(taskId))
		{
			_logger.LogError("Failed to reindex {Type} data to '{SemanticWriteAlias}' {Response}", typeOfSync, semanticWriteAlias, reindexNewChanges);
			_collector.EmitGlobalError($"Failed to reindex {typeOfSync} data to '{semanticWriteAlias}'");
			return;
		}
		_logger.LogInformation("_reindex {Type} task id: {TaskId}", typeOfSync, taskId);
		await PollTaskUntilComplete(taskId, $"_reindex {typeOfSync}", lexicalWriteAlias, semanticWriteAlias, ctx);
	}

	/// <inheritdoc />
	public void Dispose()
	{
		_lexicalChannel.Dispose();
		_semanticChannel.Dispose();
		_llmClient?.Dispose();
		GC.SuppressFinalize(this);
	}
}

internal sealed record SynonymsSet
{
	[JsonPropertyName("synonyms_set")]
	public required List<SynonymRule> Synonyms { get; init; } = [];
}

internal sealed record SynonymRule
{
	public required string Id { get; init; }
	public required string Synonyms { get; init; }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
[JsonSerializable(typeof(SynonymsSet))]
[JsonSerializable(typeof(SynonymRule))]
internal sealed partial class SynonymSerializerContext : JsonSerializerContext;

internal sealed record QueryRuleset
{
	[JsonPropertyName("rules")]
	public required List<QueryRulesetRule> Rules { get; init; } = [];
}

internal sealed record QueryRulesetRule
{
	[JsonPropertyName("rule_id")]
	public required string RuleId { get; init; }

	[JsonPropertyName("type")]
	public required string Type { get; init; }

	[JsonPropertyName("criteria")]
	public required List<QueryRulesetCriteria> Criteria { get; init; } = [];

	[JsonPropertyName("actions")]
	public required QueryRulesetActions Actions { get; init; }
}

internal sealed record QueryRulesetCriteria
{
	[JsonPropertyName("type")]
	public required string Type { get; init; }

	[JsonPropertyName("metadata")]
	public required string Metadata { get; init; }

	[JsonPropertyName("values")]
	public required List<string> Values { get; init; } = [];
}

internal sealed record QueryRulesetActions
{
	[JsonPropertyName("ids")]
	public required List<string> Ids { get; init; } = [];
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
[JsonSerializable(typeof(QueryRuleset))]
[JsonSerializable(typeof(QueryRulesetRule))]
[JsonSerializable(typeof(QueryRulesetCriteria))]
[JsonSerializable(typeof(QueryRulesetActions))]
internal sealed partial class QueryRulesetSerializerContext : JsonSerializerContext;
