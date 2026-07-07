// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Search.Contract.Mapping;
using Elastic.Mapping;
using Elastic.Mapping.Analysis;
using Elastic.Mapping.Mappings;

namespace Elastic.Documentation.Search.Contract;

/// <summary>
/// docs-builder index topology: <c>docs-{type}.{lexical|semantic}-{env}</c>.
/// Defines the Elasticsearch index configuration for the documentation search indices,
/// including analysis settings, mapping extensions, and AI enrichment configuration.
/// </summary>
[ElasticsearchMappingContext]
[Index<DocumentationDocument>(
	NameTemplate = "docs-{type}.lexical-{env}",
	DatePattern = "yyyy.MM.dd.HHmmss",
	Configuration = typeof(DocumentationLexicalConfig)
)]
[Index<DocumentationDocument>(
	NameTemplate = "docs-{type}.semantic-{env}",
	Variant = "Semantic",
	DatePattern = "yyyy.MM.dd.HHmmss",
	Configuration = typeof(DocumentationSemanticConfig)
)]
[AiEnrichment<DocumentationDocument>(
	Role = "Expert technical writer creating search metadata for Elastic documentation (Elasticsearch, Kibana, Beats, Logstash). Audience: developers, DevOps, data engineers.",
	MatchField = "path",
	IndexVariant = "Semantic"
)]
public static partial class DocumentationMappingContext;

public class DocumentationLexicalConfig : IConfigureElasticsearch<DocumentationDocument>
{
	public AnalysisBuilder ConfigureAnalysis(AnalysisBuilder analysis) => SharedAnalysisFactory.BuildBaseAnalysis(analysis);

	public IReadOnlyDictionary<string, string>? IndexSettings => null;

	public MappingsBuilder<DocumentationDocument> ConfigureMappings(MappingsBuilder<DocumentationDocument> mappings) =>
		mappings.AddSearchDocumentMappings().AddDocumentationMappings();
}

public class DocumentationSemanticConfig : IConfigureElasticsearch<DocumentationDocument>
{
	public AnalysisBuilder ConfigureAnalysis(AnalysisBuilder analysis) => analysis;

	public IReadOnlyDictionary<string, string>? IndexSettings => null;

	public MappingsBuilder<DocumentationDocument> ConfigureMappings(MappingsBuilder<DocumentationDocument> mappings) =>
		mappings.AddSearchDocumentMappings(semantic: true).AddDocumentationMappings();
}
