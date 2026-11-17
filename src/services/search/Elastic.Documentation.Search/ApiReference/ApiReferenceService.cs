// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Search.ApiReference;

/// <summary>What a call to <see cref="ApiReferenceService"/> answers: one response, or why the call was rejected.</summary>
public readonly union ApiReferenceReply(
	ApiOperationSearchResponse,
	ApiOperationCollectionResponse,
	ApiOperationResponse,
	ApiSummary,
	ApiListResponse,
	ApiOperationListResponse,
	ApiReferenceRejection
);

/// <summary>A call the API reference cannot answer: invalid arguments, or nothing to answer with.</summary>
public sealed record ApiReferenceRejection(string ErrorType, string Message)
{
	/// <summary>Nothing matched, as opposed to arguments that can never match.</summary>
	public bool IsNotFound { get; private init; }

	/// <summary>Arguments that can never match, whatever the index holds.</summary>
	public static ApiReferenceRejection Invalid(string message) => new("invalid_arguments", message);

	/// <summary>Valid arguments that nothing in the index matches.</summary>
	public static ApiReferenceRejection NotFound(string errorType, string message) => new(errorType, message) { IsNotFound = true };
}

/// <summary>Searches, reads and lists the operations of the REST API reference. The MCP tools and the docs API share it.</summary>
public class ApiReferenceService(IApiGateway apiGateway)
{
	/// <summary>Above this many operations, listing an API without a group returns the group names only.</summary>
	public const int MaxListedOperations = 150;
	public const int MaxPageSize = 50;
	private const string UngroupedName = "Other";

	private static readonly ApiReferenceRejection VersionNeedsApi = ApiReferenceRejection.Invalid(
		"version needs api: pass the API key the version belongs to."
	);

	/// <summary>Finds operations by words, a typed route or a field path.</summary>
	public async Task<ApiReferenceReply> SearchAsync(
		string query,
		string? api,
		string? version,
		bool includeDeprecated,
		int pageNumber,
		int pageSize,
		CancellationToken ct
	)
	{
		if (VersionWithoutApi(api, version))
			return VersionNeedsApi;
		var search = new ApiOperationSearch(
			query,
			ToScope(api, version),
			includeDeprecated,
			Math.Max(1, pageNumber),
			Math.Clamp(pageSize, 1, MaxPageSize)
		);
		var page = await apiGateway.SearchOperationsAsync(search, ct);
		return new ApiOperationSearchResponse { Query = query, TotalHits = page.Total, Results = page.Hits };
	}

	/// <summary>Reads one operation by exactly one of <paramref name="operationId"/> or <paramref name="endpoint"/>.</summary>
	public async Task<ApiReferenceReply> GetAsync(
		string? api,
		string? operationId,
		string? endpoint,
		string? version,
		bool includeBody,
		CancellationToken ct
	)
	{
		if (VersionWithoutApi(api, version))
			return VersionNeedsApi;
		if (ToOperationKey(operationId, endpoint) is not { } key)
			return ApiReferenceRejection.Invalid("Pass exactly one of an operation id or an endpoint.");

		var detail = await apiGateway.GetOperationAsync(new ApiOperationLookup(ToScope(api, version), key), ct);
		if (detail is null)
			return ApiReferenceRejection.NotFound(
				"operation_not_found",
				$"No API operation matches {operationId ?? endpoint}{(api is null ? "" : $" in {api}")}."
			);
		return new ApiOperationResponse
		{
			Operation = detail.Operation,
			Versions = detail.Versions,
			Fields = detail.Fields,
			AiShortSummary = detail.AiShortSummary,
			Body = includeBody ? detail.Body : null,
			BodyLength = detail.Body?.Length ?? 0,
			OtherMatches = detail.OtherMatches
		};
	}

