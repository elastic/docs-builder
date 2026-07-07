// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;
using Elastic.Mapping;

namespace Elastic.Documentation.Search.Contract;

/// <summary>
/// Lean base record for every indexed search document. Carries fields shared by every concrete
/// subclass (e.g. <see cref="DocumentationDocument"/>, <see cref="SiteDocument"/>); the
/// polymorphic dispatch attributes live on <see cref="ISearchDocument"/>, not here, so reads
/// declared as <c>SearchDocumentBase</c> stay flat. Read as <c>ISearchDocument</c> if you need
/// <c>$type</c>-driven dispatch into the concrete subtype. For a fallback-safe polymorphic read
/// (unknown <c>$type</c> → <see cref="SearchDocumentBase"/>), use
/// <see cref="SearchDocumentPolymorphism.WithFallback"/> in the resolver.
/// </summary>
public record SearchDocumentBase : ISearchDocument
{
	[AiInput]
	[Text]
	[JsonPropertyName("title")]
	public required string Title { get; set; }

	[Text]
	[JsonPropertyName("search_title")]
	public required string SearchTitle { get; set; }

	/// <summary>
	/// CLR-only discriminator — overridden by subclasses to match the <c>$type</c> JSON polymorphic value.
	/// Indexed-and-stored equivalent lives on <see cref="ContentType"/>.
	/// </summary>
	[JsonIgnore]
	public virtual string Type { get; } = "docs";

	/// <summary>
	/// Indexed document kind for filtering. When <c>content_type</c> is present in JSON it wins
	/// over <see cref="Type"/>. Otherwise follows <see cref="Type"/> (CLR / <c>$type</c>).
	/// Uses <c>docs</c> when neither is in the payload (sparse <c>_source</c>).
	/// </summary>
	[Keyword]
	[JsonPropertyName("content_type")]
	public string ContentType
	{
		get => field ?? Type ?? "docs";
		set
		{
			if (string.IsNullOrWhiteSpace(value))
			{
				field = null;
				return;
			}

			field = string.Equals(value, Type, StringComparison.Ordinal) ? null : value;
		}
	}

	[Id]
	[Keyword]
	[JsonPropertyName("path")]
	public required string Path { get; set; } = string.Empty;

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

	/// <summary>
	/// Plain-text, markup-stripped page content. The single source of truth for full-text search,
	/// AI enrichment input, and content hashing — producers must not feed raw Markdown/HTML here.
	/// </summary>
	[AiInput]
	[Text]
	[JsonPropertyName("body")]
	public string? Body { get; set; }

	[Text]
	[JsonPropertyName("summary")]
	public string? Summary { get; set; }

	[JsonPropertyName("parents")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public ParentDocument[] Parents { get; set; } = [];

	[JsonPropertyName("hidden")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public bool Hidden { get; set; }

	/// <summary>
	/// Whether this document's source pipeline ever produces locale-specific translations.
	/// ContentStack site content sets this <c>true</c> (even for pages that aren't actually
	/// translated); docs and Labs content have no translation concept and stay at the default
	/// <c>false</c>. Downstream locale filters use <c>language:{locale} OR translated:false</c>
	/// so untranslated (English-only) docs/Labs content isn't excluded from non-English searches.
	/// </summary>
	[JsonPropertyName("translated")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public bool Translated { get; set; }

	// AI enrichment fields — populated post-index by AI enrichment orchestrators.

	[AiField("3-5 sentences densely packed with key concepts for semantic vector matching.")]
	[Text]
	[JsonPropertyName("ai_rag_optimized_summary")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? AiRagOptimizedSummary { get; set; }

	[AiField("Exactly 5-10 words for UI tooltip or search snippet.")]
	[Text]
	[JsonPropertyName("ai_short_summary")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? AiShortSummary { get; set; }

	[AiField("3-8 keywords representing a realistic search query a user would type. Always include the " +
		"relevant Elastic product/brand token (e.g. Elasticsearch, Kibana, Observability, Security) when " +
		"the page is about a product concept, so brand-qualified queries like \"elasticsearch security\" prefix-match.")]
	[Keyword]
	[JsonPropertyName("ai_search_query")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? AiSearchQuery { get; set; }

	[AiField("Natural questions a user would ask (6-15 words).", MinItems = 3, MaxItems = 5)]
	[Text]
	[JsonPropertyName("ai_questions")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string[]? AiQuestions { get; set; }

	[AiField("Simple 2-4 word tasks a user wants to do.", MinItems = 2, MaxItems = 4)]
	[Text]
	[JsonPropertyName("ai_use_cases")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string[]? AiUseCases { get; set; }

	/// <summary>Top-level navigation section (e.g. "reference", "getting-started"). Used for boosting and faceting.</summary>
	[Keyword(Normalizer = "keyword_normalizer")]
	[JsonPropertyName("section")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Section { get; set; }

	/// <summary>
	/// Editorial weight — one of <see cref="ContentTiers"/>. Used to demote low-value content
	/// (marketing, legal, downloads) without a hand-maintained downstream section list.
	/// Defaults to <see cref="ContentTiers.Reference"/> — a NEUTRAL default, unlike
	/// <see cref="NavigationDepth"/>/<see cref="NavigationTableOfContents"/> which default to a
	/// penalty value. Every producer (essc and docs-builder) sets its own value on this shared field.
	/// </summary>
	[Keyword]
	[JsonPropertyName("content_tier")]
	public string ContentTier { get; set; } = ContentTiers.Reference;

	/// <summary>Rank-feature signals derived from the page's position in the navigation tree.</summary>
	[Object]
	[JsonPropertyName("navigation")]
	public NavigationMetrics Navigation { get; set; } = new();

	[AiField("Short 2-6 word search queries a user would type into a search bar to find this page.", MinItems = 3, MaxItems = 6)]
	[Text]
	[JsonPropertyName("ai_autocomplete_questions")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string[]? AiAutocompleteQuestions { get; set; }
}
