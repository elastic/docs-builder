// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization.Metadata;
using Elastic.Documentation.Serialization;
using InternalSearch = Elastic.Internal.Search;

namespace Elastic.Documentation.Search.Common;

/// <summary>
/// Combined JSON type info resolver for the shared Elasticsearch client: external search contract types,
/// docs-builder document metadata, and internal query-rule criteria from the Elasticsearch search package.
/// </summary>
internal static class ElasticsearchClientJsonResolver
{
	public static IJsonTypeInfoResolver Default { get; } = Create();

	private static IJsonTypeInfoResolver Create()
	{
		var combined = JsonTypeInfoResolver.Combine(
			InternalSearch.SourceGenerationContext.Default,
			SourceGenerationContext.Default);

		return new RuleQueryMatchCriteriaTypeInfoResolver(combined);
	}
}
