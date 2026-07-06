// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Search.Contract;

/// <summary>Product facet bucket — doc count plus the display name resolved by the consumer.</summary>
public record ProductAggregationBucket
{
	public required long Count { get; init; }
	public string? DisplayName { get; init; }
}
