// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Search.Contract;

/// <summary>
/// Typeahead / autocomplete request — leaner than <see cref="SearchRequest"/>. Always uses
/// lexical-only execution with highlighting enabled; the optional <see cref="TypeFilter"/> is
/// applied as a <c>post_filter</c> so the type aggregation still reflects the unfiltered counts.
/// </summary>
public record AutocompleteRequest
{
	public required string Query { get; init; }
	public int PageNumber { get; init; } = 1;
	public int PageSize { get; init; } = 20;

	/// <summary>Single <c>content_type</c> filter, applied as a post_filter.</summary>
	public string? TypeFilter { get; init; }
}
