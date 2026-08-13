// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Search.Contract.Mapping;
using Elastic.Mapping;
using Elastic.Mapping.Analysis;
using Elastic.Mapping.Mappings;

namespace Elastic.Documentation.Search.Contract;

[ElasticsearchMappingContext]
[Index<LabsDocument>(
	NameTemplate = "labs-{type}.lexical-{env}",
	DatePattern = "yyyy.MM.dd.HHmmss",
	Configuration = typeof(LabsLexicalConfig)
)]
[Index<LabsDocument>(
	NameTemplate = "labs-{type}.semantic-{env}",
	Variant = "Semantic",
	DatePattern = "yyyy.MM.dd.HHmmss",
	Configuration = typeof(LabsSemanticConfig)
)]
[AiEnrichment<LabsDocument>(
	Role = "Expert content analyst creating search metadata for Elastic's website pages (blogs, labs articles, product pages, events). Audience: developers, DevOps engineers, security analysts, and IT decision-makers.",
	MatchField = "url",
	IndexVariant = "Semantic"
)]
public static partial class LabsMappingContext;

public class LabsLexicalConfig : IConfigureElasticsearch<LabsDocument>
{
	public AnalysisBuilder ConfigureAnalysis(AnalysisBuilder analysis) => SharedAnalysisFactory.BuildBaseAnalysis(analysis);

	public IReadOnlyDictionary<string, string>? IndexSettings => null;

	public MappingsBuilder<LabsDocument> ConfigureMappings(MappingsBuilder<LabsDocument> mappings) =>
		mappings.AddSearchDocumentMappings().AddSiteMappings();
}

public class LabsSemanticConfig : IConfigureElasticsearch<LabsDocument>
{
	public AnalysisBuilder ConfigureAnalysis(AnalysisBuilder analysis) => SharedAnalysisFactory.BuildBaseAnalysis(analysis);

	public IReadOnlyDictionary<string, string>? IndexSettings => null;

	public MappingsBuilder<LabsDocument> ConfigureMappings(MappingsBuilder<LabsDocument> mappings) =>
		mappings.AddSearchDocumentMappings(semantic: true).AddSiteMappings();
}
