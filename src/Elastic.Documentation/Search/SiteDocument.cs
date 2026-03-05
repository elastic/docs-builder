// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;

namespace Elastic.Documentation.Search;

/// <summary>
/// Document type for non-documentation pages on elastic.co (marketing, blog, product pages, etc.)
/// </summary>
public record SiteDocument
{
	[JsonPropertyName("title")]
	public required string Title { get; set; }

	[JsonPropertyName("search_title")]
	public required string SearchTitle { get; set; }

	[JsonPropertyName("type")]
	public required string Type { get; set; } = "site";

	[JsonPropertyName("url")]
	public required string Url { get; set; } = string.Empty;

	[JsonPropertyName("hash")]
	public string Hash { get; set; } = string.Empty;

	[JsonPropertyName("batch_index_date")]
	public DateTimeOffset BatchIndexDate { get; set; }

	[JsonPropertyName("last_updated")]
	public DateTimeOffset LastUpdated { get; set; }

	[JsonPropertyName("description")]
	public string? Description { get; set; }

	[JsonPropertyName("headings")]
	public string[] Headings { get; set; } = [];

	[JsonPropertyName("body")]
	public string? Body { get; set; }

	[JsonPropertyName("stripped_body")]
	public string? StrippedBody { get; set; }

	[JsonPropertyName("abstract")]
	public string? Abstract { get; set; }

	[JsonPropertyName("hidden")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public bool Hidden { get; set; }

	// Site-specific fields

	/// <summary>
	/// Page type: "blog", "marketing", "product", "event", "resource", "webinar", etc.
	/// </summary>
	[JsonPropertyName("page_type")]
	public string? PageType { get; set; }

	/// <summary>
	/// Language code (e.g., "en", "de", "fr", "ja", "ko", "zh", "es", "pt")
	/// </summary>
	[JsonPropertyName("language")]
	public string Language { get; set; } = "en";

	/// <summary>
	/// Author name from article:author meta tag
	/// </summary>
	[JsonPropertyName("author")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Author { get; set; }

	/// <summary>
	/// Published date from article:published_time meta tag
	/// </summary>
	[JsonPropertyName("published_date")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public DateTimeOffset? PublishedDate { get; set; }

	/// <summary>
	/// Modified date from article:modified_time meta tag
	/// </summary>
	[JsonPropertyName("modified_date")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public DateTimeOffset? ModifiedDate { get; set; }

	/// <summary>
	/// Relevance tier for query boosting: "high", "medium", "low"
	/// </summary>
	[JsonPropertyName("relevance")]
	public string Relevance { get; set; } = "medium";

	// Social metadata fields

	[JsonPropertyName("og_title")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? OgTitle { get; set; }

	[JsonPropertyName("og_description")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? OgDescription { get; set; }

	[JsonPropertyName("og_image")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? OgImage { get; set; }

	[JsonPropertyName("twitter_image")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? TwitterImage { get; set; }

	[JsonPropertyName("twitter_card")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? TwitterCard { get; set; }

	// AI Enrichment fields

	[JsonPropertyName("enrichment_key")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? EnrichmentKey { get; set; }

	[JsonPropertyName("ai_rag_optimized_summary")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? AiRagOptimizedSummary { get; set; }

	[JsonPropertyName("ai_short_summary")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? AiShortSummary { get; set; }

	[JsonPropertyName("ai_search_query")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? AiSearchQuery { get; set; }

	[JsonPropertyName("ai_questions")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string[]? AiQuestions { get; set; }

	[JsonPropertyName("ai_use_cases")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string[]? AiUseCases { get; set; }

	[JsonPropertyName("enrichment_prompt_hash")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? EnrichmentPromptHash { get; set; }

	// HTTP caching fields for incremental sync

	/// <summary>
	/// ETag header from last crawl - used for conditional HTTP requests.
	/// </summary>
	[JsonPropertyName("http_etag")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? HttpEtag { get; set; }

	/// <summary>
	/// Last-Modified header from last crawl - used for conditional HTTP requests.
	/// </summary>
	[JsonPropertyName("http_last_modified")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public DateTimeOffset? HttpLastModified { get; set; }
}
