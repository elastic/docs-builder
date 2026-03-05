// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Channels;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Search;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Search;
using Elastic.Documentation.Serialization;
using Elastic.Ingest.Elasticsearch;
using Elastic.Ingest.Elasticsearch.Enrichment;
using Elastic.Ingest.Elasticsearch.Helpers;
using Elastic.Ingest.Elasticsearch.Indices;
using Elastic.Markdown.Exporters.Elasticsearch;
using Elastic.Transport;
using Microsoft.Extensions.Logging;

namespace CrawlIndexer.Indexing;

/// <summary>Result of AI enrichment run.</summary>
public sealed record AiEnrichmentResult(int Enriched, int Failed, int TotalCandidates, TimeSpan Duration);

/// <summary>Progress snapshot during finalization (reindex, delete, cleanup).</summary>
public sealed record SyncProgressInfo(string Label, long Total, long Processed, bool IsComplete);

/// <summary>
/// Exports site documents to Elasticsearch using IncrementalSyncOrchestrator for dual-index mode.
/// </summary>
public class SiteIndexerExporter : IDisposable
{
	private readonly ILogger _logger;
	private readonly IDiagnosticsCollector _diagnostics;
	private readonly IncrementalSyncOrchestrator<SiteDocument> _orchestrator;
	private readonly AiEnrichmentOrchestrator? _aiEnrichment;
	private string? _secondaryWriteAlias;
	private int _primaryIndexed;
	private int _secondaryIndexed;

	public bool AiEnrichmentEnabled => _aiEnrichment is not null;

	public IngestSyncStrategy Strategy => _orchestrator.Strategy;

	/// <summary>Fired during finalization for reindex/delete progress updates.</summary>
	public Action<SyncProgressInfo>? OnSyncProgress { get; set; }

	public SiteIndexerExporter(
		ILoggerFactory loggerFactory,
		IDiagnosticsCollector diagnostics,
		IndexingErrorTracker errorTracker,
		ElasticsearchEndpoint endpoint,
		DistributedTransport transport,
		string buildType,
		string environment,
		SearchConfiguration searchConfiguration,
		bool enableAiEnrichment
	)
	{
		_logger = loggerFactory.CreateLogger<SiteIndexerExporter>();
		_diagnostics = diagnostics;

		var lexicalContext = SiteMappingContext.SiteDocument
			.CreateContext(type: buildType, env: environment) with
		{
			ConfigureAnalysis = SiteAnalysisFactory.BuildAnalysis
		};
		var semanticContext = SiteMappingContext.SiteDocumentSemantic
			.CreateContext(type: buildType, env: environment) with
		{
			ConfigureAnalysis = SiteAnalysisFactory.BuildAnalysis
		};

		if (enableAiEnrichment && endpoint.EnableAiEnrichment)
		{
			_aiEnrichment = new AiEnrichmentOrchestrator(transport, semanticContext);
			var provider = semanticContext.AiEnrichmentProvider!;
			var infra = provider.CreateInfrastructure($"{semanticContext.IndexStrategy!.WriteTarget}-ai-cache");
			_logger.LogInformation(
				"AI enrichment enabled — pipeline: {Pipeline}, policy: {Policy}, lookup: {Lookup}",
				infra.PipelineName, infra.EnrichPolicyName, infra.LookupIndexName);

			semanticContext = semanticContext with
			{
				IndexSettings = new Dictionary<string, string> { ["index.default_pipeline"] = infra.PipelineName }
			};
		}
		else
		{
			_logger.LogInformation("AI enrichment disabled");
		}

		_orchestrator = new IncrementalSyncOrchestrator<SiteDocument>(transport, lexicalContext, semanticContext)
		{
			ConfigurePrimary = o => ConfigureChannelOptions("primary", o, endpoint, errorTracker),
			ConfigureSecondary = o => ConfigureChannelOptions("secondary", o, endpoint, errorTracker),
			OnRolloverDecision = info =>
				_logger.LogInformation(
					"[{Label}] rollover={RolledOver}, localHash={LocalHash}, remoteHash={RemoteHash}",
					info.Label, info.RolledOver, info.LocalHash, info.RemoteHash),
			OnReindexProgress = (label, p) =>
			{
				_logger.LogInformation(
					"[{Label}] total={Total} created={Created} updated={Updated} deleted={Deleted} noops={Noops} completed={IsCompleted}",
					label, p.Total, p.Created, p.Updated, p.Deleted, p.Noops, p.IsCompleted);
				OnSyncProgress?.Invoke(new SyncProgressInfo(
					label, p.Total, p.Created + p.Updated + p.Deleted + p.Noops, p.IsCompleted));
			},
			OnDeleteByQueryProgress = (label, p) =>
			{
				_logger.LogInformation(
					"[{Label}] total={Total} deleted={Deleted} completed={IsCompleted}",
					label, p.Total, p.Deleted, p.IsCompleted);
				OnSyncProgress?.Invoke(new SyncProgressInfo(label, p.Total, p.Deleted, p.IsCompleted));
			}
		};

		var publisher = new SearchConfigPublisher(transport, _logger, diagnostics);
		_ = _orchestrator.AddPreBootstrapTask(async (_, ct) =>
		{
			if (_aiEnrichment is not null)
			{
				_logger.LogInformation("Initializing AI enrichment infrastructure...");
				await _aiEnrichment.InitializeAsync(ct);
				_logger.LogInformation("AI enrichment infrastructure ready");
			}
			await publisher.PublishSynonymsAsync(searchConfiguration.Synonyms, buildType, environment, ct);
			await publisher.PublishQueryRulesAsync(searchConfiguration.Rules, buildType, environment, ct);
		});
	}

