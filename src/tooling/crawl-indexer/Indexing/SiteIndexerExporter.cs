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
/// Exports site documents to Elasticsearch lexical and semantic indices.
/// </summary>
public class SiteIndexerExporter : IAsyncDisposable
{
	private readonly ILogger _logger;
	private readonly IDiagnosticsCollector _diagnostics;
	private readonly SiteLexicalChannel _lexicalChannel;
	private readonly SiteSemanticChannel? _semanticChannel;
	private readonly bool _enableSemantic;

	public SiteIndexerExporter(
		ILoggerFactory loggerFactory,
		IDiagnosticsCollector diagnostics,
		IndexingErrorTracker errorTracker,
		ElasticsearchEndpoint endpoint,
		DistributedTransport transport,
		bool noSemantic
	)
	{
		_logger = loggerFactory.CreateLogger<SiteIndexerExporter>();
		_diagnostics = diagnostics;
		_enableSemantic = !noSemantic;

		_lexicalChannel = new SiteLexicalChannel(loggerFactory, diagnostics, errorTracker, endpoint, transport);
		_logger.LogInformation("Created site lexical index channel");

		if (_enableSemantic)
		{
			_semanticChannel = new SiteSemanticChannel(loggerFactory, diagnostics, errorTracker, endpoint, transport);
			_logger.LogInformation("Created site semantic index channel");
		}
	}

	public async Task StartAsync(Cancel ctx = default)
	{
		_logger.LogInformation("Bootstrapping site indices...");

		var lexicalStarted = await _lexicalChannel.StartAsync(ctx);
		if (!lexicalStarted)
			_diagnostics.EmitGlobalError("Failed to bootstrap site lexical index");

		if (_enableSemantic && _semanticChannel is not null)
		{
			var semanticStarted = await _semanticChannel.StartAsync(ctx);
			if (!semanticStarted)
				_diagnostics.EmitGlobalError("Failed to bootstrap site semantic index");
		}

		_logger.LogInformation("Site index bootstrap complete. Lexical reusing: {LexicalReuse}, Semantic reusing: {SemanticReuse}",
			_lexicalChannel.IsReusingIndex,
			_semanticChannel?.IsReusingIndex ?? false);
	}

	public async Task ExportAsync(SiteDocument document, Cancel ctx = default)
	{
		// Write to lexical index
		if (!await _lexicalChannel.TryWriteAsync(document, ctx))
		{
			_diagnostics.EmitWarning(document.Url, "Failed to write document to site lexical index");
		}

		// Write to semantic index if enabled
		if (_enableSemantic && _semanticChannel is not null)
		{
			if (!await _semanticChannel.TryWriteAsync(document, ctx))
			{
				_diagnostics.EmitWarning(document.Url, "Failed to write document to site semantic index");
			}
		}
	}

	public async Task FinalizeAsync(Cancel ctx = default)
	{
		_logger.LogInformation("Finalizing site indexing...");

		var lexicalSuccess = await _lexicalChannel.StopAsync(ctx);
		if (!lexicalSuccess)
			_diagnostics.EmitGlobalError("Failed to finalize site lexical index");

		if (_enableSemantic && _semanticChannel is not null)
		{
			var semanticSuccess = await _semanticChannel.StopAsync(ctx);
			if (!semanticSuccess)
				_diagnostics.EmitGlobalError("Failed to finalize site semantic index");
		}

		_logger.LogInformation("Site indexing finalized");
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

internal sealed class SiteLexicalChannel(
	ILoggerFactory loggerFactory,
	IDiagnosticsCollector diagnostics,
	IndexingErrorTracker errorTracker,
	ElasticsearchEndpoint endpoint,
	DistributedTransport transport
) : CrawlIndexerIngestChannel<SiteDocument,
	CatalogIndexChannelOptions<SiteDocument>,
	CatalogIndexChannel<SiteDocument>>(
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
		GetMapping = () => SiteIndexMapping.CreateMapping(null),
		GetMappingSettings = () => SiteIndexMapping.CreateSettings(),
		IndexFormat = "site-lexical-{0:yyyy.MM.dd.HHmmss}",
		ActiveSearchAlias = "site-lexical"
	},
	"site-lexical",
	"site-lexical"
);

internal sealed class SiteSemanticChannel(
	ILoggerFactory loggerFactory,
	IDiagnosticsCollector diagnostics,
	IndexingErrorTracker errorTracker,
	ElasticsearchEndpoint endpoint,
	DistributedTransport transport
) : CrawlIndexerIngestChannel<SiteDocument,
	SemanticIndexChannelOptions<SiteDocument>,
	SemanticIndexChannel<SiteDocument>>(
	loggerFactory,
	diagnostics,
	errorTracker,
	endpoint,
	transport,
	o => new(o),
	t => new(t)
	{
		BulkOperationIdLookup = d => d.Url,
		GetMapping = (inferenceId, _) => SiteIndexMapping.CreateMapping(inferenceId),
		GetMappingSettings = (_, _) => SiteIndexMapping.CreateSettings(),
		IndexFormat = "site-semantic-{0:yyyy.MM.dd.HHmmss}",
		ActiveSearchAlias = "site-semantic",
		IndexNumThreads = endpoint.IndexNumThreads,
		SearchNumThreads = endpoint.SearchNumThreads,
		InferenceCreateTimeout = TimeSpan.FromMinutes(endpoint.BootstrapTimeout ?? 4),
		UsePreexistingInferenceIds = !endpoint.NoElasticInferenceService,
		InferenceId = endpoint.NoElasticInferenceService ? null : ".elser-2-elastic",
		SearchInferenceId = endpoint.NoElasticInferenceService ? null : ".elser-2-elastic"
	},
	"site-semantic",
	"site-semantic"
);
