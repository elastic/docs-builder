// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net;
using AwesomeAssertions;
using Elastic.Documentation.Api.Tests.Fixtures;
using Elastic.Documentation.Search;
using Elastic.Documentation.Search.Contract;
using FakeItEasy;

namespace Elastic.Documentation.Api.Tests;

// The tests share one host and read the request its one search service received, so they run one after another.
[NotInParallel(nameof(NavigationSearchEndpointTests))]
public class NavigationSearchEndpointTests
{
	private static readonly SwappableService<INavigationSearchService> SearchService = new();

	private static readonly Lazy<ApiWebApplicationFactory> SharedFactory = new(
		() => ApiWebApplicationFactory.WithMockedServices(
			replacements => replacements.Replace<INavigationSearchService>(_ => SearchService.Current)
		)
	);

	[After(Class)]
	public static void DisposeSharedFactory()
	{
		if (SharedFactory.IsValueCreated)
			SharedFactory.Value.Dispose();
	}

	private static async Task<(HttpStatusCode Status, NavigationSearchRequest? Received)> Get(string queryString)
	{
		NavigationSearchRequest? received = null;
		var service = A.Fake<INavigationSearchService>();
		_ = A
			.CallTo(() => service.NavigationSearchAsync(A<NavigationSearchRequest>._, A<CancellationToken>._))
			.Invokes((NavigationSearchRequest request, CancellationToken _) => received = request)
			.Returns(new NavigationSearchResponse { Results = [], TotalResults = 0, PageNumber = 1, PageSize = 20 });
		SearchService.Current = service;
		using var client = SharedFactory.Value.CreateClient();

		using var response = await client.GetAsync(
			$"/docs/_api/v1/navigation-search/?q=knn&type=api{queryString}",
			TestContext.Current!.Execution.CancellationToken
		);
		return (response.StatusCode, received);
	}

	[Test]
	public async Task Get_NoTreeParameters_SearchesWithoutScope()
	{
		var (status, received) = await Get("");

		status.Should().Be(HttpStatusCode.OK);
		received.Should().NotBeNull();
		received.ApiTree.Should().BeNull();
	}

	[Test]
	[Arguments("main")]
	[Arguments("8")]
	public async Task Get_KeyAndTree_SearchesInThatTree(string tree)
	{
		var (status, received) = await Get($"&api_key=elasticsearch&api_tree={tree}");

		status.Should().Be(HttpStatusCode.OK);
		received.Should().NotBeNull();
		received.ApiTree.Should().Be(new ApiTreeScope("elasticsearch", tree));
	}

	[Test]
	[Arguments("&api_tree=8")]
	[Arguments("&api_key=elasticsearch")]
	[Arguments("&api_key=elasticsearch&api_tree=abc")]
	[Arguments("&api_key=elasticsearch&api_tree=1234")]
	[Arguments("&api_key=Elastic_Search&api_tree=8")]
	[Arguments("&api_key=&api_tree=8")]
	public async Task Get_IncompleteOrMalformedScope_ReturnsBadRequestWithoutSearching(string queryString)
	{
		var (status, received) = await Get(queryString);

		status.Should().Be(HttpStatusCode.BadRequest);
		received.Should().BeNull();
	}
}
