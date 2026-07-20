// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Search.Common;
using Elastic.Documentation.Search.Contract;
using Elastic.Transport;
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
	ILogger<FullSearchService> logger)
	: IFullSearchService, IDisposable
{
	public async Task<FullSearchResponse> SearchAsync(FullSearchRequest request, Cancel ctx = default)
	{
		SearchResponse<DocumentationDocument> resp;
		try
		{
			resp = await inner.SearchAsync(new SearchRequest
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
		}
		catch (TransportException ex) when (IsTransient(ex))
		{
			// Surface transient ES backend failures (timeout, overload, 429/503) as a typed exception
			// so callers (e.g. MCP tools) can signal "retry in a few seconds" to their clients.
			throw new SearchUnavailableException(
				$"Search backend is temporarily unavailable ({ex.FailureReason}). Transient — retry in a few seconds.",
				ex);
		}

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

	// True for transport failures that are inherently transient: request timeout, retry exhaustion
	// on a single-node pool, or server-side overload (HTTP 429 / 503).
	private static bool IsTransient(TransportException ex) =>
		ex.FailureReason is PipelineFailure.MaxTimeoutReached or PipelineFailure.MaxRetriesReached
		|| (ex.FailureReason is PipelineFailure.BadResponse
			&& ex.ApiCallDetails?.HttpStatusCode is 429 or 503);

	[LoggerMessage(Level = LogLevel.Information, Message = "Full search completed with {PageSize} (page {PageNumber}) results for query '{SearchQuery}' (semantic: {IsSemantic}): {Urls}")]
	private static partial void LogFullSearchResults(
		ILogger logger, int pageSize, int pageNumber, string searchQuery, bool isSemantic, string[] urls);

	private FullSearchResultItem MapHit(SearchResultItem<DocumentationDocument> item) => new()
	{
		Type = item.Document.ContentType,
		Url = item.Document.Path,
		Title = item.Title,
		Description = item.Description,
		Parents = (item.Document.Parents ?? [])
			.Select(p => new FullSearchResultParent { Title = p.Title, Url = p.Path })
			.ToArray(),
		Score = item.Score,
		AiShortSummary = item.Document.AiShortSummary,
		AiRagOptimizedSummary = item.Document.AiRagOptimizedSummary,
		NavigationSection = item.Document.Section,
		LastUpdated = item.Document.LastUpdated,
		Product = MapProduct(item.Document.Product),
		RelatedProducts = (item.Document.RelatedProducts?
			.Where(p => p.Id is not null)
			.Select(p => MapProduct(p.Id)!)
			.ToArray()) ?? []
	};

	private FullSearchProduct? MapProduct(string? id) =>
		id is not null
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

	public void Dispose() => GC.SuppressFinalize(this);
}
