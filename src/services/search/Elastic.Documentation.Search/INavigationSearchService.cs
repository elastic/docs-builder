// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Search.Contract;

namespace Elastic.Documentation.Search;

public interface INavigationSearchService
{
	Task<NavigationSearchResponse> NavigationSearchAsync(NavigationSearchRequest request, Cancel ctx = default);
}

public record NavigationSearchRequest
{
	public required string Query { get; init; }
	public int PageNumber { get; init; } = 1;
	public int PageSize { get; init; } = 20;
	public string? TypeFilter { get; init; }

	/// <summary>The API page the reader is on; <c>null</c> lifts no API and shows every API's default rendering.</summary>
	public ApiPageContext? ApiPage { get; init; }
}

/// <summary>Pages and API operations whose title or navigation entry matches the query.</summary>
public record NavigationSearchResponse
{
	/// <summary>The results on this page.</summary>
	public required IEnumerable<NavigationSearchResultItem> Results { get; init; }
	/// <summary>Every result the query matches, across pages.</summary>
	/// <example>314</example>
	public required int TotalResults { get; init; }
	/// <summary>The page returned, from 1.</summary>
	/// <example>1</example>
	public required int PageNumber { get; init; }
	/// <summary>The number of results per page.</summary>
	/// <example>20</example>
	public required int PageSize { get; init; }
	/// <summary>Result counts by type, for building filters.</summary>
	public NavigationSearchAggregations Aggregations { get; init; } = new();
	/// <summary>The number of pages at this page size.</summary>
	/// <example>16</example>
	public int PageCount => TotalResults > 0 ? (int)Math.Ceiling((double)TotalResults / PageSize) : 0;
}

/// <summary>Result counts for building filters.</summary>
public record NavigationSearchAggregations
{
	/// <summary>Result counts by result type, such as <c>docs</c> or <c>api</c>.</summary>
	/// <example>{"docs": 306, "api": 8}</example>
	public IReadOnlyDictionary<string, long> Type { get; init; } = new Dictionary<string, long>();
}

public record NavigationSearchResult
{
	public required int TotalHits { get; init; }
	public required List<NavigationSearchResultItem> Results { get; init; }
	public IReadOnlyDictionary<string, long> Aggregations { get; init; } = new Dictionary<string, long>();
}

/// <summary>One step of a result's breadcrumb.</summary>
public record NavigationSearchResultItemParent
{
	/// <summary>The parent page's title.</summary>
	/// <example>"Docs"</example>
	public required string Title { get; init; }
	/// <summary>The parent page's path.</summary>
	/// <example>"/docs"</example>
	public required string Url { get; init; }
}

/// <summary>A page or an API operation that matches the query.</summary>
public record NavigationSearchResultItem
{
	/// <summary>The operation's request and response fields that match the query; empty for a docs page.</summary>
	public NavigationSearchProperty[] Properties { get; init; } = [];

	/// <summary>The operation's routes with their methods; empty for a docs page.</summary>
	public ApiRouteEntry[] Routes { get; init; } = [];

	/// <summary>True when the result is an API operation the spec marks deprecated; false for a docs page.</summary>
	/// <example>false</example>
	public bool Deprecated { get; init; }

	/// <summary>True when the result is an API's landing page, which has no operation.</summary>
	/// <example>false</example>
	public bool Landing { get; init; }

	/// <summary>Key of the API the result belongs to, e.g. <c>kibana</c>; null for a docs page.</summary>
	/// <example>"elasticsearch"</example>
	public string? ApiKey { get; init; }

	/// <summary>Product id the page was inferred to, e.g. <c>cloud-hosted</c>. Picks the landing row's icon, as it does on the catalog.</summary>
	/// <example>"elasticsearch"</example>
	public string? Product { get; init; }
	/// <summary><c>docs</c> for a documentation page, <c>api</c> for an API reference page.</summary>
	/// <example>"docs"</example>
	public required string Type { get; init; }
	/// <summary>The page's path on www.elastic.co.</summary>
	/// <example>"/docs/troubleshoot/elasticsearch/red-yellow-cluster-status"</example>
	public required string Url { get; init; }
	/// <summary>The page title, with matched words in <c>&lt;mark&gt;</c> tags.</summary>
	/// <example>"Red or yellow &lt;mark&gt;cluster&lt;/mark&gt; &lt;mark&gt;health&lt;/mark&gt; status"</example>
	public required string Title { get; init; }
	/// <summary>A passage of the page, with matched words in <c>&lt;mark&gt;</c> tags.</summary>
	/// <example>"and cleanup activities until the &lt;mark&gt;cluster&lt;/mark&gt; returns to green &lt;mark&gt;health&lt;/mark&gt; status. In many cases, your &lt;mark&gt;cluster&lt;/mark&gt; will recover to g…"</example>
	public required string Description { get; init; }
	/// <summary>The page's breadcrumb, from the docs root down.</summary>
	public required NavigationSearchResultItemParent[] Parents { get; init; }
	/// <summary>The relevance score. Compare it only within one response.</summary>
	/// <example>4.7976103</example>
	public float Score { get; init; }
}

/// <summary>A request or response field of an API operation that matches the query.</summary>
public record NavigationSearchProperty
{
	/// <summary>The field name.</summary>
	/// <example>"wait_for_status"</example>
	public required string Name { get; init; }
	/// <summary>The field's dotted path, such as <c>query.match</c>.</summary>
	/// <example>"wait_for_status"</example>
	public required string Path { get; init; }
	/// <summary>The field's type, such as <c>string</c> or <c>object</c>.</summary>
	/// <example>"enum"</example>
	public required string Type { get; init; }
	/// <summary>The field's description.</summary>
	/// <example>"Wait (until the timeout expires) for the cluster to reach a specific health status (or a better status)."</example>
	public required string Description { get; init; }
	/// <summary>The field's anchor on the operation page.</summary>
	/// <example>"query-wait_for_status"</example>
	public required string Anchor { get; init; }
	/// <summary>Where the field is: <c>path</c>, <c>query</c>, <c>header</c>, <c>request</c> or <c>response</c>.</summary>
	/// <example>"query"</example>
	public required string Location { get; init; }
	/// <summary>The operation page, with the field's anchor.</summary>
	/// <example>"/docs/api/elasticsearch/operation/operation-cluster-health#query-wait_for_status"</example>
	public required string Url { get; init; }
}
