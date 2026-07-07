// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization.Metadata;
using Elastic.Documentation.Search;
using Elastic.Documentation.Search.Contract;
using DocSerializationContext = Elastic.Documentation.Serialization.SourceGenerationContext;
using QuerySerializationContext = Elastic.Documentation.Search.SourceGenerationContext;

namespace Elastic.Documentation.Search.Common;

/// <summary>
/// Combined JSON type info resolver for the shared Elasticsearch client.
/// <para>
/// Combines the contract source-gen context (ISearchDocument, SearchDocumentBase,
/// SiteDocument, LabsDocument, GuideDocument, WebsiteSearchDocument) with the
/// in-repo query context and the docs-builder context (DocumentationDocument,
/// AppliesToEntry, IndexedProduct).
/// </para>
/// <para>
/// Registers <c>DocumentationDocument → "docs"</c> as a runtime-derived type on both
/// <c>ISearchDocument</c> and <c>SearchDocumentBase</c>, and configures
/// <c>SearchDocumentBase</c> with <c>FallBackToBaseType</c> so that a missing or
/// unrecognized <c>$type</c> deserializes to a <see cref="SearchDocumentBase"/> instance
/// rather than throwing.
/// </para>
/// </summary>
internal static class ElasticsearchClientJsonResolver
{
	public static IJsonTypeInfoResolver Default { get; } = Create();

	private static IJsonTypeInfoResolver Create() =>
		SearchDocumentPolymorphism.Compose(
			consumerContexts:
			[
				QuerySerializationContext.Default,
				DocSerializationContext.Default,
			],
			SearchDocumentPolymorphism.AddDerivedType<ISearchDocument>(typeof(DocumentationDocument), "docs"),
			SearchDocumentPolymorphism.AddDerivedType<SearchDocumentBase>(typeof(DocumentationDocument), "docs"),
			SearchDocumentPolymorphism.WithFallback()
		);
}
