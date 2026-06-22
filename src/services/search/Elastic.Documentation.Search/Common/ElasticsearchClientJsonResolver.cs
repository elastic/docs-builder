// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization.Metadata;
using DocSerializationContext = Elastic.Documentation.Serialization.SourceGenerationContext;
using QuerySerializationContext = Elastic.Documentation.Search.SourceGenerationContext;
using InternalSearch = Elastic.Internal.Search;

namespace Elastic.Documentation.Search.Common;

/// <summary>
/// Combined JSON type info resolver for the shared Elasticsearch client: external search contract types,
/// docs-builder document metadata, and query-rule criteria.
/// </summary>
internal static class ElasticsearchClientJsonResolver
{
	public static IJsonTypeInfoResolver Default { get; } = Create();

	private static IJsonTypeInfoResolver Create() =>
		JsonTypeInfoResolver.Combine(
			InternalSearch.SourceGenerationContext.Default,
			QuerySerializationContext.Default,
			DocSerializationContext.Default
		);
}
