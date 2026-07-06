// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Mapping.Analysis;

namespace Elastic.Documentation.Search.Contract.Mapping;

/// <summary>
/// Builds the analysis (normalizers / analyzers / tokenizers / token filters) required by the
/// shared field topology. Two overloads:
/// <list type="bullet">
/// <item><description>Base — no synonym support.</description></item>
/// <item><description>With synonyms — adds the <c>synonyms_fixed</c> (index-time) and
/// <c>synonyms</c> (search-time, updateable) graphs.</description></item>
/// </list>
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
			.Delimiter('/'))
		// content_tags is populated purely via copy_to from content_type/navigation_section (both
		// single-token keyword-ish values) — a keyword tokenizer preserves each value whole. kstem
		// already folds regular plurals (labs -> lab, blogs -> blog, webinars -> webinar); the
		// synonym filter only needs to cover what stemming can't: "customer-story" (hyphenated,
		// stored form) vs "customer story" (the two-word phrase users actually type).
		.Analyzer("content_tags_analyzer", a => a.Custom()
			.Tokenizer("keyword")
			.Filters("lowercase", "kstem", "content_tags_synonyms_filter"))
		.TokenFilter("content_tags_synonyms_filter", tf => tf.SynonymGraph()
			.Synonyms("customer story, customer-story"));

	public static AnalysisBuilder BuildAnalysis(
		AnalysisBuilder analysis,
		string synonymSetName,
		string[] indexTimeSynonyms) =>
		BuildBaseAnalysis(analysis)
			.Analyzer("synonyms_fixed_analyzer", a => a.Custom()
				.CharFilter("symbol_rewrite_char_filter")
				.Tokenizer("group_tokenizer")
				.Filters("lowercase", "morphology_override_filter", "synonyms_fixed_filter", "kstem"))
			.Analyzer("synonyms_analyzer", a => a.Custom()
				.CharFilter("symbol_rewrite_char_filter")
				.Tokenizer("group_tokenizer")
				.Filters("lowercase", "morphology_override_filter", "synonyms_filter", "kstem"))
			// Rewrite "c#" / standalone ".net" -> "dotnet" BEFORE tokenization: group_tokenizer never
			// splits on "#" or ".", so the tokenizer would otherwise see one opaque "c#"/".net" token
			// that can't match a plain "dotnet" query term. The negative lookbehind on ".net" avoids
			// mangling compound tokens like "asp.net" (left untouched, same as today).
			.CharFilter("symbol_rewrite_char_filter", cf => cf.PatternReplace()
				.Pattern(@"(?i)\bc#|(?<![a-zA-Z0-9])\.net\b")
				.Replacement("dotnet"))
			// kstem is conservative and does not fold these curated pairs — stemmer_override runs
			// first so both forms collapse to the same token before kstem (and synonym expansion) sees them.
			.TokenFilter("morphology_override_filter", tf => tf.StemmerOverride().Rules(
				"config, configuration => config",
				"install, installation => install",
				"auth, authentication => auth"))
			.TokenFilter("synonyms_fixed_filter", tf => tf.SynonymGraph()
				.Synonyms(indexTimeSynonyms))
			.TokenFilter("synonyms_filter", tf => tf.SynonymGraph()
				.SynonymsSet(synonymSetName)
				.Updateable(true));
}
