// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Mapping;
using Elastic.Mapping.Analysis;

namespace Elastic.Documentation.Search;

[ElasticsearchMappingContext]
[Entity<DocumentationDocument>(
	Target = EntityTarget.Index,
	Name = "docs-lexical",
	WriteAlias = "docs-lexical",
	ReadAlias = "docs-lexical",
	SearchPattern = "docs-lexical-*",
	DatePattern = "yyyy.MM.dd.HHmmss",
	Configuration = typeof(LexicalConfig)
)]
[Entity<DocumentationDocument>(
	Target = EntityTarget.Index,
	Name = "docs-semantic",
	Variant = "Semantic",
	WriteAlias = "docs-semantic",
	ReadAlias = "docs-semantic",
	SearchPattern = "docs-semantic-*",
	DatePattern = "yyyy.MM.dd.HHmmss",
	Configuration = typeof(SemanticConfig)
)]
public static partial class DocumentationMappingContext;

public static class LexicalConfig
{
	public static AnalysisBuilder ConfigureAnalysis(AnalysisBuilder analysis) => analysis;

	public static DocumentationDocumentMappingsBuilder ConfigureMappings(DocumentationDocumentMappingsBuilder m) =>
		ConfigureCommonMappings(m);

	internal static DocumentationDocumentMappingsBuilder ConfigureCommonMappings(DocumentationDocumentMappingsBuilder m) => m
		// Text fields with custom analyzers and multi-fields
		.SearchTitle(f => f
			.Analyzer("synonyms_fixed_analyzer")
			.SearchAnalyzer("synonyms_analyzer")
			.MultiField("completion", mf => mf.SearchAsYouType()
				.Analyzer("synonyms_fixed_analyzer")
				.SearchAnalyzer("synonyms_analyzer")))
		.Title(f => f
			.SearchAnalyzer("synonyms_analyzer")
			.MultiField("keyword", mf => mf.Keyword().Normalizer("keyword_normalizer"))
			.MultiField("starts_with", mf => mf.Text()
				.Analyzer("starts_with_analyzer")
				.SearchAnalyzer("starts_with_analyzer_search"))
			.MultiField("completion", mf => mf.SearchAsYouType().SearchAnalyzer("synonyms_analyzer")))
		.StrippedBody(f => f
			.Analyzer("synonyms_fixed_analyzer")
			.SearchAnalyzer("synonyms_analyzer"))
		.Abstract(f => f
			.Analyzer("synonyms_fixed_analyzer")
			.SearchAnalyzer("synonyms_analyzer"))
		.Headings(f => f
			.Analyzer("synonyms_fixed_analyzer")
			.SearchAnalyzer("synonyms_analyzer"))
		// JsonIgnore fields — [Text]/[Keyword] attributes handle the type,
		// AddField only needed when custom analyzers are required
		.AddField("ai_rag_optimized_summary", f => f.Text()
			.Analyzer("synonyms_fixed_analyzer")
			.SearchAnalyzer("synonyms_analyzer"))
		// Keyword fields with multi-fields
		.Url(f => f
			.MultiField("match", mf => mf.Text())
			.MultiField("prefix", mf => mf.Text().Analyzer("hierarchy_analyzer")))
		// Rank features — no attribute available, must use AddField
		.AddField("navigation_depth", f => f.RankFeature().PositiveScoreImpact(false))
		.AddField("navigation_table_of_contents", f => f.RankFeature().PositiveScoreImpact(false))
		// Nested applies_to — sub-fields don't match C# structure (custom JsonConverter)
		.AddField("applies_to.type", f => f.Keyword().Normalizer("keyword_normalizer"))
		.AddField("applies_to.sub-type", f => f.Keyword().Normalizer("keyword_normalizer"))
		.AddField("applies_to.lifecycle", f => f.Keyword().Normalizer("keyword_normalizer"))
		.AddField("applies_to.version", f => f.Version())
		// Parent document multi-fields
		.AddField("parents.url", f => f.Keyword()
			.MultiField("match", mf => mf.Text())
			.MultiField("prefix", mf => mf.Text().Analyzer("hierarchy_analyzer")))
		.AddField("parents.title", f => f.Text()
			.SearchAnalyzer("synonyms_analyzer")
			.MultiField("keyword", mf => mf.Keyword()));
}

