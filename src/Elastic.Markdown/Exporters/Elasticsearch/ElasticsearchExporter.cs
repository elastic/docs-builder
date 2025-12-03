// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Channels;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Search;
using Elastic.Documentation.Serialization;
using Elastic.Ingest.Elasticsearch.Catalog;
using Elastic.Ingest.Elasticsearch.Indices;
using Elastic.Ingest.Elasticsearch.Semantic;
using Elastic.Transport;
using Microsoft.Extensions.Logging;

namespace Elastic.Markdown.Exporters.Elasticsearch;

public class ElasticsearchLexicalExporter(
	ILoggerFactory logFactory,
	IDiagnosticsCollector collector,
	ElasticsearchEndpoint endpoint,
	string indexNamespace,
	DistributedTransport transport,
	string[] indexTimeSynonyms
)
	: ElasticsearchExporter<CatalogIndexChannelOptions<DocumentationDocument>, CatalogIndexChannel<DocumentationDocument>>
	(logFactory, collector, endpoint, transport, o => new(o), t => new(t)
	{
		BulkOperationIdLookup = d => d.Url,
		// hash, last_updated and batch_index_date are all set before the docs are written to the channel
		ScriptedHashBulkUpsertLookup = (d, _) => new HashedBulkUpdate("hash", d.Hash, "ctx._source.batch_index_date = params.batch_index_date",
			new Dictionary<string, string>
			{
				{ "batch_index_date", d.BatchIndexDate.ToString("o") }
			}),
		GetMapping = () => CreateMapping(null),
		GetMappingSettings = () => CreateMappingSetting($"docs-{indexNamespace}", indexTimeSynonyms),
		IndexFormat =
			$"{endpoint.IndexNamePrefix.Replace("semantic", "lexical").ToLowerInvariant()}-{indexNamespace.ToLowerInvariant()}-{{0:yyyy.MM.dd.HHmmss}}",
		ActiveSearchAlias = $"{endpoint.IndexNamePrefix.Replace("semantic", "lexical").ToLowerInvariant()}-{indexNamespace.ToLowerInvariant()}"
	});

public class ElasticsearchSemanticExporter(
	ILoggerFactory logFactory,
	IDiagnosticsCollector collector,
	ElasticsearchEndpoint endpoint,
	string indexNamespace,
	DistributedTransport transport,
	string[] indexTimeSynonyms
)
	: ElasticsearchExporter<SemanticIndexChannelOptions<DocumentationDocument>, SemanticIndexChannel<DocumentationDocument>>
	(logFactory, collector, endpoint, transport, o => new(o), t => new(t)
	{
		BulkOperationIdLookup = d => d.Url,
		GetMapping = (inferenceId, _) => CreateMapping(inferenceId),
		GetMappingSettings = (_, _) => CreateMappingSetting($"docs-{indexNamespace}", indexTimeSynonyms),
		IndexFormat = $"{endpoint.IndexNamePrefix.ToLowerInvariant()}-{indexNamespace.ToLowerInvariant()}-{{0:yyyy.MM.dd.HHmmss}}",
		ActiveSearchAlias = $"{endpoint.IndexNamePrefix}-{indexNamespace.ToLowerInvariant()}",
		IndexNumThreads = endpoint.IndexNumThreads,
		SearchNumThreads = endpoint.SearchNumThreads,
		InferenceCreateTimeout = TimeSpan.FromMinutes(endpoint.BootstrapTimeout ?? 4),
		UsePreexistingInferenceIds = !endpoint.NoElasticInferenceService,
		InferenceId = endpoint.NoElasticInferenceService ? null : ".elser-2-elastic",
		SearchInferenceId = endpoint.NoElasticInferenceService ? null : ".elser-2-elastic"
	});

