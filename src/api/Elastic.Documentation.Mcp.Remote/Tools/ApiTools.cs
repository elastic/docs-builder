// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using Elastic.Documentation.Assembler.Mcp;
using Elastic.Documentation.Mcp.Remote.Gateways;
using Elastic.Documentation.Mcp.Remote.Responses;
using Elastic.Documentation.Mcp.Remote.Telemetry;
using Elastic.Documentation.Search;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace Elastic.Documentation.Mcp.Remote.Tools;

/// <summary>MCP tools for the REST API reference that API Explorer publishes.</summary>
[McpServerToolType]
public class ApiTools(IApiGateway apiGateway, ILogger<ApiTools> logger)
{
	// Above this many operations, listing an API without a group returns the group names only.
	internal const int MaxListedOperations = 150;
	private const string UngroupedName = "Other";

	private static readonly ToolRejected VersionNeedsApi = new(
		"invalid_arguments",
		"version needs api: pass the API key the version belongs to."
	);

	[McpServerTool(ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false), McpToolName("search_api_operations"), Description("Searches the Elastic REST API reference for operations. "
		+ "Use when the user asks which API call does something, names an endpoint such as 'GET /_cluster/health', "
		+ "or names a request or response field such as 'retriever' or 'track_total_hits'. "
		+ "End a field name with a dot, such as 'retriever.', to list its child fields. "
		+ "Returns operations with method, path, operation id and the fields the query matched.")]
	public Task<string> SearchApiOperations(
		[Description("Words, an endpoint such as 'POST /{index}/_search', or a field path")] string query,
		[Description("Limit to one API by its key, e.g. 'elasticsearch', 'kibana', 'logstash'. list_api_operations without arguments lists the keys.")] string? api =
			null,
		[Description("Version of the API: 'main' or a major such as '9'. Needs api. Default: the API's current version.")] string? version =
			null,
		[Description("Include deprecated operations (default: false)")] bool includeDeprecated = false,
		[Description("Page number (1-based, default: 1)")] int pageNumber = 1,
		[Description("Number of results per page (default: 10, max: 50)")] int pageSize = 10,
		CancellationToken cancellationToken = default
	) =>
		Run("search_api_operations", new Dictionary<string, object?>
		{
			["query"] = query,
			["api"] = api,
			["version"] = version,
			["includeDeprecated"] = includeDeprecated,
			["pageNumber"] = pageNumber,
			["pageSize"] = pageSize
		}, async () =>
		{
			if (VersionWithoutApi(api, version))
				return VersionNeedsApi;
			var scope = ToScope(api, version);

			var search = new ApiOperationSearch(query, scope, includeDeprecated, Math.Max(1, pageNumber), Math.Clamp(pageSize, 1, 50));
			var page = await apiGateway.SearchOperationsAsync(search, cancellationToken);
			var response = new ApiOperationSearchResponse { Query = query, TotalHits = page.Total, Results = page.Hits };
			return new ToolSucceeded(JsonSerializer.Serialize(response, McpJsonContext.Default.ApiOperationSearchResponse));
		});

	[McpServerTool(ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false), McpToolName("get_api_operation"), Description("Retrieves one operation from the Elastic REST API reference. "
		+ "Use when you know the endpoint or the operation id and need its parameters, request body and response fields. "
		+ "Pass either operationId or endpoint. "
		+ "Returns the operation's routes, versions, every documented field, and the page body only when includeBody is true.")]
	public Task<string> GetApiOperation(
		[Description("The API key, e.g. 'elasticsearch' or 'kibana'. Recommended: without it, the same operation id or route in another API may match.")] string? api =
			null,
		[Description("The OpenAPI operation id, e.g. 'cluster-health'")] string? operationId = null,
		[Description("A route as you would call it, e.g. 'GET /_cluster/health', 'GET /my-index/_search' or '/{index}/_doc/{id}'")] string? endpoint =
			null,
		[Description("Version of the API: 'main' or a major such as '9'. Needs api. Default: the API's current version.")] string? version =
			null,
		[Description("Include the page body (default: false). The fields list already covers parameters, request and response.")] bool includeBody =
			false,
		CancellationToken cancellationToken = default
	) =>
		Run("get_api_operation", new Dictionary<string, object?>
		{
			["api"] = api,
			["operationId"] = operationId,
			["endpoint"] = endpoint,
			["version"] = version,
			["includeBody"] = includeBody
		}, async () =>
		{
			if (VersionWithoutApi(api, version))
				return VersionNeedsApi;
			var scope = ToScope(api, version);
			if (ToOperationKey(operationId, endpoint) is not { } key)
				return new ToolRejected("invalid_arguments", "Pass exactly one of operationId or endpoint.");

			var detail = await apiGateway.GetOperationAsync(new ApiOperationLookup(scope, key), cancellationToken);
			return detail is null
				? new ToolRejected(
					"operation_not_found",
					$"No API operation matches {operationId ?? endpoint}{(api is null ? "" : $" in {api}")}."
				)
				: new ToolSucceeded(JsonSerializer.Serialize(ToResponse(detail, includeBody), McpJsonContext.Default.ApiOperationResponse));
		});

