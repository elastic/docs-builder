// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Api.Core.Search;

public interface ISearchGateway
{
	Task<SearchResult> SearchAsync(
		string query,
		int pageNumber,
		int pageSize,
		string? filter = null,
		Cancel ctx = default
	);
}

public record SearchResult
{
	public required int TotalHits { get; init; }
	public required List<SearchResultItem> Results { get; init; }
	public IReadOnlyDictionary<string, long> Aggregations { get; init; } = new Dictionary<string, long>();
}
