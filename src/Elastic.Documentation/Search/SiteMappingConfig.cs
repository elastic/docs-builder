// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Mapping;
using Elastic.Mapping.Analysis;
using Elastic.Mapping.Mappings;

namespace Elastic.Documentation.Search;

[ElasticsearchMappingContext]
[Index<SiteDocument>(
	NameTemplate = "site-{type}.lexical-{env}",
	DatePattern = "yyyy.MM.dd.HHmmss",
	Configuration = typeof(SiteLexicalConfig)
)]
[Index<SiteDocument>(
	NameTemplate = "site-{type}.semantic-{env}",
	Variant = "Semantic",
	DatePattern = "yyyy.MM.dd.HHmmss",
	Configuration = typeof(SiteSemanticConfig)
)]
[AiEnrichment<SiteDocument>(
	Role = "Expert content analyst creating search metadata for Elastic's website pages (blogs, labs articles, product pages, events). Audience: developers, DevOps engineers, security analysts, and IT decision-makers.",
	MatchField = "url",
	IndexVariant = "Semantic"
)]
public static partial class SiteMappingContext;

public class SiteLexicalConfig : IConfigureElasticsearch<SiteDocument>
{
	public AnalysisBuilder ConfigureAnalysis(AnalysisBuilder analysis) => analysis;

	public IReadOnlyDictionary<string, string>? IndexSettings => null;

	public MappingsBuilder<SiteDocument> ConfigureMappings(MappingsBuilder<SiteDocument> mappings) =>
		ConfigureCommonMappings(mappings);

	internal static MappingsBuilder<SiteDocument> ConfigureCommonMappings(MappingsBuilder<SiteDocument> m) => m
		.AddCommonTitleMappings()
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

public class SiteSemanticConfig : IConfigureElasticsearch<SiteDocument>
{
	public AnalysisBuilder ConfigureAnalysis(AnalysisBuilder analysis) => analysis;

	public IReadOnlyDictionary<string, string>? IndexSettings => null;

	public MappingsBuilder<SiteDocument> ConfigureMappings(MappingsBuilder<SiteDocument> mappings) =>
		SiteLexicalConfig.ConfigureCommonMappings(mappings)
			.AddSemanticTextFields()
			.AddField("stripped_body.jina", f => f.SemanticText().InferenceId(".jina-embeddings-v5-text-small"));
}

/// <summary>
/// Builds analysis settings at runtime for site indices.
/// Delegates to <see cref="SharedAnalysisFactory"/> for base analysis shared with documentation indices.
/// </summary>
public static class SiteAnalysisFactory
{
	public static AnalysisBuilder BuildAnalysis(AnalysisBuilder analysis) =>
		SharedAnalysisFactory.BuildBaseAnalysis(analysis);
}
