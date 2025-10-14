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
using Elastic.Ingest.Elasticsearch.Indices;
using Elastic.Ingest.Elasticsearch.Semantic;
using Elastic.Markdown.Helpers;
using Elastic.Markdown.IO;
using Elastic.Transport;
using Elastic.Transport.Products.Elasticsearch;
using Markdig.Syntax;
using Microsoft.AspNetCore.Server.Kestrel;
using Microsoft.Extensions.Logging;

namespace Elastic.Markdown.Exporters;

public interface IElasticsearchExporter
{
	ValueTask<bool> TryWrite(DocumentationDocument document, Cancel ctx = default);
	ValueTask<bool> RefreshAsync(Cancel ctx = default);
	ValueTask<bool> StopAsync(Cancel ctx = default);
}

public abstract class ElasticsearchExporter<TChannelOptions, TChannel> : IDisposable, IElasticsearchExporter where TChannelOptions : CatalogIndexChannelOptionsBase<DocumentationDocument>
	where TChannel : CatalogIndexChannel<DocumentationDocument, TChannelOptions>
{
	private readonly IDiagnosticsCollector _collector;
	public TChannel Channel { get; }
	private readonly ILogger _logger;

	protected ElasticsearchExporter(
		ILoggerFactory logFactory,
		IDiagnosticsCollector collector,
		ElasticsearchEndpoint endpoint,
		DistributedTransport transport,
		Func<TChannelOptions, TChannel> createChannel,
		Func<DistributedTransport, TChannelOptions> createOptions
	)
	{
		_collector = collector;
		_logger = logFactory.CreateLogger<ElasticsearchExporter<TChannelOptions, TChannel>>();
		//The max num threads per allocated node, from testing its best to limit our max concurrency
		//producing to this number as well
		var options = createOptions(transport);
		var i = 0;
		options.BufferOptions = new BufferOptions
		{
			OutboundBufferMaxSize = endpoint.BufferSize,
			ExportMaxConcurrency = endpoint.IndexNumThreads,
			ExportMaxRetries = endpoint.MaxRetries
		};
		options.SerializerContext = SourceGenerationContext.Default;
		options.ExportBufferCallback = () =>
		{
			var count = Interlocked.Increment(ref i);
			_logger.LogInformation("Exported {Count} documents to Elasticsearch index {Format}",
				count * endpoint.BufferSize, options.IndexFormat);
		};
		options.ExportExceptionCallback = e =>
		{
			_logger.LogError(e, "Failed to export document");
			_collector.EmitGlobalError("Elasticsearch export: failed to export document", e);
		};
		options.ServerRejectionCallback = items => _logger.LogInformation("Server rejection: {Rejection}", items.First().Item2);
		Channel = createChannel(options);
		_logger.LogInformation($"Bootstrapping {nameof(SemanticIndexChannel<DocumentationDocument>)} Elasticsearch target for indexing");
	}

	public async ValueTask<bool> StopAsync(Cancel ctx = default)
	{
		_logger.LogInformation("Waiting to drain all inflight exports to Elasticsearch");
		var drained = await Channel.WaitForDrainAsync(null, ctx);
		if (!drained)
			_collector.EmitGlobalError("Elasticsearch export: failed to complete indexing in a timely fashion while shutting down");

		_logger.LogInformation("Refreshing target index {Index}", Channel.IndexName);
		var refreshed = await Channel.RefreshAsync(ctx);
		if (!refreshed)
			_collector.EmitGlobalError($"Refreshing target index {Channel.IndexName} did not complete successfully");

		_logger.LogInformation("Applying aliases to {Index}", Channel.IndexName);
		var swapped = await Channel.ApplyAliasesAsync(ctx);
		if (!swapped)
			_collector.EmitGlobalError($"${nameof(ElasticsearchMarkdownExporter)} failed to apply aliases to index {Channel.IndexName}");

		return drained && refreshed && swapped;
	}

	public async ValueTask<bool> RefreshAsync(Cancel ctx = default) => await Channel.RefreshAsync(ctx);

	public async ValueTask<bool> TryWrite(DocumentationDocument document, Cancel ctx = default)
	{
		if (Channel.TryWrite(document))
			return true;

		if (await Channel.WaitToWriteAsync(ctx))
			return Channel.TryWrite(document);
		return false;
	}


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


	public void Dispose()
	{
		Channel.Complete();
		Channel.Dispose();

		GC.SuppressFinalize(this);
	}

}

