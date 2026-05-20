// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Search.Common;
using Elastic.Internal.Search;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Search;

/// <summary>
/// Adapter over the shared <see cref="ISearchService{TDocument}"/>. Maps the docs-builder
/// <see cref="FullSearchRequest"/>/<see cref="FullSearchResponse"/> shapes onto the shared
/// <see cref="SearchRequest"/>/<see cref="SearchResponse{TDocument}"/> shapes; enriches the
/// per-hit <c>Product</c> and <c>RelatedProducts</c> with display names via
/// <see cref="IProductNameLookup"/> (the shared service already enriches aggregation buckets).
/// </summary>
public partial class FullSearchService(
	ISearchService<DocumentationDocument> inner,
	IProductNameLookup productNameLookup,
	ElasticsearchClientAccessor clientAccessor,
	ILogger<FullSearchService> logger)
	: IFullSearchService, IDisposable
{
	public async Task<FullSearchResponse> SearchAsync(FullSearchRequest request, Cancel ctx = default)
	{
		var resp = await inner.SearchAsync(new SearchRequest
		{
			Query = request.Query,
			PageNumber = request.PageNumber,
			PageSize = request.PageSize,
			TypeFilter = request.TypeFilter ?? [],
			SectionFilter = request.SectionFilter ?? [],
			ProductFilter = request.ProductFilter ?? [],
			DeploymentFilter = request.DeploymentFilter ?? [],
			VersionFilter = request.VersionFilter,
			SortBy = request.SortBy.ToLowerInvariant() switch
			{
				"recent" => SortMode.Recent,
				"alpha" => SortMode.Alpha,
				_ => SortMode.Relevance
			},
			IncludeHighlighting = request.IncludeHighlighting
		}, ctx);

		var response = new FullSearchResponse
		{
			TotalResults = (int)resp.TotalResults,
			PageNumber = resp.PageNumber,
			PageSize = resp.PageSize,
			IsSemanticQuery = resp.IsSemanticQuery,
			Aggregations = MapAggregations(resp.Aggregations),
			Results = resp.Results.Select(MapHit).ToList()
		};

		LogFullSearchResults(
			logger,
			response.PageSize,
			response.PageNumber,
			request.Query,
			response.IsSemanticQuery,
			response.Results.Select(i => i.Url).ToArray());

		return response;
	}

	[LoggerMessage(Level = LogLevel.Information, Message = "Full search completed with {PageSize} (page {PageNumber}) results for query '{SearchQuery}' (semantic: {IsSemantic}): {Urls}")]
	private static partial void LogFullSearchResults(
		ILogger logger, int pageSize, int pageNumber, string searchQuery, bool isSemantic, string[] urls);

	private FullSearchResultItem MapHit(SearchResultItem<DocumentationDocument> item) => new()
	{
		Type = item.Document.ContentType,
		Url = item.Document.Url,
		Title = item.Title,
		Description = item.Description,
		Parents = (item.Document.Parents ?? [])
			.Select(p => new FullSearchResultParent { Title = p.Title, Url = p.Url })
			.ToArray(),
		Score = item.Score,
		AiShortSummary = item.Document.AiShortSummary,
		AiRagOptimizedSummary = item.Document.AiRagOptimizedSummary,
		NavigationSection = item.Document.NavigationSection,
		LastUpdated = item.Document.LastUpdated,
		Product = MapProduct(item.Document.Product),
		RelatedProducts = item.Document.RelatedProducts?
			.Where(p => p.Id is not null)
			.Select(p => MapProduct(p)!)
			.ToArray()
	};

	private FullSearchProduct? MapProduct(IndexedProduct? p) =>
		p?.Id is { } id
			? new FullSearchProduct
			{
				Id = id,
				DisplayName = productNameLookup.TryGetProductName(id, out var name) ? name : id
			}
			: null;

	private FullSearchAggregations MapAggregations(SearchAggregations agg) => new()
	{
		Type = agg.Type,
		NavigationSection = agg.NavigationSection,
		DeploymentType = agg.DeploymentType,
		Product = agg.Product.ToDictionary(
			kvp => kvp.Key,
			kvp => new ProductAggregationBucket
			{
				Count = kvp.Value.Count,
				DisplayName = kvp.Value.DisplayName ?? kvp.Key
			})
	};

	public void Dispose()
	{
		GC.SuppressFinalize(this);
		clientAccessor.Dispose();
	}
}
