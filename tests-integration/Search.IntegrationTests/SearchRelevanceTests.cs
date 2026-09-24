// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using System.IO.Abstractions;
using AwesomeAssertions;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Search;
using Elastic.Documentation.FileSystems;
using Elastic.Documentation.Search;
using Elastic.Documentation.Search.Common;
using Elastic.Documentation.Search.Contract;
using Elastic.Documentation.ServiceDefaults;
using Microsoft.Extensions.Logging.Abstractions;

namespace Search.IntegrationTests;

/// <summary>
/// Integration tests for search relevance that use ElasticsearchGateway directly
/// to provide detailed explanations of search results using Elasticsearch's _explain API.
/// These tests help understand and improve search ranking by showing detailed scoring breakdowns.
/// </summary>
public class SearchRelevanceTests
{
	/// <summary>
	/// Theory data for search queries mapped to expected first hit URLs.
	/// Same as SearchIntegrationTests but with detailed explain output on failures.
	/// </summary>
	public static IEnumerable<(string, string, string[]?)> SearchQueryTestCases()
	{
		//TODO these results reflect today's result, we still have some work to do to improve the relevance of the search results

		// Elasticsearch specific queries
		yield return ("elasticsearch get started", "/docs/solutions/search/get-started", null);
		yield return ("elasticsearch getting started", "/docs/solutions/search/get-started", null);
		yield return ("elastic common schema", "/docs/reference/ecs", null);
		yield return ("ecs", "/docs/reference/ecs", null);
		yield return ("c# client", "/docs/reference/elasticsearch/clients/dotnet/installation", [
			"/docs/reference/elasticsearch/clients/dotnet"
		]);
		yield return ("dotnet client", "/docs/reference/elasticsearch/clients/dotnet/installation", [
			"/docs/reference/elasticsearch/clients/dotnet"
		]);
		yield return ("runscript", "/docs/api/doc/kibana/operation/operation-runscriptaction", [
			"/docs/solutions/security/endpoint-response-actions"
		]);
		yield return ("data-streams", "/docs/manage-data/data-store/data-streams", null);
		yield return ("datastream", "/docs/manage-data/data-store/data-streams", null);
		yield return ("data stream", "/docs/manage-data/data-store/data-streams", null);
		yield return ("saml sso", "/docs/deploy-manage/users-roles/cloud-organization/configure-saml-authentication", [
			"/docs/deploy-manage/users-roles/cloud-organization/configure-saml-authentication"
		]);
		yield return ("templates", "/docs/manage-data/data-store/templates", null);
		// different results because of the exact match on title, QueryDSL needs to be normalized in the content
		yield return ("query dsl", "/docs/explore-analyze/query-filter/languages/querydsl", [
			"/docs/explore-analyze/query-filter/languages/querydsl"
		]);
		yield return ("querydsl", "/docs/reference/query-languages/querydsl", ["/docs/explore-analyze/query-filter/languages/querydsl"]);
		yield return ("Agent policy", "/docs/reference/fleet/agent-policy", null);
		yield return ("aliases", "/docs/manage-data/data-store/aliases", null);
		yield return ("Kibana privilege", "/docs/deploy-manage/users-roles/cluster-or-deployment-auth/kibana-privileges", null);
		yield return ("lens", "/docs/explore-analyze/visualize/lens", null);
		yield return ("machine learning node", "/docs/deploy-manage/autoscaling/autoscaling-in-ece-and-ech", null);
		yield return ("machine learning", "/docs/reference/machine-learning", null);
		yield return ("ml", "/docs/reference/machine-learning", null);
		yield return ("elasticsearch", "/docs/reference/elasticsearch", null);
		yield return ("kibana", "/docs/reference/kibana", null);
		yield return ("cloud", "/docs/reference/cloud", null);
		yield return ("logstash", "/docs/reference/logstash", null);
		yield return ("logstash release", "/docs/release-notes/logstash", null);
		yield return ("esql", "/docs/reference/query-languages/esql", null);
		yield return ("ES|QL", "/docs/reference/query-languages/esql", null);
		yield return ("Output plugins for Logstash", "/docs/reference/logstash/plugins/output-plugins", null);
		// exact match on title wins but with variations we prefer the more general topic page
		yield return ("Sending data to Elastic Cloud Hosted", "/docs/reference/logstash/connecting-to-cloud", [
			"/docs/solutions/observability/get-started/quickstart-elastic-cloud-otel-endpoint"
		]);
		yield return ("Send data to Elastic Cloud Hosted", "/docs/solutions/observability/get-started/quickstart-elastic-cloud-otel-endpoint", [
			"/docs/reference/logstash/connecting-to-cloud"
		]);
		yield return ("universal profiling", "/docs/solutions/observability/infra-and-hosts/universal-profiling", null);
		yield return ("agg", "/docs/explore-analyze/query-filter/aggregations", null);
		yield return ("a", "/docs/reference/apm/observability/apm", null);
		yield return ("index.number_of_replicas", "/docs/reference/elasticsearch/index-settings/index-modules", null);
		//{ "index.use_time_series_doc_values_format", "/docs/reference/elasticsearch/index-settings/index-modules", null},
		//universal profiling
	}

