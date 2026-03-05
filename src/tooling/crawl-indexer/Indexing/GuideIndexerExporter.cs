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
using Elastic.Ingest.Elasticsearch.Indices;
using Elastic.Markdown.Exporters.Elasticsearch;
using Elastic.Transport;
using Microsoft.Extensions.Logging;

namespace CrawlIndexer.Indexing;

/// <summary>
/// Exports guide documents to Elasticsearch using IncrementalSyncOrchestrator for dual-index mode.
/// Reuses DocumentationMappingContext since guides use the same DocumentationDocument type.
/// </summary>
public class GuideIndexerExporter : IDisposable
{
	private readonly ILogger _logger;
	private readonly IDiagnosticsCollector _diagnostics;
	private readonly IncrementalSyncOrchestrator<DocumentationDocument> _orchestrator;
	private readonly AiEnrichmentOrchestrator? _aiEnrichment;
	private string? _secondaryWriteAlias;

	public bool AiEnrichmentEnabled => _aiEnrichment is not null;

	public GuideIndexerExporter(
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
		_logger = loggerFactory.CreateLogger<GuideIndexerExporter>();
		_diagnostics = diagnostics;

		var synonymSetName = $"docs-{buildType}-{environment}";
		var indexTimeSynonyms = SearchConfigPublisher.GetIndexTimeSynonyms(searchConfiguration.Synonyms);

		var lexicalContext = DocumentationMappingContext.DocumentationDocument
			.CreateContext(type: buildType, env: environment) with
		{
			ConfigureAnalysis = a => DocumentationAnalysisFactory.BuildAnalysis(a, synonymSetName, indexTimeSynonyms)
		};
		var semanticContext = DocumentationMappingContext.DocumentationDocumentSemantic
			.CreateContext(type: buildType, env: environment) with
		{
			ConfigureAnalysis = a => DocumentationAnalysisFactory.BuildAnalysis(a, synonymSetName, indexTimeSynonyms)
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

		_orchestrator = new IncrementalSyncOrchestrator<DocumentationDocument>(transport, lexicalContext, semanticContext)
		{
			ConfigurePrimary = o => ConfigureChannelOptions(o, endpoint, errorTracker),
			ConfigureSecondary = o => ConfigureChannelOptions(o, endpoint, errorTracker),
			OnPostComplete = _aiEnrichment is not null
				? async (ctx, _, ct) => await PostCompleteAsync(ctx, ct)
				: null
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
		DocumentationMappingContext.DocumentationDocument
			.CreateContext(type: buildType, env: environment)
			.ResolveReadTarget();

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

	private void ConfigureChannelOptions(
		IngestChannelOptions<DocumentationDocument> options,
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
		options.ExportExceptionCallback = e =>
		{
			_logger.LogError(e, "Failed to export guide document");
			_diagnostics.EmitGlobalError("Guide export: failed to export document", e);
			errorTracker.RecordException(e);
		};
		options.ServerRejectionCallback = items =>
		{
			foreach (var (doc, responseItem) in items)
			{
				_diagnostics.EmitGlobalError(
					$"Server rejection: {responseItem.Status} {responseItem.Error?.Type} {responseItem.Error?.Reason} for {doc.Url}");
			}
		};
	}

	public async ValueTask StartAsync(Cancel ctx = default)
	{
		var context = await _orchestrator.StartAsync(BootstrapMethod.Failure, ctx);
		_secondaryWriteAlias = context.SecondaryWriteAlias;
		_logger.LogInformation("Guide orchestrator started with {Strategy} strategy", _orchestrator.Strategy);
	}

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

	public async Task ExportAsync(DocumentationDocument document, Cancel ctx = default)
	{
		if (_orchestrator.TryWrite(document))
			return;
		_ = await _orchestrator.WaitToWriteAsync(document, ctx);
	}

	public async ValueTask FinalizeAsync(Cancel ctx = default)
	{
		_logger.LogInformation("Finalizing guide indexing...");
		_ = await _orchestrator.CompleteAsync(null, ctx);
		_logger.LogInformation("Guide indexing finalized");
	}

	public void Dispose()
	{
		_orchestrator.Dispose();
		_aiEnrichment?.Dispose();
		GC.SuppressFinalize(this);
	}
}