public static class SemanticConfig
{
	private const string ElserInferenceId = ".elser-2-elastic";
	private const string JinaInferenceId = ".jina-embeddings-v5-text-small";

	public static AnalysisBuilder ConfigureAnalysis(AnalysisBuilder analysis) => analysis;

	public static DocumentationDocumentMappingsBuilder ConfigureMappings(DocumentationDocumentMappingsBuilder m) =>
		LexicalConfig.ConfigureCommonMappings(m)
			.StrippedBody(s => s
				.Analyzer("synonyms_fixed_analyzer")
				.SearchAnalyzer("synonyms_analyzer")
				.MultiField("jina", mf => mf.Text().Analyzer(JinaInferenceId))
			)
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
/// Builds analysis settings at runtime (includes synonyms that are loaded from configuration).
/// </summary>
public static class DocumentationAnalysisFactory
{
	public static AnalysisBuilder BuildAnalysis(AnalysisBuilder analysis, string synonymSetName, string[] indexTimeSynonyms) => analysis
		.Normalizer("keyword_normalizer", n => n.Custom()
			.CharFilter("strip_non_word_chars")
			.Filters("lowercase", "asciifolding", "trim"))
		.Analyzer("starts_with_analyzer", a => a.Custom()
			.Tokenizer("starts_with_tokenizer")
			.Filter("lowercase"))
		.Analyzer("starts_with_analyzer_search", a => a.Custom()
			.Tokenizer("keyword")
			.Filter("lowercase"))
		.Analyzer("synonyms_fixed_analyzer", a => a.Custom()
			.Tokenizer("group_tokenizer")
			.Filters("lowercase", "synonyms_fixed_filter", "kstem"))
		.Analyzer("synonyms_analyzer", a => a.Custom()
			.Tokenizer("group_tokenizer")
			.Filters("lowercase", "synonyms_filter", "kstem"))
		.Analyzer("highlight_analyzer", a => a.Custom()
			.Tokenizer("group_tokenizer")
			.Filters("lowercase", "english_stop"))
		.Analyzer("hierarchy_analyzer", a => a.Custom()
			.Tokenizer("path_tokenizer"))
		.CharFilter("strip_non_word_chars", cf => cf.PatternReplace()
			.Pattern(@"\W")
			.Replacement(" "))
		.TokenFilter("synonyms_fixed_filter", tf => tf.SynonymGraph()
			.Synonyms(indexTimeSynonyms))
		.TokenFilter("synonyms_filter", tf => tf.SynonymGraph()
			.SynonymsSet(synonymSetName)
			.Updateable(true))
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

	/// <summary>
	/// Creates an ElasticsearchTypeContext with runtime analysis settings and dynamic index name.
	/// Analysis is provided via <see cref="ElasticsearchTypeContext.ConfigureAnalysis"/>, which
	/// <c>Elastic.Ingest.Elasticsearch</c> merges into the settings automatically.
	/// </summary>
	public static ElasticsearchTypeContext CreateContext(
		ElasticsearchTypeContext baseContext,
		string indexName,
		string synonymSetName,
		string[] indexTimeSynonyms,
		string? defaultPipeline = null)
	{
		var analysisJson = BuildAnalysis(new AnalysisBuilder(), synonymSetName, indexTimeSynonyms).Build().ToJsonString();
		var settingsHash = ContentHash.Create(analysisJson, defaultPipeline ?? "");
		var hash = ContentHash.Create(settingsHash, baseContext.MappingsHash);

		return baseContext.WithIndexName(indexName) with
		{
			GetSettingsJson = defaultPipeline is not null
				? () => $$"""{ "default_pipeline": "{{defaultPipeline}}" }"""
				: () => "{}",
			SettingsHash = settingsHash,
			Hash = hash,
			ConfigureAnalysis = a => BuildAnalysis(a, synonymSetName, indexTimeSynonyms),
			IndexPatternUseBatchDate = true
		};
	}
}
