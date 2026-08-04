// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Search;

public interface INavigationSearchService
{
	Task<NavigationSearchResponse> NavigationSearchAsync(NavigationSearchRequest request, Cancel ctx = default);
}

public record NavigationSearchRequest
{
	public required string Query { get; init; }
	public int PageNumber { get; init; } = 1;
	public int PageSize { get; init; } = 20;
	public string? TypeFilter { get; init; }
}

public record NavigationSearchResponse
{
	public required IEnumerable<NavigationSearchResultItem> Results { get; init; }
	public required int TotalResults { get; init; }
	public required int PageNumber { get; init; }
	public required int PageSize { get; init; }
	public NavigationSearchAggregations Aggregations { get; init; } = new();
	public int PageCount => TotalResults > 0
				? (int)Math.Ceiling((double)TotalResults / PageSize)
				: 0;
}

public record NavigationSearchAggregations
{
	public IReadOnlyDictionary<string, long> Type { get; init; } = new Dictionary<string, long>();
}

public record NavigationSearchResult
{
	public required int TotalHits { get; init; }
	public required List<NavigationSearchResultItem> Results { get; init; }
	public IReadOnlyDictionary<string, long> Aggregations { get; init; } = new Dictionary<string, long>();
}

public record NavigationSearchResultItemParent
{
	public required string Title { get; init; }
	public required string Url { get; init; }
}

public record NavigationSearchResultItem
{
	public required string Type { get; init; }
	public required string Url { get; init; }
	public required string Title { get; init; }
	public required string Description { get; init; }
	public required NavigationSearchResultItemParent[] Parents { get; init; }
	public float Score { get; init; }
}