	/// <summary>Resolves the lexical read alias for cache lookups.</summary>
	public static string ResolveLexicalReadAlias(string buildType, string environment) =>
		SiteMappingContext.SiteDocument
			.CreateContext(type: buildType, env: environment)
			.ResolveReadTarget();

	/// <summary>
	/// Runs AI enrichment as a separate post-indexing phase.
	/// Yields progress snapshots for display.
	/// </summary>
	public async IAsyncEnumerable<AiEnrichmentProgress> RunAiEnrichmentAsync(
		int maxDocs = 0,
		[System.Runtime.CompilerServices.EnumeratorCancellation] Cancel ctx = default
	)
	{
		if (_aiEnrichment is null || _secondaryWriteAlias is null)
			yield break;

		var options = new AiEnrichmentOptions
		{
			CompletionTimeout = TimeSpan.FromMinutes(5),
			CompletionMaxRetries = 2,
		};
		if (maxDocs > 0)
			options.MaxEnrichmentsPerRun = maxDocs;

		await foreach (var p in _aiEnrichment.EnrichAsync(_secondaryWriteAlias, options, ctx))
			yield return p;
	}

	private void ConfigureChannelOptions(
		string label,
		IngestChannelOptions<SiteDocument> options,
		ElasticsearchEndpoint endpoint,
		IndexingErrorTracker errorTracker
	)
	{
		options.BufferOptions = new BufferOptions
		{
			OutboundBufferMaxSize = endpoint.BufferSize,
			ExportMaxConcurrency = endpoint.IndexNumThreads,
			ExportMaxRetries = endpoint.MaxRetries
		};
		options.SerializerContext = SourceGenerationContext.Default;
		options.ExportResponseCallback = (response, _) =>
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
			_logger.LogError(e, "[{Label}] Failed to export site document", label);
			_diagnostics.EmitGlobalError($"Site export ({label}): failed to export document", e);
			errorTracker.RecordException(e);
		};
		options.ExportMaxRetriesCallback = failed =>
		{
			_logger.LogError("[{Label}] Max retries exceeded for {Count} items", label, failed.Count);
			_diagnostics.EmitGlobalError($"Site export ({label}): max retries exceeded for {failed.Count} items");
		};
		options.ServerRejectionCallback = items =>
		{
			foreach (var (doc, responseItem) in items)
			{
				_diagnostics.EmitGlobalError(
					$"[{label}] Server rejection: {responseItem.Status} {responseItem.Error?.Type} {responseItem.Error?.Reason} for {doc.Url}");
			}
		};
	}

	public async ValueTask StartAsync(Cancel ctx = default)
	{
		var context = await _orchestrator.StartAsync(BootstrapMethod.Failure, ctx);
		_secondaryWriteAlias = context.SecondaryWriteAlias;
		_logger.LogInformation("Site orchestrator started with {Strategy} strategy", _orchestrator.Strategy);
	}

	public async Task ExportAsync(SiteDocument document, Cancel ctx = default)
	{
		if (_orchestrator.TryWrite(document))
			return;
		_ = await _orchestrator.WaitToWriteAsync(document, ctx);
	}

	public async ValueTask FinalizeAsync(Cancel ctx = default)
	{
		_logger.LogInformation("Finalizing site indexing...");
		_ = await _orchestrator.CompleteAsync(null, ctx);
		_logger.LogInformation("Site indexing finalized");
	}

	public void Dispose()
	{
		_orchestrator.Dispose();
		_aiEnrichment?.Dispose();
		GC.SuppressFinalize(this);
	}
}
