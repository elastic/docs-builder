// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.ComponentModel;
using Elastic.Documentation.Assembler.Mcp;
using Elastic.Documentation.Mcp.Remote.Responses;
using Elastic.Documentation.Search.ApiReference;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace Elastic.Documentation.Mcp.Remote.Tools;

/// <summary>MCP tools for the REST API reference that API Explorer publishes.</summary>
[McpServerToolType]
public class ApiTools(ApiReferenceService apiReference, ILogger<ApiTools> logger)
{
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
		McpToolRunner.RunAsync(
			logger,
			"search_api_operations",
			new Dictionary<string, object?>
			{
				["query"] = query,
				["api"] = api,
				["version"] = version,
				["includeDeprecated"] = includeDeprecated,
				["pageNumber"] = pageNumber,
				["pageSize"] = pageSize
			},
			async () => ToToolReply(
				await apiReference.SearchAsync(query, api, version, includeDeprecated, pageNumber, pageSize, cancellationToken)
			)
		);

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
		McpToolRunner.RunAsync(
			logger,
			"get_api_operation",
			new Dictionary<string, object?>
			{
				["api"] = api,
				["operationId"] = operationId,
				["endpoint"] = endpoint,
				["version"] = version,
				["includeBody"] = includeBody
			},
			async () => ToToolReply(
				await apiReference.GetAsync(api, operationId, endpoint, version, includeBody, cancellationToken),
				includeBody
			)
		);

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
		McpToolRunner.RunAsync(
			logger,
			"list_api_operations",
			new Dictionary<string, object?> { ["api"] = api, ["version"] = version, ["group"] = group },
			async () => ToToolReply(await apiReference.ListAsync(api, version, group, cancellationToken))
		);

	internal static ToolReply ToToolReply(ApiReferenceReply reply, bool includeBody = false) => reply switch
	{
		ApiOperationSearchResponse r => ToolSucceeded.Serialize(r, McpJsonContext.Default.ApiOperationSearchResponse),
		ApiOperationResponse r =>
			ToolSucceeded.Serialize(
				r with { BodyNote = ResponseBody.Note(r.BodyLength, includeBody) },
				McpJsonContext.Default.ApiOperationResponse
			),
		ApiListResponse r => ToolSucceeded.Serialize(r, McpJsonContext.Default.ApiListResponse),
		ApiEndpointMatchResponse r => ToolSucceeded.Serialize(r, McpJsonContext.Default.ApiEndpointMatchResponse),
		ApiOperationListResponse r => ToolSucceeded.Serialize(r, McpJsonContext.Default.ApiOperationListResponse),
		ApiReferenceRejection r => new ToolRejected(r.ErrorType, r.Message)
	};
}
