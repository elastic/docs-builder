// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Api.Core.Search;

// note still called SearchUseCase because we'll re-add Search() and ensure both share the same client.
public partial class NavigationSearchUsecase(INavigationSearchGateway navigationSearchGateway, ILogger<NavigationSearchUsecase> logger)
{
	public async Task<NavigationSearchApiResponse> NavigationSearchAsync(NavigationSearchApiRequest request, Cancel ctx = default)
	{
		var result = await navigationSearchGateway.NavigationSearchAsync(
			request.Query,
			request.PageNumber,
			request.PageSize,
			request.TypeFilter,
			ctx
		);

		var response = new NavigationSearchApiResponse
		{
			Results = result.Results,
			TotalResults = result.TotalHits,
			PageNumber = request.PageNumber,
			PageSize = request.PageSize,
			Aggregations = new NavigationSearchAggregations { Type = result.Aggregations }
		};

		LogNavigationSearchResults(
			logger,
			response.PageSize,
			response.PageNumber,
			request.Query,
			new AutoCompleteResultsLogProperties(result.Results.Select(i => i.Url).ToArray())
		);

		return response;
	}

	[LoggerMessage(Level = LogLevel.Information, Message = "Navigation search completed with {PageSize} (page {PageNumber}) results for query '{SearchQuery}'")]
	private static partial void LogNavigationSearchResults(ILogger logger, int pageSize, int pageNumber, string searchQuery, [LogProperties] AutoCompleteResultsLogProperties result);

	private sealed record AutoCompleteResultsLogProperties(string[] Urls);
}

public record NavigationSearchApiRequest
{
	public required string Query { get; init; }
	public int PageNumber { get; init; } = 1;
	public int PageSize { get; init; } = 20;
	public string? TypeFilter { get; init; }
}

public record NavigationSearchApiResponse
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