	/// <summary>
	/// Reads one API's operations: a search with <see cref="ApiOperationQuery.Q"/>, the operations that serve an
	/// <see cref="ApiOperationQuery.Endpoint"/> (best route first), or a listing by path when neither is set.
	/// </summary>
	public async Task<ApiReferenceReply> QueryAsync(ApiOperationQuery query, CancellationToken ct)
	{
		// Every lookup is inside one API, so an operation id or a route can never match in two of them.
		if (ToScope(query.Api, query.Version) is not { } scope)
			return ApiReferenceRejection.Invalid("Pass an API key, such as 'elasticsearch'.");
		var hasQuery = !string.IsNullOrWhiteSpace(query.Q);
		var hasEndpoint = !string.IsNullOrWhiteSpace(query.Endpoint);
		if (hasQuery && hasEndpoint)
			return ApiReferenceRejection.Invalid("Pass q or endpoint, not both.");
		if (query.View is not (null or "basic" or "full"))
			return ApiReferenceRejection.Invalid("view is 'basic' or 'full'.");
		if (query.Full && !hasEndpoint)
			return ApiReferenceRejection.Invalid("view=full needs endpoint.");
		if (query.IncludeBody && !query.Full)
			return ApiReferenceRejection.Invalid("include_body needs view=full.");
		if (!string.IsNullOrWhiteSpace(query.Group) && (hasQuery || hasEndpoint))
			return ApiReferenceRejection.Invalid("group filters a listing; pass it without q or endpoint.");

		var page = Math.Max(1, query.Page);
		var size = Math.Clamp(query.Size, 1, MaxPageSize);

		if (hasQuery)
		{
			var hits = await apiGateway.SearchOperationsAsync(
				new ApiOperationSearch(query.Q!, scope, query.IncludeDeprecated, page, size),
				ct
			);
			return new ApiOperationCollectionResponse
			{
				TotalHits = hits.Total,
				Results =
				[
					.. hits.Hits.Select(
						h => new ApiOperationItem { Operation = h.Operation, Score = h.Score, MatchedFields = h.MatchedFields }
					)
				]
			};
		}

		if (hasEndpoint)
		{
			// Within one API, a route made only of parameters, such as /{index}, can still serve several operations.
			var matches = await apiGateway.FindOperationsAsync(
				new ApiOperationLookup(scope, new ApiEndpointKey(query.Endpoint!.Trim())),
				ct
			);
			return new ApiOperationCollectionResponse
			{
				TotalHits = matches.Count,
				Results = [.. matches.Select(m => ToItem(m.Operation, query.Full ? m : null, query.IncludeBody))]
			};
		}

		var operations = await apiGateway.ListOperationsAsync(scope, ct);
		if (operations.Count == 0)
			return ApiNotFound(scope);
		var listed = operations.Where(o => query.IncludeDeprecated || !o.Deprecated);
		if (!string.IsNullOrWhiteSpace(query.Group))
		{
			var group = query.Group.Trim();
			listed = listed.Where(o => string.Equals(o.Group ?? UngroupedName, group, StringComparison.OrdinalIgnoreCase)).ToList();
			if (!listed.Any())
				return ApiReferenceRejection.NotFound("group_not_found", $"API '{scope.Key}' has no group '{group}'.");
		}
		var sorted = listed.OrderBy(o => o.Path, StringComparer.Ordinal).ThenBy(o => o.Method, StringComparer.Ordinal).ToList();
		return new ApiOperationCollectionResponse
		{
			TotalHits = sorted.Count,
			Results = [.. sorted.Skip((page - 1) * size).Take(size).Select(o => new ApiOperationItem { Operation = o })]
		};
	}

	/// <summary>Reads one operation of one API by its operation id.</summary>
	public Task<ApiReferenceReply> GetOperationAsync(
		string api,
		string operationId,
		string? version,
		bool includeBody,
		CancellationToken ct
	) =>
		string.IsNullOrWhiteSpace(api)
			? Task.FromResult<ApiReferenceReply>(ApiReferenceRejection.Invalid("Pass an API key, such as 'elasticsearch'."))
			: GetAsync(api, operationId, null, version, includeBody, ct);

	/// <summary>Reads one API by its key.</summary>
	public async Task<ApiReferenceReply> GetApiAsync(string api, CancellationToken ct)
	{
		var apis = await apiGateway.ListApisAsync(ct);
		var key = api.Trim().ToLowerInvariant();
		return apis.FirstOrDefault(a => a.Key == key) is { } found
			? found
			: ApiReferenceRejection.NotFound("api_not_found", $"No API '{api}'.");
	}

