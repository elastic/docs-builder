// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Threading;
using Elastic.Channels;
using Elastic.Documentation.Search.Contract;
using Elastic.Documentation.Search.Contract.Mapping;
using Elastic.Ingest.Elasticsearch;
using Elastic.Ingest.Elasticsearch.Enrichment;
using Elastic.SiteSearch.Cli.LabsCrawl;
using Elastic.Transport;
using Microsoft.Extensions.Logging;

namespace Elastic.SiteSearch.Cli.Elasticsearch;

internal sealed class LabsDocumentExporter : IDocumentExporter<LabsDocument>, IDisposable
{
	private readonly ILogger _logger;
	private readonly IncrementalSyncOrchestrator<LabsDocument> _orchestrator;
	private readonly AiEnrichmentOrchestrator? _aiEnrichment;
	private string? _secondaryWriteAlias;
	private int _primaryIndexed;
	private int _secondaryIndexed;
	private int _rejectedCount;
	private int _failedCount;
	private volatile bool _isFinalizing;
	private long _reindexTotal;
	private long _reindexProcessed;
	private long _reindexVersionConflicts;
	private int _postSyncAiMaxDocs = 100;
	private TimeSpan? _postSyncAiWallClock;

	public bool AiEnrichmentEnabled => _aiEnrichment is not null;

	/// <inheritdoc cref="SiteDocumentExporter.ConfigurePostSyncAiBatch"/>
	public void ConfigurePostSyncAiBatch(int maxDocsPerRun, TimeSpan? maxWallClock)
	{
		_postSyncAiMaxDocs = maxDocsPerRun;
		_postSyncAiWallClock = maxWallClock;
	}

	public IngestSyncStrategy Strategy => _orchestrator.Strategy;

	public int RejectedCount => _rejectedCount;
	public int FailedCount => _failedCount;
	public long ReindexTotal => _reindexTotal;
	public long ReindexProcessed => _reindexProcessed;
	public long ReindexVersionConflicts => _reindexVersionConflicts;
	public string? ReindexError { get; private set; }

	public Action<SyncProgressInfo>? OnSyncProgress { get; set; }

