// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using AwesomeAssertions;
using Elastic.Documentation.Mcp.Remote.Responses;
using Elastic.Documentation.Mcp.Remote.Tools;
using Elastic.Documentation.Search.ApiReference;

namespace Mcp.Remote.IntegrationTests;

public class ApiToolsIntegrationTests : McpToolsIntegrationTestsBase
{
	private static CancellationToken Ct => TestContext.Current!.Execution.CancellationToken;

	/// <summary>Tools on a reachable index that holds API Explorer documents; skips the test otherwise.</summary>
	private async Task<ApiTools> ConnectedTools()
	{
		var (tools, clientAccessor) = CreateApiTools();
		LogDiagnostics(clientAccessor);
		Skip.Unless(await clientAccessor.CanConnect(Ct), "Elasticsearch is not connected");

		var apis = Reply(await tools.ListApiOperations(cancellationToken: Ct), McpJsonContext.Default.ApiListResponse);
		Skip.When(apis.Apis.Count == 0, "API Explorer documents are not in the target index");
		return tools;
	}

	/// <summary>Fails on an error reply instead of skipping, so a broken query or ranking shows up as a failure.</summary>
	private static T Reply<T>(string json, JsonTypeInfo<T> typeInfo)
	{
		TestContext.Current?.Output.WriteLine($"Result: {json}");
		using (var document = JsonDocument.Parse(json))
			document.RootElement.TryGetProperty("error", out _).Should().BeFalse(json);
		return JsonSerializer.Deserialize(json, typeInfo)!;
	}

	[Test]
	public async Task SearchApiOperations_TypedRoute_FindsTheClusterHealthOperation()
	{
		var tools = await ConnectedTools();

		var json = await tools.SearchApiOperations("GET /_cluster/health", api: "elasticsearch", cancellationToken: Ct);
		var response = Reply(json, McpJsonContext.Default.ApiOperationSearchResponse);
		response.Results.Should().NotBeEmpty();
		response.Results[0].Operation.Path.Should().StartWith("/_cluster/health");
	}

	[Test]
	public async Task GetApiOperation_Endpoint_ReturnsLogstashNodeStatsWithFields()
	{
		var tools = await ConnectedTools();

		var json = await tools.GetApiOperation("logstash", endpoint: "GET /_node/stats", cancellationToken: Ct);
		var response = Reply(json, McpJsonContext.Default.ApiOperationResponse);
		response.Operation.OperationId.Should().Be("nodestats");
		response.Fields.Should().Contain(f => f.Location == "response");
	}

	[Test]
	public async Task ListApiOperations_NoApi_ListsElasticsearch()
	{
		var tools = await ConnectedTools();

		var json = await tools.ListApiOperations(cancellationToken: Ct);
		var response = Reply(json, McpJsonContext.Default.ApiListResponse);
		response.Apis.Should().Contain(a => a.Key == "elasticsearch");
	}

	[Test]
	public async Task ListApiOperations_Elasticsearch_ReturnsGroups()
	{
		var tools = await ConnectedTools();

		var json = await tools.ListApiOperations("elasticsearch", cancellationToken: Ct);
		var response = Reply(json, McpJsonContext.Default.ApiOperationListResponse);
		response.TotalOperations.Should().BeGreaterThan(ApiReferenceService.MaxListedOperations);
		response.Groups.Should().NotBeEmpty();
	}
}
