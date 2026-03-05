// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace CrawlIndexer.Indexing;

/// <summary>
/// Shared index mapping components for crawl-indexer indices.
/// </summary>
internal static class IndexMappingBase
{
	/// <summary>
	/// Creates common analysis settings used by all crawl-indexer indices.
	/// </summary>
	public static string CreateAnalysisSettings(string? defaultPipeline = null, bool includeSynonymsAnalyzer = false)
	{
		var pipelineSetting = defaultPipeline is not null ? $"\"default_pipeline\": \"{defaultPipeline}\"," : "";
		var synonymsAnalyzer = includeSynonymsAnalyzer
			? """
			  "synonyms_fixed_analyzer": {
			    "tokenizer": "group_tokenizer",
			    "filter": [
			      "lowercase",
			      "kstem"
			    ]
			  },
			  """
			: "";

		// language=json
		return
			$$$"""
			{
				{{{pipelineSetting}}}
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
					{{{synonymsAnalyzer}}}
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

	/// <summary>
	/// Creates the semantic_text field mapping for inference.
	/// </summary>
	public static string InferenceMapping(string inferenceId) =>
		$"""
		 	"type": "semantic_text",
		 	"inference_id": "{inferenceId}"
		 """;

	/// <summary>
	/// Creates optional semantic_text subfield for a text field.
	/// </summary>
	public static string OptionalSemanticField(string? inferenceId) =>
		!string.IsNullOrWhiteSpace(inferenceId)
			? $", \"semantic_text\": {{{InferenceMapping(inferenceId)}}}"
			: "";

	/// <summary>
	/// Creates optional semantic_text subfield as first field (no leading comma).
	/// </summary>
	public static string OptionalSemanticFieldFirst(string? inferenceId) =>
		!string.IsNullOrWhiteSpace(inferenceId)
			? $"\"semantic_text\": {{{InferenceMapping(inferenceId)}}}"
			: "";

	/// <summary>
	/// Common AI enrichment field mappings.
	/// </summary>
	public static string AiEnrichmentFields(string? inferenceId, string? textAnalyzer = null)
	{
		var analyzerSuffix = textAnalyzer is not null ? $", \"analyzer\": \"{textAnalyzer}\"" : "";
		return $$"""
		      "enrichment_key" : { "type" : "keyword" },
		      "ai_rag_optimized_summary": {
		        "type": "text"{{analyzerSuffix}},
		        "fields": {
		          {{OptionalSemanticFieldFirst(inferenceId)}}
		        }
		      },
		      "ai_short_summary": {
		        "type": "text"
		      },
		      "ai_search_query": {
		        "type": "keyword"
		      },
		      "ai_questions": {
		        "type": "text",
		        "fields": {
		          {{OptionalSemanticFieldFirst(inferenceId)}}
		        }
		      },
		      "ai_use_cases": {
		        "type": "text",
		        "fields": {
		          {{OptionalSemanticFieldFirst(inferenceId)}}
		        }
		      },
		      "enrichment_prompt_hash": {
		        "type": "keyword"
		      }
		""";
	}

	/// <summary>
	/// Common HTTP caching field mappings.
	/// </summary>
	public static string HttpCachingFields() =>
		"""
		      "http_etag": { "type": "keyword", "index": false },
		      "http_last_modified": { "type": "date" }
		""";

	/// <summary>
	/// Common URL field mapping with prefix analyzer.
	/// </summary>
	public static string UrlField() =>
		"""
		      "url": {
		        "type": "keyword",
		        "fields": {
		          "match": { "type": "text" },
		          "prefix": { "type": "text", "analyzer" : "hierarchy_analyzer" }
		        }
		      }
		""";
}
