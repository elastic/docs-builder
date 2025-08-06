// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Search;
using Elastic.Documentation.Serialization;
using Elastic.Ingest.Elasticsearch;
using Elastic.Ingest.Elasticsearch.Catalog;
using Elastic.Ingest.Elasticsearch.Semantic;
using Elastic.Markdown.Exporters;
using Elastic.Transport;
using Elastic.Transport.Products.Elasticsearch;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Tooling.Exporters;

public class ElasticsearchMarkdownExporter(ILoggerFactory logFactory, IDiagnosticsCollector collector, DocumentationEndpoints endpoints)
	: IMarkdownExporter, IDisposable
{
	private CatalogIndexChannel<DocumentationDocument>? _channel;
	private readonly ILogger<ElasticsearchMarkdownExporter> _logger = logFactory.CreateLogger<ElasticsearchMarkdownExporter>();

	public async ValueTask StartAsync(Cancel ctx = default)
	{
		if (_channel != null)
			return;

		var es = endpoints.Elasticsearch;
		var configuration = new ElasticsearchConfiguration(es.Uri)
		{
			Authentication = es.ApiKey is { } apiKey
				? new ApiKey(apiKey)
				: es.Username is { } username && es.Password is { } password
					? new BasicAuthentication(username, password)
					: null
		};

		var transport = new DistributedTransport(configuration);
		//The max num threads per allocated node, from testing its best to limit our max concurrency
		//producing to this number as well
		var indexNumThreads = 8;
		var options = new CatalogIndexChannelOptions<DocumentationDocument>(transport)
		{
			BufferOptions =
			{
				OutboundBufferMaxSize = 100,
				ExportMaxConcurrency = indexNumThreads,
				ExportMaxRetries = 3
			},
			SerializerContext = SourceGenerationContext.Default,
			// IndexNumThreads = indexNumThreads,
			IndexFormat = "documentation-{0:yyyy.MM.dd.HHmmss}",
			ActiveSearchAlias = "documentation",
			ExportBufferCallback = () => _logger.LogInformation("Exported buffer to Elasticsearch"),
			ExportExceptionCallback = e => _logger.LogError(e, "Failed to export document"),
			ServerRejectionCallback = items => _logger.LogInformation("Server rejection: {Rejection}", items.First().Item2),
			//GetMapping = (inferenceId, _) => // language=json
			GetMapping = () => // language=json
				$$"""
				  {
				    "properties": {
				      "title": { "type": "text" },
				      "body": {
				        "type": "text"
				      }
				    }
				  }
				  """
		};
		_channel = new CatalogIndexChannel<DocumentationDocument>(options);
		_logger.LogInformation($"Bootstrapping {nameof(SemanticIndexChannel<DocumentationDocument>)} Elasticsearch target for indexing");
		_ = await _channel.BootstrapElasticsearchAsync(BootstrapMethod.Failure, null, ctx);
	}

	public async ValueTask StopAsync(Cancel ctx = default)
	{
		if (_channel is null)
			return;

		_logger.LogInformation("Waiting to drain all inflight exports to Elasticsearch");
		var drained = await _channel.WaitForDrainAsync(null, ctx);
		if (!drained)
			collector.EmitGlobalError("Elasticsearch export: failed to complete indexing in a timely fashion while shutting down");

		_logger.LogInformation("Refreshing target index {Index}", _channel.IndexName);
		var refreshed = await _channel.RefreshAsync(ctx);
		if (!refreshed)
			_logger.LogError("Refreshing target index {Index} did not complete successfully", _channel.IndexName);

		_logger.LogInformation("Applying aliases to {Index}", _channel.IndexName);
		var swapped = await _channel.ApplyAliasesAsync(ctx);
		if (!swapped)
			collector.EmitGlobalError($"{nameof(ElasticsearchMarkdownExporter)} failed to apply aliases to index {_channel.IndexName}");
	}

	public void Dispose()
	{
		if (_channel is not null)
		{
			_channel.Complete();
			_channel.Dispose();
		}
		GC.SuppressFinalize(this);
	}

	private async ValueTask<bool> TryWrite(DocumentationDocument document, Cancel ctx = default)
	{
		if (_channel is null)
			return false;

		if (_channel.TryWrite(document))
			return true;

		if (await _channel.WaitToWriteAsync(ctx))
			return _channel.TryWrite(document);
		return false;
	}

	public async ValueTask<bool> ExportAsync(MarkdownExportFileContext fileContext, Cancel ctx)
	{
		var file = fileContext.SourceFile;
		var document = fileContext.Document;

		var url = file.Url;

		//use LLM text if it was already provided (because we run with both llm and elasticsearch output)
		var body = fileContext.LLMText ??= LlmMarkdownExporter.ConvertToLlmMarkdown(fileContext.Document, fileContext.BuildContext);
		var doc = new DocumentationDocument
		{
			Title = file.Title,
			Url = url,
			Body = body,
			Description = fileContext.SourceFile.YamlFrontMatter?.Description,
			Applies = fileContext.SourceFile.YamlFrontMatter?.AppliesTo,
		};
		return await TryWrite(doc, ctx);
	}

	/// <inheritdoc />
	public async ValueTask<bool> FinishExportAsync(IDirectoryInfo outputFolder, Cancel ctx)
	{
		if (_channel is null)
			return false;

		return await _channel.RefreshAsync(ctx);
	}
}
