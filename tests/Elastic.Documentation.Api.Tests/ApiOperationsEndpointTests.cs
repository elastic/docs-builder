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
		using var response = await client.GetAsync($"/docs/_api/v1{pathAndQuery}", TestContext.Current!.Execution.CancellationToken);
		var json = await response.Content.ReadAsStringAsync(TestContext.Current!.Execution.CancellationToken);
		return (response.StatusCode, JsonElement.Parse(json));
	}

	private static ApiOperationDetail Detail(string? body = null, params ApiOperationSummary[] otherMatches) =>
		new() { Operation = Operation, Versions = ["9"], Fields = [Field], Body = body, OtherMatches = otherMatches };

	private static readonly ApiField Field = new()
	{
		Name = "level",
		Path = "level",
		Type = "string",
		Location = "query",
		Description = "Detail level"
	};

	private static readonly ApiOperationSummary KibanaOperation = Operation with { Api = "kibana", OperationId = "kibana-health" };

	private static IEnumerable<string?> Ids(JsonElement body) =>
		body.GetProperty("results").EnumerateArray().Select(r => r.GetProperty("operation").GetProperty("id").GetString());

	[Test]
	public async Task Search_PassesTheNormalizedScopeAndClampedPageSize()
	{
		ApiOperationSearch? received = null;
		var gateway = A.Fake<IApiGateway>();
		_ = A
			.CallTo(() => gateway.SearchOperationsAsync(A<ApiOperationSearch>._, A<CancellationToken>._))
			.Invokes((ApiOperationSearch search, CancellationToken _) => received = search)
			.Returns(new ApiOperationPage(1, [new ApiOperationHit(Operation, 2, [Field])]));

		var (status, body) = await Get(
			gateway,
			"/api-operations?q=health&api=%20Elasticsearch%20&version=9&include_deprecated=true&size=500"
		);

		status.Should().Be(HttpStatusCode.OK);
		received.Should().Be(
			new ApiOperationSearch("health", new ApiScope("elasticsearch", "9"), true, 1, ApiReferenceService.MaxPageSize)
		);
		body.GetProperty("totalHits").GetInt64().Should().Be(1);
		var hit = body.GetProperty("results")[0];
		hit.GetProperty("operation").GetProperty("id").GetString().Should().Be("elasticsearch:cluster-health");
		hit.GetProperty("score").GetSingle().Should().Be(2);
		hit.GetProperty("matchedFields")[0].GetProperty("path").GetString().Should().Be("level");
	}

	[Test]
	[Arguments("/api-operations?q=health&version=9", "version needs api: pass the API key the version belongs to.")]
	[Arguments("/api-operations?q=health&endpoint=GET%20/x", "Pass at most one of q, endpoint or operation_id.")]
	[Arguments("/api-operations?q=health&view=full", "view=full needs endpoint or operation_id.")]
	[Arguments("/api-operations?operation_id=x&include_body=true", "include_body needs view=full.")]
	[Arguments("/api-operations?api=elasticsearch&group=cluster&q=health", "group filters a listing; pass it without q, endpoint or operation_id.")]
	[Arguments("/api-operations", "Pass api to list operations, or one of q, endpoint or operation_id.")]
	[Arguments("/api-operations?operation_id=x&view=everything", "view is 'basic' or 'full'.")]
	[Arguments("/api-operations/cluster-health", "An operation id looks like 'elasticsearch:cluster-health'.")]
	[Arguments("/api-operations/elasticsearch:", "An operation id looks like 'elasticsearch:cluster-health'.")]
	public async Task InvalidArguments_AreABadRequestWithoutQueryingTheIndex(string pathAndQuery, string message)
	{
		var gateway = A.Fake<IApiGateway>();

		var (status, body) = await Get(gateway, pathAndQuery);

		status.Should().Be(HttpStatusCode.BadRequest);
		body.GetProperty("errorType").GetString().Should().Be("invalid_arguments");
		body.GetProperty("message").GetString().Should().Be(message);
		A.CallTo(gateway).MustNotHaveHappened();
	}

	[Test]
	public async Task Endpoint_FullView_ReturnsEveryMatchWithItsFieldsInOneRequest()
	{
		List<ApiOperationLookup> received = [];
		var gateway = A.Fake<IApiGateway>();
		A
			.CallTo(() => gateway.GetOperationAsync(A<ApiOperationLookup>._, A<CancellationToken>._))
			.Invokes((ApiOperationLookup lookup, CancellationToken _) => received.Add(lookup))
			.ReturnsNextFromSequence(Detail("Body", KibanaOperation), Detail() with { Operation = KibanaOperation });

		var (status, body) = await Get(gateway, "/api-operations?endpoint=GET%20/my-index/_search&view=full&include_body=true");

		status.Should().Be(HttpStatusCode.OK);
		Ids(body).Should().Equal("elasticsearch:cluster-health", "kibana:kibana-health");
		var best = body.GetProperty("results")[0];
		best.GetProperty("fields")[0].GetProperty("path").GetString().Should().Be("level");
		best.GetProperty("body").GetString().Should().Be("Body");
		body.GetProperty("results")[1].GetProperty("fields").GetArrayLength().Should().Be(1);
		ApiOperationKey endpointKey = new ApiEndpointKey("GET /my-index/_search");
		ApiOperationKey kibanaKey = new ApiOperationIdKey("kibana-health");
		received.Should().Equal(new ApiOperationLookup(null, endpointKey), new ApiOperationLookup(new ApiScope("kibana", null), kibanaKey));
	}

	[Test]
	public async Task OperationId_BasicView_ListsTheSameIdInEveryApiWithoutFields()
	{
		var gateway = A.Fake<IApiGateway>();
		_ = A.CallTo(() => gateway.GetOperationAsync(A<ApiOperationLookup>._, A<CancellationToken>._)).Returns(
			Detail(otherMatches: KibanaOperation)
		);

		var (status, body) = await Get(gateway, "/api-operations?operation_id=cluster-health");

		status.Should().Be(HttpStatusCode.OK);
		Ids(body).Should().Equal("elasticsearch:cluster-health", "kibana:kibana-health");
		body.GetProperty("results")[0].GetProperty("fields").ValueKind.Should().Be(JsonValueKind.Null);
		A.CallTo(() => gateway.GetOperationAsync(A<ApiOperationLookup>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
	}

	[Test]
	public async Task Endpoint_NoMatch_IsAnEmptyPage()
	{
		var gateway = A.Fake<IApiGateway>();
		_ = A.CallTo(() => gateway.GetOperationAsync(A<ApiOperationLookup>._, A<CancellationToken>._)).Returns((ApiOperationDetail?)null);

		var (status, body) = await Get(gateway, "/api-operations?endpoint=GET%20/nope");

		status.Should().Be(HttpStatusCode.OK);
		body.GetProperty("totalHits").GetInt64().Should().Be(0);
		body.GetProperty("results").GetArrayLength().Should().Be(0);
	}

	[Test]
	public async Task Listing_FiltersByGroupHidesDeprecatedSortsByPathAndPages()
	{
		var gateway = A.Fake<IApiGateway>();
		_ = A.CallTo(() => gateway.ListOperationsAsync(new ApiScope("elasticsearch", null), A<CancellationToken>._)).Returns([
			Operation with { OperationId = "c", Path = "/_cluster/state" },
			Operation with { OperationId = "a", Path = "/_cluster/allocation" },
			Operation with { OperationId = "b", Path = "/_cluster/health", Method = "POST" },
			Operation with { OperationId = "old", Path = "/_cluster/old", Deprecated = true },
			Operation with { OperationId = "s", Path = "/_search", Group = "search" }
		]);

		var (status, body) = await Get(gateway, "/api-operations?api=elasticsearch&group=CLUSTER&size=2&page=2");

		status.Should().Be(HttpStatusCode.OK);
		body.GetProperty("totalHits").GetInt64().Should().Be(3);
		Ids(body).Should().Equal("elasticsearch:c");
	}

	[Test]
	[Arguments("/api-operations?api=nope", "api_not_found")]
	[Arguments("/api-operations?api=elasticsearch&group=nope", "group_not_found")]
	[Arguments("/apis/nope/groups", "api_not_found")]
	public async Task Listing_UnknownApiOrGroup_IsNotFound(string pathAndQuery, string errorType)
	{
		var gateway = A.Fake<IApiGateway>();
		_ = A.CallTo(() => gateway.ListOperationsAsync(A<ApiScope>._, A<CancellationToken>._)).ReturnsLazily(
			(ApiScope scope, CancellationToken _) => scope.Key == "elasticsearch" ? [Operation] : []
		);

		var (status, body) = await Get(gateway, pathAndQuery);

		status.Should().Be(HttpStatusCode.NotFound);
		body.GetProperty("errorType").GetString().Should().Be(errorType);
	}

	[Test]
	public async Task Operation_ById_SplitsAtTheFirstColonAndReturnsTheBodyOnlyWhenAsked()
	{
		List<ApiOperationLookup> received = [];
		var gateway = A.Fake<IApiGateway>();
		_ = A
			.CallTo(() => gateway.GetOperationAsync(A<ApiOperationLookup>._, A<CancellationToken>._))
			.Invokes((ApiOperationLookup lookup, CancellationToken _) => received.Add(lookup))
			.Returns(Detail("Full body"));

		var (withoutBody, withoutJson) = await Get(gateway, "/api-operations/elasticsearch:cluster-health");
		var (withBody, withJson) = await Get(gateway, "/api-operations/kibana%3Aa%3Ab?version=9&include_body=true");

		withoutBody.Should().Be(HttpStatusCode.OK);
		withoutJson.GetProperty("operation").GetProperty("id").GetString().Should().Be("elasticsearch:cluster-health");
		withoutJson.GetProperty("body").ValueKind.Should().Be(JsonValueKind.Null);
		withoutJson.GetProperty("bodyLength").GetInt32().Should().Be(9);
		withBody.Should().Be(HttpStatusCode.OK);
		withJson.GetProperty("body").GetString().Should().Be("Full body");
		ApiOperationKey first = new ApiOperationIdKey("cluster-health");
		ApiOperationKey second = new ApiOperationIdKey("a:b");
		received.Should().Equal(
			new ApiOperationLookup(new ApiScope("elasticsearch", null), first),
			new ApiOperationLookup(new ApiScope("kibana", "9"), second)
		);
	}

	[Test]
	public async Task Operation_UnknownId_IsNotFound()
	{
		var gateway = A.Fake<IApiGateway>();
		_ = A.CallTo(() => gateway.GetOperationAsync(A<ApiOperationLookup>._, A<CancellationToken>._)).Returns((ApiOperationDetail?)null);

		var (status, body) = await Get(gateway, "/api-operations/elasticsearch:nope");

		status.Should().Be(HttpStatusCode.NotFound);
		body.GetProperty("errorType").GetString().Should().Be("operation_not_found");
		body.GetProperty("message").GetString().Should().Be("No API operation matches nope in elasticsearch.");
	}

	private static readonly ApiSummary Elasticsearch = new()
	{
		Key = "elasticsearch",
		Title = "Elasticsearch API",
		Url = "/docs/api/doc/elasticsearch",
		Versions = ["9"]
	};

	[Test]
	public async Task Apis_ListsEveryApiAndReadsOneByKey()
	{
		var gateway = A.Fake<IApiGateway>();
		_ = A.CallTo(() => gateway.ListApisAsync(A<CancellationToken>._)).Returns([Elasticsearch]);

		var (listStatus, list) = await Get(gateway, "/apis");
		var (oneStatus, one) = await Get(gateway, "/apis/Elasticsearch");
		var (missingStatus, missing) = await Get(gateway, "/apis/nope");

		listStatus.Should().Be(HttpStatusCode.OK);
		list.GetProperty("apis")[0].GetProperty("key").GetString().Should().Be("elasticsearch");
		oneStatus.Should().Be(HttpStatusCode.OK);
		one.GetProperty("title").GetString().Should().Be("Elasticsearch API");
		missingStatus.Should().Be(HttpStatusCode.NotFound);
		missing.GetProperty("errorType").GetString().Should().Be("api_not_found");
	}

	[Test]
	public async Task Groups_CountOperationsWithoutListingThem()
	{
		var gateway = A.Fake<IApiGateway>();
		_ = A.CallTo(() => gateway.ListOperationsAsync(new ApiScope("elasticsearch", "9"), A<CancellationToken>._)).Returns([
			Operation,
			Operation with { OperationId = "x" },
			Operation with { OperationId = "s", Group = "search" }
		]);

		var (status, body) = await Get(gateway, "/apis/elasticsearch/groups?version=9");

		status.Should().Be(HttpStatusCode.OK);
		body
			.GetProperty("groups")
			.EnumerateArray()
			.Select(g => (g.GetProperty("name").GetString(), g.GetProperty("count").GetInt32()))
			.Should()
			.Equal(("cluster", 2), ("search", 1));
		body.GetProperty("groups")[0].GetProperty("operations").ValueKind.Should().Be(JsonValueKind.Null);
		body.GetProperty("note").ValueKind.Should().Be(JsonValueKind.Null);
	}
}
