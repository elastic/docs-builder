// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Search;

public class RelatedPagesService(IFullSearchService searchService) : IRelatedPagesService
{
	private const int ResultCount = 5;

	public async Task<RelatedPagesResponse> GetRelatedPagesAsync(string path, Cancel ctx = default)
	{
		var query = RelatedPagesQuery.FromPath(path);
		if (query.Length == 0)
			return new RelatedPagesResponse { Query = query, Results = [] };

		var response = await searchService.SearchAsync(new FullSearchRequest
		{
			Query = query,
			PageSize = ResultCount + 1,
			IncludeHighlighting = false,
			ForceSemantic = true
		}, ctx);

		var normalizedPath = NormalizePath(path);
		var results = response.Results
			.Where(result => !string.Equals(NormalizePath(result.Url), normalizedPath, StringComparison.OrdinalIgnoreCase))
			.Where(result => result.Score > 0)
			.Take(ResultCount)
			.Select(result => new RelatedPage
			{
				Url = result.Url,
				Title = result.Title,
				Description = result.AiShortSummary ?? result.Description,
				Parents = result.Parents.Select(parent => new RelatedPageParent
				{
					Title = parent.Title,
					Url = parent.Url
				}).ToArray()
			})
			.ToArray();

		return new RelatedPagesResponse { Query = query, Results = results };
	}

	private static string NormalizePath(string path)
	{
		if (Uri.TryCreate(path, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https")
			path = uri.AbsolutePath;
		return path.TrimEnd('/');
	}
}
