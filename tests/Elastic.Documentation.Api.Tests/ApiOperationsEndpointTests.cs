// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Elastic.Documentation.Api.Tests.Fixtures;
using Elastic.Documentation.Search.ApiReference;
using FakeItEasy;

namespace Elastic.Documentation.Api.Tests;

// The tests share one host and swap the gateway it resolves, so they run one after another.
[NotInParallel(nameof(ApiOperationsEndpointTests))]
public class ApiOperationsEndpointTests
{
	private static readonly SwappableService<IApiGateway> Gateway = new();

	private static readonly Lazy<ApiWebApplicationFactory> SharedFactory = new(
		() => ApiWebApplicationFactory.WithMockedServices(replacements => replacements.Replace<IApiGateway>(_ => Gateway.Current))
	);

	[After(Class)]
	public static void DisposeSharedFactory()
	{
		if (SharedFactory.IsValueCreated)
			SharedFactory.Value.Dispose();
	}

	private static readonly ApiOperationSummary Operation = new()
	{
		Api = "elasticsearch",
		Title = "Get cluster health",
		Url = "/docs/api/doc/elasticsearch/operation/operation-cluster-health",
		Method = "GET",
		Path = "/_cluster/health",
		OperationId = "cluster-health",
		Group = "cluster",
		Endpoints = ["GET /_cluster/health"]
	};

	private static async Task<(HttpStatusCode Status, JsonElement Body)> Get(IApiGateway gateway, string pathAndQuery)
	{
		Gateway.Current = gateway;
		using var client = SharedFactory.Value.CreateClient();
		using var response = await client.GetAsync(
			$"/docs/_api/v1/api-operations{pathAndQuery}",
			TestContext.Current!.Execution.CancellationToken
		);
		var json = await response.Content.ReadAsStringAsync(TestContext.Current!.Execution.CancellationToken);
		return (response.StatusCode, JsonElement.Parse(json));
	}

	private static ApiOperationDetail Detail(string? body = null, params ApiOperationSummary[] otherMatches) =>
		new() { Operation = Operation, Versions = ["9"], Fields = [], Body = body, OtherMatches = otherMatches };

	[Test]
	public async Task Search_PassesTheNormalizedScopeAndClampedPageSize()
	{
		ApiOperationSearch? received = null;
		var gateway = A.Fake<IApiGateway>();
		_ = A
			.CallTo(() => gateway.SearchOperationsAsync(A<ApiOperationSearch>._, A<CancellationToken>._))
			.Invokes((ApiOperationSearch search, CancellationToken _) => received = search)
			.Returns(new ApiOperationPage(1, [new ApiOperationHit(Operation, 1, [])]));

		var (status, body) = await Get(gateway, "?q=health&api=%20Elasticsearch%20&version=9&include_deprecated=true&size=500");

		status.Should().Be(HttpStatusCode.OK);
		received.Should().Be(
			new ApiOperationSearch("health", new ApiScope("elasticsearch", "9"), true, 1, ApiReferenceService.MaxPageSize)
		);
		body.GetProperty("totalHits").GetInt64().Should().Be(1);
		body.GetProperty("results")[0].GetProperty("operation").GetProperty("operationId").GetString().Should().Be("cluster-health");
	}

	[Test]
	public async Task Search_VersionWithoutApi_IsABadRequest()
	{
		var gateway = A.Fake<IApiGateway>();

		var (status, body) = await Get(gateway, "?q=health&version=9");

		status.Should().Be(HttpStatusCode.BadRequest);
		body.GetProperty("errorType").GetString().Should().Be("invalid_arguments");
		A.CallTo(gateway).MustNotHaveHappened();
	}

	[Test]
	[Arguments("")]
	[Arguments("?q=%20")]
	[Arguments("?q=health&endpoint=GET%20/_cluster/health")]
	public async Task Collection_WithoutExactlyOneOfQOrEndpoint_IsABadRequest(string query)
	{
		var gateway = A.Fake<IApiGateway>();

		var (status, body) = await Get(gateway, query);

		status.Should().Be(HttpStatusCode.BadRequest);
		body.GetProperty("message").GetString().Should().Be("Pass exactly one of q or endpoint.");
		A.CallTo(gateway).MustNotHaveHappened();
	}

	[Test]
	public async Task Endpoint_ListsTheBestMatchFirst()
	{
		List<ApiOperationLookup> received = [];
		var gateway = A.Fake<IApiGateway>();
		_ = A
			.CallTo(() => gateway.GetOperationAsync(A<ApiOperationLookup>._, A<CancellationToken>._))
			.Invokes((ApiOperationLookup lookup, CancellationToken _) => received.Add(lookup))
			.Returns(Detail(otherMatches: Operation with { Api = "kibana", OperationId = "kibana-health" }));

		var (status, body) = await Get(gateway, "?endpoint=GET%20/my-index/_search&api=elasticsearch");

		status.Should().Be(HttpStatusCode.OK);
		ApiOperationKey key = new ApiEndpointKey("GET /my-index/_search");
		received.Should().Equal(new ApiOperationLookup(new ApiScope("elasticsearch", null), key));
		body.GetProperty("endpoint").GetString().Should().Be("GET /my-index/_search");
		body
			.GetProperty("results")
			.EnumerateArray()
			.Select(r => r.GetProperty("operationId").GetString())
			.Should()
			.Equal("cluster-health", "kibana-health");
	}

