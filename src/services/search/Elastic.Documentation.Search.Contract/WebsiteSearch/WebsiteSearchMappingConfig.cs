// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Search.Contract.Mapping;
using Elastic.Mapping;
using Elastic.Mapping.Analysis;
using Elastic.Mapping.Mappings;

namespace Elastic.Documentation.Search.Contract;

[ElasticsearchMappingContext]
[Index<WebsiteSearchDocument>(
	NameTemplate = "website-search.lexical-{env}",
	DatePattern = "yyyy.MM.dd.HHmmss",
	Configuration = typeof(WebsiteSearchLexicalConfig)
)]
[Index<WebsiteSearchDocument>(
	NameTemplate = "website-search.semantic-{env}",
	Variant = "Semantic",
	DatePattern = "yyyy.MM.dd.HHmmss",
	Configuration = typeof(WebsiteSearchSemanticConfig)
)]
public static partial class WebsiteSearchMappingContext;

public class WebsiteSearchLexicalConfig : IConfigureElasticsearch<WebsiteSearchDocument>
{
	public AnalysisBuilder ConfigureAnalysis(AnalysisBuilder analysis) => SharedAnalysisFactory.BuildBaseAnalysis(analysis);

	public IReadOnlyDictionary<string, string>? IndexSettings => null;

	public MappingsBuilder<WebsiteSearchDocument> ConfigureMappings(MappingsBuilder<WebsiteSearchDocument> mappings) =>
		mappings.AddSearchDocumentMappings().AddSiteMappings();
}

public class WebsiteSearchSemanticConfig : IConfigureElasticsearch<WebsiteSearchDocument>
{
	public AnalysisBuilder ConfigureAnalysis(AnalysisBuilder analysis) => SharedAnalysisFactory.BuildBaseAnalysis(analysis);

	public IReadOnlyDictionary<string, string>? IndexSettings => null;

	public MappingsBuilder<WebsiteSearchDocument> ConfigureMappings(MappingsBuilder<WebsiteSearchDocument> mappings) =>
		mappings.AddSearchDocumentMappings(semantic: true).AddSiteMappings();
}