	/// <summary>
	/// Counts one API's operations by group, without listing them. Deprecated operations count only with
	/// <paramref name="includeDeprecated"/>, as in the listing, so a count matches the group's listing.
	/// </summary>
	public async Task<ApiReferenceReply> ListGroupsAsync(string api, string? version, bool includeDeprecated, CancellationToken ct)
	{
		if (ToScope(api, version) is not { } scope)
			return ApiReferenceRejection.Invalid("Pass an API key, such as 'elasticsearch'.");
		var operations = await apiGateway.ListOperationsAsync(scope, ct);
		return operations.Count == 0
			? ApiNotFound(scope)
			: GroupOperations(scope, [.. operations.Where(o => includeDeprecated || !o.Deprecated)], group: null, groupsOnly: true);
	}

	/// <summary>Lists every API without <paramref name="api"/>, or the operations of one API by group.</summary>
	public async Task<ApiReferenceReply> ListAsync(string? api, string? version, string? group, CancellationToken ct)
	{
		if (VersionWithoutApi(api, version))
			return VersionNeedsApi;
		if (ToScope(api, version) is not { } scope)
			return new ApiListResponse { Apis = await apiGateway.ListApisAsync(ct) };

		var operations = await apiGateway.ListOperationsAsync(scope, ct);
		return operations.Count == 0 ? ApiNotFound(scope) : GroupOperations(scope, operations, group);
	}

	private static ApiReferenceRejection ApiNotFound(ApiScope scope) =>
		ApiReferenceRejection.NotFound(
			"api_not_found",
			$"No operations found for API '{scope.Key}'{(scope.Version is null ? "" : $" version '{scope.Version}'")}."
		);

	private static ApiOperationItem ToItem(ApiOperationSummary operation, ApiOperationDetail? detail, bool includeBody) =>
		new() { Operation = operation, Versions = detail?.Versions, Fields = detail?.Fields, Body = includeBody ? detail?.Body : null };

	internal static ApiReferenceReply GroupOperations(
		ApiScope scope,
		IReadOnlyList<ApiOperationSummary> operations,
		string? group,
		bool groupsOnly = false
	)
	{
		var groups = operations
			.GroupBy(o => o.Group ?? UngroupedName, StringComparer.OrdinalIgnoreCase)
			.OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
			.ToList();
		if (group is not null)
		{
			groups = [.. groups.Where(g => string.Equals(g.Key, group.Trim(), StringComparison.OrdinalIgnoreCase))];
			if (groups.Count == 0)
				return ApiReferenceRejection.NotFound(
					"group_not_found",
					$"API '{scope.Key}' has no group '{group}'. List the API without group to see its groups."
				);
		}

		var listOperations = !groupsOnly && (group is not null || operations.Count <= MaxListedOperations);
		return new ApiOperationListResponse
		{
			Api = scope.Key,
			Version = scope.Version,
			TotalOperations = groups.Sum(g => g.Count()),
			Groups = [.. groups.Select(g => ToGroup(g, listOperations))],
			Note = listOperations || groupsOnly
				? null
				: $"This API has {operations.Count} operations. Pass group to list the operations of one group."
		};
	}

	private static ApiOperationGroupDto ToGroup(IGrouping<string, ApiOperationSummary> group, bool listOperations) =>
		new()
		{
			Name = group.Key,
			Count = group.Count(),
			Operations = listOperations
				? [.. group.OrderBy(o => o.Path, StringComparer.Ordinal).ThenBy(o => o.Method, StringComparer.Ordinal).Select(ToListItem)]
				: null
		};

	private static ApiOperationListItemDto ToListItem(ApiOperationSummary operation) =>
		new()
		{
			Title = operation.Title,
			Url = operation.Url,
			Method = operation.Method,
			Path = operation.Path,
			OperationId = operation.OperationId,
			Deprecated = operation.Deprecated
		};

	private static bool VersionWithoutApi(string? api, string? version) =>
		string.IsNullOrWhiteSpace(api) && !string.IsNullOrWhiteSpace(version);

