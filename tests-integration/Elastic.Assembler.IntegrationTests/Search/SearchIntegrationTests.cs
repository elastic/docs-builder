// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net.Http.Json;
using Elastic.Documentation.Api.Core.Search;
using FluentAssertions;

namespace Elastic.Assembler.IntegrationTests.Search;

/// <summary>
/// Integration tests for the search endpoint exposed through MapSearchEndpoint.
/// These tests verify that the ElasticsearchGateway correctly processes queries
/// and returns expected results. Inherits from SearchTestBase which handles
/// conditional indexing based on remote Elasticsearch state.
/// </summary>
[Collection(SearchBootstrapFixture.Collection)]
public class SearchIntegrationTests(SearchBootstrapFixture searchFixture, ITestOutputHelper output) : SearchTestBase
{
	/// <summary>
	/// Theory data for search queries mapped to expected first hit URLs.
	/// Format: (query, expectedFirstResultUrl)
	/// Note: These URLs reflect the actual search results from the indexed documentation.
	/// </summary>
	public static TheoryData<string, string> SearchQueryTestCases => new()
	{
		//TODO these results reflect todays result, we still have some work to do to improve the relevance of the search results

		// Elasticsearch specific queries
		{ "elasticsearch getting started", "/docs/reference/elasticsearch/clients/java/getting-started" },
		{ "apm", "/docs/reference/apm/observability/apm" },
		{ "kibana dashboard", "/docs/reference/beats/auditbeat/configuration-dashboards" },

		// .NET specific queries (testing dotnet -> net replacement)
		{ "dotnet client", "/docs/reference/elasticsearch/clients/dotnet/using-net-client" },
		{ ".net apm agent", "/docs/reference/apm/agents/dotnet" },

		// General queries
		{ "machine learning", "/docs/reference/machine-learning" },
		{ "ingest pipeline", "/docs/reference/beats/metricbeat/configuring-ingest-node" },
	};

	[Theory]
	[MemberData(nameof(SearchQueryTestCases))]
	public async Task SearchEndpointReturnsExpectedFirstResult(string query, string expectedFirstResultUrl)
	{
		Assert.SkipUnless(searchFixture.Connected, "Elasticsearch is not connected");

		// Arrange
		searchFixture.HttpClient.Should().NotBeNull("HTTP client should be initialized");

		// Act
		var response = await searchFixture.HttpClient.GetAsync($"/docs/_api/v1/search?q={Uri.EscapeDataString(query)}&page=1", TestContext.Current.CancellationToken);

		// Assert - Response should be successful
		response.EnsureSuccessStatusCode();

		var searchResponse = await response.Content.ReadFromJsonAsync<SearchResponse>(cancellationToken: TestContext.Current.CancellationToken);
		searchResponse.Should().NotBeNull("Search response should be deserialized");

		// Log results for debugging
		output.WriteLine($"Query: {query}");
		output.WriteLine($"Total results: {searchResponse.TotalResults}");
		output.WriteLine($"Results returned: {searchResponse.Results.Count()}");

		if (searchResponse.Results.Any())
		{
			output.WriteLine("First result:");
			var firstResult = searchResponse.Results.First();
			output.WriteLine($"  Title: {firstResult.Title}");
			output.WriteLine($"  URL: {firstResult.Url}");
			output.WriteLine($"  Score: {firstResult.Score}");
		}

		// Assert - Should have at least one result
		searchResponse.Results.Should().NotBeEmpty($"Search for '{query}' should return results");

		// Assert - First result should match expected URL
		var actualFirstResultUrl = searchResponse.Results.First().Url;
		actualFirstResultUrl.Should().Be(expectedFirstResultUrl,
			$"First result for query '{query}' should be the expected documentation page");
	}

	[Fact]
	public async Task SearchEndpointWithPaginationReturnsCorrectPage()
	{
		Assert.SkipUnless(searchFixture.Connected, "Elasticsearch is not connected");

		// Arrange
		searchFixture.HttpClient.Should().NotBeNull("HTTP client should be initialized");
		const string query = "elasticsearch";

		// Act - Get first page
		var page1Response = await searchFixture.HttpClient!.GetAsync($"/docs/_api/v1/search?q={Uri.EscapeDataString(query)}&page=1", TestContext.Current.CancellationToken);
		page1Response.EnsureSuccessStatusCode();
		var page1Data = await page1Response.Content.ReadFromJsonAsync<SearchResponse>(cancellationToken: TestContext.Current.CancellationToken);

		// Act - Get second page
		var page2Response = await searchFixture.HttpClient.GetAsync($"/docs/_api/v1/search?q={Uri.EscapeDataString(query)}&page=2", TestContext.Current.CancellationToken);
		page2Response.EnsureSuccessStatusCode();
		var page2Data = await page2Response.Content.ReadFromJsonAsync<SearchResponse>(cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		page1Data.Should().NotBeNull();
		page2Data.Should().NotBeNull();
		page1Data.PageNumber.Should().Be(1);
		page2Data.PageNumber.Should().Be(2);
		page1Data.TotalResults.Should().Be(page2Data.TotalResults, "Total results should be the same across pages");

		// Results on different pages should be different
		var page1Urls = page1Data.Results.Select(r => r.Url).ToHashSet();
		var page2Urls = page2Data.Results.Select(r => r.Url).ToHashSet();
		page1Urls.Should().NotIntersectWith(page2Urls, "Different pages should contain different results");
	}

	[Fact]
	public async Task SearchEndpointWithEmptyQueryReturnsError()
	{
		// Arrange
		searchFixture.HttpClient.Should().NotBeNull("HTTP client should be initialized");

		// Act
		var response = await searchFixture.HttpClient.GetAsync("/docs/_api/v1/search?q=&page=1", TestContext.Current.CancellationToken);

		// Assert - Should return bad request for empty query
		response.IsSuccessStatusCode.Should().BeFalse("Empty query should not be allowed");
	}
}
