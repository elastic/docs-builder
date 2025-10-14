// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Channels;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Extensions;
using Elastic.Documentation.Search;
using Elastic.Documentation.Serialization;
using Elastic.Ingest.Elasticsearch;
using Elastic.Ingest.Elasticsearch.Catalog;
using Elastic.Ingest.Elasticsearch.Semantic;
using Elastic.Markdown.Helpers;
using Elastic.Markdown.IO;
using Elastic.Transport;
using Elastic.Transport.Products.Elasticsearch;
using Markdig.Syntax;
using Microsoft.Extensions.Logging;

namespace Elastic.Markdown.Exporters;

public class ElasticsearchMarkdownExporter(ILoggerFactory logFactory, IDiagnosticsCollector collector, string indexNamespace, DocumentationEndpoints endpoints)
	: ElasticsearchMarkdownExporterBase<CatalogIndexChannelOptions<DocumentationDocument>, CatalogIndexChannel<DocumentationDocument>>
		(logFactory, collector, endpoints)
{
	/// <inheritdoc />
	protected override CatalogIndexChannelOptions<DocumentationDocument> NewOptions(DistributedTransport transport) => new(transport)
	{
		BulkOperationIdLookup = d => d.Url,
		GetMapping = () => CreateMapping(null),
		GetMappingSettings = CreateMappingSetting,
		IndexFormat = $"{Endpoint.IndexNamePrefix.ToLowerInvariant()}-{indexNamespace.ToLowerInvariant()}-{{0:yyyy.MM.dd.HHmmss}}",
		ActiveSearchAlias = $"{Endpoint.IndexNamePrefix}-{indexNamespace.ToLowerInvariant()}",
	};

	/// <inheritdoc />
	protected override CatalogIndexChannel<DocumentationDocument> NewChannel(CatalogIndexChannelOptions<DocumentationDocument> options) => new(options);
}

public class ElasticsearchMarkdownSemanticExporter(ILoggerFactory logFactory, IDiagnosticsCollector collector, string indexNamespace, DocumentationEndpoints endpoints)
	: ElasticsearchMarkdownExporterBase<SemanticIndexChannelOptions<DocumentationDocument>, SemanticIndexChannel<DocumentationDocument>>
		(logFactory, collector, endpoints)
{
	/// <inheritdoc />
	protected override SemanticIndexChannelOptions<DocumentationDocument> NewOptions(DistributedTransport transport) => new(transport)
	{
		BulkOperationIdLookup = d => d.Url,
		GetMapping = (inferenceId, _) => CreateMapping(inferenceId),
		GetMappingSettings = (_, _) => CreateMappingSetting(),
		IndexFormat = $"{Endpoint.IndexNamePrefix.ToLowerInvariant()}-{indexNamespace.ToLowerInvariant()}-{{0:yyyy.MM.dd.HHmmss}}",
		ActiveSearchAlias = $"{Endpoint.IndexNamePrefix}-{indexNamespace.ToLowerInvariant()}",
		IndexNumThreads = Endpoint.IndexNumThreads,
		SearchNumThreads = Endpoint.SearchNumThreads,
		InferenceCreateTimeout = TimeSpan.FromMinutes(Endpoint.BootstrapTimeout ?? 4),
	};

	/// <inheritdoc />
	protected override SemanticIndexChannel<DocumentationDocument> NewChannel(SemanticIndexChannelOptions<DocumentationDocument> options) => new(options);
}


