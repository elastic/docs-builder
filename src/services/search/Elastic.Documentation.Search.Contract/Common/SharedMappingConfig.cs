// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Mapping.Analysis;
using Elastic.Mapping.Mappings;

namespace Elastic.Documentation.Search.Contract.Mapping;

/// <summary>
/// Shared mapping extensions for fields that are identical across all document types.
/// Per-document <see cref="MappingsBuilder{T}"/> configurations chain these to compose
/// the lexical/semantic field topology.
/// </summary>
public static class SharedMappingConfig
{
	// Custom analysis component names — must match SharedAnalysisFactory definitions.
	// Public so consumer assemblies (e.g. the docs Elasticsearch exporter) can reuse the same names
	// when composing document-type-specific mapping extensions.
	public const string KeywordNormalizer = "keyword_normalizer";
	public const string StartsWithAnalyzer = "starts_with_analyzer";
	public const string StartsWithAnalyzerSearch = "starts_with_analyzer_search";
	public const string HierarchyAnalyzer = "hierarchy_analyzer";
	public const string SynonymsFixedAnalyzer = "synonyms_fixed_analyzer";
	public const string SynonymsAnalyzer = "synonyms_analyzer";
	public const string ContentTagsAnalyzer = "content_tags_analyzer";

	private static MappingsBuilder<T> AddCommonTitleMappings<T>(this MappingsBuilder<T> m) where T : SearchDocumentBase => m
		.Title(f => f
			.MultiField("keyword", mf => mf.Keyword().Normalizer(KeywordNormalizer))
			.MultiField("starts_with", mf => mf.Text()
				.Analyzer(StartsWithAnalyzer)
				.SearchAnalyzer(StartsWithAnalyzerSearch))
			.MultiField("completion", mf => mf.SearchAsYouType()))
		.SearchTitle(f => f
			.MultiField("completion", mf => mf.SearchAsYouType()))
		.Path(f => f
			.MultiField("match", mf => mf.Text())
			.MultiField("prefix", mf => mf.Text().Analyzer(HierarchyAnalyzer)));

	private static MappingsBuilder<T> AddNavigationFields<T>(this MappingsBuilder<T> m) where T : SearchDocumentBase => m
		.Section(f => f.Normalizer(KeywordNormalizer).CopyTo("tags"))
		.AddProperty("navigation.depth", f => f.RankFeature().PositiveScoreImpact(false))
		.AddProperty("navigation.table_of_contents", f => f.RankFeature().PositiveScoreImpact(false))
		.AiAutocompleteQuestions(f => f.MultiField("completion", mf => mf.SearchAsYouType()));

	// content_type/section are set on every doc — copy_to routes both into a single tags field
	// so a query term naming a type/section ("labs", "blog", "api") rises by native BM25 with no
	// client-side term->type re-promotion rules.
	private static MappingsBuilder<T> AddContentTagsField<T>(this MappingsBuilder<T> m) where T : SearchDocumentBase => m
		.ContentType(f => f.CopyTo("tags"))
		.AddField("tags", f => f.Text().Analyzer(ContentTagsAnalyzer));

	private static MappingsBuilder<T> AddSemanticFields<T>(this MappingsBuilder<T> m) where T : SearchDocumentBase => m
		// All parents are [Text] leaf fields — AddField places the semantic child under "fields"
		.AddField("title.semantic_text", f => f.SemanticText())
		.AddField("summary.semantic_text", f => f.SemanticText())
		.AddField("ai_rag_optimized_summary.semantic_text", f => f.SemanticText())
		.AddField("ai_questions.semantic_text", f => f.SemanticText())
		.AddField("ai_use_cases.semantic_text", f => f.SemanticText())
		.AddField("body.semantic_text", f => f.SemanticText());

	/// <summary>
	/// Elasticsearch <c>alias</c> fields for pre-restructure flat field names, so in-flight consumers
	/// (dashboards, saved searches, external queries) resolve during rollout without changes.
	/// Remove once all indices are rebuilt under the new shape and no consumer queries the old names.
	/// </summary>
	private static MappingsBuilder<T> AddLegacyFieldAliases<T>(this MappingsBuilder<T> m) where T : SearchDocumentBase => m
		.AddField("url", f => f.Alias("path"))
		.AddField("abstract", f => f.Alias("summary"))
		.AddField("navigation_section", f => f.Alias("section"))
		.AddField("content_tags", f => f.Alias("tags"))
		.AddField("stripped_body", f => f.Alias("body"))
		.AddField("navigation_depth", f => f.Alias("navigation.depth"))
		.AddField("navigation_table_of_contents", f => f.Alias("navigation.table_of_contents"));

