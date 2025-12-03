// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Search;
using Elastic.Ingest.Elasticsearch.Catalog;

namespace Elastic.Markdown.Exporters.Elasticsearch;

public abstract partial class ElasticsearchIngestChannel<TChannelOptions, TChannel>
	where TChannelOptions : CatalogIndexChannelOptionsBase<DocumentationDocument>
	where TChannel : CatalogIndexChannel<DocumentationDocument, TChannelOptions>
{
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

	// language=json
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
}