public abstract class ElasticsearchExporter<TChannelOptions, TChannel> : IDisposable
	where TChannelOptions : CatalogIndexChannelOptionsBase<DocumentationDocument>
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
			_logger.LogInformation("Exported {Count} documents to Elasticsearch index {IndexName}",
				count * endpoint.BufferSize, Channel?.IndexName ?? string.Format(options.IndexFormat, "latest"));
		};
		options.ExportExceptionCallback = e =>
		{
			_logger.LogError(e, "Failed to export document");
			_collector.EmitGlobalError("Elasticsearch export: failed to export document", e);
		};
		options.ServerRejectionCallback = items =>
		{
			foreach (var (doc, responseItem) in items)
			{
				_collector.EmitGlobalError(
					$"Server rejection: {responseItem.Status} {responseItem.Error?.Type} {responseItem.Error?.Reason} for document {doc.Url}");
			}
		};
		Channel = createChannel(options);
		_logger.LogInformation("Created {Channel} Elasticsearch target for indexing", typeof(TChannel).Name);
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

	protected static string CreateMappingSetting(string synonymSetName, string[] synonyms)
	{
		var indexTimeSynonyms = $"[{string.Join(",", synonyms.Select(r => $"\"{r}\""))}]";
		// language=json
		return
			$$$"""
			{
				"analysis": {
				  "normalizer": {
					"keyword_normalizer": {
					  "type": "custom",
					  "char_filter": ["strip_non_word_chars"],
					  "filter": ["lowercase", "asciifolding", "trim"]
					}
				  },
				  "analyzer": {
					"starts_with_analyzer": {
					  "tokenizer": "starts_with_tokenizer",
					  "filter": [ "lowercase" ]
					},
					"starts_with_analyzer_search": {
					  "tokenizer": "keyword",
					  "filter": [ "lowercase" ]
					},
					"synonyms_fixed_analyzer": {
					  "tokenizer": "group_tokenizer",
					  "filter": [
						"lowercase",
						"synonyms_fixed_filter",
						"kstem"
					  ]
					},
					"synonyms_analyzer": {
					  "tokenizer": "group_tokenizer",
					  "filter": [
						"lowercase",
						"synonyms_filter",
						"kstem"
					  ]
					},
					"highlight_analyzer": {
					  "tokenizer": "group_tokenizer",
					  "filter": [
						"lowercase",
						"english_stop"
					  ]
					},
					"hierarchy_analyzer": { "tokenizer": "path_tokenizer" }
				  },
				  "char_filter": {
					"strip_non_word_chars": {
					  "type": "pattern_replace",
					  "pattern": "\\W",
					  "replacement": " "
					}
				  },
				  "filter": {
					"synonyms_fixed_filter": {
					  "type": "synonym_graph",
					  "synonyms": {{{indexTimeSynonyms}}}
					},
					"synonyms_filter": {
					  "type": "synonym_graph",
					  "synonyms_set": "{{{synonymSetName}}}",
					  "updateable": true
					},
					"english_stop": {
					  "type": "stop",
					  "stopwords": "_english_"
					}
				  },
				  "tokenizer": {
					"starts_with_tokenizer": {
					  "type": "edge_ngram",
					  "min_gram": 1,
					  "max_gram": 10,
					  "token_chars": [
						"letter",
						"digit",
						"symbol",
						"whitespace"
					  ]
					},
					"group_tokenizer": {
					  "type": "char_group",
					  "tokenize_on_chars": [ "whitespace", ",", ";", "?", "!", "(", ")", "&", "'", "\"", "/", "[", "]", "{", "}" ]
					},
					"path_tokenizer": {
					  "type": "path_hierarchy",
					  "delimiter": "/"
					}
				  }
				}
			  }
			""";
	}

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
		      "navigation_depth" : { "type" : "rank_feature", "positive_score_impact": false },
		      "navigation_table_of_contents" : { "type" : "rank_feature", "positive_score_impact": false },
		      "navigation_section" : { "type" : "keyword", "normalizer": "keyword_normalizer" },
		      "hidden" : {
		        "type" : "boolean"
		      },
		      "applies_to" : {
		        "type" : "nested",
		        "properties" : {
		          "type" : { "type" : "keyword", "normalizer": "keyword_normalizer" },
		          "sub-type" : { "type" : "keyword", "normalizer": "keyword_normalizer" },
		          "lifecycle" : { "type" : "keyword", "normalizer": "keyword_normalizer" },
		          "version" : { "type" : "version" }
		        }
		      },
		      "parents" : {
		        "type" : "object",
		        "properties" : {
		          "url" : {
		            "type": "keyword",
		            "fields": {
		              "match": { "type": "text" },
		              "prefix": { "type": "text", "analyzer" : "hierarchy_analyzer" }
		            }
		          },
		          "title": {
		            "type": "text",
		            "search_analyzer": "synonyms_analyzer",
		            "fields": {
		              "keyword": { "type": "keyword" }
		            }
		          }
		        }
		      },
		      "hash" : { "type" : "keyword" },
		      "search_title": {
		        "type": "text",
		        "analyzer": "synonyms_fixed_analyzer",
		        "search_analyzer": "synonyms_analyzer",
		        "fields": {
		          "completion": {
		            "type": "search_as_you_type",
		            "analyzer": "synonyms_fixed_analyzer",
		            "search_analyzer": "synonyms_analyzer",
		            "term_vector": "with_positions_offsets",
		            "index_options": "offsets"
		          }
		        }
		      },
		      "title": {
		        "type": "text",
		        "search_analyzer": "synonyms_analyzer",
		        "fields": {
		          "keyword": { "type": "keyword", "normalizer": "keyword_normalizer" },
		          "starts_with": { "type": "text", "analyzer": "starts_with_analyzer", "search_analyzer": "starts_with_analyzer_search" },
		          "completion": { "type": "search_as_you_type", "search_analyzer": "synonyms_analyzer" }
		          {{(!string.IsNullOrWhiteSpace(inferenceId) ? $$""", "semantic_text": {{{InferenceMapping(inferenceId)}}}""" : "")}}
		        }
		      },
		      "body": {
		        "type": "text"
		      },
		      "stripped_body": {
		        "type": "text",
		        "analyzer": "synonyms_fixed_analyzer",
		        "search_analyzer": "synonyms_analyzer",
		        "term_vector": "with_positions_offsets"
		      },
		      "headings": {
		        "type": "text",
		        "analyzer": "synonyms_fixed_analyzer",
		        "search_analyzer": "synonyms_analyzer"
		      },
		      "abstract": {
		        "type" : "text",
		        "analyzer": "synonyms_fixed_analyzer",
		        "search_analyzer": "synonyms_analyzer",
		        "fields" : {
		          {{(!string.IsNullOrWhiteSpace(inferenceId) ? $"\"semantic_text\": {{{InferenceMapping(inferenceId)}}}" : "")}}
		        }
		      }
		    }
		  }
		  """;

	private static string InferenceMapping(string inferenceId) =>
		$"""
		 	"type": "semantic_text",
		 	"inference_id": "{inferenceId}"
		 """;


	public void Dispose()
	{
		Channel.Complete();
		Channel.Dispose();

		GC.SuppressFinalize(this);
	}
}
