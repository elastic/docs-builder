// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Mapping.Analysis;
using Elastic.Mapping.Mappings;

namespace Elastic.Documentation.Search;

/// <summary>
/// Shared mapping extensions for fields that are identical across all document types.
/// Uses string-based <c>AddField</c> to work across different <c>MappingsBuilder&lt;T&gt;</c> types.
/// </summary>
public static class SharedMappingConfig
{
	private const string ElserInferenceId = ".elser-2-elastic";
	private const string JinaInferenceId = ".jina-embeddings-v5-text-small";

	/// <summary>Adds common Title multi-fields: keyword (normalized), starts_with (edge ngram), completion (search-as-you-type).</summary>
	public static MappingsBuilder<T> AddCommonTitleMappings<T>(this MappingsBuilder<T> m) where T : class => m
		.AddField("title", f => f.Text()
			.MultiField("keyword", mf => mf.Keyword().Normalizer("keyword_normalizer"))
			.MultiField("starts_with", mf => mf.Text()
				.Analyzer("starts_with_analyzer")
				.SearchAnalyzer("starts_with_analyzer_search"))
			.MultiField("completion", mf => mf.SearchAsYouType()))
		.AddField("search_title", f => f.Text()
			.MultiField("completion", mf => mf.SearchAsYouType()))
		.AddField("url", f => f.Keyword()
			.MultiField("match", mf => mf.Text())
			.MultiField("prefix", mf => mf.Text().Analyzer("hierarchy_analyzer")));

	/// <summary>Adds ELSER sparse + Jina v5 dense semantic text fields for the standard AI-enriched field set.</summary>
	public static MappingsBuilder<T> AddSemanticTextFields<T>(this MappingsBuilder<T> m) where T : class => m
		// ELSER sparse embeddings
		.AddField("title.semantic_text", f => f.SemanticText().InferenceId(ElserInferenceId))
		.AddField("abstract.semantic_text", f => f.SemanticText().InferenceId(ElserInferenceId))
		.AddField("ai_rag_optimized_summary.semantic_text", f => f.SemanticText().InferenceId(ElserInferenceId))
		.AddField("ai_questions.semantic_text", f => f.SemanticText().InferenceId(ElserInferenceId))
		.AddField("ai_use_cases.semantic_text", f => f.SemanticText().InferenceId(ElserInferenceId))
		// Jina v5 dense embeddings
		.AddField("title.jina", f => f.SemanticText().InferenceId(JinaInferenceId))
		.AddField("abstract.jina", f => f.SemanticText().InferenceId(JinaInferenceId))
		.AddField("ai_rag_optimized_summary.jina", f => f.SemanticText().InferenceId(JinaInferenceId))
		.AddField("ai_questions.jina", f => f.SemanticText().InferenceId(JinaInferenceId))
		.AddField("ai_use_cases.jina", f => f.SemanticText().InferenceId(JinaInferenceId));
}

/// <summary>
/// Base analysis settings shared across all document types: normalizers, analyzers, tokenizers.
/// Documentation indices extend this with synonym filters via <see cref="DocumentationAnalysisFactory"/>.
/// </summary>
public static class SharedAnalysisFactory
{
	public static AnalysisBuilder BuildBaseAnalysis(AnalysisBuilder analysis) => analysis
		.Normalizer("keyword_normalizer", n => n.Custom()
			.CharFilter("strip_non_word_chars")
			.Filters("lowercase", "asciifolding", "trim"))
		.Analyzer("starts_with_analyzer", a => a.Custom()
			.Tokenizer("starts_with_tokenizer")
			.Filter("lowercase"))
		.Analyzer("starts_with_analyzer_search", a => a.Custom()
			.Tokenizer("keyword")
			.Filter("lowercase"))
		.Analyzer("highlight_analyzer", a => a.Custom()
			.Tokenizer("group_tokenizer")
			.Filters("lowercase", "english_stop"))
		.Analyzer("hierarchy_analyzer", a => a.Custom()
			.Tokenizer("path_tokenizer"))
		.CharFilter("strip_non_word_chars", cf => cf.PatternReplace()
			.Pattern(@"\W")
			.Replacement(" "))
		.TokenFilter("english_stop", tf => tf.Stop()
			.Stopwords("_english_"))
		.Tokenizer("starts_with_tokenizer", t => t.EdgeNGram()
			.MinGram(1)
			.MaxGram(10)
			.TokenChars("letter", "digit", "symbol", "whitespace"))
		.Tokenizer("group_tokenizer", t => t.CharGroup()
			.TokenizeOnChars("whitespace", ",", ";", "?", "!", "(", ")", "&", "'", "\"", "/", "[", "]", "{", "}"))
		.Tokenizer("path_tokenizer", t => t.PathHierarchy()
			.Delimiter('/'));
}
