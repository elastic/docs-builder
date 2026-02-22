// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using System.Text.Json.Serialization;
using Elastic.Channels;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Search;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Search;
using Elastic.Documentation.Serialization;
using Elastic.Ingest.Elasticsearch;
using Elastic.Ingest.Elasticsearch.Indices;
using Elastic.Mapping;
using Elastic.Markdown.Exporters.Elasticsearch.Enrichment;
using Elastic.Transport;
using Microsoft.Extensions.Logging;

namespace Elastic.Markdown.Exporters.Elasticsearch;

public partial class ElasticsearchMarkdownExporter : IMarkdownExporter, IDisposable
{
	private readonly IDiagnosticsCollector _collector;
	private readonly IDocumentationConfigurationContext _context;
	private readonly ILogger _logger;
	private readonly ElasticsearchEndpoint _endpoint;
	private readonly DistributedTransport _transport;
	private readonly string _indexNamespace;
	private readonly DateTimeOffset _batchIndexDate;

	// Ingest: orchestrator for dual-index mode, plain channel for --no-semantic
	private readonly IncrementalSyncOrchestrator<DocumentationDocument>? _orchestrator;
	private readonly IngestChannel<DocumentationDocument>? _lexicalOnlyChannel;

	// Type context hashes for document content hash computation
	private readonly ElasticsearchTypeContext _lexicalTypeContext;
	private readonly ElasticsearchTypeContext? _semanticTypeContext;

	// Alias names for queries/statistics
	private readonly string _lexicalAlias;

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

	// Shared ES operations with retry and task polling
	private readonly ElasticsearchOperations _operations;

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
		_indexNamespace = indexNamespace;
		_versionsConfiguration = context.VersionsConfiguration;
		_synonyms = context.SearchConfiguration.Synonyms;
		_rules = context.SearchConfiguration.Rules;
		var es = endpoints.Elasticsearch;

		_transport = ElasticsearchTransportFactory.Create(es);
		_operations = new ElasticsearchOperations(_transport, _logger, collector);

		string[] fixedSynonyms = ["esql", "data-stream", "data-streams", "machine-learning"];
		var indexTimeSynonyms = _synonyms.Aggregate(new List<SynonymRule>(), (acc, synonym) =>
		{
			var id = synonym.Key;
			acc.Add(new SynonymRule { Id = id, Synonyms = string.Join(", ", synonym.Value) });
			return acc;
		}).Where(r => fixedSynonyms.Contains(r.Id)).Select(r => r.Synonyms).ToArray();
		_fixedSynonymsHash = HashedBulkUpdate.CreateHash(string.Join(",", indexTimeSynonyms));

		var aiPipeline = es.EnableAiEnrichment ? EnrichPolicyManager.PipelineName : null;
		var synonymSetName = $"docs-{indexNamespace}";
		var ns = indexNamespace.ToLowerInvariant();
		var lexicalPrefix = es.IndexNamePrefix.Replace("semantic", "lexical").ToLowerInvariant();
		_lexicalAlias = $"{lexicalPrefix}-{ns}";

		_lexicalTypeContext = DocumentationAnalysisFactory.CreateContext(
			DocumentationMappingContext.DocumentationDocument.Context,
			_lexicalAlias, synonymSetName, indexTimeSynonyms, aiPipeline
		);

		// Initialize AI enrichment services if enabled
		if (es.EnableAiEnrichment)
		{
			_enrichmentCache = new ElasticsearchEnrichmentCache(_transport, logFactory.CreateLogger<ElasticsearchEnrichmentCache>(), _operations);
			_llmClient = new ElasticsearchLlmClient(_transport, logFactory.CreateLogger<ElasticsearchLlmClient>(), _operations);
			_enrichPolicyManager = new EnrichPolicyManager(_transport, logFactory.CreateLogger<EnrichPolicyManager>(), _enrichmentCache.IndexName);
		}