	[Test]
	[MethodDataSource(nameof(SearchQueryTestCases))]
	public async Task SearchReturnsExpectedFirstResultWithExplain(
		string query,
		string expectedFirstResultUrl,
		string[]? additionalExpectedUrls
	)
	{
		// Arrange - Create ElasticsearchGateway directly
		var (gateway, clientAccessor) = CreateFindPageGateway();
		Skip.Unless(gateway is not null, "Elasticsearch is not connected");

		TestContext.Current?.Output.WriteLine($"Endpoint: {clientAccessor.Endpoint.Uri}");
		TestContext.Current?.Output.WriteLine($"SearchIndex: {clientAccessor.SearchIndex}");
		TestContext.Current?.Output.WriteLine($"RulesetName: {clientAccessor.RulesetName ?? "(none)"}");

		var canConnect = await gateway.CanConnect(TestContext.Current!.Execution.CancellationToken);
		Skip.Unless(canConnect, "Elasticsearch is not connected");

		// Act - Perform the search via the adapter's autocomplete path
		var searchResult = await gateway.NavigationSearchAsync(
			new NavigationSearchRequest { Query = query, PageNumber = 1, PageSize = 5 },
			TestContext.Current!.Execution.CancellationToken
		);

		// Log basic results
		TestContext.Current?.Output.WriteLine($"Query: {query}");
		TestContext.Current?.Output.WriteLine($"Total hits: {searchResult.TotalResults}");
		TestContext.Current?.Output.WriteLine($"Results returned: {searchResult.Results.Count()}");

		var results = searchResult.Results.ToList();

		if (results.Count == 0)
		{
			var countResponse = await clientAccessor.Client.CountAsync(
				c => c.Indices(clientAccessor.SearchIndex),
				TestContext.Current!.Execution.CancellationToken
			);
			TestContext.Current?.Output.WriteLine(
				$"Index document count: {(countResponse.IsValidResponse ? countResponse.Count.ToString(CultureInfo.InvariantCulture) : $"ERROR: {countResponse.ElasticsearchServerError?.Error?.Reason}")}"
			);
		}

		results.Should().NotBeEmpty($"Search for '{query}' should return results (index: {clientAccessor.SearchIndex})");

		var actualFirstResultUrl = results.First().Url;

		// If the first result doesn't match expectations, use _explain API for detailed analysis
		if (actualFirstResultUrl != expectedFirstResultUrl)
		{
			TestContext.Current?.Output.WriteLine("\n❌ FIRST RESULT MISMATCH - Fetching detailed explanations...\n");

			// Get explain for both the actual top result and the expected result
			var (topResultExplain, expectedResultExplain) = await gateway.ExplainTopResultAndExpectedAsync(
				query,
				expectedFirstResultUrl,
				TestContext.Current!.Execution.CancellationToken
			);

			// Output the actual top result explanation
			TestContext.Current?.Output.WriteLine("═══════════════════════════════════════════════════════════════");
			TestContext.Current?.Output.WriteLine($"ACTUAL TOP RESULT: {topResultExplain.DocumentUrl}");
			TestContext.Current?.Output.WriteLine($"Search Title: {topResultExplain.SearchTitle}");
			TestContext.Current?.Output.WriteLine($"Score: {topResultExplain.Score:F4}");
			TestContext.Current?.Output.WriteLine($"Matched: {topResultExplain.Matched}");
			TestContext.Current?.Output.WriteLine("───────────────────────────────────────────────────────────────");
			TestContext.Current?.Output.WriteLine("Scoring Breakdown:");
			TestContext.Current?.Output.WriteLine(topResultExplain.Explanation);

			// Output the expected result explanation
			TestContext.Current?.Output.WriteLine("═══════════════════════════════════════════════════════════════");
			TestContext.Current?.Output.WriteLine($"EXPECTED RESULT: {expectedResultExplain.DocumentUrl}");
			TestContext.Current?.Output.WriteLine($"Search Title: {expectedResultExplain.SearchTitle}");
			TestContext.Current?.Output.WriteLine($"Score: {expectedResultExplain.Score:F4}");
			TestContext.Current?.Output.WriteLine($"Matched: {expectedResultExplain.Matched}");
			TestContext.Current?.Output.WriteLine("───────────────────────────────────────────────────────────────");
			TestContext.Current?.Output.WriteLine("Scoring Breakdown:");
			TestContext.Current?.Output.WriteLine(expectedResultExplain.Explanation);
			TestContext.Current?.Output.WriteLine("═══════════════════════════════════════════════════════════════\n");

			// Create a detailed failure message
			var scoreDiff = topResultExplain.Score - expectedResultExplain.Score;
			var failureMessage =
				$@"
First result for query '{query}' did not match expectation.

Expected: {expectedFirstResultUrl}
  - Score: {expectedResultExplain.Score:F4}
  - Matched: {expectedResultExplain.Matched}

Actual: {actualFirstResultUrl}
  - Score: {topResultExplain.Score:F4}
  - Matched: {topResultExplain.Matched}

Score Difference: {scoreDiff:F4} (actual is {(scoreDiff > 0 ? "higher" : "lower")})

See test output above for detailed scoring breakdowns from Elasticsearch's _explain API.
";

			actualFirstResultUrl.Should().Be(expectedFirstResultUrl, failureMessage);
		}
		else
		{
			TestContext.Current?.Output.WriteLine($"✅ First result matches expected: {actualFirstResultUrl}");
			TestContext.Current?.Output.WriteLine($"   Score: {results.First().Score:F4}");
		}

		// Check for additional expected URLs if provided
		if (additionalExpectedUrls?.Length > 0)
		{
			TestContext.Current?.Output.WriteLine(
				$"\nChecking for {additionalExpectedUrls.Length} additional expected URLs on first page..."
			);
			var resultUrls = results.Select(r => r.Url).ToList();

			foreach (var expectedUrl in additionalExpectedUrls)
			{
				if (resultUrls.Contains(expectedUrl))
				{
					var position = resultUrls.IndexOf(expectedUrl) + 1;
					TestContext.Current?.Output.WriteLine($"✅ Found expected URL at position {position}: {expectedUrl}");
				}
				else
				{
					TestContext.Current?.Output.WriteLine($"❌ Expected URL not found on first page: {expectedUrl}");
					TestContext.Current?.Output.WriteLine($"   First page results ({results.Count}):");
					for (var i = 0; i < results.Count; i++)
					{
						TestContext.Current?.Output.WriteLine($"   {i + 1}. {results[i].Url} (score: {results[i].Score:F4})");
					}
					resultUrls.Should().Contain(
						expectedUrl,
						$"Expected URL '{expectedUrl}' should be present on the first page of results for query '{query}'"
					);
				}
			}
		}
	}

