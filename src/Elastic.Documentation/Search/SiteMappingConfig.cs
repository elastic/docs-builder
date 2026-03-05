// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Mapping;
using Elastic.Mapping.Analysis;

namespace Elastic.Documentation.Search;

[ElasticsearchMappingContext]
[Entity<SiteDocument>(
	Target = EntityTarget.Index,
	Name = "site-lexical",
	WriteAlias = "site-lexical",
	ReadAlias = "site-lexical",
	SearchPattern = "site-lexical-*",
	DatePattern = "yyyy.MM.dd.HHmmss",
	Configuration = typeof(SiteLexicalConfig)
)]
[Entity<SiteDocument>(
	Target = EntityTarget.Index,
	Name = "site-semantic",
	Variant = "Semantic",
	WriteAlias = "site-semantic",
	ReadAlias = "site-semantic",
	SearchPattern = "site-semantic-*",
	DatePattern = "yyyy.MM.dd.HHmmss",
	Configuration = typeof(SiteSemanticConfig)
)]
public static partial class SiteMappingContext;

public static class SiteLexicalConfig
{
	public static AnalysisBuilder ConfigureAnalysis(AnalysisBuilder analysis) => analysis;

	public static SiteDocumentMappingsBuilder ConfigureMappings(SiteDocumentMappingsBuilder m) =>
		ConfigureCommonMappings(m);

	internal static SiteDocumentMappingsBuilder ConfigureCommonMappings(SiteDocumentMappingsBuilder m) => m
		.SearchTitle(f => f
			.MultiField("completion", mf => mf.SearchAsYouType()))
		.Title(f => f
			.MultiField("keyword", mf => mf.Keyword().Normalizer("keyword_normalizer"))
			.MultiField("starts_with", mf => mf.Text()
				.Analyzer("starts_with_analyzer")
				.SearchAnalyzer("starts_with_analyzer_search"))
			.MultiField("completion", mf => mf.SearchAsYouType()))
		.Url(f => f
			.MultiField("match", mf => mf.Text())
			.MultiField("prefix", mf => mf.Text().Analyzer("hierarchy_analyzer")))
		// Multilingual body sub-fields
		.AddField("body.en", f => f.Text().Analyzer("english"))
		.AddField("body.de", f => f.Text().Analyzer("german"))
		.AddField("body.fr", f => f.Text().Analyzer("french"))
		.AddField("body.ja", f => f.Text().Analyzer("cjk"))
		.AddField("body.ko", f => f.Text().Analyzer("cjk"))
		.AddField("body.zh", f => f.Text().Analyzer("cjk"))
		.AddField("body.es", f => f.Text().Analyzer("spanish"))
		.AddField("body.pt", f => f.Text().Analyzer("portuguese"));
}

public static class SiteSemanticConfig
{
	private const string ElserInferenceId = ".elser-2-elastic";
	private const string JinaInferenceId = ".jina-embeddings-v5-text-small";

	public static AnalysisBuilder ConfigureAnalysis(AnalysisBuilder analysis) => analysis;

	public static SiteDocumentMappingsBuilder ConfigureMappings(SiteDocumentMappingsBuilder m) =>
		SiteLexicalConfig.ConfigureCommonMappings(m)
			.StrippedBody(s => s
				.MultiField("jina", mf => mf.Text().Analyzer(JinaInferenceId)))
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
			.AddField("ai_use_cases.jina", f => f.SemanticText().InferenceId(JinaInferenceId))
			.AddField("stripped_body.jina", f => f.SemanticText().InferenceId(JinaInferenceId));
}

/// <summary>
/// Builds analysis settings at runtime for site indices.
/// </summary>
public static class SiteAnalysisFactory
{
	public static AnalysisBuilder BuildAnalysis(AnalysisBuilder analysis) => analysis
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

	public static ElasticsearchTypeContext CreateContext(
		ElasticsearchTypeContext baseContext,
		string indexName,
		string? defaultPipeline = null)
	{
		var analysisJson = BuildAnalysis(new AnalysisBuilder()).Build().ToJsonString();
		var settingsHash = ContentHash.Create(analysisJson, defaultPipeline ?? "");
		var hash = ContentHash.Create(settingsHash, baseContext.MappingsHash);

		return baseContext.WithIndexName(indexName) with
		{
			GetSettingsJson = defaultPipeline is not null
				? () => $$"""{ "default_pipeline": "{{defaultPipeline}}" }"""
				: () => "{}",
			SettingsHash = settingsHash,
			Hash = hash,
			ConfigureAnalysis = BuildAnalysis,
			IndexPatternUseBatchDate = true
		};
	}
}
