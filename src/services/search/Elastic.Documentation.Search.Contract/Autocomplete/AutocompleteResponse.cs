// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Search.Contract;

/// <summary>Typeahead response — leaner aggregations and always-lexical execution.</summary>
public record AutocompleteResponse<TDocument> where TDocument : SearchDocumentBase
{
	public required IReadOnlyList<SearchResultItem<TDocument>> Results { get; init; }
	public required long TotalResults { get; init; }
	public required int PageNumber { get; init; }
	public required int PageSize { get; init; }
	public required AutocompleteAggregations Aggregations { get; init; }

	public int PageCount => PageSize > 0
		? (int)Math.Ceiling((double)TotalResults / PageSize)
		: 0;

	/// <summary>
	/// Time Elasticsearch spent processing the query, in milliseconds (the <c>took</c> field
	/// from the ES response JSON). Excludes network latency.
	/// </summary>
	public long ElasticsearchTookMs { get; init; }

	/// <summary>
	/// <c>true</c> when the Elasticsearch response was a successful 2xx with a valid hits body;
	/// <c>false</c> on 4xx/5xx errors or transport failures.
	/// </summary>
	public bool IsValidResponse { get; init; }
}