	[Test]
	public async Task ExplainTopResultAndExpectedAsyncReturnsDetailedScoring()
	{
		// Arrange
		var (gateway, clientAccessor) = CreateFindPageGateway();
		Skip.Unless(gateway is not null, "Elasticsearch is not connected");

		TestContext.Current?.Output.WriteLine($"Endpoint: {clientAccessor.Endpoint.Uri}");
		TestContext.Current?.Output.WriteLine($"SearchIndex: {clientAccessor.SearchIndex}");

		var canConnect = await gateway.CanConnect(TestContext.Current!.Execution.CancellationToken);
		Skip.Unless(canConnect, "Elasticsearch is not connected");

		const string query = "elasticsearch getting started";
		const string expectedUrl = "/docs/reference/elasticsearch/clients/java/getting-started";

		// Act - Use the ExplainTopResultAndExpectedAsync method which gets top result and explains both
		var (topResultExplain, expectedResultExplain) = await gateway.ExplainTopResultAndExpectedAsync(
			query,
			expectedUrl,
			TestContext.Current!.Execution.CancellationToken
		);

		// Assert - Top result should have explanation
		TestContext.Current?.Output.WriteLine($"Query: {query}");
		TestContext.Current?.Output.WriteLine($"\nTOP RESULT: {topResultExplain.DocumentUrl}");
		TestContext.Current?.Output.WriteLine($"Found: {topResultExplain.Found}");
		TestContext.Current?.Output.WriteLine($"Matched: {topResultExplain.Matched}");
		TestContext.Current?.Output.WriteLine($"Score: {topResultExplain.Score:F4}");
		TestContext.Current?.Output.WriteLine("Explanation:");
		TestContext.Current?.Output.WriteLine(topResultExplain.Explanation);

		TestContext.Current?.Output.WriteLine($"\nEXPECTED RESULT: {expectedResultExplain.DocumentUrl}");
		TestContext.Current?.Output.WriteLine($"Found: {expectedResultExplain.Found}");
		TestContext.Current?.Output.WriteLine($"Matched: {expectedResultExplain.Matched}");
		TestContext.Current?.Output.WriteLine($"Score: {expectedResultExplain.Score:F4}");
		TestContext.Current?.Output.WriteLine("Explanation:");
		TestContext.Current?.Output.WriteLine(expectedResultExplain.Explanation);

		// Both results should have explanations (even if scores are different)
		topResultExplain.Explanation.Should().NotBeEmpty("Top result should have an explanation");
		expectedResultExplain.Explanation.Should().NotBeEmpty("Expected result should have an explanation");
	}