	[Test]
	public async Task Endpoint_NoMatch_IsAnEmptyResult()
	{
		var gateway = A.Fake<IApiGateway>();
		_ = A.CallTo(() => gateway.GetOperationAsync(A<ApiOperationLookup>._, A<CancellationToken>._)).Returns((ApiOperationDetail?)null);

		var (status, body) = await Get(gateway, "?endpoint=GET%20/nope");

		status.Should().Be(HttpStatusCode.OK);
		body.GetProperty("results").GetArrayLength().Should().Be(0);
	}

	[Test]
	public async Task Operation_ById_ReturnsTheBodyOnlyWhenAsked()
	{
		List<ApiOperationLookup> received = [];
		var gateway = A.Fake<IApiGateway>();
		_ = A
			.CallTo(() => gateway.GetOperationAsync(A<ApiOperationLookup>._, A<CancellationToken>._))
			.Invokes((ApiOperationLookup lookup, CancellationToken _) => received.Add(lookup))
			.Returns(Detail("Full body"));

		var (withoutBody, withoutJson) = await Get(gateway, "/cluster-health");
		var (withBody, withJson) = await Get(gateway, "/cluster-health?api=elasticsearch&include_body=true");

		withoutBody.Should().Be(HttpStatusCode.OK);
		withoutJson.GetProperty("body").ValueKind.Should().Be(JsonValueKind.Null);
		withoutJson.GetProperty("bodyLength").GetInt32().Should().Be(9);
		withBody.Should().Be(HttpStatusCode.OK);
		withJson.GetProperty("body").GetString().Should().Be("Full body");
		ApiOperationKey key = new ApiOperationIdKey("cluster-health");
		received.Should().Equal(new ApiOperationLookup(null, key), new ApiOperationLookup(new ApiScope("elasticsearch", null), key));
	}

	[Test]
	public async Task Operation_UnknownId_IsNotFound()
	{
		var gateway = A.Fake<IApiGateway>();
		_ = A.CallTo(() => gateway.GetOperationAsync(A<ApiOperationLookup>._, A<CancellationToken>._)).Returns((ApiOperationDetail?)null);

		var (status, body) = await Get(gateway, "/nope?api=elasticsearch");

		status.Should().Be(HttpStatusCode.NotFound);
		body.GetProperty("errorType").GetString().Should().Be("operation_not_found");
		body.GetProperty("message").GetString().Should().Be("No API operation matches nope in elasticsearch.");
	}

	[Test]
	public async Task Apis_ListsEveryApi()
	{
		var gateway = A.Fake<IApiGateway>();
		_ = A.CallTo(() => gateway.ListApisAsync(A<CancellationToken>._)).Returns([
			new ApiSummary { Key = "elasticsearch", Title = "Elasticsearch API", Url = "/docs/api/doc/elasticsearch", Versions = ["9"] }
		]);

		var (status, body) = await Get(gateway, "/apis");

		status.Should().Be(HttpStatusCode.OK);
		body.GetProperty("apis")[0].GetProperty("key").GetString().Should().Be("elasticsearch");
		A.CallTo(() => gateway.GetOperationAsync(A<ApiOperationLookup>._, A<CancellationToken>._)).MustNotHaveHappened();
	}

	[Test]
	public async Task Groups_ListsTheOperationsOfOneGroup()
	{
		var gateway = A.Fake<IApiGateway>();
		_ = A.CallTo(() => gateway.ListOperationsAsync(new ApiScope("elasticsearch", null), A<CancellationToken>._)).Returns([Operation]);

		var (status, body) = await Get(gateway, "/groups?api=elasticsearch&group=Cluster");

		status.Should().Be(HttpStatusCode.OK);
		body.GetProperty("groups")[0].GetProperty("operations")[0].GetProperty("path").GetString().Should().Be("/_cluster/health");
	}

	[Test]
	public async Task Groups_WithoutApi_IsABadRequest()
	{
		var gateway = A.Fake<IApiGateway>();

		var (status, body) = await Get(gateway, "/groups");

		status.Should().Be(HttpStatusCode.BadRequest);
		body.GetProperty("errorType").GetString().Should().Be("invalid_arguments");
		A.CallTo(gateway).MustNotHaveHappened();
	}

	[Test]
	public async Task Groups_UnknownApi_IsNotFound()
	{
		var gateway = A.Fake<IApiGateway>();
		_ = A.CallTo(() => gateway.ListOperationsAsync(A<ApiScope>._, A<CancellationToken>._)).Returns([]);

		var (status, body) = await Get(gateway, "/groups?api=nope");

		status.Should().Be(HttpStatusCode.NotFound);
		body.GetProperty("errorType").GetString().Should().Be("api_not_found");
	}
}
