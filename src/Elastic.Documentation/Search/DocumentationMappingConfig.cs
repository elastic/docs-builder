// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Mapping;
using Elastic.Mapping.Analysis;
using Elastic.Mapping.Mappings;

namespace Elastic.Documentation.Search;

[ElasticsearchMappingContext]
[Index<DocumentationDocument>(
	NameTemplate = "docs-{type}.lexical-{env}",
	DatePattern = "yyyy.MM.dd.HHmmss",
	Configuration = typeof(LexicalConfig)
)]
[Index<DocumentationDocument>(
	NameTemplate = "docs-{type}.semantic-{env}",
	Variant = "Semantic",
	DatePattern = "yyyy.MM.dd.HHmmss",
	Configuration = typeof(SemanticConfig)
)]
[AiEnrichment<DocumentationDocument>(
	Role = "Expert technical writer creating search metadata for Elastic documentation (Elasticsearch, Kibana, Beats, Logstash). Audience: developers, DevOps, data engineers.",
	MatchField = "url",
	IndexVariant = "Semantic"
)]
public static partial class DocumentationMappingContext;

public class LexicalConfig : IConfigureElasticsearch<DocumentationDocument>
{
	public AnalysisBuilder ConfigureAnalysis(AnalysisBuilder analysis) => analysis;

	public IReadOnlyDictionary<string, string>? IndexSettings => null;

	public MappingsBuilder<DocumentationDocument> ConfigureMappings(MappingsBuilder<DocumentationDocument> mappings) =>
		ConfigureCommonMappings(mappings)
			.StrippedBody(f => f
				.Analyzer("synonyms_fixed_analyzer")
				.SearchAnalyzer("synonyms_analyzer")
				.TermVector("with_positions_offsets")
			);

	internal static MappingsBuilder<DocumentationDocument> ConfigureCommonMappings(MappingsBuilder<DocumentationDocument> m) => m
		.AddCommonTitleMappings()
		// Override Title/SearchTitle/Abstract/Headings with synonym analyzers
		.SearchTitle(f => f
			.Analyzer("synonyms_fixed_analyzer")
			.SearchAnalyzer("synonyms_analyzer")
			.MultiField("completion", mf => mf.SearchAsYouType()
				.Analyzer("synonyms_fixed_analyzer")
				.SearchAnalyzer("synonyms_analyzer")
				.IndexOptions("offsets")))
		.Title(f => f
			.SearchAnalyzer("synonyms_analyzer")
			.MultiField("keyword", mf => mf.Keyword().Normalizer("keyword_normalizer"))
			.MultiField("starts_with", mf => mf.Text()
				.Analyzer("starts_with_analyzer")
				.SearchAnalyzer("starts_with_analyzer_search"))
			.MultiField("completion", mf => mf.SearchAsYouType().SearchAnalyzer("synonyms_analyzer")))
		.Abstract(f => f
			.Analyzer("synonyms_fixed_analyzer")
			.SearchAnalyzer("synonyms_analyzer"))
		.Headings(f => f
			.Analyzer("synonyms_fixed_analyzer")
			.SearchAnalyzer("synonyms_analyzer"))
		.AddField("ai_rag_optimized_summary", f => f.Text()
			.Analyzer("synonyms_fixed_analyzer")
			.SearchAnalyzer("synonyms_analyzer"))
		// Rank features
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

public class SemanticConfig : IConfigureElasticsearch<DocumentationDocument>
{
	public AnalysisBuilder ConfigureAnalysis(AnalysisBuilder analysis) => analysis;

	public IReadOnlyDictionary<string, string>? IndexSettings => null;

	public MappingsBuilder<DocumentationDocument> ConfigureMappings(MappingsBuilder<DocumentationDocument> mappings) =>
		LexicalConfig.ConfigureCommonMappings(mappings)
			.StrippedBody(s => s
				.Analyzer("synonyms_fixed_analyzer")
				.SearchAnalyzer("synonyms_analyzer")
				.TermVector("with_positions_offsets")
			)
			.AddSemanticTextFields();
}

/// <summary>
/// Builds analysis settings at runtime (includes synonyms that are loaded from configuration).
/// </summary>
/// <summary>
/// Extends <see cref="SharedAnalysisFactory"/> with synonym filters for documentation indices.
/// </summary>
public static class DocumentationAnalysisFactory
{
	public static AnalysisBuilder BuildAnalysis(AnalysisBuilder analysis, string synonymSetName, string[] indexTimeSynonyms) =>
		SharedAnalysisFactory.BuildBaseAnalysis(analysis)
			.Analyzer("synonyms_fixed_analyzer", a => a.Custom()
				.Tokenizer("group_tokenizer")
				.Filters("lowercase", "synonyms_fixed_filter", "kstem"))
			.Analyzer("synonyms_analyzer", a => a.Custom()
				.Tokenizer("group_tokenizer")
				.Filters("lowercase", "synonyms_filter", "kstem"))
			.TokenFilter("synonyms_fixed_filter", tf => tf.SynonymGraph()
				.Synonyms(indexTimeSynonyms))
			.TokenFilter("synonyms_filter", tf => tf.SynonymGraph()
				.SynonymsSet(synonymSetName)
				.Updateable(true));
}
