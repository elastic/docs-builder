// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Channels;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Search;
using Elastic.Documentation.Serialization;
using Elastic.Ingest.Elasticsearch;
using Elastic.Ingest.Elasticsearch.Catalog;
using Elastic.Ingest.Elasticsearch.Semantic;
using Elastic.Markdown.Exporters;
using Elastic.Markdown.IO;
using Elastic.Transport;
using Elastic.Transport.Products.Elasticsearch;
using Markdig.Syntax;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Tooling.Exporters;

public class ElasticsearchMarkdownExporter(ILoggerFactory logFactory, IDiagnosticsCollector collector, DocumentationEndpoints endpoints)
	: ElasticsearchMarkdownExporterBase<CatalogIndexChannelOptions<DocumentationDocument>, CatalogIndexChannel<DocumentationDocument>>
		(logFactory, collector, endpoints)
{
	/// <inheritdoc />
	protected override CatalogIndexChannelOptions<DocumentationDocument> NewOptions(DistributedTransport transport) => new(transport)
	{
		GetMapping = () => CreateMapping(null),
		IndexFormat = "documentation{0:yyyy.MM.dd.HHmmss}",
		ActiveSearchAlias = "documentation"
	};

	/// <inheritdoc />
	protected override CatalogIndexChannel<DocumentationDocument> NewChannel(CatalogIndexChannelOptions<DocumentationDocument> options) => new(options);
}

public class ElasticsearchMarkdownSemanticExporter(ILoggerFactory logFactory, IDiagnosticsCollector collector, DocumentationEndpoints endpoints)
	: ElasticsearchMarkdownExporterBase<SemanticIndexChannelOptions<DocumentationDocument>, SemanticIndexChannel<DocumentationDocument>>
		(logFactory, collector, endpoints)
{
	/// <inheritdoc />
	protected override SemanticIndexChannelOptions<DocumentationDocument> NewOptions(DistributedTransport transport) => new(transport)
	{
		GetMapping = (inferenceId, _) => CreateMapping(inferenceId),
		GetMappingSettings = (_, _) => CreateMappingSetting(),
		IndexFormat = "semantic-documentation-{0:yyyy.MM.dd.HHmmss}",
		ActiveSearchAlias = "semantic-documentation",
		IndexNumThreads = IndexNumThreads,
		InferenceCreateTimeout = TimeSpan.FromMinutes(4)
	};

	/// <inheritdoc />
	protected override SemanticIndexChannel<DocumentationDocument> NewChannel(SemanticIndexChannelOptions<DocumentationDocument> options) => new(options);
}

public abstract class ElasticsearchMarkdownExporterBase<TChannelOptions, TChannel>(
	ILoggerFactory logFactory,
	IDiagnosticsCollector collector,
	DocumentationEndpoints endpoints)
	: IMarkdownExporter, IDisposable
	where TChannelOptions : CatalogIndexChannelOptionsBase<DocumentationDocument>
	where TChannel : CatalogIndexChannel<DocumentationDocument, TChannelOptions>
{
	private TChannel? _channel;
	private readonly ILogger<IMarkdownExporter> _logger = logFactory.CreateLogger<IMarkdownExporter>();

	protected abstract TChannelOptions NewOptions(DistributedTransport transport);
	protected abstract TChannel NewChannel(TChannelOptions options);

	protected int IndexNumThreads => 8;

	protected static string CreateMappingSetting() =>
		// language=json
		"""
		{
		  "analysis": {
		    "analyzer": {
		      "synonyms_analyzer": {
		        "tokenizer": "whitespace",
		        "filter": [
		          "lowercase",
		          "synonyms_filter"
		        ]
		      }
		    },
		    "filter": {
		      "synonyms_filter": {
		        "type": "synonym",
		        "synonyms_set": "docs",
		        "updateable": true
		      }
		    }
		  }
		}
		""";

	protected static string CreateMapping(string? inferenceId) =>
		// langugage=json
		$$"""
		  {
		    "properties": {
		      "title": {
		        "type": "text",
		        "search_analyzer": "synonyms_analyzer",
		        "fields": {
		          "keyword": {
		            "type": "keyword"
		          }
		          {{(!string.IsNullOrWhiteSpace(inferenceId) ? $$""", "semantic_text": {{{InferenceMapping(inferenceId)}}}""" : "")}}
		        }
		      },
		      "url": {
		        "type": "text",
		        "fields": {
		          "keyword": {
		            "type": "keyword"
		          }
		        }
		      },
		      "url_segment_count": {
		        "type": "integer"
		      },
		      "body": {
		        "type": "text"
		      }
		      {{(!string.IsNullOrWhiteSpace(inferenceId) ? AbstractInferenceMapping(inferenceId) : AbstractMapping())}}
		    }
		  }
		  """;

	private static string AbstractMapping() =>
		// langugage=json
		"""
		, "abstract": {
			"type": "text"
		}
		""";

	private static string InferenceMapping(string inferenceId) =>
		// langugage=json
		$"""
		 	"type": "semantic_text",
		 	"inference_id": "{inferenceId}"
		 """;

	private static string AbstractInferenceMapping(string inferenceId) =>
		// langugage=json
		$$"""
		  , "abstract": {
		  	{{InferenceMapping(inferenceId)}}
		  }
		  """;

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
		var options = NewOptions(transport);
		options.BufferOptions = new BufferOptions
		{
			OutboundBufferMaxSize = 100,
			ExportMaxConcurrency = IndexNumThreads,
			ExportMaxRetries = 3
		};
		options.SerializerContext = SourceGenerationContext.Default;
		options.ExportBufferCallback = () => _logger.LogInformation("Exported buffer to Elasticsearch");
		options.ExportExceptionCallback = e => _logger.LogError(e, "Failed to export document");
		options.ServerRejectionCallback = items => _logger.LogInformation("Server rejection: {Rejection}", items.First().Item2);
		_channel = NewChannel(options);
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
		var url = file.Url;

		if (url is "/docs" or "/docs/404")
		{
			// Skip the root and 404 pages
			_logger.LogInformation("Skipping export for {Url}", url);
			return true;
		}

		IPositionalNavigation navigation = fileContext.DocumentationSet;

		//use LLM text if it was already provided (because we run with both llm and elasticsearch output)
		var body = fileContext.LLMText ??= LlmMarkdownExporter.ConvertToLlmMarkdown(document, fileContext.BuildContext);

		var headings = fileContext.Document.Descendants<HeadingBlock>()
			.Select(h => (h.GetData("header") as string) ?? string.Empty)
			.Where(text => !string.IsNullOrEmpty(text))
			.ToArray();

		var doc = new DocumentationDocument
		{
			Title = file.Title,
			Url = url,
			Body = body,
			Description = fileContext.SourceFile.YamlFrontMatter?.Description,

			Abstract = !string.IsNullOrEmpty(body)
				? body[..Math.Min(body.Length, 400)] + " " + string.Join(" \n- ", headings)
				: string.Empty,
			Applies = fileContext.SourceFile.YamlFrontMatter?.AppliesTo,
			UrlSegmentCount = url.Split('/', StringSplitOptions.RemoveEmptyEntries).Length,
			Parents = navigation.GetParentsOfMarkdownFile(file).Select(i => new ParentDocument
			{
				Title = i.NavigationTitle,
				Url = i.Url
			}).Reverse().ToArray(),
			Headings = headings
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
