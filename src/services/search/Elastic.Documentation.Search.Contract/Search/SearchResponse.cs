// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Search.Contract;

/// <summary>
/// Full-page search response. <typeparamref name="TDocument"/> is the typed hit shape —
/// e.g. <c>DocumentationDocument</c> for the docs-only index,
/// or <see cref="WebsiteSearchDocument"/> for the unified <c>website-search.semantic-*</c> index.
/// </summary>
public record SearchResponse<TDocument> where TDocument : SearchDocumentBase
{
	public required IReadOnlyList<SearchResultItem<TDocument>> Results { get; init; }
	public required long TotalResults { get; init; }
	public required int PageNumber { get; init; }
	public required int PageSize { get; init; }
	public required SearchAggregations Aggregations { get; init; }

	/// <summary>True when the request executed the hybrid lex+semantic path.</summary>
	public bool IsSemanticQuery { get; init; }

	/// <summary>
	/// Time Elasticsearch spent processing the query, in milliseconds (the <c>took</c> field
	/// from the ES response JSON). Excludes network latency — compare with wall-clock round-trip
	/// time to separate ES processing cost from transport overhead.
	/// </summary>
	public long ElasticsearchTookMs { get; init; }

	/// <summary>
	/// <c>true</c> when the Elasticsearch response was a successful 2xx with a valid hits body;
	/// <c>false</c> on 4xx/5xx errors or transport failures. Check this before using
	/// <see cref="ElasticsearchTookMs"/> — error responses do not populate <c>took</c>.
	/// </summary>
	public bool IsValidResponse { get; init; }

	public int PageCount => PageSize > 0
		? (int)Math.Ceiling((double)TotalResults / PageSize)
		: 0;
}
