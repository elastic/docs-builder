// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace CrawlIndexer.Indexing;

/// <summary>
/// Elasticsearch index mapping for site documents with multilingual support.
/// </summary>
internal static class SiteIndexMapping
{
	public static string CreateSettings(string? defaultPipeline = null) =>
		IndexMappingBase.CreateAnalysisSettings(defaultPipeline, includeSynonymsAnalyzer: false);

	// language=json
	public static string CreateMapping(string? inferenceId) =>
		$$"""
		  {
		    "properties": {
		      "type": { "type" : "keyword", "normalizer": "keyword_normalizer" },
		      {{IndexMappingBase.UrlField()}},
		      "hash" : { "type" : "keyword" },
		      "hidden" : { "type" : "boolean" },
		      "search_title": {
		        "type": "text",
		        "fields": {
		          "completion": {
		            "type": "search_as_you_type",
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
		      "body": {
		        "type": "text",
		        "fields": {
		          "en": { "type": "text", "analyzer": "english" },
		          "de": { "type": "text", "analyzer": "german" },
		          "fr": { "type": "text", "analyzer": "french" },
		          "ja": { "type": "text", "analyzer": "cjk" },
		          "ko": { "type": "text", "analyzer": "cjk" },
		          "zh": { "type": "text", "analyzer": "cjk" },
		          "es": { "type": "text", "analyzer": "spanish" },
		          "pt": { "type": "text", "analyzer": "portuguese" }
		        }
		      },
		      "stripped_body": {
		        "type": "text",
		        "term_vector": "with_positions_offsets"
		      },
		      "headings": { "type": "text" },
		      "abstract": {
		        "type" : "text",
		        "fields" : {
		          {{IndexMappingBase.OptionalSemanticFieldFirst(inferenceId)}}
		        }
		      },
		      "page_type": { "type": "keyword", "normalizer": "keyword_normalizer" },
		      "language": { "type": "keyword" },
		      "author": { "type": "keyword" },
		      "published_date": { "type": "date" },
		      "modified_date": { "type": "date" },
		      "relevance": { "type": "keyword" },
		      "og_title": { "type": "text" },
		      "og_description": { "type": "text" },
		      "og_image": { "type": "keyword", "index": false },
		      "twitter_image": { "type": "keyword", "index": false },
		      "twitter_card": { "type": "keyword" },
		      {{IndexMappingBase.AiEnrichmentFields(inferenceId)}},
		      {{IndexMappingBase.HttpCachingFields()}}
		    }
		  }
		  """;
}
