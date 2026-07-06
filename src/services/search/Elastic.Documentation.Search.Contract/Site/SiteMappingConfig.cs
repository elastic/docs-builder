// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Search.Contract.Mapping;
using Elastic.Mapping;
using Elastic.Mapping.Analysis;
using Elastic.Mapping.Mappings;

namespace Elastic.Documentation.Search.Contract;

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
	public AnalysisBuilder ConfigureAnalysis(AnalysisBuilder analysis) => SharedAnalysisFactory.BuildBaseAnalysis(analysis);

	public IReadOnlyDictionary<string, string>? IndexSettings => null;

	public MappingsBuilder<SiteDocument> ConfigureMappings(MappingsBuilder<SiteDocument> mappings) =>
		mappings.AddSearchDocumentMappings();
}

public class SiteSemanticConfig : IConfigureElasticsearch<SiteDocument>
{
	public AnalysisBuilder ConfigureAnalysis(AnalysisBuilder analysis) => SharedAnalysisFactory.BuildBaseAnalysis(analysis);

	public IReadOnlyDictionary<string, string>? IndexSettings => null;

	public MappingsBuilder<SiteDocument> ConfigureMappings(MappingsBuilder<SiteDocument> mappings) =>
		mappings.AddSearchDocumentMappings(semantic: true);
}
