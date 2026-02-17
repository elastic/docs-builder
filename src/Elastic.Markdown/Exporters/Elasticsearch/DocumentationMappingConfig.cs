// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Search;
using Elastic.Ingest.Elasticsearch.Indices;
using Elastic.Mapping;
using Elastic.Mapping.Analysis;

namespace Elastic.Markdown.Exporters.Elasticsearch;

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
	private const string InferenceId = ".elser-2-elastic";

	public static AnalysisBuilder ConfigureAnalysis(AnalysisBuilder analysis) => analysis;

	public static DocumentationDocumentMappingsBuilder ConfigureMappings(DocumentationDocumentMappingsBuilder m) =>
		LexicalConfig.ConfigureCommonMappings(m)
			.AddField("title.semantic_text", f => f.SemanticText().InferenceId(InferenceId))
			.AddField("abstract.semantic_text", f => f.SemanticText().InferenceId(InferenceId))
			.AddField("ai_rag_optimized_summary.semantic_text", f => f.SemanticText().InferenceId(InferenceId))
			.AddField("ai_questions.semantic_text", f => f.SemanticText().InferenceId(InferenceId))
			.AddField("ai_use_cases.semantic_text", f => f.SemanticText().InferenceId(InferenceId));
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
	/// Creates the index settings JSON with analysis configuration and optional default pipeline.
	/// </summary>
	public static string BuildSettingsJson(string synonymSetName, string[] indexTimeSynonyms, string? defaultPipeline = null)
	{
		var analysis = BuildAnalysis(new AnalysisBuilder(), synonymSetName, indexTimeSynonyms);
		var analysisJson = analysis.Build().ToJsonString();

		if (defaultPipeline is not null)
		{
			// Merge default_pipeline into the settings JSON
			return $$"""
				{
					"default_pipeline": "{{defaultPipeline}}",
					"analysis": {{analysisJson}}
				}
				""";
		}

		return $$"""
			{
				"analysis": {{analysisJson}}
			}
			""";
	}

	/// <summary>
	/// Creates an ElasticsearchTypeContext with runtime analysis settings and dynamic index name.
	/// </summary>
	public static ElasticsearchTypeContext CreateContext(
		ElasticsearchTypeContext baseContext,
		string indexName,
		string synonymSetName,
		string[] indexTimeSynonyms,
		string? defaultPipeline = null)
	{
		var settingsJson = BuildSettingsJson(synonymSetName, indexTimeSynonyms, defaultPipeline);
		var settingsHash = HashedBulkUpdate.CreateHash(settingsJson);
		var hash = HashedBulkUpdate.CreateHash(settingsHash, baseContext.MappingsHash);

		return baseContext.WithIndexName(indexName) with
		{
			GetSettingsJson = () => settingsJson,
			SettingsHash = settingsHash,
			Hash = hash,
			ConfigureAnalysis = a => BuildAnalysis(a, synonymSetName, indexTimeSynonyms)
		};
	}
}
