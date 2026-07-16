// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Search.Common;
using Elastic.Documentation.Search.Contract;
using Elastic.Documentation.Search.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Search;

/// <summary>
/// Adapter over <see cref="ISearchService{TDocument}"/>. Translates the docs-builder-specific
/// <see cref="NavigationSearchRequest"/> / <see cref="NavigationSearchResponse"/> shapes into the
/// shared <see cref="AutocompleteRequest"/> / <see cref="AutocompleteResponse{TDocument}"/> shapes
/// and back. All query construction, highlighting, and ES interaction lives in
/// <see cref="DefaultSearchService{TDocument}"/>.
/// </summary>
public partial class NavigationSearchService(
	ISearchService<DocumentationDocument> inner,
	ElasticsearchClientAccessor clientAccessor,
	ILogger<NavigationSearchService> logger)
	: INavigationSearchService, IDisposable
{
	public async Task<bool> CanConnect(Cancel ctx) => await clientAccessor.CanConnect(ctx);

	public async Task<NavigationSearchResponse> NavigationSearchAsync(NavigationSearchRequest request, Cancel ctx = default)
	{
		var resp = await inner.AutocompleteAsync(new AutocompleteRequest
		{
			Query = request.Query,
			PageNumber = request.PageNumber,
			PageSize = request.PageSize,
			TypeFilter = request.TypeFilter
		}, ctx);

		var response = new NavigationSearchResponse
		{
			TotalResults = (int)resp.TotalResults,
			PageNumber = resp.PageNumber,
			PageSize = resp.PageSize,
			Aggregations = new NavigationSearchAggregations { Type = resp.Aggregations.Type },
			Results = resp.Results.Select(item => new NavigationSearchResultItem
			{
				Type = item.Document.ContentType,
				Url = item.Document.Path,
				Title = item.Title,
				Description = item.Description,
				Parents = (item.Document.Parents ?? [])
					.Select(p => new NavigationSearchResultItemParent { Title = p.Title, Url = p.Path })
					.ToArray(),
				Score = item.Score
			}).ToList()
		};

		LogNavigationSearchResults(
			logger,
			response.PageSize,
			response.PageNumber,
			request.Query,
			response.Results.Select(i => i.Url).ToArray());

		return response;
	}

	[LoggerMessage(Level = LogLevel.Information, Message = "Navigation search completed with {PageSize} (page {PageNumber}) results for query '{SearchQuery}': {Urls}")]
	private static partial void LogNavigationSearchResults(ILogger logger, int pageSize, int pageNumber, string searchQuery, string[] urls);

	/// <summary>
	/// Explains why a document did or didn't match for a given query. Used by
	/// <c>SearchRelevanceTests</c> for relevance regression assertions. Delegates to the
	/// shared diagnostic extension methods on <see cref="DefaultSearchService{TDocument}"/>.
	/// </summary>
	public async Task<ExplainResult> ExplainDocumentAsync(string query, string documentUrl, Cancel ctx = default)
	{
		if (inner is DefaultSearchService<DocumentationDocument> defaultImpl)
			return await defaultImpl.ExplainDocumentAsync(query, documentUrl, ctx);

		return new ExplainResult
		{
			SearchTitle = "N/A",
			DocumentUrl = documentUrl,
			Found = false,
			Explanation = "Explain is only available when the underlying ISearchService is DefaultSearchService<DocumentationDocument>."
		};
	}

	public async Task<(ExplainResult TopResult, ExplainResult ExpectedResult)> ExplainTopResultAndExpectedAsync(
		string query, string expectedDocumentUrl, Cancel ctx = default)
	{
		if (inner is DefaultSearchService<DocumentationDocument> defaultImpl)
			return await defaultImpl.ExplainTopResultAndExpectedAsync(query, expectedDocumentUrl, ctx);

		var noop = new ExplainResult { SearchTitle = "N/A", DocumentUrl = "N/A", Found = false, Explanation = "Explain unavailable on non-default ISearchService impl." };
		return (noop, noop);
	}

	public void Dispose() => GC.SuppressFinalize(this);
}
