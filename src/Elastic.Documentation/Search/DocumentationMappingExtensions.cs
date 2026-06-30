// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Internal.Search.Mapping;
using Elastic.Mapping.Mappings;

namespace Elastic.Documentation.Search;

/// <summary>
/// Documentation-specific mapping extensions for <c>docs-{type}.{lexical|semantic}-{env}</c> indices.
/// Layers the <c>applies_to</c> nested and <c>parents</c> object sub-field definitions on top of
/// <see cref="SharedMappingConfig.AddSearchDocumentMappings{T}"/>.
/// </summary>
public static class DocumentationMappingExtensions
{
	/// <summary>
	/// Applies documentation-specific field topology to a <see cref="DocumentationDocument"/> mappings builder:
	/// keyword overrides for <c>applies_to</c> sub-fields and multi-field configuration for <c>parents</c>.
	/// </summary>
	public static MappingsBuilder<DocumentationDocument> AddDocumentationMappings(this MappingsBuilder<DocumentationDocument> m) =>
		m
			// applies_to is [Nested] — AddProperty places sub-fields under "properties".
			// Note: AppliesToEntry properties have no [Keyword] attributes so the generated
			// AppliesToEntryNestedBuilder pre-types them as Text; AddProperty lets us override to Keyword.
			.AddProperty("applies_to.type", f => f.Keyword().Normalizer(SharedMappingConfig.KeywordNormalizer))
			.AddProperty("applies_to.sub_type", f => f.Keyword().Normalizer(SharedMappingConfig.KeywordNormalizer))
			.AddProperty("applies_to.lifecycle", f => f.Keyword().Normalizer(SharedMappingConfig.KeywordNormalizer))
			.AddProperty("applies_to.version", f => f.Version())
			// parents is an object array — AddProperty places sub-fields under "properties".
			.AddProperty("parents.url", f => f.Keyword()
				.MultiField("match", mf => mf.Text())
				.MultiField("prefix", mf => mf.Text().Analyzer(SharedMappingConfig.HierarchyAnalyzer)))
			.AddProperty("parents.title", f => f.Text()
				.SearchAnalyzer(SharedMappingConfig.SynonymsAnalyzer)
				.MultiField("keyword", mf => mf.Keyword()));
}