public class ElasticsearchLexicalExporter(
	ILoggerFactory logFactory,
	IDiagnosticsCollector collector,
	ElasticsearchEndpoint endpoint,
	string indexNamespace,
	DistributedTransport transport,
	DateTimeOffset batchIndexDate
)
	: ElasticsearchExporter<CatalogIndexChannelOptions<DocumentationDocument>, CatalogIndexChannel<DocumentationDocument>>
	(logFactory, collector, endpoint, transport, o => new(o), t => new(t)
	{
		BulkOperationIdLookup = d => d.Url,
		ScriptedHashBulkUpsertLookup = (d, channelHash) =>
		{
			var rand = string.Empty;
			//if (d.Url.StartsWith("/docs/reference/search-connectors"))
			//	rand = Guid.NewGuid().ToString("N");
			d.Hash = HashedBulkUpdate.CreateHash(channelHash, rand, d.Url, d.Body ?? string.Empty, string.Join(",", d.Headings.OrderBy(h => h)));
			d.LastUpdated = batchIndexDate;
			d.BatchIndexDate = batchIndexDate;
			return new HashedBulkUpdate("hash", d.Hash, "ctx._source.batch_index_date = params.batch_index_date",
				new Dictionary<string, string>
				{
					{ "batch_index_date", d.BatchIndexDate.ToString("o") }
				});
		},
		GetMapping = () => CreateMapping(null),
		GetMappingSettings = CreateMappingSetting,
		IndexFormat =
			$"{endpoint.IndexNamePrefix.Replace("semantic", "lexical").ToLowerInvariant()}-{indexNamespace.ToLowerInvariant()}-{{0:yyyy.MM.dd.HHmmss}}",
		ActiveSearchAlias = $"{endpoint.IndexNamePrefix.Replace("semantic", "lexical").ToLowerInvariant()}-{indexNamespace.ToLowerInvariant()}"
	});

public class ElasticsearchSemanticExporter(
	ILoggerFactory logFactory,
	IDiagnosticsCollector collector,
	ElasticsearchEndpoint endpoint,
	string indexNamespace,
	DistributedTransport transport
)
	: ElasticsearchExporter<SemanticIndexChannelOptions<DocumentationDocument>, SemanticIndexChannel<DocumentationDocument>>
	(logFactory, collector, endpoint, transport, o => new(o), t => new(t)
	{
		BulkOperationIdLookup = d => d.Url,
		GetMapping = (inferenceId, _) => CreateMapping(inferenceId),
		GetMappingSettings = (_, _) => CreateMappingSetting(),
		IndexFormat = $"{endpoint.IndexNamePrefix.ToLowerInvariant()}-{indexNamespace.ToLowerInvariant()}-{{0:yyyy.MM.dd.HHmmss}}",
		ActiveSearchAlias = $"{endpoint.IndexNamePrefix}-{indexNamespace.ToLowerInvariant()}",
		IndexNumThreads = endpoint.IndexNumThreads,
		SearchNumThreads = endpoint.SearchNumThreads,
		InferenceCreateTimeout = TimeSpan.FromMinutes(endpoint.BootstrapTimeout ?? 4)
	});

public class ElasticsearchMarkdownExporter : IMarkdownExporter, IDisposable
{
#pragma warning disable IDE0052
	private readonly IDiagnosticsCollector _collector;
#pragma warning restore IDE0052
	private readonly ILogger _logger;
	private readonly ElasticsearchLexicalExporter _lexicalChannel;
#pragma warning disable IDE0052
	private readonly ElasticsearchSemanticExporter _semanticChannel;
#pragma warning restore IDE0052
	private readonly IElasticsearchExporter _channel;

	protected ElasticsearchEndpoint Endpoint { get; }

	private readonly DateTimeOffset _batchIndexDate = DateTimeOffset.UtcNow;
	private readonly DistributedTransport _transport;

	public ElasticsearchMarkdownExporter(
		ILoggerFactory logFactory,
		IDiagnosticsCollector collector,
		DocumentationEndpoints endpoints,
		string indexNamespace
	)
	{
		_collector = collector;
		_logger = logFactory.CreateLogger<ElasticsearchMarkdownExporter>();
		Endpoint = endpoints.Elasticsearch;

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

		_transport = new DistributedTransport(configuration);

		_lexicalChannel = new ElasticsearchLexicalExporter(logFactory, collector, es, indexNamespace, _transport, _batchIndexDate);
		_semanticChannel = new ElasticsearchSemanticExporter(logFactory, collector, es, indexNamespace, _transport);
		_channel = _lexicalChannel;

	}

	/// <inheritdoc />
	public async ValueTask StartAsync(Cancel ctx = default)
	{
		_ = await _lexicalChannel.Channel.BootstrapElasticsearchAsync(BootstrapMethod.Failure, null, ctx);
		return;
	}

	/// <inheritdoc />
	public async ValueTask StopAsync(Cancel ctx = default)
	{
		var stopped = await _channel.StopAsync(ctx);

		var semanticIndex = _semanticChannel.Channel.IndexName;
		var semanticIndexHead = await _transport.HeadAsync(semanticIndex, ctx);
		if (!semanticIndexHead.ApiCallDetails.HasSuccessfulStatusCode)
		{
			_ = await _semanticChannel.Channel.BootstrapElasticsearchAsync(BootstrapMethod.Failure, null, ctx);
			var semanticIndexPut = await _transport.PutAsync<StringResponse>(semanticIndex, PostData.String("{}"), ctx);
			if (!semanticIndexPut.ApiCallDetails.HasSuccessfulStatusCode)
				throw new Exception($"Failed to create index {semanticIndex}: {semanticIndexPut}");
			_ = await _semanticChannel.Channel.ApplyAliasesAsync(ctx);
		}
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
			Headings = headings
		};
		return await _channel.TryWrite(doc, ctx);
	}

	/// <inheritdoc />
	public async ValueTask<bool> FinishExportAsync(IDirectoryInfo outputFolder, Cancel ctx)
	{
		if (_channel is null)
			return false;

		return await _channel.RefreshAsync(ctx);
	}

	/// <inheritdoc />
	public void Dispose()
	{
		_lexicalChannel.Dispose();
		GC.SuppressFinalize(this);
	}
}