	/// <summary>
	/// Creates an ElasticsearchGateway instance using configuration from the distributed application.
	/// </summary>
	private static (NavigationSearchService Gateway, ElasticsearchClientAccessor ClientAccessor) CreateFindPageGateway()
	{
		var endpoints = ElasticsearchEndpointFactory.Create(buildType: "assembler");
		var configProvider = new ConfigurationFileProvider(
			NullLoggerFactory.Instance,
			new ConfigurationFileSystem(),
			configurationSource: ConfigurationSource.Embedded
		);
		var searchConfig = configProvider.CreateSearchConfiguration();

		var clientAccessor = new ElasticsearchClientAccessor(endpoints, searchConfig);

		var queryConfig = new SearchQueryConfiguration
		{
			SynonymBiDirectional = clientAccessor.SynonymBiDirectional,
			DiminishTerms = clientAccessor.DiminishTerms,
			RulesetName = clientAccessor.RulesetName,
			SemanticEnabled = true
		};
		var inner = new DefaultSearchService<DocumentationDocument>(
			clientAccessor.Client,
			clientAccessor.SearchIndex,
			queryConfig,
			NullLogger<DefaultSearchService<DocumentationDocument>>.Instance
		);

		var gateway = new NavigationSearchService(inner, clientAccessor, NullLogger<NavigationSearchService>.Instance);
		return (gateway, clientAccessor);
	}
}
