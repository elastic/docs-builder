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
		mappings.AddSearchDocumentMappings().AddSiteMappings();
}

public class SiteSemanticConfig : IConfigureElasticsearch<SiteDocument>
{
	public AnalysisBuilder ConfigureAnalysis(AnalysisBuilder analysis) => SharedAnalysisFactory.BuildBaseAnalysis(analysis);

	public IReadOnlyDictionary<string, string>? IndexSettings => null;

	public MappingsBuilder<SiteDocument> ConfigureMappings(MappingsBuilder<SiteDocument> mappings) =>
		mappings.AddSearchDocumentMappings(semantic: true).AddSiteMappings();
}

/// <summary>
/// Site-specific field topology and legacy-name aliases (Og/Twitter/Http nesting, locale rename).
/// Generic over <see cref="SiteDocument"/> so <see cref="LabsDocument"/> and
/// <see cref="WebsiteSearchDocument"/> can reuse it too.
/// </summary>
public static class SiteMappingExtensions
{
	public static MappingsBuilder<T> AddSiteMappings<T>(this MappingsBuilder<T> m) where T : SiteDocument =>
		m
			// Aliases for pre-nesting/pre-rename field names — remove once all indices are rebuilt
			// under the new `og.*`/`twitter.*`/`http.*`/`locale` shape and no consumer queries the old names.
			.AddField("language", f => f.Alias("locale"))
			.AddField("og_title", f => f.Alias("og.title"))
			.AddField("og_description", f => f.Alias("og.description"))
			.AddField("og_image", f => f.Alias("og.image"))
			.AddField("twitter_image", f => f.Alias("twitter.image"))
			.AddField("twitter_card", f => f.Alias("twitter.card"))
			.AddField("http_etag", f => f.Alias("http.etag"))
			.AddField("http_last_modified", f => f.Alias("http.last_modified"));
}
