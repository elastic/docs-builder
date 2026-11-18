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

public sealed record ApiOperationSummary
{
	public required string Api { get; init; }
	public required string Title { get; init; }
	public required string Url { get; init; }
	public string? Description { get; init; }
	public required string Method { get; init; }
	public required string Path { get; init; }
	public required string OperationId { get; init; }

	public string? Group { get; init; }
	public bool Deprecated { get; init; }

	/// <summary>Every route the page lists as <c>METHOD /spec/path</c>.</summary>
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
	public required string Name { get; init; }
	public required string Path { get; init; }
	public required string Type { get; init; }

	/// <summary>Where the field lives: <c>path</c>, <c>query</c>, <c>header</c>, <c>request</c> or <c>response</c>.</summary>
	public required string Location { get; init; }

	public required string Description { get; init; }
}

public sealed record ApiSummary
{
	public required string Key { get; init; }
	public required string Title { get; init; }
	public required string Url { get; init; }
	public string? Description { get; init; }
	public required string[] Versions { get; init; }
}
