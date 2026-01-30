// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration.Search;
using Elastic.Documentation.Search;
using Elastic.Documentation.Search.Common;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Search.IntegrationTests;

/// <summary>
/// Integration tests for search relevance that use ElasticsearchGateway directly
/// to provide detailed explanations of search results using Elasticsearch's _explain API.
/// These tests help understand and improve search ranking by showing detailed scoring breakdowns.
/// </summary>
public class SearchRelevanceTests(ITestOutputHelper output)
{
	/// <summary>
	/// Theory data for search queries mapped to expected first hit URLs.
	/// Same as SearchIntegrationTests but with detailed explain output on failures.
	/// </summary>
	public static TheoryData<string, string, string[]?> SearchQueryTestCases => new()
	{
		//TODO these results reflect today's result, we still have some work to do to improve the relevance of the search results

		// Elasticsearch specific queries
		{ "elasticsearch get started", "/docs/solutions/search/get-started", null },
		{ "elasticsearch getting started", "/docs/solutions/search/get-started", null },
		{ "elastic common schema", "/docs/reference/ecs", null },
		{ "ecs", "/docs/reference/ecs", null },
		{ "c# client", "/docs/reference/elasticsearch/clients/dotnet/installation", ["/docs/reference/elasticsearch/clients/dotnet"] },
		{ "dotnet client", "/docs/reference/elasticsearch/clients/dotnet/installation", ["/docs/reference/elasticsearch/clients/dotnet"] },
		{ "runscript", "/docs/api/doc/kibana/operation/operation-runscriptaction", [ "/docs/solutions/security/endpoint-response-actions" ] },
		{ "data-streams", "/docs/manage-data/data-store/data-streams", null },
		{ "datastream", "/docs/manage-data/data-store/data-streams", null },
		{ "data stream", "/docs/manage-data/data-store/data-streams", null },
		{ "saml sso", "/docs/deploy-manage/users-roles/cloud-organization/configure-saml-authentication", ["/docs/deploy-manage/users-roles/cloud-organization/configure-saml-authentication"] },
		{ "templates", "/docs/manage-data/data-store/templates", null},
		// different results because of the exact match on title, QueryDSL needs to be normalized in the content
		{ "query dsl", "/docs/explore-analyze/query-filter/languages/querydsl", ["/docs/explore-analyze/query-filter/languages/querydsl"]},
		{ "querydsl", "/docs/reference/query-languages/querydsl", ["/docs/explore-analyze/query-filter/languages/querydsl"]},
		{ "Agent policy", "/docs/reference/fleet/agent-policy", null},
		{ "aliases", "/docs/manage-data/data-store/aliases", null},
		{ "Kibana privilege", "/docs/deploy-manage/users-roles/cluster-or-deployment-auth/kibana-privileges", null},
		{ "lens", "/docs/explore-analyze/visualize/lens", null},
		{ "machine learning node", "/docs/deploy-manage/autoscaling/autoscaling-in-ece-and-ech", null },
		{ "machine learning", "/docs/reference/machine-learning", null},
		{ "ml", "/docs/reference/machine-learning", null},
		{ "elasticsearch", "/docs/reference/elasticsearch", null},
		{ "kibana", "/docs/reference/kibana", null},
		{ "cloud", "/docs/reference/cloud", null},
		{ "logstash", "/docs/reference/logstash", null},
		{ "logstash release", "/docs/release-notes/logstash", null},
		{ "esql", "/docs/reference/query-languages/esql", null},
		{ "ES|QL", "/docs/reference/query-languages/esql", null},
		{ "Output plugins for Logstash", "/docs/reference/logstash/plugins/output-plugins", null},
		// exact match on title wins but with variations we prefer the more general topic page
		{ "Sending data to Elastic Cloud Hosted", "/docs/reference/logstash/connecting-to-cloud", ["/docs/solutions/observability/get-started/quickstart-elastic-cloud-otel-endpoint"]},
		{ "Send data to Elastic Cloud Hosted", "/docs/solutions/observability/get-started/quickstart-elastic-cloud-otel-endpoint", ["/docs/reference/logstash/connecting-to-cloud"]},

		{ "universal profiling", "/docs/solutions/observability/infra-and-hosts/universal-profiling", null},
		{ "agg", "/docs/explore-analyze/query-filter/aggregations", null},
		{ "a", "/docs/reference/apm/observability/apm", null},
		{ "index.number_of_replicas", "/docs/reference/elasticsearch/index-settings/index-modules", null},
		//{ "index.use_time_series_doc_values_format", "/docs/reference/elasticsearch/index-settings/index-modules", null},
		//universal profiling
	};

