// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Search;
using Elastic.Ingest.Elasticsearch.Catalog;
using Elastic.Ingest.Elasticsearch.Indices;
using Elastic.Ingest.Elasticsearch.Semantic;
using Elastic.Markdown.Exporters.Elasticsearch;
using Elastic.Transport;
using Microsoft.Extensions.Logging;

namespace CrawlIndexer.Indexing;

/// <summary>
/// Exports guide documents to Elasticsearch lexical and semantic indices.
/// </summary>
public class GuideIndexerExporter : IAsyncDisposable
{
	private readonly ILogger _logger;
	private readonly IDiagnosticsCollector _diagnostics;
	private readonly GuideLexicalChannel _lexicalChannel;
	private readonly GuideSemanticChannel? _semanticChannel;
	private readonly bool _enableSemantic;

	public GuideIndexerExporter(
		ILoggerFactory loggerFactory,
		IDiagnosticsCollector diagnostics,
		IndexingErrorTracker errorTracker,
		ElasticsearchEndpoint endpoint,
		DistributedTransport transport,
		bool noSemantic
	)
	{
		_logger = loggerFactory.CreateLogger<GuideIndexerExporter>();
		_diagnostics = diagnostics;
		_enableSemantic = !noSemantic;

		_lexicalChannel = new GuideLexicalChannel(loggerFactory, diagnostics, errorTracker, endpoint, transport);
		_logger.LogInformation("Created lexical index channel");

		if (_enableSemantic)
		{
			_semanticChannel = new GuideSemanticChannel(loggerFactory, diagnostics, errorTracker, endpoint, transport);
			_logger.LogInformation("Created semantic index channel");
		}
	}

	public async Task StartAsync(Cancel ctx = default)
	{
		_logger.LogInformation("Bootstrapping guide indices...");

		var lexicalStarted = await _lexicalChannel.StartAsync(ctx);
		if (!lexicalStarted)
			_diagnostics.EmitGlobalError("Failed to bootstrap guide lexical index");

		if (_enableSemantic && _semanticChannel is not null)
		{
			var semanticStarted = await _semanticChannel.StartAsync(ctx);
			if (!semanticStarted)
				_diagnostics.EmitGlobalError("Failed to bootstrap guide semantic index");
		}

		_logger.LogInformation("Guide index bootstrap complete. Lexical reusing: {LexicalReuse}, Semantic reusing: {SemanticReuse}",
			_lexicalChannel.IsReusingIndex,
			_semanticChannel?.IsReusingIndex ?? false);
	}

	public async Task ExportAsync(DocumentationDocument document, Cancel ctx = default)
	{
		// Write to lexical index
		if (!await _lexicalChannel.TryWriteAsync(document, ctx))
		{
			_diagnostics.EmitWarning(document.Url, "Failed to write document to lexical index");
		}

		// Write to semantic index if enabled
		if (_enableSemantic && _semanticChannel is not null)
		{
			if (!await _semanticChannel.TryWriteAsync(document, ctx))
			{
				_diagnostics.EmitWarning(document.Url, "Failed to write document to semantic index");
			}
		}
	}

	public async Task FinalizeAsync(Cancel ctx = default)
	{
		_logger.LogInformation("Finalizing indexing...");

		var lexicalSuccess = await _lexicalChannel.StopAsync(ctx);
		if (!lexicalSuccess)
			_diagnostics.EmitGlobalError("Failed to finalize lexical index");

		if (_enableSemantic && _semanticChannel is not null)
		{
			var semanticSuccess = await _semanticChannel.StopAsync(ctx);
			if (!semanticSuccess)
				_diagnostics.EmitGlobalError("Failed to finalize semantic index");
		}

		_logger.LogInformation("Indexing finalized");
	}

	/// <summary>
	/// Gets information about the indexed channels for display.
	/// </summary>
	public IEnumerable<IndexChannelInfo> GetChannelInfo()
	{
		yield return _lexicalChannel.GetChannelInfo();
		if (_enableSemantic && _semanticChannel is not null)
			yield return _semanticChannel.GetChannelInfo();
	}

	public async ValueTask DisposeAsync()
	{
		// Drain any remaining data before disposal
		await _lexicalChannel.DrainAsync();
		if (_semanticChannel is not null)
			await _semanticChannel.DrainAsync();

		_lexicalChannel.Dispose();
		_semanticChannel?.Dispose();
		GC.SuppressFinalize(this);
	}
}

internal sealed class GuideLexicalChannel(
	ILoggerFactory loggerFactory,
	IDiagnosticsCollector diagnostics,
	IndexingErrorTracker errorTracker,
	ElasticsearchEndpoint endpoint,
	DistributedTransport transport
) : CrawlIndexerIngestChannel<DocumentationDocument,
	CatalogIndexChannelOptions<DocumentationDocument>,
	CatalogIndexChannel<DocumentationDocument>>(
	loggerFactory,
	diagnostics,
	errorTracker,
	endpoint,
	transport,
	o => new(o),
	t => new(t)
	{
		BulkOperationIdLookup = d => d.Url,
		ScriptedHashBulkUpsertLookup = (d, _) => new HashedBulkUpdate(
			"hash",
			d.Hash,
			"ctx._source.batch_index_date = params.batch_index_date",
			new Dictionary<string, string> { { "batch_index_date", d.BatchIndexDate.ToString("o") } }
		),
		GetMapping = () => GuideIndexMapping.CreateMapping(null),
		GetMappingSettings = () => GuideIndexMapping.CreateSettings(),
		IndexFormat = "guide-lexical-{0:yyyy.MM.dd.HHmmss}",
		ActiveSearchAlias = "guide-lexical"
	},
	"guide-lexical",
	"guide-lexical"
);

internal sealed class GuideSemanticChannel(
	ILoggerFactory loggerFactory,
	IDiagnosticsCollector diagnostics,
	IndexingErrorTracker errorTracker,
	ElasticsearchEndpoint endpoint,
	DistributedTransport transport
) : CrawlIndexerIngestChannel<DocumentationDocument,
	SemanticIndexChannelOptions<DocumentationDocument>,
	SemanticIndexChannel<DocumentationDocument>>(
	loggerFactory,
	diagnostics,
	errorTracker,
	endpoint,
	transport,
	o => new(o),
	t => new(t)
	{
		BulkOperationIdLookup = d => d.Url,
		GetMapping = (inferenceId, _) => GuideIndexMapping.CreateMapping(inferenceId),
		GetMappingSettings = (_, _) => GuideIndexMapping.CreateSettings(),
		IndexFormat = "guide-semantic-{0:yyyy.MM.dd.HHmmss}",
		ActiveSearchAlias = "guide-semantic",
		IndexNumThreads = endpoint.IndexNumThreads,
		SearchNumThreads = endpoint.SearchNumThreads,
		InferenceCreateTimeout = TimeSpan.FromMinutes(endpoint.BootstrapTimeout ?? 4),
		UsePreexistingInferenceIds = !endpoint.NoElasticInferenceService,
		InferenceId = endpoint.NoElasticInferenceService ? null : ".elser-2-elastic",
		SearchInferenceId = endpoint.NoElasticInferenceService ? null : ".elser-2-elastic"
	},
	"guide-semantic",
	"guide-semantic"
);
