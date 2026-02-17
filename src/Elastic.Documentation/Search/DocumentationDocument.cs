// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;
using Elastic.Documentation.AppliesTo;
using Elastic.Mapping;

namespace Elastic.Documentation.Search;

public record ParentDocument
{
	[JsonPropertyName("title")]
	public required string Title { get; set; }

	[Keyword]
	[JsonPropertyName("url")]
	public required string Url { get; set; }
}

public record DocumentationDocument
{
	[JsonPropertyName("title")]
	public required string Title { get; set; }

	/// <summary>
	/// Search title is a combination of the title and the url components.
	/// This is used for querying to not reward documents with short titles contributing to heavily to scoring
	/// </summary>
	[JsonPropertyName("search_title")]
	public required string SearchTitle { get; set; }

	[Keyword(Normalizer = "keyword_normalizer")]
	[JsonPropertyName("type")]
	public required string Type { get; set; } = "doc";

	/// <summary>
	/// The canonical/primary product for this document (nested object with id and repository).
	/// Name and version are looked up dynamically by product id.
	/// </summary>
	[Object]
	[JsonPropertyName("product")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public IndexedProduct? Product { get; set; }

	/// <summary>
	/// All related products found during inference (from legacy mappings, applicability, etc.)
	/// </summary>
	[Object]
	[JsonPropertyName("related_products")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public IndexedProduct[]? RelatedProducts { get; set; }

	[Id]
	[Keyword]
	[JsonPropertyName("url")]
	public required string Url { get; set; } = string.Empty;

	[ContentHash]
	[Keyword]
	[JsonPropertyName("hash")]
	public string Hash { get; set; } = string.Empty;

	[JsonPropertyName("navigation_depth")]
	public int NavigationDepth { get; set; } = 50; //default to a high number so that omission gets penalized.

	[JsonPropertyName("navigation_table_of_contents")]
	public int NavigationTableOfContents { get; set; } = 50; //default to a high number so that omission gets penalized.

	[Keyword(Normalizer = "keyword_normalizer")]
	[JsonPropertyName("navigation_section")]
	public string? NavigationSection { get; set; }

	/// The date of the batch update this document was part of last.
	/// This date could be higher than the date_last_updated.
	[JsonPropertyName("batch_index_date")]
	public DateTimeOffset BatchIndexDate { get; set; }

	/// The date this document was last updated,
	[Timestamp]
	[JsonPropertyName("last_updated")]
	public DateTimeOffset LastUpdated { get; set; }

	[JsonPropertyName("description")]
	public string? Description { get; set; }

	[Text]
	[JsonPropertyName("headings")]
	public string[] Headings { get; set; } = [];

	[JsonPropertyName("links")]
	public string[] Links { get; set; } = [];

	[Nested]
	[JsonPropertyName("applies_to")]
	public ApplicableTo? Applies { get; set; }

	[JsonPropertyName("body")]
	public string? Body { get; set; }

	/// Stripped body is the body with Markdown removed, suitable for search indexing
	[JsonPropertyName("stripped_body")]
	public string? StrippedBody { get; set; }

	[JsonPropertyName("abstract")]
	public string? Abstract { get; set; }

	[Object]
	[JsonPropertyName("parents")]
	public ParentDocument[] Parents { get; set; } = [];

	[JsonPropertyName("hidden")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public bool Hidden { get; set; }

	// AI Enrichment fields - populated by DocumentEnrichmentService

	/// <summary>
	/// Key for enrichment cache lookups. Derived from normalized content + prompt hash.
	/// Used by enrich processor to join AI-generated fields at index time.
	/// </summary>
	[Keyword]
	[JsonPropertyName("enrichment_key")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? EnrichmentKey { get; set; }

	/// <summary>
	/// 3-5 sentences dense with technical entities, API names, and core functionality for vector matching.
	/// </summary>
	[Text]
	[JsonPropertyName("ai_rag_optimized_summary")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? AiRagOptimizedSummary { get; set; }

	/// <summary>
	/// Exactly 5-10 words for a UI tooltip.
	/// </summary>
	[Text]
	[JsonPropertyName("ai_short_summary")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? AiShortSummary { get; set; }

	/// <summary>
	/// A 3-8 word keyword string representing a high-intent user search for this doc.
	/// </summary>
	[Keyword]
	[JsonPropertyName("ai_search_query")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? AiSearchQuery { get; set; }

	/// <summary>
	/// Array of 3-5 specific questions answered by this document.
	/// </summary>
	[Text]
	[JsonPropertyName("ai_questions")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string[]? AiQuestions { get; set; }

	/// <summary>
	/// Array of 2-4 specific use cases this doc helps with.
	/// </summary>
	[Text]
	[JsonPropertyName("ai_use_cases")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string[]? AiUseCases { get; set; }

	/// <summary>
	/// Hash of the LLM prompt templates used to generate AI fields.
	/// Used to detect stale enrichments when prompts change.
	/// </summary>
	[Keyword]
	[JsonPropertyName("enrichment_prompt_hash")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? EnrichmentPromptHash { get; set; }
}