		if (!es.NoSemantic)
		{
			var semanticAlias = $"{es.IndexNamePrefix.ToLowerInvariant()}-{ns}";
			_semanticTypeContext = DocumentationAnalysisFactory.CreateContext(
				DocumentationMappingContext.DocumentationDocumentSemantic.Context,
				semanticAlias, synonymSetName, indexTimeSynonyms, aiPipeline
			);

			_orchestrator = new IncrementalSyncOrchestrator<DocumentationDocument>(_transport, _lexicalTypeContext, _semanticTypeContext)
			{
				ConfigurePrimary = ConfigureChannelOptions,
				ConfigureSecondary = ConfigureChannelOptions,
				OnPostComplete = es.EnableAiEnrichment
					? async (ctx, ct) => await PostCompleteAsync(ctx, ct)
					: null
			};
			_ = _orchestrator.AddPreBootstrapTask(async (_, ct) =>
			{
				await InitializeEnrichmentAsync(ct);
				await PublishSynonymsAsync(ct);
				await PublishQueryRulesAsync(ct);
			});

			_batchIndexDate = _orchestrator.BatchTimestamp;
		}
		else
		{
			_batchIndexDate = DateTimeOffset.UtcNow;
			var options = new IngestChannelOptions<DocumentationDocument>(_transport, _lexicalTypeContext, _batchIndexDate);
			ConfigureChannelOptions(options);
			_lexicalOnlyChannel = new IngestChannel<DocumentationDocument>(options);
		}
	}

	private void ConfigureChannelOptions(IngestChannelOptions<DocumentationDocument> options)
	{
		options.BufferOptions = new BufferOptions
		{
			OutboundBufferMaxSize = _endpoint.BufferSize,
			ExportMaxConcurrency = _endpoint.IndexNumThreads,
			ExportMaxRetries = _endpoint.MaxRetries
		};
		options.SerializerContext = SourceGenerationContext.Default;
		options.ExportExceptionCallback = e =>
		{
			_logger.LogError(e, "Failed to export document");
			_collector.EmitGlobalError("Elasticsearch export: failed to export document", e);
		};
		options.ServerRejectionCallback = items =>
		{
			foreach (var (doc, responseItem) in items)
			{
				_collector.EmitGlobalError(
					$"Server rejection: {responseItem.Status} {responseItem.Error?.Type} {responseItem.Error?.Reason} for document {doc.Url}");
			}
		};
	}

	/// <inheritdoc />
	public async ValueTask StartAsync(Cancel ctx = default)
	{
		if (_orchestrator is not null)
		{
			_ = await _orchestrator.StartAsync(BootstrapMethod.Failure, ctx);
			_logger.LogInformation("Orchestrator started with {Strategy} strategy", _orchestrator.Strategy);
			return;
		}

		// NoSemantic path
		await InitializeEnrichmentAsync(ctx);
		await PublishSynonymsAsync(ctx);
		await PublishQueryRulesAsync(ctx);
		_ = await _lexicalOnlyChannel!.BootstrapElasticsearchAsync(BootstrapMethod.Failure, ctx);
	}

	/// <inheritdoc />
	public async ValueTask StopAsync(Cancel ctx = default)
	{
		if (_orchestrator is not null)
		{
			_ = await _orchestrator.CompleteAsync(null, ctx);
			return;
		}

		// NoSemantic path — drain, delete stale, refresh, alias
		var drained = await _lexicalOnlyChannel!.WaitForDrainAsync(null, ctx);
		if (!drained)
			_collector.EmitGlobalError("Elasticsearch export: failed to drain in a timely fashion");

		// Delete stale documents not part of this batch
		var deleteQuery = PostData.String($$"""
			{
				"query": {
					"range": {
						"batch_index_date": {
							"lt": "{{_batchIndexDate:o}}"
						}
					}
				}
			}
			""");
		await _operations.DeleteByQueryAsync(_lexicalAlias, deleteQuery, ctx);

		_ = await _lexicalOnlyChannel.RefreshAsync(ctx);
		_ = await _lexicalOnlyChannel.ApplyAliasesAsync(_lexicalAlias, ctx);
	}

	private async Task InitializeEnrichmentAsync(Cancel ctx)
	{
		if (_enrichmentCache is null || _enrichPolicyManager is null)
			return;

		_logger.LogInformation("Initializing AI enrichment cache...");
		await _enrichmentCache.InitializeAsync(ctx);
		_logger.LogInformation("AI enrichment cache ready with {Count} existing entries", _enrichmentCache.Count);

		_logger.LogInformation("Setting up enrich policy and pipeline...");
		await _enrichPolicyManager.ExecutePolicyAsync(ctx);
		await _enrichPolicyManager.EnsurePipelineExistsAsync(ctx);
	}

	private async Task PostCompleteAsync(OrchestratorContext<DocumentationDocument> context, Cancel ctx) =>
		await ExecuteEnrichPolicyIfNeededAsync(context.SecondaryWriteAlias, ctx);

	private async ValueTask ExecuteEnrichPolicyIfNeededAsync(string? semanticAlias, Cancel ctx)
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

			if (semanticAlias is not null)
				await BackfillMissingAiFieldsAsync(semanticAlias, ctx);
		}
	}

	private async ValueTask BackfillMissingAiFieldsAsync(string semanticAlias, Cancel ctx)
	{
		if (_endpoint.NoSemantic || _enrichmentCache is null || _llmClient is null)
			return;

		var currentPromptHash = ElasticsearchLlmClient.PromptHash;

		_logger.LogInformation(
			"Starting AI backfill for documents missing or stale AI fields (cache has {CacheCount} entries, prompt hash: {PromptHash})",
			_enrichmentCache.Count, currentPromptHash[..8]);

		var query = $$"""
			{
				"query": {
					"bool": {
						"must": { "exists": { "field": "enrichment_key" } },
						"should": [
							{ "bool": { "must_not": { "exists": { "field": "ai_questions" } } } },
							{ "bool": { "must_not": { "term": { "enrichment_prompt_hash": "{{currentPromptHash}}" } } } }
						],
						"minimum_should_match": 1
					}
				}
			}
			""";

		await _operations.UpdateByQueryAsync(semanticAlias, PostData.String(query), EnrichPolicyManager.PipelineName, ctx);
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

		var response = await _operations.WithRetryAsync(
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

		var response = await _operations.WithRetryAsync(
			() => _transport.PutAsync<StringResponse>($"_query_rules/{rulesetName}", PostData.String(json), ctx),
			$"PUT _query_rules/{rulesetName}",
			ctx);

		if (!response.ApiCallDetails.HasSuccessfulStatusCode)
			_collector.EmitGlobalError($"Failed to publish query ruleset '{rulesetName}'. Reason: {response.ApiCallDetails.OriginalException?.Message ?? response.ToString()}");
		else
			_logger.LogInformation("Successfully published query ruleset '{RulesetName}'.", rulesetName);
	}

	internal async ValueTask<bool> WriteDocumentAsync(DocumentationDocument doc, Cancel ctx)
	{
		if (_orchestrator is not null)
		{
			if (_orchestrator.TryWrite(doc))
				return true;
			_ = await _orchestrator.WaitToWriteAsync(doc, ctx);
			return true;
		}

		if (_lexicalOnlyChannel!.TryWrite(doc))
			return true;
		if (await _lexicalOnlyChannel.WaitToWriteAsync(ctx))
			return _lexicalOnlyChannel.TryWrite(doc);
		return false;
	}

	/// <inheritdoc />
	public void Dispose()
	{
		_orchestrator?.Dispose();
		_lexicalOnlyChannel?.Dispose();
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