	[McpServerTool(ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false), McpToolName("list_api_operations"), Description("Lists the Elastic REST APIs, or the operations of one API. "
		+ "Use when the user wants to browse what an API offers, or you need an API key for the other API tools. "
		+ "Without api, returns every API with its key and versions. "
		+ "With api, returns its operations by group; a large API returns the group names only until you pass group.")]
	public Task<string> ListApiOperations(
		[Description("The API key, e.g. 'elasticsearch'. Omit to list every API.")] string? api = null,
		[Description("Version of the API: 'main' or a major such as '9'. Needs api. Default: the API's current version.")] string? version =
			null,
		[Description("Only the operations of this group, e.g. 'ES|QL' or 'cluster'")] string? group = null,
		CancellationToken cancellationToken = default
	) =>
		Run("list_api_operations", new Dictionary<string, object?> { ["api"] = api, ["version"] = version, ["group"] = group }, async () =>
		{
			if (VersionWithoutApi(api, version))
				return VersionNeedsApi;
			var scope = ToScope(api, version);
			if (scope is null)
			{
				var apis = await apiGateway.ListApisAsync(cancellationToken);
				return new ToolSucceeded(
					JsonSerializer.Serialize(new ApiListResponse { Apis = apis }, McpJsonContext.Default.ApiListResponse)
				);
			}

			var operations = await apiGateway.ListOperationsAsync(scope, cancellationToken);
			return operations.Count == 0
				? new ToolRejected(
					"api_not_found",
					$"No operations found for API '{api}'{(version is null ? "" : $" version '{version}'")}."
				)
				: GroupOperations(scope, operations, group);
		});

	internal static ToolReply GroupOperations(ApiScope scope, IReadOnlyList<ApiOperationSummary> operations, string? group)
	{
		var groups = operations
			.GroupBy(o => o.Group ?? UngroupedName, StringComparer.OrdinalIgnoreCase)
			.OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
			.ToList();
		if (group is not null)
		{
			groups = [.. groups.Where(g => string.Equals(g.Key, group.Trim(), StringComparison.OrdinalIgnoreCase))];
			if (groups.Count == 0)
				return new ToolRejected(
					"group_not_found",
					$"API '{scope.Key}' has no group '{group}'. Call list_api_operations without group to see its groups."
				);
		}

		var listOperations = group is not null || operations.Count <= MaxListedOperations;
		var response = new ApiOperationListResponse
		{
			Api = scope.Key,
			Version = scope.Version,
			TotalOperations = groups.Sum(g => g.Count()),
			Groups = [.. groups.Select(g => ToGroup(g, listOperations))],
			Note = listOperations ? null : $"This API has {operations.Count} operations. Pass group to list the operations of one group."
		};
		return new ToolSucceeded(JsonSerializer.Serialize(response, McpJsonContext.Default.ApiOperationListResponse));
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

	private static ApiOperationResponse ToResponse(ApiOperationDetail detail, bool includeBody) =>
		new()
		{
			Operation = detail.Operation,
			Versions = detail.Versions,
			Fields = detail.Fields,
			AiShortSummary = detail.AiShortSummary,
			Body = includeBody ? detail.Body : null,
			BodyLength = detail.Body?.Length ?? 0,
			BodyNote = !includeBody && detail.Body is { Length: > 0 }
				? "The body is omitted by default. Call this tool again with includeBody=true to read it."
				: null,
			OtherMatches = detail.OtherMatches
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

	private async Task<string> Run(string toolName, Dictionary<string, object?> arguments, Func<Task<ToolReply>> handle)
	{
		using var activity = McpToolTelemetry.StartActivity(toolName);
		McpToolTelemetry.LogStart(logger, toolName, McpToolTelemetry.SetPayloadMetadata(activity, arguments));
		var duration = Stopwatch.StartNew();
		var outcome = "failure";

		try
		{
			switch (await handle())
			{
				case ToolSucceeded succeeded:
					McpToolTelemetry.MarkSuccess(activity);
					outcome = "success";
					return succeeded.Json;
				case ToolRejected rejected:
					McpToolTelemetry.MarkFailure(activity, rejected.ErrorType, rejected.Message);
					return JsonSerializer.Serialize(new ErrorResponse(rejected.Message), McpJsonContext.Default.ErrorResponse);
			}
		}
		catch (OperationCanceledException)
		{
			McpToolTelemetry.MarkCancelled(activity);
			outcome = "cancelled";
			throw;
		}
		catch (SearchUnavailableException ex)
		{
			// Transient backend failure: McpException gives the client IsError=true so it knows to retry.
			McpToolTelemetry.MarkFailure(activity, ex);
			outcome = "unavailable";
			throw new McpException(ex.Message, ex);
		}
		catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
		{
			McpToolTelemetry.MarkFailure(activity, ex);
			logger.LogError(ex, "{ToolName} failed", toolName);
			return JsonSerializer.Serialize(new ErrorResponse(ex.Message), McpJsonContext.Default.ErrorResponse);
		}
		finally
		{
			duration.Stop();
			McpToolTelemetry.LogCompletion(logger, toolName, duration.ElapsedMilliseconds, outcome);
		}
		throw new UnreachableException();
	}
}

/// <summary>What a tool handler produced: a JSON reply, or a reason it declined the call.</summary>
public readonly union ToolReply(ToolSucceeded, ToolRejected);

public sealed record ToolSucceeded(string Json);

public sealed record ToolRejected(string ErrorType, string Message);
