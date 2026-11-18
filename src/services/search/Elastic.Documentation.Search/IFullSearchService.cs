// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Search;

/// <summary>
/// Service interface for full-page search operations.
/// Supports hybrid RRF search with semantic query detection.
/// </summary>
public interface IFullSearchService
{
	Task<FullSearchResponse> SearchAsync(FullSearchRequest request, Cancel ctx = default);
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
	public string[]? SectionFilter { get; init; } // navigation_section
	public string[]? DeploymentFilter { get; init; } // applies_to.type
	public string[]? ProductFilter { get; init; } // product.id (AND behavior)
	public string? VersionFilter { get; init; } // "9.0+" | "8.19" | "7.17"
	public string SortBy { get; init; } = "relevance"; // relevance | recent | alpha
	public bool IncludeHighlighting { get; init; } = true;
}

/// <summary>
/// Response model for full-page search (unified API+service response).
/// </summary>
public record FullSearchResponse
{
	/// <summary>The results on this page.</summary>
	public required IReadOnlyList<FullSearchResultItem> Results { get; init; }
	/// <summary>Every result the query matches, across pages.</summary>
	/// <example>314</example>
	public required int TotalResults { get; init; }
	/// <summary>The page returned, from 1.</summary>
	/// <example>1</example>
	public required int PageNumber { get; init; }
	/// <summary>The number of results per page, from 1 to 50.</summary>
	/// <example>1</example>
	public required int PageSize { get; init; }
	/// <summary>Result counts by type, navigation section, deployment type and product, for building filters.</summary>
	public FullSearchAggregations Aggregations { get; init; } = new();
	/// <summary>True when the query also ran as a semantic search and both rankings were combined.</summary>
	/// <example>false</example>
	public bool IsSemanticQuery { get; init; }
	/// <summary>The number of pages at this page size.</summary>
	/// <example>314</example>
	public int PageCount => TotalResults > 0 ? (int)Math.Ceiling((double)TotalResults / PageSize) : 0;
}

/// <summary>
/// Result model for full-page search (used internally by the service implementation).
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
	/// <summary>Result counts by result type, such as <c>docs</c> or <c>api</c>.</summary>
	/// <example>{"docs": 306, "api": 8}</example>
	public IReadOnlyDictionary<string, long> Type { get; init; } = new Dictionary<string, long>();
	/// <summary>Result counts by top-level navigation section, such as <c>reference</c>.</summary>
	/// <example>{"reference": 150, "deploy and manage": 78, "troubleshoot": 33}</example>
	public IReadOnlyDictionary<string, long> NavigationSection { get; init; } = new Dictionary<string, long>();
	/// <summary>Result counts by the deployment types the pages apply to.</summary>
	/// <example>{}</example>
	public IReadOnlyDictionary<string, long> DeploymentType { get; init; } = new Dictionary<string, long>();
	/// <summary>Result counts by product id, with each product's display name.</summary>
	/// <example>{"elastic docs": {"count": 155, "displayName": "elastic docs"}, "elasticsearch": {"count": 99, "displayName": "Elasticsearch"}, "cloud kubernetes": {"count": 62, "displayName": "cloud kubernetes"}}</example>
	public IReadOnlyDictionary<string, ProductAggregationBucket> Product
	{
		get;
		init;
	} = new Dictionary<string, ProductAggregationBucket>();
}

/// <summary>
/// Product aggregation bucket with count and display name.
/// </summary>
public record ProductAggregationBucket
{
	/// <summary>The number of results for the product.</summary>
	/// <example>155</example>
	public required long Count { get; init; }
	/// <summary>The product's name for display.</summary>
	/// <example>"elastic docs"</example>
	public required string DisplayName { get; init; }
}

/// <summary>
/// Individual search result item for full-page search.
/// </summary>
public record FullSearchResultItem
{
	/// <summary><c>docs</c> for a documentation page, <c>api</c> for an API reference page.</summary>
	/// <example>"docs"</example>
	public required string Type { get; init; }
	/// <summary>The page's path on www.elastic.co, such as <c>/docs/deploy-manage</c>.</summary>
	/// <example>"/docs/troubleshoot/elasticsearch/red-yellow-cluster-status"</example>
	public required string Url { get; init; }
	/// <summary>The page title, with matched words in <c>&lt;mark&gt;</c> tags.</summary>
	/// <example>"Red or yellow &lt;mark&gt;cluster&lt;/mark&gt; &lt;mark&gt;health&lt;/mark&gt; status"</example>
	public required string Title { get; init; }
	/// <summary>A passage of the page, with matched words in <c>&lt;mark&gt;</c> tags.</summary>
	/// <example>"and cleanup activities until the &lt;mark&gt;cluster&lt;/mark&gt; returns to green &lt;mark&gt;health&lt;/mark&gt; status. In many cases, your &lt;mark&gt;cluster&lt;/mark&gt; will recover to g…"</example>
	public required string Description { get; init; }
	/// <summary>The page's breadcrumb, from the docs root down.</summary>
	public required FullSearchResultParent[] Parents { get; init; }
	/// <summary>The relevance score. Compare it only within one response.</summary>
	/// <example>4.7976103</example>
	public float Score { get; init; }
	/// <summary>A one-sentence summary of the page, generated by AI, when there is one.</summary>
	/// <example>"Fix red or yellow Elasticsearch cluster health status."</example>
	public string? AiShortSummary { get; init; }
	/// <summary>A longer AI-generated summary of the page, written for answer generation, when there is one.</summary>
	/// <example>"A red Elasticsearch cluster health status indicates unassigned primary shards causing potential search and indexing failures, while yellow status means unass…"</example>
	public string? AiRagOptimizedSummary { get; init; }
	/// <summary>The top-level navigation section the page is in.</summary>
	/// <example>"troubleshoot"</example>
	public string? NavigationSection { get; init; }
	/// <summary>When the page's content last changed.</summary>
	/// <example>"2026-11-17T19:20:53.9554741+00:00"</example>
	public DateTimeOffset? LastUpdated { get; init; }
	/// <summary>The product the page is about.</summary>
	public FullSearchProduct? Product { get; init; }
	/// <summary>Other products the page applies to.</summary>
	public FullSearchProduct[]? RelatedProducts { get; init; }
}

/// <summary>
/// Product reference in search results with id and display name.
/// </summary>
public record FullSearchProduct
{
	/// <summary>The product id, such as <c>elasticsearch</c>.</summary>
	/// <example>"elasticsearch"</example>
	public required string Id { get; init; }
	/// <summary>The product's name for display.</summary>
	/// <example>"Elasticsearch"</example>
	public required string DisplayName { get; init; }
}

/// <summary>
/// Parent document reference in breadcrumb trail.
/// </summary>
public record FullSearchResultParent
{
	/// <summary>The parent page's title.</summary>
	/// <example>"Docs"</example>
	public required string Title { get; init; }
	/// <summary>The parent page's path.</summary>
	/// <example>"/docs"</example>
	public required string Url { get; init; }
}
