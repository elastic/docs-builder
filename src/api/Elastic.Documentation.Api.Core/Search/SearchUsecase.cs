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
			request.PageSize,
			ctx
		);


		return new SearchResponse
		{
			Results = results,
			TotalResults = totalHits,
			PageNumber = request.PageNumber,
			PageSize = request.PageSize,
		};
	}
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
}
