// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Search.Contract;

/// <summary>
/// Resolves a product id (e.g. <c>elasticsearch</c>, <c>kibana</c>) to a human-friendly display
/// name. Injected into <see cref="DefaultSearchService{TDocument}"/> (or its equivalents) to
/// enrich product aggregation buckets and per-hit product references during result post-processing.
/// Consumers without product metadata can omit the dependency — DefaultSearchService treats it as
/// optional and emits null <c>DisplayName</c> values when no lookup is wired.
/// </summary>
public interface IProductNameLookup
{
	/// <summary>
	/// Try to resolve <paramref name="productId"/> to a display name. Implementations should
	/// return <c>false</c> (and an arbitrary <paramref name="displayName"/>) for unknown ids
	/// rather than throwing.
	/// </summary>
	bool TryGetProductName(string productId, out string displayName);
}
