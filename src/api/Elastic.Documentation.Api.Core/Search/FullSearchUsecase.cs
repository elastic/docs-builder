// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Api.Core.Search;

/// <summary>
/// Use case for full-page search operations.
/// </summary>
public partial class FullSearchUsecase(IFullSearchGateway fullSearchGateway, ILogger<FullSearchUsecase> logger)
{
	public async Task<FullSearchApiResponse> SearchAsync(FullSearchApiRequest request, Cancel ctx = default)
	{
		var result = await fullSearchGateway.SearchAsync(
			new FullSearchRequest
			{
				Query = request.Query,
				PageNumber = request.PageNumber,
				PageSize = request.PageSize,
				TypeFilter = request.TypeFilter,
				SectionFilter = request.SectionFilter,
				DeploymentFilter = request.DeploymentFilter,
				ProductFilter = request.ProductFilter,
				VersionFilter = request.VersionFilter,
				SortBy = request.SortBy
			},
			ctx
		);

		var response = new FullSearchApiResponse
		{
			Results = result.Results,
			TotalResults = result.TotalHits,
			PageNumber = request.PageNumber,
			PageSize = request.PageSize,
			Aggregations = result.Aggregations,
			IsSemanticQuery = result.IsSemanticQuery
		};

		LogFullSearchResults(
			logger,
			response.PageSize,
			response.PageNumber,
			request.Query,
			result.IsSemanticQuery,
			new FullSearchResultsLogProperties(result.Results.Select(i => i.Url).ToArray())
		);

		return response;
	}

	[LoggerMessage(Level = LogLevel.Information, Message = "Full search completed with {PageSize} (page {PageNumber}) results for query '{SearchQuery}' (semantic: {IsSemantic})")]
	private static partial void LogFullSearchResults(
		ILogger logger,
		int pageSize,
		int pageNumber,
		string searchQuery,
		bool isSemantic,
		[LogProperties] FullSearchResultsLogProperties result);

	private sealed record FullSearchResultsLogProperties(string[] Urls);
}

/// <summary>
/// API request model for full-page search.
/// </summary>
public record FullSearchApiRequest
{
	public required string Query { get; init; }
	public int PageNumber { get; init; } = 1;
	public int PageSize { get; init; } = 20;
	public string[]? TypeFilter { get; init; }
	public string[]? SectionFilter { get; init; }
	public string[]? DeploymentFilter { get; init; }
	public string[]? ProductFilter { get; init; }
	public string? VersionFilter { get; init; }
	public string SortBy { get; init; } = "relevance";
}

/// <summary>
/// API response model for full-page search.
/// </summary>
public record FullSearchApiResponse
{
	public required IEnumerable<FullSearchResultItem> Results { get; init; }
	public required int TotalResults { get; init; }
	public required int PageNumber { get; init; }
	public required int PageSize { get; init; }
	public FullSearchAggregations Aggregations { get; init; } = new();
	public bool IsSemanticQuery { get; init; }
	public int PageCount => TotalResults > 0
		? (int)Math.Ceiling((double)TotalResults / PageSize)
		: 0;
}
