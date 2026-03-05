// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Channels;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Search;
using Elastic.Documentation.Serialization;
using Elastic.Ingest.Elasticsearch;
using Elastic.Transport;
using Microsoft.Extensions.Logging;

namespace CrawlIndexer.Indexing;

/// <summary>
/// Exports site documents to Elasticsearch using IncrementalSyncOrchestrator for dual-index mode.
/// </summary>
public class SiteIndexerExporter : IDisposable
{
	private readonly ILogger _logger;
	private readonly IDiagnosticsCollector _diagnostics;
	private readonly IncrementalSyncOrchestrator<SiteDocument> _orchestrator;

	public SiteIndexerExporter(
		ILoggerFactory loggerFactory,
		IDiagnosticsCollector diagnostics,
		IndexingErrorTracker errorTracker,
		ElasticsearchEndpoint endpoint,
		DistributedTransport transport,
		string buildType,
		string environment
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

		_orchestrator = new IncrementalSyncOrchestrator<SiteDocument>(transport, lexicalContext, semanticContext)
		{
			ConfigurePrimary = o => ConfigureChannelOptions(o, endpoint, errorTracker),
			ConfigureSecondary = o => ConfigureChannelOptions(o, endpoint, errorTracker)
		};
	}

	/// <summary>Resolves the lexical read alias for cache lookups.</summary>
	public static string ResolveLexicalReadAlias(string buildType, string environment) =>
		SiteMappingContext.SiteDocument
			.CreateContext(type: buildType, env: environment)
			.ResolveReadTarget();

	private void ConfigureChannelOptions(
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
		options.ExportExceptionCallback = e =>
		{
			_logger.LogError(e, "Failed to export site document");
			_diagnostics.EmitGlobalError("Site export: failed to export document", e);
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
		_ = await _orchestrator.StartAsync(BootstrapMethod.Failure, ctx);
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
		GC.SuppressFinalize(this);
	}
}