public abstract class ElasticsearchMarkdownExporterBase<TChannelOptions, TChannel>(
	ILoggerFactory logFactory,
	IDiagnosticsCollector collector,
	DocumentationEndpoints endpoints
)
	: IMarkdownExporter, IDisposable
	where TChannelOptions : CatalogIndexChannelOptionsBase<DocumentationDocument>
	where TChannel : CatalogIndexChannel<DocumentationDocument, TChannelOptions>
{
	private TChannel? _channel;
	private readonly ILogger<IMarkdownExporter> _logger = logFactory.CreateLogger<IMarkdownExporter>();

	protected abstract TChannelOptions NewOptions(DistributedTransport transport);
	protected abstract TChannel NewChannel(TChannelOptions options);

	protected ElasticsearchEndpoint Endpoint { get; } = endpoints.Elasticsearch;

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
		      },
		      "highlight_analyzer": {
		        "tokenizer": "standard",
		        "filter": [
		          "lowercase",
		          "english_stop"
		        ]
		      },
		      "hierarchy_analyzer": { "tokenizer": "path_tokenizer" }
		    },
		    "filter": {
		      "synonyms_filter": {
		        "type": "synonym",
		        "synonyms_set": "docs",
		        "updateable": true
		      },
		      "english_stop": {
		        "type": "stop",
		        "stopwords": "_english_"
		      }
		    },
		    "tokenizer": {
		      "path_tokenizer": {
		        "type": "path_hierarchy",
		        "delimiter": "/"
		      }
		    }
		  }
		}
		""";

	protected static string CreateMapping(string? inferenceId) =>
		$$"""
		  {
		    "properties": {
		      "url" : {
		        "type": "keyword",
		        "fields": {
		          "match": { "type": "text" },
		          "prefix": { "type": "text", "analyzer" : "hierarchy_analyzer" }
		        }
		      },
		      "hash" : { "type" : "keyword" },
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
		      "url_segment_count": {
		        "type": "integer"
		      },
		      "body": {
		        "type": "text"
		      },
		      "stripped_body": {
		        "type": "text",
		        "search_analyzer": "highlight_analyzer",
		        "term_vector": "with_positions_offsets"
		      }
		      {{(!string.IsNullOrWhiteSpace(inferenceId) ? AbstractInferenceMapping(inferenceId) : AbstractMapping())}}
		    }
		  }
		  """;

	private static string AbstractMapping() =>
		"""
		, "abstract": {
			"type": "text"
		}
		""";

	private static string InferenceMapping(string inferenceId) =>
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
				: es is { Username: { } username, Password: { } password }
					? new BasicAuthentication(username, password)
					: null,
			EnableHttpCompression = true,
			DebugMode = Endpoint.DebugMode,
			CertificateFingerprint = Endpoint.CertificateFingerprint,
			ProxyAddress = Endpoint.ProxyAddress,
			ProxyPassword = Endpoint.ProxyPassword,
			ProxyUsername = Endpoint.ProxyUsername,
			ServerCertificateValidationCallback = Endpoint.DisableSslVerification
				? CertificateValidations.AllowAll
				: Endpoint.Certificate is { } cert
					? Endpoint.CertificateIsNotRoot
						? CertificateValidations.AuthorityPartOfChain(cert)
						: CertificateValidations.AuthorityIsRoot(cert)
					: null
		};

		var transport = new DistributedTransport(configuration);

		//The max num threads per allocated node, from testing its best to limit our max concurrency
		//producing to this number as well
		var options = NewOptions(transport);
		var i = 0;
		options.BufferOptions = new BufferOptions
		{
			OutboundBufferMaxSize = Endpoint.BufferSize,
			ExportMaxConcurrency = Endpoint.IndexNumThreads,
			ExportMaxRetries = Endpoint.MaxRetries,
		};
		options.SerializerContext = SourceGenerationContext.Default;
		options.ExportBufferCallback = () =>
		{
			var count = Interlocked.Increment(ref i);
			_logger.LogInformation("Exported {Count} documents to Elasticsearch index {Format}",
				count * Endpoint.BufferSize, options.IndexFormat);
		};
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
			collector.EmitGlobalError($"${nameof(ElasticsearchMarkdownExporter)} failed to apply aliases to index {_channel.IndexName}");
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
		var url = fileContext.NavigationItem.Url;

		if (url is "/docs" or "/docs/404")
		{
			// Skip the root and 404 pages
			_logger.LogInformation("Skipping export for {Url}", url);
			return true;
		}

		IPositionalNavigation navigation = fileContext.DocumentationSet;

		// Remove the first h1 because we already have the title
		// and we don't want it to appear in the body
		var h1 = fileContext.Document.Descendants<HeadingBlock>().FirstOrDefault(h => h.Level == 1);
		if (h1 is not null)
			_ = fileContext.Document.Remove(h1);

		var body = LlmMarkdownExporter.ConvertToLlmMarkdown(fileContext.Document, fileContext.BuildContext);

		var headings = fileContext.Document.Descendants<HeadingBlock>()
			.Select(h => h.GetData("header") as string ?? string.Empty) // TODO: Confirm that 'header' data is correctly set for all HeadingBlock instances and that this extraction is reliable.
			.Where(text => !string.IsNullOrEmpty(text))
			.ToArray();

		var @abstract = !string.IsNullOrEmpty(body)
			? body[..Math.Min(body.Length, 400)] + " " + string.Join(" \n- ", headings)
			: string.Empty;

		var doc = new DocumentationDocument
		{
			Url = url,
			Hash = ShortId.Create(url, body),
			Title = file.Title,
			Body = body,
			StrippedBody = body.StripMarkdown(),
			Description = fileContext.SourceFile.YamlFrontMatter?.Description,
			Abstract = @abstract,
			Applies = fileContext.SourceFile.YamlFrontMatter?.AppliesTo,
			UrlSegmentCount = url.Split('/', StringSplitOptions.RemoveEmptyEntries).Length,
			Parents = navigation.GetParentsOfMarkdownFile(file).Select(i => new ParentDocument
			{
				Title = i.NavigationTitle,
				Url = i.Url
			}).Reverse().ToArray(),
			Headings = headings,
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
