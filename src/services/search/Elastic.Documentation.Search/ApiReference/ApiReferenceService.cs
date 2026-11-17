// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Search.ApiReference;

/// <summary>What a call to <see cref="ApiReferenceService"/> answers: one response, or why the call was rejected.</summary>
public readonly union ApiReferenceReply(
	ApiOperationSearchResponse,
	ApiEndpointMatchResponse,
	ApiOperationResponse,
	ApiListResponse,
	ApiOperationListResponse,
	ApiReferenceRejection
);

/// <summary>A call the API reference cannot answer. <see cref="ErrorType"/> ends in <c>_not_found</c> when nothing matched.</summary>
public sealed record ApiReferenceRejection(string ErrorType, string Message);

/// <summary>Searches, reads and lists the operations of the REST API reference. The MCP tools and the docs API share it.</summary>
public class ApiReferenceService(IApiGateway apiGateway)
{
	/// <summary>Above this many operations, listing an API without a group returns the group names only.</summary>
	public const int MaxListedOperations = 150;
	public const int MaxPageSize = 50;
	private const string UngroupedName = "Other";

	private static readonly ApiReferenceRejection VersionNeedsApi = new(
		"invalid_arguments",
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
			return new ApiReferenceRejection("invalid_arguments", "Pass exactly one of an operation id or an endpoint.");

		var detail = await apiGateway.GetOperationAsync(new ApiOperationLookup(ToScope(api, version), key), ct);
		if (detail is null)
			return new ApiReferenceRejection(
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
	/// Finds the operations that serve a request such as <c>GET /my-index/_search</c>, best match first.
	/// No match is an empty result, not a rejection: this filters the operations rather than reading one.
	/// </summary>
	public async Task<ApiReferenceReply> ResolveAsync(string endpoint, string? api, string? version, CancellationToken ct)
	{
		if (VersionWithoutApi(api, version))
			return VersionNeedsApi;
		if (string.IsNullOrWhiteSpace(endpoint))
			return new ApiReferenceRejection("invalid_arguments", "Pass an endpoint, such as 'GET /_cluster/health'.");

		var lookup = new ApiOperationLookup(ToScope(api, version), new ApiEndpointKey(endpoint.Trim()));
		var detail = await apiGateway.GetOperationAsync(lookup, ct);
		return new ApiEndpointMatchResponse
		{
			Endpoint = endpoint.Trim(),
			Results = detail is null ? [] : [detail.Operation, .. detail.OtherMatches]
		};
	}

	/// <summary>Lists the operations of one API by group; <paramref name="api"/> is required.</summary>
	public Task<ApiReferenceReply> ListGroupsAsync(string? api, string? version, string? group, CancellationToken ct) =>
		string.IsNullOrWhiteSpace(api)
			? Task.FromResult<ApiReferenceReply>(new ApiReferenceRejection("invalid_arguments", "Pass api, such as 'elasticsearch'."))
			: ListAsync(api, version, group, ct);

	/// <summary>Lists every API without <paramref name="api"/>, or the operations of one API by group.</summary>
	public async Task<ApiReferenceReply> ListAsync(string? api, string? version, string? group, CancellationToken ct)
	{
		if (VersionWithoutApi(api, version))
			return VersionNeedsApi;
		if (ToScope(api, version) is not { } scope)
			return new ApiListResponse { Apis = await apiGateway.ListApisAsync(ct) };

		var operations = await apiGateway.ListOperationsAsync(scope, ct);
		return operations.Count == 0
			? new ApiReferenceRejection(
				"api_not_found",
				$"No operations found for API '{api}'{(version is null ? "" : $" version '{version}'")}."
			)
			: GroupOperations(scope, operations, group);
	}

	internal static ApiReferenceReply GroupOperations(ApiScope scope, IReadOnlyList<ApiOperationSummary> operations, string? group)
	{
		var groups = operations
			.GroupBy(o => o.Group ?? UngroupedName, StringComparer.OrdinalIgnoreCase)
			.OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
			.ToList();
		if (group is not null)
		{
			groups = [.. groups.Where(g => string.Equals(g.Key, group.Trim(), StringComparison.OrdinalIgnoreCase))];
			if (groups.Count == 0)
				return new ApiReferenceRejection(
					"group_not_found",
					$"API '{scope.Key}' has no group '{group}'. List the API without group to see its groups."
				);
		}

		var listOperations = group is not null || operations.Count <= MaxListedOperations;
		return new ApiOperationListResponse
		{
			Api = scope.Key,
			Version = scope.Version,
			TotalOperations = groups.Sum(g => g.Count()),
			Groups = [.. groups.Select(g => ToGroup(g, listOperations))],
			Note = listOperations ? null : $"This API has {operations.Count} operations. Pass group to list the operations of one group."
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

/// <summary>The operations that serve one request, best match first.</summary>
public sealed record ApiEndpointMatchResponse
{
	public required string Endpoint { get; init; }
	public required IReadOnlyList<ApiOperationSummary> Results { get; init; }
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
