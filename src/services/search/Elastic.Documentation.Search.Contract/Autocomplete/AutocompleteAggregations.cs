// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Search.Contract;

/// <summary>Aggregations returned by <see cref="ISearchService{TDocument}.AutocompleteAsync"/>.</summary>
public record AutocompleteAggregations
{
	public IReadOnlyDictionary<string, long> Type { get; init; } = new Dictionary<string, long>();
}