	/// <summary>
	/// Full standard field set shared by Site, Labs, Guide, and WebsiteSearch lexical/semantic indices.
	/// Includes synonym-aware title/search_title, AI fields, multilingual body, and navigation rank features.
	/// Pass <paramref name="semantic"/> = <c>true</c> for semantic index variants.
	/// </summary>
	public static MappingsBuilder<T> AddSearchDocumentMappings<T>(this MappingsBuilder<T> m, bool semantic = false) where T : SearchDocumentBase
	{
		m = m
			.AddNavigationFields()
			.AddContentTagsField()
			.AddCommonTitleMappings()
			.AddLegacyFieldAliases()
			.SearchTitle(f => f
				.Analyzer(SynonymsFixedAnalyzer)
				.SearchAnalyzer(SynonymsAnalyzer)
				.MultiField("completion", mf => mf.SearchAsYouType()
					.Analyzer(SynonymsFixedAnalyzer)
					.SearchAnalyzer(SynonymsAnalyzer)
					.IndexOptions("offsets")))
			.Title(f => f
				.SearchAnalyzer(SynonymsAnalyzer)
				.MultiField("keyword", mf => mf.Keyword().Normalizer(KeywordNormalizer))
				.MultiField("starts_with", mf => mf.Text()
					.Analyzer(StartsWithAnalyzer)
					.SearchAnalyzer(StartsWithAnalyzerSearch))
				.MultiField("completion", mf => mf.SearchAsYouType().SearchAnalyzer(SynonymsAnalyzer)))
			.AiQuestions(f => f
				.MultiField("completion", mf => mf.SearchAsYouType()))
			// search_as_you_type only — no semantic_text here; this field is used by downstream typeahead.
			.AiSearchQuery(f => f
				.MultiField("completion", mf => mf.SearchAsYouType()))
			.Summary(f => f
				.Analyzer(SynonymsFixedAnalyzer)
				.SearchAnalyzer(SynonymsAnalyzer))
			.Headings(f => f
				.Analyzer(SynonymsFixedAnalyzer)
				.SearchAnalyzer(SynonymsAnalyzer))
			.AiRagOptimizedSummary(f => f
				.Analyzer(SynonymsFixedAnalyzer)
				.SearchAnalyzer(SynonymsAnalyzer))
			.AiQuestions(f => f
				.Analyzer(SynonymsFixedAnalyzer)
				.SearchAnalyzer(SynonymsAnalyzer)
				.MultiField("completion", mf => mf.SearchAsYouType()
					.Analyzer(SynonymsFixedAnalyzer)
					.SearchAnalyzer(SynonymsAnalyzer)
					.IndexOptions("offsets")))
			.AiAutocompleteQuestions(f => f
				.Analyzer(SynonymsFixedAnalyzer)
				.SearchAnalyzer(SynonymsAnalyzer)
				.MultiField("completion", mf => mf.SearchAsYouType()
					.Analyzer(SynonymsFixedAnalyzer)
					.SearchAnalyzer(SynonymsAnalyzer)
					.IndexOptions("offsets"))
				.MultiField("suggest", mf => mf.Completion()))
			.Body(f => f
				.Analyzer(SynonymsFixedAnalyzer)
				.SearchAnalyzer(SynonymsAnalyzer)
				.TermVector("with_positions_offsets")
				.MultiField("en", mf => mf.Text().Analyzer(BuiltInAnalysis.Analyzers.Language.English))
				.MultiField("de", mf => mf.Text().Analyzer(BuiltInAnalysis.Analyzers.Language.German))
				.MultiField("fr", mf => mf.Text().Analyzer(BuiltInAnalysis.Analyzers.Language.French))
				.MultiField("ja", mf => mf.Text().Analyzer(BuiltInAnalysis.Analyzers.Language.Cjk))
				.MultiField("ko", mf => mf.Text().Analyzer(BuiltInAnalysis.Analyzers.Language.Cjk))
				.MultiField("zh", mf => mf.Text().Analyzer(BuiltInAnalysis.Analyzers.Language.Cjk))
				.MultiField("es", mf => mf.Text().Analyzer(BuiltInAnalysis.Analyzers.Language.Spanish))
				.MultiField("pt", mf => mf.Text().Analyzer(BuiltInAnalysis.Analyzers.Language.Portuguese)));

		return semantic ? m.AddSemanticFields() : m;
	}

}