	[Theory]
	[MemberData(nameof(SearchQueryTestCases))]
	public async Task SearchReturnsExpectedFirstResultWithExplain(string query, string expectedFirstResultUrl, string[]? additionalExpectedUrls)
	{
		// Arrange - Create ElasticsearchGateway directly
		var gateway = CreateFindPageGateway();
		Assert.SkipUnless(gateway is not null, "Elasticsearch is not connected");
		var canConnect = await gateway.CanConnect(TestContext.Current.CancellationToken);
		Assert.SkipUnless(canConnect, "Elasticsearch is not connected");

		// Act - Perform the search
		var searchResult = await gateway.SearchImplementation(query, 1, 5, null, TestContext.Current.CancellationToken);

		// Log basic results
		output.WriteLine($"Query: {query}");
		output.WriteLine($"Total hits: {searchResult.TotalHits}");
		output.WriteLine($"Results returned: {searchResult.Results.Count}");

		var results = searchResult.Results;

		results.Should().NotBeEmpty($"Search for '{query}' should return results");

		var actualFirstResultUrl = results.First().Url;

		// If the first result doesn't match expectations, use _explain API for detailed analysis
		if (actualFirstResultUrl != expectedFirstResultUrl)
		{
			output.WriteLine("\n❌ FIRST RESULT MISMATCH - Fetching detailed explanations...\n");

			// Get explain for both the actual top result and the expected result
			var (topResultExplain, expectedResultExplain) = await gateway.ExplainTopResultAndExpectedAsync(
				query,
				expectedFirstResultUrl,
				TestContext.Current.CancellationToken);

			// Output the actual top result explanation
			output.WriteLine("═══════════════════════════════════════════════════════════════");
			output.WriteLine($"ACTUAL TOP RESULT: {topResultExplain.DocumentUrl}");
			output.WriteLine($"Search Title: {topResultExplain.SearchTitle}");
			output.WriteLine($"Score: {topResultExplain.Score:F4}");
			output.WriteLine($"Matched: {topResultExplain.Matched}");
			output.WriteLine("───────────────────────────────────────────────────────────────");
			output.WriteLine("Scoring Breakdown:");
			output.WriteLine(topResultExplain.Explanation);

			// Output the expected result explanation
			output.WriteLine("═══════════════════════════════════════════════════════════════");
			output.WriteLine($"EXPECTED RESULT: {expectedResultExplain.DocumentUrl}");
			output.WriteLine($"Search Title: {expectedResultExplain.SearchTitle}");
			output.WriteLine($"Score: {expectedResultExplain.Score:F4}");
			output.WriteLine($"Matched: {expectedResultExplain.Matched}");
			output.WriteLine("───────────────────────────────────────────────────────────────");
			output.WriteLine("Scoring Breakdown:");
			output.WriteLine(expectedResultExplain.Explanation);
			output.WriteLine("═══════════════════════════════════════════════════════════════\n");

			// Create a detailed failure message
			var scoreDiff = topResultExplain.Score - expectedResultExplain.Score;
			var failureMessage = $@"
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
			output.WriteLine($"✅ First result matches expected: {actualFirstResultUrl}");
			output.WriteLine($"   Score: {results.First().Score:F4}");
		}

		// Check for additional expected URLs if provided
		if (additionalExpectedUrls?.Length > 0)
		{
			output.WriteLine($"\nChecking for {additionalExpectedUrls.Length} additional expected URLs on first page...");
			var resultUrls = results.Select(r => r.Url).ToList();

			foreach (var expectedUrl in additionalExpectedUrls)
			{
				if (resultUrls.Contains(expectedUrl))
				{
					var position = resultUrls.IndexOf(expectedUrl) + 1;
					output.WriteLine($"✅ Found expected URL at position {position}: {expectedUrl}");
				}
				else
				{
					output.WriteLine($"❌ Expected URL not found on first page: {expectedUrl}");
					output.WriteLine($"   First page results ({results.Count}):");
					for (var i = 0; i < results.Count; i++)
					{
						output.WriteLine($"   {i + 1}. {results[i].Url} (score: {results[i].Score:F4})");
					}
					resultUrls.Should().Contain(expectedUrl, $"Expected URL '{expectedUrl}' should be present on the first page of results for query '{query}'");
				}
			}
		}
	}

