// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using AwesomeAssertions;
using Elastic.Documentation.Mcp.Remote.Responses;
using Elastic.Documentation.Mcp.Remote.Tools;
using Elastic.Documentation.Search;
using Elastic.Documentation.Search.ApiReference;
using FakeItEasy;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol;
using Nullean.Snapshots;
using Nullean.Snapshots.Json;

namespace Mcp.Remote.Tests;

public class ApiToolsTests
{
	private static CancellationToken Ct => TestContext.Current!.Execution.CancellationToken;

	private static ApiOperationSummary Operation(string operationId, string? group, string path = "/_search") =>
		new()
		{
			Api = "elasticsearch",
			Title = operationId,
			Url = $"/docs/api/doc/elasticsearch/operation/operation-{operationId}",
			Method = "GET",
			Path = path,
			OperationId = operationId,
			Group = group,
			Endpoints = [$"GET {path}"]
		};

	private static string Error(string json) => JsonSerializer.Deserialize(json, McpJsonContext.Default.ErrorResponse)!.Error;

	[Test]
	public async Task SearchApiOperations_VersionWithoutApi_RejectsTheCall()
	{
		var gateway = A.Fake<IApiGateway>();
		var tools = new ApiTools(new ApiReferenceService(gateway), NullLogger<ApiTools>.Instance);

		var json = await tools.SearchApiOperations("health", version: "9", cancellationToken: Ct);

		Error(json).Should().MatchInlineSnapshot("""version needs api: pass the API key the version belongs to.""");
		A.CallTo(gateway).MustNotHaveHappened();
	}

	[Test]
	public async Task SearchApiOperations_PassesTheNormalizedScopeToTheGateway()
	{
		var gateway = A.Fake<IApiGateway>();
		_ = A.CallTo(() => gateway.SearchOperationsAsync(A<ApiOperationSearch>._, A<CancellationToken>._)).Returns(new ApiOperationPage(
			0,
			[]
		));
		var tools = new ApiTools(new ApiReferenceService(gateway), NullLogger<ApiTools>.Instance);

		_ = await tools.SearchApiOperations("health", api: " Elasticsearch ", version: "9", pageSize: 500, cancellationToken: Ct);

		_ = A.CallTo(
			() => gateway.SearchOperationsAsync(
				new ApiOperationSearch("health", new ApiScope("elasticsearch", "9"), IncludeDeprecated: false, PageNumber: 1, PageSize: 50),
				A<CancellationToken>._
			)
		).MustHaveHappenedOnceExactly();
	}

	[Test]
	[Arguments(null, null)]
	[Arguments("search", "GET /_search")]
	public async Task GetApiOperation_NeedsExactlyOneOfOperationIdAndEndpoint(string? operationId, string? endpoint)
	{
		var gateway = A.Fake<IApiGateway>();
		var tools = new ApiTools(new ApiReferenceService(gateway), NullLogger<ApiTools>.Instance);

		var json = await tools.GetApiOperation("elasticsearch", operationId, endpoint, cancellationToken: Ct);

		Error(json).Should().MatchInlineSnapshot("""Pass exactly one of operationId or endpoint.""");
		A.CallTo(gateway).MustNotHaveHappened();
	}

	[Test]
	public async Task GetApiOperation_NoMatch_ReportsWhatWasLookedUp()
	{
		var gateway = A.Fake<IApiGateway>();
		_ = A.CallTo(() => gateway.GetOperationAsync(A<ApiOperationLookup>._, A<CancellationToken>._)).Returns((ApiOperationDetail?)null);
		var tools = new ApiTools(new ApiReferenceService(gateway), NullLogger<ApiTools>.Instance);

		var json = await tools.GetApiOperation("elasticsearch", endpoint: "GET /_nope", cancellationToken: Ct);

		Error(json).Should().MatchInlineSnapshot("""No API operation matches GET /_nope in elasticsearch.""");
	}

