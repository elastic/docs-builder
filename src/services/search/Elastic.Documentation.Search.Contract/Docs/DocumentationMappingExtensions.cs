// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Search.Contract.Mapping;
using Elastic.Mapping.Mappings;

namespace Elastic.Documentation.Search.Contract;

/// <summary>
/// Documentation-specific mapping extensions for <c>docs-{type}.{lexical|semantic}-{env}</c> indices.
/// Layers the <c>applies_to</c> nested sub-field definitions on top of
/// <see cref="SharedMappingConfig.AddSearchDocumentMappings{T}"/>. <c>parents</c> topology lives in
/// <see cref="SharedMappingConfig"/> itself since <c>Parents</c> is declared on
/// <see cref="SearchDocumentBase"/> and shared by every document type, not just documentation.
/// </summary>
public static class DocumentationMappingExtensions
{
	/// <summary>
	/// Applies documentation-specific field topology to a <see cref="DocumentationDocument"/> mappings
	/// builder: keyword overrides for <c>applies_to</c> sub-fields (the only field unique to
	/// <see cref="DocumentationDocument"/> among the shared search document types).
	/// </summary>
	public static MappingsBuilder<DocumentationDocument> AddDocumentationMappings(this MappingsBuilder<DocumentationDocument> m) =>
		m
			// applies_to is [Nested] — AddProperty places sub-fields under "properties".
			// Note: AppliesToEntry properties have no [Keyword] attributes so the generated
			// AppliesToEntryNestedBuilder pre-types them as Text; AddProperty lets us override to Keyword.
			.AddProperty("applies_to.type", f => f.Keyword().Normalizer(SharedMappingConfig.KeywordNormalizer))
			.AddProperty("applies_to.sub_type", f => f.Keyword().Normalizer(SharedMappingConfig.KeywordNormalizer))
			.AddProperty("applies_to.lifecycle", f => f.Keyword().Normalizer(SharedMappingConfig.KeywordNormalizer))
			.AddProperty("applies_to.version", f => f.Version());
}
