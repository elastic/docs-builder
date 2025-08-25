// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Api.Core.Search;

public class SearchUsecase(ISearchGateway searchGateway)
{
	public async Task<SearchResponse> Search(SearchRequest request, Cancel ctx = default)
	{
		// var validationResult = validator.Validate(request);
		// if (!validationResult.IsValid)
		// 	throw new ArgumentException(validationResult.Message);

		var (totalHits, results) = await searchGateway.SearchAsync(
			request.Query,
			request.PageNumber,
			request.PageSize, ctx
		);

		return new SearchResponse
		{
			Results = results,
			TotalResults = totalHits
		};
	}
}

public record SearchRequest
{
	public required string Query { get; init; }
	public int PageNumber { get; init; } = 1;
	public int PageSize { get; init; } = 10;
}

public record SearchResponse
{
	public required IEnumerable<SearchResultItem> Results { get; init; }
	public required int TotalResults { get; init; }
}

public record SearchResultItem
{
	public required string Url { get; init; }
	public required string Title { get; init; }
	public required string Description { get; init; }
	public required double Score { get; init; }
}
