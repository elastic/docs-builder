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
/// Exports guide documents to Elasticsearch using IncrementalSyncOrchestrator for dual-index mode.
/// Reuses DocumentationMappingContext since guides use the same DocumentationDocument type.
/// </summary>
public class GuideIndexerExporter : IDisposable
{
	private readonly ILogger _logger;
	private readonly IDiagnosticsCollector _diagnostics;
	private readonly IncrementalSyncOrchestrator<DocumentationDocument> _orchestrator;

	public GuideIndexerExporter(
		ILoggerFactory loggerFactory,
		IDiagnosticsCollector diagnostics,
		IndexingErrorTracker errorTracker,
		ElasticsearchEndpoint endpoint,
		DistributedTransport transport
	)
	{
		_logger = loggerFactory.CreateLogger<GuideIndexerExporter>();
		_diagnostics = diagnostics;

		var lexicalContext = DocumentationAnalysisFactory.CreateContext(
			DocumentationMappingContext.DocumentationDocument.Context,
			"guide-lexical",
			"guide-synonyms",
			[]
		);
		var semanticContext = DocumentationAnalysisFactory.CreateContext(
			DocumentationMappingContext.DocumentationDocumentSemantic.Context,
			"guide-semantic",
			"guide-synonyms",
			[]
		);

		_orchestrator = new IncrementalSyncOrchestrator<DocumentationDocument>(transport, lexicalContext, semanticContext)
		{
			ConfigurePrimary = o => ConfigureChannelOptions(o, endpoint, errorTracker),
			ConfigureSecondary = o => ConfigureChannelOptions(o, endpoint, errorTracker)
		};
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
		_ = await _orchestrator.StartAsync(BootstrapMethod.Failure, ctx);
		_logger.LogInformation("Guide orchestrator started with {Strategy} strategy", _orchestrator.Strategy);
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
		GC.SuppressFinalize(this);
	}
}
