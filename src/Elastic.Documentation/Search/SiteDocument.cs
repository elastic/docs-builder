// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;
using Elastic.Mapping;

namespace Elastic.Documentation.Search;

/// <summary>
/// Document type for non-documentation pages on elastic.co (marketing, blog, product pages, etc.)
/// </summary>
public record SiteDocument : IDocument
{
	[AiInput]
	[Text]
	[JsonPropertyName("title")]
	public required string Title { get; set; }

	[Text]
	[JsonPropertyName("search_title")]
	public required string SearchTitle { get; set; }

	[Keyword(Normalizer = "keyword_normalizer")]
	[JsonPropertyName("type")]
	public required string Type { get; set; } = "site";

	[Id]
	[Keyword]
	[JsonPropertyName("url")]
	public required string Url { get; set; } = string.Empty;

	[ContentHash]
	[Keyword]
	[JsonPropertyName("hash")]
	public string Hash { get; set; } = string.Empty;

	[BatchIndexDate]
	[JsonPropertyName("batch_index_date")]
	public DateTimeOffset BatchIndexDate { get; set; }

	[LastUpdated]
	[Timestamp]
	[JsonPropertyName("last_updated")]
	public DateTimeOffset LastUpdated { get; set; }

	[JsonPropertyName("description")]
	public string? Description { get; set; }

	[Text]
	[JsonPropertyName("headings")]
	public string[] Headings { get; set; } = [];

	[Text]
	[JsonPropertyName("body")]
	public string? Body { get; set; }

	[AiInput]
	[Text]
	[JsonPropertyName("stripped_body")]
	public string? StrippedBody { get; set; }

	[Text]
	[JsonPropertyName("abstract")]
	public string? Abstract { get; set; }

	[JsonPropertyName("hidden")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public bool Hidden { get; set; }

	[Keyword(Normalizer = "keyword_normalizer")]
	[JsonPropertyName("page_type")]
	public string? PageType { get; set; }

	[Keyword]
	[JsonPropertyName("language")]
	public string Language { get; set; } = "en";

	[Keyword]
	[JsonPropertyName("author")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Author { get; set; }

	[JsonPropertyName("published_date")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public DateTimeOffset? PublishedDate { get; set; }

	[JsonPropertyName("modified_date")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public DateTimeOffset? ModifiedDate { get; set; }

	[Text]
	[JsonPropertyName("og_title")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? OgTitle { get; set; }

	[Text]
	[JsonPropertyName("og_description")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? OgDescription { get; set; }

	[Keyword(Index = false)]
	[JsonPropertyName("og_image")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? OgImage { get; set; }

	[Keyword(Index = false)]
	[JsonPropertyName("twitter_image")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? TwitterImage { get; set; }

	[Keyword]
	[JsonPropertyName("twitter_card")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? TwitterCard { get; set; }

	// AI Enrichment fields

	[Keyword]
	[JsonPropertyName("enrichment_key")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? EnrichmentKey { get; set; }

	[AiField("3-5 sentences densely packed with key concepts for semantic vector matching. Include: product names, feature names, use cases, and core value propositions. Write for RAG retrieval - someone searching for Elastic products and capabilities should match this text.")]
	[Text]
	[JsonPropertyName("ai_rag_optimized_summary")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? AiRagOptimizedSummary { get; set; }

	[AiField("Exactly 5-10 words for UI tooltip or search snippet. Action-oriented, starts with a verb. Example: 'Learn how Elastic protects cloud-native workloads'")]
	[Text]
	[JsonPropertyName("ai_short_summary")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? AiShortSummary { get; set; }

	[AiField("3-8 keywords representing a realistic search query a user would type. Include product and topic terms. Example: 'elastic security SIEM threat detection'")]
	[Keyword]
	[JsonPropertyName("ai_search_query")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? AiSearchQuery { get; set; }

	[AiField("Natural questions a user would ask (6-15 words). Not too short, not too verbose. Examples: 'What is Elastic Observability?', 'How does Elastic Security detect threats?', 'What are the benefits of Elastic Cloud?'",
		MinItems = 3, MaxItems = 5)]
	[Text]
	[JsonPropertyName("ai_questions")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string[]? AiQuestions { get; set; }

	[AiField("Simple 2-4 word tasks a user wants to do. Examples: 'monitor applications', 'detect threats', 'search logs', 'visualize data', 'manage clusters'",
		MinItems = 2, MaxItems = 4)]
	[Text]
	[JsonPropertyName("ai_use_cases")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string[]? AiUseCases { get; set; }

	[Keyword]
	[JsonPropertyName("enrichment_prompt_hash")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? EnrichmentPromptHash { get; set; }

	// HTTP caching fields for incremental sync

	[Keyword(Index = false)]
	[JsonPropertyName("http_etag")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? HttpEtag { get; set; }

	[JsonPropertyName("http_last_modified")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public DateTimeOffset? HttpLastModified { get; set; }
}
