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
using Elastic.Ingest.Elasticsearch.Enrichment;
using Elastic.Ingest.Elasticsearch.Indices;
using Elastic.Mapping;
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
	private readonly string _buildType;

	// Ingest: orchestrator for dual-index mode
	private readonly IncrementalSyncOrchestrator<DocumentationDocument> _orchestrator;

	// Type context hashes for document content hash computation
	private readonly ElasticsearchTypeContext _lexicalTypeContext;
	private readonly ElasticsearchTypeContext _semanticTypeContext;

	private readonly IReadOnlyDictionary<string, string[]> _synonyms;
	private readonly IReadOnlyCollection<QueryRule> _rules;
	private readonly VersionsConfiguration _versionsConfiguration;
	private readonly string _fixedSynonymsHash;

	// AI Enrichment - post-indexing via AiEnrichmentOrchestrator
	private readonly AiEnrichmentOrchestrator? _aiEnrichment;

	// Per-channel running totals for progress logging
	private int _primaryIndexed;
	private int _secondaryIndexed;

	// Shared ES operations with retry and task polling
	private readonly ElasticsearchOperations _operations;

	public ElasticsearchMarkdownExporter(
		ILoggerFactory logFactory,
		IDiagnosticsCollector collector,
		DocumentationEndpoints endpoints,
		string buildType,
		IDocumentationConfigurationContext context
	)
	{
		_collector = collector;
		_context = context;
		_logger = logFactory.CreateLogger<ElasticsearchMarkdownExporter>();
		_endpoint = endpoints.Elasticsearch;
		_buildType = buildType;
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

		var synonymSetName = $"docs-{buildType}";

		_lexicalTypeContext = DocumentationMappingContext.DocumentationDocument.CreateContext(type: buildType) with
		{
			ConfigureAnalysis = a => DocumentationAnalysisFactory.BuildAnalysis(a, synonymSetName, indexTimeSynonyms)
		};

		_semanticTypeContext = DocumentationMappingContext.DocumentationDocumentSemantic.CreateContext(type: buildType) with
		{
			ConfigureAnalysis = a => DocumentationAnalysisFactory.BuildAnalysis(a, synonymSetName, indexTimeSynonyms)
		};

		if (es.EnableAiEnrichment)
		{
			_aiEnrichment = new AiEnrichmentOrchestrator(_transport, _lexicalTypeContext);
			var provider = _lexicalTypeContext.AiEnrichmentProvider!;
			var infra = provider.CreateInfrastructure($"{_lexicalTypeContext.IndexStrategy!.WriteTarget}-ai-cache");
			_logger.LogInformation(
				"AI enrichment enabled — pipeline: {Pipeline}, policy: {Policy}, lookup: {Lookup}",
				infra.PipelineName, infra.EnrichPolicyName, infra.LookupIndexName);

			_semanticTypeContext = _semanticTypeContext with
			{
				IndexSettings = new Dictionary<string, string> { ["index.default_pipeline"] = infra.PipelineName }
			};
		}
		else
		{
			_logger.LogInformation("AI enrichment disabled");
		}

		_orchestrator = new IncrementalSyncOrchestrator<DocumentationDocument>(
			_transport, _lexicalTypeContext, _semanticTypeContext)
		{
			ConfigurePrimary = opts => ConfigureChannelOptions("primary", opts),
			ConfigureSecondary = opts => ConfigureChannelOptions("secondary", opts),
			OnPostComplete = _aiEnrichment is not null
				? async (ctx, _, ct) => await PostCompleteAsync(ctx, ct)
				: null,
			OnReindexProgress = (label, p) =>
				_logger.LogInformation(
					"[{Label}] total={Total} created={Created} updated={Updated} deleted={Deleted} noops={Noops} completed={IsCompleted}",
					label, p.Total, p.Created, p.Updated, p.Deleted, p.Noops, p.IsCompleted),
			OnDeleteByQueryProgress = (label, p) =>
				_logger.LogInformation(
					"[{Label}] total={Total} deleted={Deleted} completed={IsCompleted}",
					label, p.Total, p.Deleted, p.IsCompleted)
		};
		_ = _orchestrator.AddPreBootstrapTask(async (_, ct) =>
		{
			if (_aiEnrichment is not null)
			{
				_logger.LogInformation("Initializing AI enrichment infrastructure...");
				await _aiEnrichment.InitializeAsync(ct);
				_logger.LogInformation("AI enrichment infrastructure ready");
			}
			await PublishSynonymsAsync(ct);
			await PublishQueryRulesAsync(ct);
		});
	}

	private void ConfigureChannelOptions(string label, IngestChannelOptions<DocumentationDocument> options)
	{
		options.BufferOptions = new BufferOptions
		{
			OutboundBufferMaxSize = _endpoint.BufferSize,
			ExportMaxConcurrency = _endpoint.IndexNumThreads,
			ExportMaxRetries = _endpoint.MaxRetries
		};
		options.SerializerContext = SourceGenerationContext.Default;
		options.ExportResponseCallback = (response, buffer) =>
		{
			var sent = response.Items?.Count ?? 0;
			var errors = response.Items?.Count(i => i.Status >= 400) ?? 0;
			var indexed = label == "primary"
				? Interlocked.Add(ref _primaryIndexed, sent - errors)
				: Interlocked.Add(ref _secondaryIndexed, sent - errors);
			_logger.LogInformation("[{Label}] indexed {Indexed} items. {Errors} errors. sent: {Sent} items",
				label, indexed, errors, sent);
			if (!response.ApiCallDetails.HasSuccessfulStatusCode)
				_logger.LogWarning("[{Label}] {DebugInfo}", label, response.ApiCallDetails.DebugInformation);
		};
		options.ExportExceptionCallback = e =>
		{
			_logger.LogError(e, "[{Label}] Failed to export document", label);
			_collector.EmitGlobalError($"Elasticsearch export ({label}): failed to export document", e);
		};
		options.ExportMaxRetriesCallback = failed =>
		{
			_logger.LogError("[{Label}] Max retries exceeded for {Count} items", label, failed.Count);
			_collector.EmitGlobalError($"Elasticsearch export ({label}): max retries exceeded for {failed.Count} items");
		};
		options.ServerRejectionCallback = items =>
		{
			foreach (var (doc, responseItem) in items)
			{
				_collector.EmitGlobalError(
					$"[{label}] Server rejection: {responseItem.Status} {responseItem.Error?.Type} {responseItem.Error?.Reason} for document {doc.Url}");
			}
		};
	}

	/// <inheritdoc />
	public async ValueTask StartAsync(Cancel ctx = default)
	{
		_ = await _orchestrator.StartAsync(BootstrapMethod.Failure, ctx);
		_logger.LogInformation("Orchestrator started with {Strategy} strategy", _orchestrator.Strategy);
	}

	/// <inheritdoc />
	public async ValueTask StopAsync(Cancel ctx = default) =>
		_ = await _orchestrator.CompleteAsync(null, ctx);

	private async Task PostCompleteAsync(OrchestratorContext<DocumentationDocument> context, Cancel ctx)
	{
		if (_aiEnrichment is null)
			return;

		_logger.LogInformation("Starting post-indexing AI enrichment for {Alias}...", context.SecondaryWriteAlias);
		var sw = System.Diagnostics.Stopwatch.StartNew();

		AiEnrichmentProgress? last = null;
		var options = new AiEnrichmentOptions
		{
			CompletionTimeout = TimeSpan.FromMinutes(2),
			CompletionMaxRetries = 2,
		};
		await foreach (var p in _aiEnrichment.EnrichAsync(context.SecondaryWriteAlias, options, ctx))
		{
			_logger.LogInformation(
				"[AI enrichment] {Phase}: enriched={Enriched} failed={Failed} candidates={Candidates}{Message}",
				p.Phase, p.Enriched, p.Failed, p.TotalCandidates, p.Message is not null ? $" — {p.Message}" : "");
			last = p;
		}

		if (last is not null)
			_logger.LogInformation(
				"AI enrichment complete in {Elapsed}: {Enriched} enriched, {Failed} failed, {Candidates} candidates",
				sw.Elapsed.ToString(@"hh\:mm\:ss"), last.Enriched, last.Failed, last.TotalCandidates);
	}

	private async Task PublishSynonymsAsync(Cancel ctx)
	{
		var setName = $"docs-{_buildType}";
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
			_collector.EmitGlobalError(
				$"Failed to publish synonym set '{setName}'. Reason: {response.ApiCallDetails.OriginalException?.Message ?? response.ToString()}");
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

		var rulesetName = $"docs-ruleset-{_buildType}";
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
			_collector.EmitGlobalError(
				$"Failed to publish query ruleset '{rulesetName}'. Reason: {response.ApiCallDetails.OriginalException?.Message ?? response.ToString()}");
		else
			_logger.LogInformation("Successfully published query ruleset '{RulesetName}'.", rulesetName);
	}

	internal async ValueTask<bool> WriteDocumentAsync(DocumentationDocument doc, Cancel ctx)
	{
		if (_orchestrator.TryWrite(doc))
			return true;
		_ = await _orchestrator.WaitToWriteAsync(doc, ctx);
		return true;
	}

	/// <inheritdoc />
	public void Dispose()
	{
		_orchestrator.Dispose();
		_aiEnrichment?.Dispose();
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
