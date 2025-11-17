// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Api.Core.Search;

public partial class SearchUsecase(ISearchGateway searchGateway, ILogger<SearchUsecase> logger)
{
	public async Task<SearchResponse> Search(SearchRequest request, Cancel ctx = default)
	{
		var (totalHits, results) = await searchGateway.SearchAsync(
			request.Query,
			request.PageNumber,
			request.PageSize,
			ctx
		);

		var response = new SearchResponse
		{
			Results = results,
			TotalResults = totalHits,
			PageNumber = request.PageNumber,
			PageSize = request.PageSize,
		};

		LogSearchResults(
			logger,
			response.PageSize,
			response.PageNumber,
			request.Query,
			new SearchResultsLogProperties(results.Select(i => i.Url).ToArray())
		);

		return response;
	}

	[LoggerMessage(Level = LogLevel.Information, Message = "Search completed with {PageSize} (page {PageNumber}) results for query '{SearchQuery}'")]
	private static partial void LogSearchResults(ILogger logger, int pageSize, int pageNumber, string searchQuery, [LogProperties] SearchResultsLogProperties result);

	private sealed record SearchResultsLogProperties(string[] Urls);
}

public record SearchRequest
{
	public required string Query { get; init; }
	public int PageNumber { get; init; } = 1;
	public int PageSize { get; init; } = 5;
}

public record SearchResponse
{
	public required IEnumerable<SearchResultItem> Results { get; init; }
	public required int TotalResults { get; init; }
	public required int PageNumber { get; init; }
	public required int PageSize { get; init; }
	public int PageCount => TotalResults > 0
				? (int)Math.Ceiling((double)TotalResults / PageSize)
				: 0;
}

public record SearchResultItemParent
{
	public required string Title { get; init; }
	public required string Url { get; init; }
}

public record SearchResultItem
{
	public required string Url { get; init; }
	public required string Title { get; init; }
	public required string Description { get; init; }
	public required SearchResultItemParent[] Parents { get; init; }
	public float Score { get; init; }
	public string? HighlightedBody { get; init; }
}
