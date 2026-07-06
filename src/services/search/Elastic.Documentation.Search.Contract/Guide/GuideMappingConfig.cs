// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Search.Contract.Mapping;
using Elastic.Mapping;
using Elastic.Mapping.Analysis;
using Elastic.Mapping.Mappings;

namespace Elastic.Documentation.Search.Contract;

[ElasticsearchMappingContext]
[Index<GuideDocument>(
	NameTemplate = "guide-{type}.lexical-{env}",
	DatePattern = "yyyy.MM.dd.HHmmss",
	Configuration = typeof(GuideLexicalConfig)
)]
[Index<GuideDocument>(
	NameTemplate = "guide-{type}.semantic-{env}",
	Variant = "Semantic",
	DatePattern = "yyyy.MM.dd.HHmmss",
	Configuration = typeof(GuideSemanticConfig)
)]
[AiEnrichment<GuideDocument>(
	Role = "Expert content analyst creating search metadata for Elastic's legacy /guide documentation pages.",
	MatchField = "url",
	IndexVariant = "Semantic"
)]
public static partial class GuideMappingContext;

public class GuideLexicalConfig : IConfigureElasticsearch<GuideDocument>
{
	public AnalysisBuilder ConfigureAnalysis(AnalysisBuilder analysis) => SharedAnalysisFactory.BuildBaseAnalysis(analysis);

	public IReadOnlyDictionary<string, string>? IndexSettings => null;

	public MappingsBuilder<GuideDocument> ConfigureMappings(MappingsBuilder<GuideDocument> mappings) =>
		mappings.AddSearchDocumentMappings();
}

public class GuideSemanticConfig : IConfigureElasticsearch<GuideDocument>
{
	public AnalysisBuilder ConfigureAnalysis(AnalysisBuilder analysis) => SharedAnalysisFactory.BuildBaseAnalysis(analysis);

	public IReadOnlyDictionary<string, string>? IndexSettings => null;

	public MappingsBuilder<GuideDocument> ConfigureMappings(MappingsBuilder<GuideDocument> mappings) =>
		mappings.AddSearchDocumentMappings(semantic: true);
}
