// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Api.Core.Search;

/// <summary>
/// Gateway interface for full-page search operations.
/// Supports hybrid RRF search with semantic query detection.
/// </summary>
public interface IFullSearchGateway
{
	Task<FullSearchResult> SearchAsync(FullSearchRequest request, Cancel ctx = default);
}

/// <summary>
/// Request model for full-page search.
/// </summary>
public record FullSearchRequest
{
	public required string Query { get; init; }
	public int PageNumber { get; init; } = 1;
	public int PageSize { get; init; } = 20;
	public string[]? TypeFilter { get; init; }
	public string[]? SectionFilter { get; init; }       // navigation_section
	public string[]? DeploymentFilter { get; init; }    // applies_to.type
	public string[]? ProductFilter { get; init; }       // product.id (AND behavior)
	public string? VersionFilter { get; init; }         // "9.0+" | "8.19" | "7.17"
	public string SortBy { get; init; } = "relevance";  // relevance | recent | alpha
	public bool IncludeHighlighting { get; init; } = true;
}

/// <summary>
/// Result model for full-page search.
/// </summary>
public record FullSearchResult
{
	public required IReadOnlyList<FullSearchResultItem> Results { get; init; }
	public required int TotalHits { get; init; }
	public required FullSearchAggregations Aggregations { get; init; }
	public bool IsSemanticQuery { get; init; }
}

/// <summary>
/// Aggregation buckets for full-page search facets.
/// </summary>
public record FullSearchAggregations
{
	public IReadOnlyDictionary<string, long> Type { get; init; } = new Dictionary<string, long>();
	public IReadOnlyDictionary<string, long> NavigationSection { get; init; } = new Dictionary<string, long>();
	public IReadOnlyDictionary<string, long> DeploymentType { get; init; } = new Dictionary<string, long>();
	public IReadOnlyDictionary<string, ProductAggregationBucket> Product { get; init; } = new Dictionary<string, ProductAggregationBucket>();
}

/// <summary>
/// Product aggregation bucket with count and display name.
/// </summary>
public record ProductAggregationBucket
{
	public required long Count { get; init; }
	public required string DisplayName { get; init; }
}

/// <summary>
/// Individual search result item for full-page search.
/// </summary>
public record FullSearchResultItem
{
	public required string Type { get; init; }
	public required string Url { get; init; }
	public required string Title { get; init; }
	public required string Description { get; init; }
	public required FullSearchResultParent[] Parents { get; init; }
	public float Score { get; init; }
	public string? AiShortSummary { get; init; }
	public string? AiRagOptimizedSummary { get; init; }
	public string? NavigationSection { get; init; }
	public DateTimeOffset? LastUpdated { get; init; }
	public FullSearchProduct? Product { get; init; }
	public FullSearchProduct[]? RelatedProducts { get; init; }
}

/// <summary>
/// Product reference in search results with id and display name.
/// </summary>
public record FullSearchProduct
{
	public required string Id { get; init; }
	public required string DisplayName { get; init; }
}

/// <summary>
/// Parent document reference in breadcrumb trail.
/// </summary>
public record FullSearchResultParent
{
	public required string Title { get; init; }
	public required string Url { get; init; }
}