	[Test]
	public async Task GetApiOperation_OmitsTheBodyByDefault()
	{
		var gateway = A.Fake<IApiGateway>();
		_ = A.CallTo(() => gateway.GetOperationAsync(A<ApiOperationLookup>._, A<CancellationToken>._)).Returns(new ApiOperationDetail
		{
			Operation = Operation("search", "Search"),
			Versions = ["main"],
			Fields = [],
			Body = "Get search hits that match the query."
		});
		var tools = new ApiTools(new ApiReferenceService(gateway), NullLogger<ApiTools>.Instance);

		var json = await tools.GetApiOperation("elasticsearch", operationId: "search", cancellationToken: Ct);

		var response = JsonSerializer.Deserialize(json, McpJsonContext.Default.ApiOperationResponse);
		response.Should().NotBeNull();
		response.Body.Should().BeNull();
		response.BodyLength.Should().Be(37);
		response.BodyNote.Should().NotBeNull();
	}

	[Test]
	public async Task ListApiOperations_SmallApi_ListsOperationsByGroup()
	{
		var gateway = A.Fake<IApiGateway>();
		_ = A.CallTo(() => gateway.ListOperationsAsync(A<ApiScope>._, A<CancellationToken>._)).Returns([
			Operation("search", "Search"),
			Operation("cluster-health", "Cluster", "/_cluster/health"),
			Operation("ping", null, "/")
		]);
		var tools = new ApiTools(new ApiReferenceService(gateway), NullLogger<ApiTools>.Instance);

		var json = await tools.ListApiOperations("elasticsearch", cancellationToken: Ct);

		json.Should().MatchInlineJsonSnapshot(
			"""
			{
			  "api": "elasticsearch",
			  "version": null,
			  "totalOperations": 3,
			  "groups": [
			    {
			      "name": "Cluster",
			      "count": 1,
			      "operations": [
			        {
			          "title": "cluster-health",
			          "url": "/docs/api/doc/elasticsearch/operation/operation-cluster-health",
			          "method": "GET",
			          "path": "/_cluster/health",
			          "operationId": "cluster-health",
			          "deprecated": false
			        }
			      ]
			    },
			    {
			      "name": "Other",
			      "count": 1,
			      "operations": [
			        {
			          "title": "ping",
			          "url": "/docs/api/doc/elasticsearch/operation/operation-ping",
			          "method": "GET",
			          "path": "/",
			          "operationId": "ping",
			          "deprecated": false
			        }
			      ]
			    },
			    {
			      "name": "Search",
			      "count": 1,
			      "operations": [
			        {
			          "title": "search",
			          "url": "/docs/api/doc/elasticsearch/operation/operation-search",
			          "method": "GET",
			          "path": "/_search",
			          "operationId": "search",
			          "deprecated": false
			        }
			      ]
			    }
			  ],
			  "note": null
			}
			"""
		);
	}

	[Test]
	public void GroupOperations_LargeApiWithoutGroup_ReturnsGroupNamesOnly()
	{
		var operations = Enumerable
			.Range(0, ApiReferenceService.MaxListedOperations + 1)
			.Select(i => Operation($"op-{i}", i % 2 == 0 ? "Search" : "Cluster"))
			.ToList();

		var reply = ApiReferenceService.GroupOperations(new ApiScope("elasticsearch", null), operations, group: null);

		var response = reply is ApiOperationListResponse list ? list : null;
		response.Should().NotBeNull();
		response.Groups.Select(g => (g.Name, g.Count)).Should().Equal(("Cluster", 75), ("Search", 76));
		response.Groups.Should().AllSatisfy(g => g.Operations.Should().BeNull());
		response.Note.Should().NotBeNull();
	}

	[Test]
	public void GroupOperations_UnknownGroup_RejectsTheCall()
	{
		var reply = ApiReferenceService.GroupOperations(
			new ApiScope("elasticsearch", null),
			[Operation("search", "Search")],
			group: "nope"
		);

		(reply is ApiReferenceRejection rejected ? rejected.ErrorType : null).Should().Be("group_not_found");
	}

	[Test]
	public async Task SearchApiOperations_BackendUnavailable_ThrowsMcpExceptionSoTheClientRetries()
	{
		var gateway = A.Fake<IApiGateway>();
		_ = A.CallTo(() => gateway.SearchOperationsAsync(A<ApiOperationSearch>._, A<CancellationToken>._)).Throws(
			SearchUnavailableException.Transient("MaxTimeoutReached")
		);
		var tools = new ApiTools(new ApiReferenceService(gateway), NullLogger<ApiTools>.Instance);

		var search = () => tools.SearchApiOperations("health", cancellationToken: Ct);

		_ = await search.Should().ThrowAsync<McpException>();
	}
}
