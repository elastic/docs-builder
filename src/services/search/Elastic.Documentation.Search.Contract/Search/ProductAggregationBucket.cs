// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Search.Contract;

/// <summary>Product facet bucket — doc count plus the display name resolved by the consumer.</summary>
public record ProductAggregationBucket
{
	/// <summary>The number of results for the product.</summary>
	/// <example>155</example>
	public required long Count { get; init; }
	/// <summary>The product's name for display.</summary>
	/// <example>"elastic docs"</example>
	public string? DisplayName { get; init; }
}