	public LabsDocumentExporter(
		ILoggerFactory loggerFactory,
		ElasticsearchEndpoint endpoint,
		DistributedTransport transport,
		string buildType,
		string environment,
		bool enableAiEnrichment
	)
	{
		_logger = loggerFactory.CreateLogger<LabsDocumentExporter>();

		var synonymSetName = $"docs-assembler-{environment}";
		var indexTimeSynonyms = IndexTimeSynonyms.Docs;

		var lexicalContext = LabsMappingContext.LabsDocument
			.CreateContext(type: buildType, env: environment) with
		{
			ConfigureAnalysis = a => SharedAnalysisFactory.BuildAnalysis(a, synonymSetName, indexTimeSynonyms)
		};
		var semanticContext = LabsMappingContext.LabsDocumentSemantic
			.CreateContext(type: buildType, env: environment) with
		{
			ConfigureAnalysis = a => SharedAnalysisFactory.BuildAnalysis(a, synonymSetName, indexTimeSynonyms)
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
			_logger.LogInformation("AI enrichment disabled");

		_orchestrator = new IncrementalSyncOrchestrator<LabsDocument>(transport, lexicalContext, semanticContext)
		{
			ConfigurePrimary = o => ConfigureChannelOptions("primary", o, endpoint),
			ConfigureSecondary = o => ConfigureChannelOptions("secondary", o, endpoint, semantic: true),
			OnRolloverDecision = info =>
			{
				_logger.LogInformation(
					"[{Label}] rollover={RolledOver}, localHash={LocalHash}, remoteHash={RemoteHash}",
					info.Label, info.RolledOver, info.LocalHash, info.RemoteHash);
				var roll = info.RolledOver ? "new write index" : "unchanged";
				OnSyncProgress?.Invoke(new SyncProgressInfo($"Index rollover — {info.Label} ({roll})", 0, 0, false));
			},
			OnReindexProgress = (label, p) =>
			{
				_logger.LogInformation(
					"[{Label}] total={Total} created={Created} updated={Updated} deleted={Deleted} noops={Noops} versionConflicts={VersionConflicts} completed={IsCompleted}",
					label, p.Total, p.Created, p.Updated, p.Deleted, p.Noops, p.VersionConflicts, p.IsCompleted);
				if (p.Error is { } err)
					_logger.LogError("[{Label}] reindex error: {Error}", label, err);
				var processed = p.Created + p.Updated + p.Deleted + p.Noops;
				_ = Interlocked.Exchange(ref _reindexTotal, p.Total);
				_ = Interlocked.Exchange(ref _reindexProcessed, processed);
				_ = Interlocked.Exchange(ref _reindexVersionConflicts, p.VersionConflicts);
				if (p.Error is not null)
					ReindexError = p.Error;
				OnSyncProgress?.Invoke(new SyncProgressInfo($"Reindex — {label}", p.Total, processed, p.IsCompleted));
			},
			OnDeleteByQueryProgress = (label, p) =>
			{
				_logger.LogInformation(
					"[{Label}] total={Total} deleted={Deleted} completed={IsCompleted}",
					label, p.Total, p.Deleted, p.IsCompleted);
				OnSyncProgress?.Invoke(new SyncProgressInfo($"Delete by query — {label}", p.Total, p.Deleted, p.IsCompleted));
			},
			OnPostComplete = _aiEnrichment is not null ? OnPostCompleteAiAsync : null
		};

		var validator = new SearchResourceValidator(transport, _logger);
		_ = _orchestrator.AddPreBootstrapTask(async (_, ct) =>
		{
			await validator.ValidateAsync(environment, ct);
			if (_aiEnrichment is not null)
			{
				_logger.LogInformation("Initializing AI enrichment infrastructure...");
				await _aiEnrichment.InitializeAsync(ct);
				_logger.LogInformation("AI enrichment infrastructure ready");
			}
		});
	}

	private Task OnPostCompleteAiAsync(OrchestratorContext<LabsDocument> context, ITransport _, CancellationToken ct) =>
		AiPostSyncBatch.RunAsync(
			_aiEnrichment!,
			context,
			_postSyncAiMaxDocs,
			_postSyncAiWallClock,
			_logger,
			ct,
			p => OnSyncProgress?.Invoke(SyncProgressConsole.FromAiProgress(p)));

	private void ConfigureChannelOptions(
		string label,
		IngestChannelOptions<LabsDocument> options,
		ElasticsearchEndpoint endpoint,
		bool semantic = false
	)
	{
		options.BufferOptions = new BufferOptions
		{
			// Semantic index runs ML inference per document — use a smaller batch to
			// avoid Elastic Cloud dropping the connection mid-request.
			OutboundBufferMaxSize = semantic ? Math.Max(1, endpoint.BufferSize / 2) : endpoint.BufferSize,
			ExportMaxConcurrency = endpoint.IndexNumThreads,
			ExportMaxRetries = endpoint.MaxRetries
		};
		options.SerializerContext = SourceGenerationContext.Default;
		options.ExportResponseCallback = (response, _) =>
		{
			if (response.Items is null)
			{
				_logger.LogWarning("[{Label}] export response had no items: {DebugInfo}",
					label, response.ApiCallDetails.DebugInformation);
				return;
			}
			var sent = response.Items.Count;
			var errors = response.Items.Count(i => i.Status >= 400);
			var indexed = label == "primary"
				? Interlocked.Add(ref _primaryIndexed, sent - errors)
				: Interlocked.Add(ref _secondaryIndexed, sent - errors);
			_logger.LogInformation("[{Label}] indexed {Indexed} items. {Errors} errors. sent: {Sent} items",
				label, indexed, errors, sent);
			if (_isFinalizing)
				OnSyncProgress?.Invoke(new SyncProgressInfo($"Flush — {label}", Total: 0, indexed, IsComplete: false));
			if (!response.ApiCallDetails.HasSuccessfulStatusCode)
				_logger.LogWarning("[{Label}] {DebugInfo}", label, response.ApiCallDetails.DebugInformation);
		};
		options.ExportExceptionCallback = e =>
		{
			_ = Interlocked.Increment(ref _failedCount);
			_logger.LogError(e, "[{Label}] Failed to export labs document", label);
		};
		options.ExportMaxRetriesCallback = failed =>
		{
			_ = Interlocked.Add(ref _failedCount, failed.Count);
			_logger.LogError("[{Label}] Max retries exceeded for {Count} items", label, failed.Count);
		};
		options.ServerRejectionCallback = items =>
		{
			_ = Interlocked.Add(ref _rejectedCount, items.Count);
			foreach (var (doc, responseItem) in items)
			{
				_logger.LogError("[{Label}] Server rejection: {Status} {Type} {Reason} for {Url}",
					label, responseItem.Status, responseItem.Error?.Type, responseItem.Error?.Reason, doc.Url);
			}
		};
	}

	public async ValueTask StartAsync(Cancel ctx = default)
	{
		var context = await _orchestrator.StartAsync(BootstrapMethod.Failure, ctx);
		_secondaryWriteAlias = context.SecondaryWriteAlias;
		_logger.LogInformation("Exporter started with {Strategy} strategy", _orchestrator.Strategy);
	}

	public async Task ExportAsync(LabsDocument document, Cancel ct = default)
	{
		if (_orchestrator.TryWrite(document))
			return;
		_ = await _orchestrator.WaitToWriteAsync(document, ct);
	}


	public async ValueTask FinalizeAsync(Cancel ctx = default)
	{
		_logger.LogInformation("Finalizing indexing...");
		_isFinalizing = true;
		var bulkAcks = Volatile.Read(ref _primaryIndexed) + Volatile.Read(ref _secondaryIndexed);
		OnSyncProgress?.Invoke(new SyncProgressInfo(SyncProgressConsole.FinalizeStartingLabel(bulkAcks), 0, 0, false));
		_ = await _orchestrator.CompleteAsync(null, ctx);
		_logger.LogInformation("Indexing finalized");
	}

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

	public void Dispose()
	{
		_orchestrator.Dispose();
		_aiEnrichment?.Dispose();
		GC.SuppressFinalize(this);
	}
}