	private static ApiScope? ToScope(string? api, string? version) =>
		string.IsNullOrWhiteSpace(api)
			? null
			: new ApiScope(api.Trim().ToLowerInvariant(), string.IsNullOrWhiteSpace(version) ? null : version.Trim().ToLowerInvariant());

	private static ApiOperationKey? ToOperationKey(string? operationId, string? endpoint)
	{
		var hasId = !string.IsNullOrWhiteSpace(operationId);
		if (hasId == !string.IsNullOrWhiteSpace(endpoint))
			return null;
		return hasId ? new ApiOperationIdKey(operationId!.Trim()) : new ApiEndpointKey(endpoint!.Trim());
	}
}

/// <summary>Operations that match a search.</summary>
public sealed record ApiOperationSearchResponse
{
	public required string Query { get; init; }
	public required long TotalHits { get; init; }
	public required IReadOnlyList<ApiOperationHit> Results { get; init; }
}

/// <summary>The filters of one API's operations. At most one of <see cref="Q"/> or <see cref="Endpoint"/>.</summary>
public sealed record ApiOperationQuery
{
	public string? Api { get; init; }
	public string? Version { get; init; }
	public string? Group { get; init; }
	public string? Q { get; init; }
	public string? Endpoint { get; init; }
	public bool IncludeDeprecated { get; init; }
	/// <summary><c>basic</c> (the default) or <c>full</c>; any other value is rejected.</summary>
	public string? View { get; init; }
	/// <summary>The <c>full</c> view adds each result's versions and fields; needs <see cref="Endpoint"/>.</summary>
	public bool Full => View == "full";
	/// <summary>Adds each result's body; needs <see cref="Full"/>.</summary>
	public bool IncludeBody { get; init; }
	public int Page { get; init; } = 1;
	public int Size { get; init; } = 10;
}

/// <summary>A page of one API's operations.</summary>
public sealed record ApiOperationCollectionResponse
{
	public required long TotalHits { get; init; }
	public required IReadOnlyList<ApiOperationItem> Results { get; init; }
}

/// <summary>One operation in a page. Search adds a score and the matched fields; the full view adds versions, fields and the body.</summary>
public sealed record ApiOperationItem
{
	public required ApiOperationSummary Operation { get; init; }
	public float? Score { get; init; }
	public IReadOnlyList<ApiField>? MatchedFields { get; init; }
	public string[]? Versions { get; init; }
	public IReadOnlyList<ApiField>? Fields { get; init; }
	public string? Body { get; init; }
}

/// <summary>One operation with its routes, versions and documented fields.</summary>
public sealed record ApiOperationResponse
{
	public required ApiOperationSummary Operation { get; init; }
	public required string[] Versions { get; init; }
	public required IReadOnlyList<ApiField> Fields { get; init; }
	public string? AiShortSummary { get; init; }
	public string? Body { get; init; }
	public int BodyLength { get; init; }
	/// <summary>Set by callers that can tell the reader how to ask for an omitted body.</summary>
	public string? BodyNote { get; init; }
	public required IReadOnlyList<ApiOperationSummary> OtherMatches { get; init; }
}

/// <summary>Every API with its key and versions.</summary>
public sealed record ApiListResponse
{
	public required IReadOnlyList<ApiSummary> Apis { get; init; }
}

/// <summary>The operations of one API, by group.</summary>
public sealed record ApiOperationListResponse
{
	public required string Api { get; init; }
	public string? Version { get; init; }
	public required int TotalOperations { get; init; }
	public required IReadOnlyList<ApiOperationGroupDto> Groups { get; init; }
	public string? Note { get; init; }
}

public sealed record ApiOperationGroupDto
{
	public required string Name { get; init; }
	public required int Count { get; init; }
	public IReadOnlyList<ApiOperationListItemDto>? Operations { get; init; }
}

public sealed record ApiOperationListItemDto
{
	public required string Title { get; init; }
	public required string Url { get; init; }
	public required string Method { get; init; }
	public required string Path { get; init; }
	public required string OperationId { get; init; }
	public bool Deprecated { get; init; }
}
