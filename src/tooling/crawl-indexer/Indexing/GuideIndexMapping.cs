// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace CrawlIndexer.Indexing;

/// <summary>
/// Elasticsearch index mapping for guide documents.
/// </summary>
internal static class GuideIndexMapping
{
	public static string CreateSettings(string? defaultPipeline = null) =>
		IndexMappingBase.CreateAnalysisSettings(defaultPipeline, includeSynonymsAnalyzer: true);

	// language=json
	public static string CreateMapping(string? inferenceId) =>
		$$"""
		  {
		    "properties": {
		      "type": { "type" : "keyword", "normalizer": "keyword_normalizer" },
		      "product": {
		        "type": "object",
		        "properties": {
		          "id": { "type": "keyword", "normalizer": "keyword_normalizer" },
		          "repository": { "type": "keyword", "normalizer": "keyword_normalizer" },
		          "version": { "type": "keyword" }
		        }
		      },
		      "related_products": {
		        "type": "object",
		        "properties": {
		          "id": { "type": "keyword", "normalizer": "keyword_normalizer" },
		          "repository": { "type": "keyword", "normalizer": "keyword_normalizer" },
		          "version": { "type": "keyword" }
		        }
		      },
		      {{IndexMappingBase.UrlField()}},
		      "navigation_depth" : { "type" : "rank_feature", "positive_score_impact": false },
		      "navigation_table_of_contents" : { "type" : "rank_feature", "positive_score_impact": false },
		      "navigation_section" : { "type" : "keyword", "normalizer": "keyword_normalizer" },
		      "hidden" : { "type" : "boolean" },
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
		        "fields": {
		          "completion": {
		            "type": "search_as_you_type",
		            "analyzer": "synonyms_fixed_analyzer",
		            "term_vector": "with_positions_offsets",
		            "index_options": "offsets"
		          }
		        }
		      },
		      "title": {
		        "type": "text",
		        "fields": {
		          "keyword": { "type": "keyword", "normalizer": "keyword_normalizer" },
		          "starts_with": { "type": "text", "analyzer": "starts_with_analyzer", "search_analyzer": "starts_with_analyzer_search" },
		          "completion": { "type": "search_as_you_type" }
		          {{IndexMappingBase.OptionalSemanticField(inferenceId)}}
		        }
		      },
		      "body": { "type": "text" },
		      "stripped_body": {
		        "type": "text",
		        "analyzer": "synonyms_fixed_analyzer",
		        "term_vector": "with_positions_offsets"
		      },
		      "headings": {
		        "type": "text",
		        "analyzer": "synonyms_fixed_analyzer"
		      },
		      "abstract": {
		        "type" : "text",
		        "analyzer": "synonyms_fixed_analyzer",
		        "fields" : {
		          {{IndexMappingBase.OptionalSemanticFieldFirst(inferenceId)}}
		        }
		      },
		      {{IndexMappingBase.AiEnrichmentFields(inferenceId, "synonyms_fixed_analyzer")}},
		      {{IndexMappingBase.HttpCachingFields()}}
		    }
		  }
		  """;
}
