// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using AwesomeAssertions;
using Elastic.Documentation.Mcp.Remote.Responses;
using Elastic.Documentation.Mcp.Remote.Tools;

namespace Mcp.Remote.IntegrationTests;

public class ApiToolsIntegrationTests : McpToolsIntegrationTestsBase
{
	private static CancellationToken Ct => TestContext.Current!.Execution.CancellationToken;

	private async Task<ApiTools> ConnectedTools()
	{
		var (tools, clientAccessor) = CreateApiTools();
		LogDiagnostics(clientAccessor);
		Skip.Unless(await clientAccessor.CanConnect(Ct), "Elasticsearch is not connected");
		return tools;
	}

	private static void SkipWhenNotIndexed(string json)
	{
		TestContext.Current?.Output.WriteLine($"Result: {json}");
		if (json.Contains("\"error\""))
			Skip.Test("API Explorer documents are not in the target index");
	}

	[Test]
	public async Task SearchApiOperations_TypedRoute_FindsTheClusterHealthOperation()
	{
		var tools = await ConnectedTools();

		var json = await tools.SearchApiOperations("GET /_cluster/health", api: "elasticsearch", cancellationToken: Ct);
		SkipWhenNotIndexed(json);

		var response = JsonSerializer.Deserialize(json, McpJsonContext.Default.ApiOperationSearchResponse);
		response.Should().NotBeNull();
		response.Results.Should().NotBeEmpty();
		response.Results[0].Operation.Path.Should().StartWith("/_cluster/health");
	}

	[Test]
	public async Task GetApiOperation_Endpoint_ReturnsLogstashNodeStatsWithFields()
	{
		var tools = await ConnectedTools();

		var json = await tools.GetApiOperation("logstash", endpoint: "GET /_node/stats", cancellationToken: Ct);
		SkipWhenNotIndexed(json);

		var response = JsonSerializer.Deserialize(json, McpJsonContext.Default.ApiOperationResponse);
		response.Should().NotBeNull();
		response.Operation.OperationId.Should().Be("nodestats");
		response.Fields.Should().Contain(f => f.Location == "response");
	}

	[Test]
	public async Task ListApiOperations_NoApi_ListsElasticsearch()
	{
		var tools = await ConnectedTools();

		var json = await tools.ListApiOperations(cancellationToken: Ct);
		SkipWhenNotIndexed(json);

		var response = JsonSerializer.Deserialize(json, McpJsonContext.Default.ApiListResponse);
		response.Should().NotBeNull();
		response.Apis.Should().Contain(a => a.Key == "elasticsearch");
	}

	[Test]
	public async Task ListApiOperations_Elasticsearch_ReturnsGroups()
	{
		var tools = await ConnectedTools();

		var json = await tools.ListApiOperations("elasticsearch", cancellationToken: Ct);
		SkipWhenNotIndexed(json);

		var response = JsonSerializer.Deserialize(json, McpJsonContext.Default.ApiOperationListResponse);
		response.Should().NotBeNull();
		response.TotalOperations.Should().BeGreaterThan(ApiTools.MaxListedOperations);
		response.Groups.Should().NotBeEmpty();
	}
}