	[Fact]
	public async Task ExplainTopResultAndExpectedAsyncReturnsDetailedScoring()
	{
		// Arrange
		var gateway = CreateFindPageGateway();
		Assert.SkipUnless(gateway is not null, "Elasticsearch is not connected");
		var canConnect = await gateway.CanConnect(TestContext.Current.CancellationToken);
		Assert.SkipUnless(canConnect, "Elasticsearch is not connected");

		const string query = "elasticsearch getting started";
		const string expectedUrl = "/docs/reference/elasticsearch/clients/java/getting-started";

		// Act - Use the ExplainTopResultAndExpectedAsync method which gets top result and explains both
		var (topResultExplain, expectedResultExplain) = await gateway.ExplainTopResultAndExpectedAsync(
			query,
			expectedUrl,
			TestContext.Current.CancellationToken);

		// Assert - Top result should have explanation
		output.WriteLine($"Query: {query}");
		output.WriteLine($"\nTOP RESULT: {topResultExplain.DocumentUrl}");
		output.WriteLine($"Found: {topResultExplain.Found}");
		output.WriteLine($"Matched: {topResultExplain.Matched}");
		output.WriteLine($"Score: {topResultExplain.Score:F4}");
		output.WriteLine("Explanation:");
		output.WriteLine(topResultExplain.Explanation);

		output.WriteLine($"\nEXPECTED RESULT: {expectedResultExplain.DocumentUrl}");
		output.WriteLine($"Found: {expectedResultExplain.Found}");
		output.WriteLine($"Matched: {expectedResultExplain.Matched}");
		output.WriteLine($"Score: {expectedResultExplain.Score:F4}");
		output.WriteLine("Explanation:");
		output.WriteLine(expectedResultExplain.Explanation);

		// Both results should have explanations (even if scores are different)
		topResultExplain.Explanation.Should().NotBeEmpty("Top result should have an explanation");
		expectedResultExplain.Explanation.Should().NotBeEmpty("Expected result should have an explanation");
	}

	/// <summary>
	/// Creates an ElasticsearchGateway instance using configuration from the distributed application.
	/// </summary>
	private NavigationSearchGateway? CreateFindPageGateway()
	{
		// Build a new ConfigurationBuilder to read user secrets and environment variables
		var configBuilder = new ConfigurationBuilder();
		configBuilder.AddUserSecrets("72f50f33-6fb9-4d08-bff3-39568fe370b3");
		configBuilder.AddEnvironmentVariables();
		var config = configBuilder.Build();

		// Get Elasticsearch configuration with fallback chain: user secrets → environment
		var elasticsearchUrl =
			config["Parameters:DocumentationElasticUrl"]
			?? config["DOCUMENTATION_ELASTIC_URL"];

		var elasticsearchApiKey =
			config["Parameters:DocumentationElasticApiKey"]
			?? config["DOCUMENTATION_ELASTIC_APIKEY"];

		if (elasticsearchUrl is null or "" || elasticsearchApiKey is null or "")
			return null;

		// Create IConfiguration with the required values for ElasticsearchOptions
		var testConfig = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				["DOCUMENTATION_ELASTIC_URL"] = elasticsearchUrl,
				["DOCUMENTATION_ELASTIC_APIKEY"] = elasticsearchApiKey,
				["DOCUMENTATION_ELASTIC_INDEX"] = "semantic-docs-dev-latest"
			})
			.Build();

		var options = new ElasticsearchOptions(testConfig);
		var searchConfig = new SearchConfiguration
		{
			Synonyms = new Dictionary<string, string[]>(),
			Rules =
			[
				new QueryRule
				{
					RuleId = "pin-data-streams",
					Type = QueryRuleType.Pinned,
					Criteria =
					[
						new QueryRuleCriteria
						{
							Type = QueryRuleCriteriaType.Exact,
							Metadata = "query_string",
							Values = ["data stream", "data-stream", "data-streams", "datastream", "datastreams"]
						}
					],
					Actions = new QueryRuleActions
					{
						Ids = ["/docs/manage-data/data-store/data-streams"]
					}
				}
			],
			DiminishTerms = ["plugin", "client", "integration", "glossary"]
		};

		var clientAccessor = new ElasticsearchClientAccessor(options, searchConfig);
		return new NavigationSearchGateway(clientAccessor, NullLogger<NavigationSearchGateway>.Instance);
	}
}
