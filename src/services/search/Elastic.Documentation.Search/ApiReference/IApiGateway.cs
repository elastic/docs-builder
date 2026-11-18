// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Search.ApiReference;

/// <summary>Reads API Explorer operations and landing pages from the docs search index.</summary>
public interface IApiGateway
{
	Task<ApiOperationPage> SearchOperationsAsync(ApiOperationSearch search, CancellationToken ct = default);

	/// <summary>The best match for <paramref name="lookup"/>, or <c>null</c> when no operation matches.</summary>
	Task<ApiOperationDetail?> GetOperationAsync(ApiOperationLookup lookup, CancellationToken ct = default);

	/// <summary>Every match for <paramref name="lookup"/>, best first, each with its versions, fields and body, from one index query.</summary>
	Task<IReadOnlyList<ApiOperationDetail>> FindOperationsAsync(ApiOperationLookup lookup, CancellationToken ct = default);

	Task<IReadOnlyList<ApiSummary>> ListApisAsync(CancellationToken ct = default);

	Task<IReadOnlyList<ApiOperationSummary>> ListOperationsAsync(ApiScope scope, CancellationToken ct = default);
}

/// <summary>
/// One API, by the key API Explorer publishes it under, e.g. <c>elasticsearch</c>. A null <paramref name="Version"/> reads
/// the API's default version tree; otherwise <c>main</c> or a major such as <c>9</c>.
/// </summary>
public sealed record ApiScope(string Key, string? Version);

public sealed record ApiOperationSearch(string Query, ApiScope? Scope, bool IncludeDeprecated, int PageNumber, int PageSize);

/// <summary>Finds one operation by <paramref name="Key"/>, within one API when <paramref name="Scope"/> is set.</summary>
public sealed record ApiOperationLookup(ApiScope? Scope, ApiOperationKey Key);

public readonly union ApiOperationKey(ApiOperationIdKey, ApiEndpointKey);

/// <summary>The OpenAPI operation id, e.g. <c>cluster-health</c>.</summary>
public sealed record ApiOperationIdKey(string OperationId);

/// <summary>A route as a caller types it: <c>GET /_cluster/health</c>, <c>/{index}/_search</c> or <c>/my-index/_search</c>.</summary>
public sealed record ApiEndpointKey(string Endpoint);

public sealed record ApiOperationPage(long Total, IReadOnlyList<ApiOperationHit> Hits);

/// <summary>A search hit, with the request and response fields the query named.</summary>
public sealed record ApiOperationHit(ApiOperationSummary Operation, float Score, IReadOnlyList<ApiField> MatchedFields);

/// <summary>An operation of an API, as API Explorer documents it.</summary>
public sealed record ApiOperationSummary
{
	/// <summary>The key of the API the operation belongs to, such as <c>elasticsearch</c>.</summary>
	/// <example>"elasticsearch"</example>
	public required string Api { get; init; }
	/// <summary>The operation's title.</summary>
	/// <example>"Get the cluster health status"</example>
	public required string Title { get; init; }
	/// <summary>The operation's page on www.elastic.co.</summary>
	/// <example>"/docs/api/doc/elasticsearch/operation/operation-cluster-health"</example>
	public required string Url { get; init; }
	/// <summary>What the operation does.</summary>
	/// <example>"You can also use the API to get the health status of only specified data streams and indices.\nFor data streams, the API retrieves the health status of the st…"</example>
	public string? Description { get; init; }
	/// <summary>The HTTP method of the operation's main route.</summary>
	/// <example>"GET"</example>
	public required string Method { get; init; }
	/// <summary>The path of the operation's main route, such as <c>/_cluster/health/{index}</c>.</summary>
	/// <example>"/_cluster/health/{index}"</example>
	public required string Path { get; init; }
	/// <summary>The operation id from the API's spec, unique within the API.</summary>
	/// <example>"cluster-health"</example>
	public required string OperationId { get; init; }

	/// <summary>The group the operation is listed under, such as <c>Cluster</c>.</summary>
	/// <example>"Cluster"</example>
	public string? Group { get; init; }
	/// <summary>True when the spec marks the operation deprecated.</summary>
	/// <example>false</example>
	public bool Deprecated { get; init; }

	/// <summary>Every route the page lists as <c>METHOD /spec/path</c>.</summary>
	/// <example>["GET /_cluster/health/{index}", "GET /_cluster/health"]</example>
	public required string[] Endpoints { get; init; }
}

public sealed record ApiOperationDetail
{
	public required ApiOperationSummary Operation { get; init; }

	/// <summary>The version trees that show this operation, e.g. <c>main</c> and <c>9</c>.</summary>
	public required string[] Versions { get; init; }

	public required IReadOnlyList<ApiField> Fields { get; init; }
	public string? AiShortSummary { get; init; }
	public string? Body { get; init; }

	/// <summary>Other operations that match the lookup as well, e.g. the same operation id or route in another API.</summary>
	public IReadOnlyList<ApiOperationSummary> OtherMatches { get; init; } = [];
}

/// <summary>A parameter, request body field or response field of an operation.</summary>
public sealed record ApiField
{
	/// <summary>The field's name.</summary>
	/// <example>"index"</example>
	public required string Name { get; init; }
	/// <summary>The field's dotted path, such as <c>query.match</c>.</summary>
	/// <example>"index"</example>
	public required string Path { get; init; }
	/// <summary>The field's type, such as <c>string</c> or <c>object</c>.</summary>
	/// <example>"union string | string[]"</example>
	public required string Type { get; init; }

	/// <summary>Where the field lives: <c>path</c>, <c>query</c>, <c>header</c>, <c>request</c> or <c>response</c>.</summary>
	/// <example>"path"</example>
	public required string Location { get; init; }

	/// <summary>The field's description.</summary>
	/// <example>"A comma-separated list of data streams, indices, and index aliases that limit the request. Wildcard expressions () are supported. To target all data streams…"</example>
	public required string Description { get; init; }
}

/// <summary>An API that API Explorer publishes.</summary>
public sealed record ApiSummary
{
	/// <summary>The API key that the other requests take, such as <c>elasticsearch</c>.</summary>
	/// <example>"cloud"</example>
	public required string Key { get; init; }
	/// <summary>The API's title.</summary>
	/// <example>"Elastic Cloud API"</example>
	public required string Title { get; init; }
	/// <summary>The API's landing page on www.elastic.co.</summary>
	/// <example>"/docs/api/doc/cloud"</example>
	public required string Url { get; init; }
	/// <summary>What the API is for.</summary>
	/// <example>"This RESTful API enables you to perform most of the operations available in the UI console through API calls."</example>
	public string? Description { get; init; }
	/// <summary>The versions you can pass as <c>version</c>, such as <c>main</c> or <c>9</c>.</summary>
	/// <example>["main"]</example>
	public required string[] Versions { get; init; }
}
