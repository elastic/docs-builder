// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Api.Infrastructure.Adapters.Search;
using Elastic.Documentation.Api.Infrastructure.Aws;
using Elastic.Documentation.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elastic.Assembler.IntegrationTests.Search;

/// <summary>
/// Integration tests for search relevance that use ElasticsearchGateway directly
/// to provide detailed explanations of search results using Elasticsearch's _explain API.
/// These tests help understand and improve search ranking by showing detailed scoring breakdowns.
/// </summary>
[Collection(SearchBootstrapFixture.Collection)]
public class SearchRelevanceTests(SearchBootstrapFixture searchFixture, DocumentationFixture documentationFixture, ITestOutputHelper output) : SearchTestBase
{
	private readonly SearchBootstrapFixture _searchFixture = searchFixture;
	private readonly DocumentationFixture _documentationFixture = documentationFixture;
	/// <summary>
	/// Theory data for search queries mapped to expected first hit URLs.
	/// Same as SearchIntegrationTests but with detailed explain output on failures.
	/// </summary>
	public static TheoryData<string, string> SearchQueryTestCases => new()
	{
		//TODO these results reflect today's result, we still have some work to do to improve the relevance of the search results

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
	public async Task SearchReturnsExpectedFirstResultWithExplain(string query, string expectedFirstResultUrl)
	{
		// Arrange - Create ElasticsearchGateway directly
		var gateway = CreateElasticsearchGateway();

		// Act - Perform the search
		var (totalHits, results) = await gateway.HybridSearchWithRrfAsync(query, 1, 5, TestContext.Current.CancellationToken);

		// Log basic results
		output.WriteLine($"Query: {query}");
		output.WriteLine($"Total hits: {totalHits}");
		output.WriteLine($"Results returned: {results.Count}");

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
			output.WriteLine($"Score: {topResultExplain.Score:F4}");
			output.WriteLine($"Matched: {topResultExplain.Matched}");
			output.WriteLine("───────────────────────────────────────────────────────────────");
			output.WriteLine("Scoring Breakdown:");
			output.WriteLine(topResultExplain.Explanation);

			// Output the expected result explanation
			output.WriteLine("═══════════════════════════════════════════════════════════════");
			output.WriteLine($"EXPECTED RESULT: {expectedResultExplain.DocumentUrl}");
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
	}

	[Fact]
	public async Task ExplainTopResultAndExpectedAsyncReturnsDetailedScoring()
	{
		// Arrange
		var gateway = CreateElasticsearchGateway();
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
	private ElasticsearchGateway CreateElasticsearchGateway()
	{
		var configuration = _documentationFixture.DistributedApplication.Services.GetRequiredService<IConfiguration>();
		var loggerFactory = _documentationFixture.DistributedApplication.Services.GetRequiredService<ILoggerFactory>();

		// Build a new ConfigurationBuilder to read user secrets
		var configBuilder = new ConfigurationBuilder();
		configBuilder.AddUserSecrets("72f50f33-6fb9-4d08-bff3-39568fe370b3");
		var userSecretsConfig = configBuilder.Build();

		// Get Elasticsearch configuration with fallback chain: user secrets → configuration → environment
		var elasticsearchUrl = userSecretsConfig["Parameters:DocumentationElasticUrl"]
			?? configuration["Parameters:DocumentationElasticUrl"]
			?? Environment.GetEnvironmentVariable("DOCUMENTATION_ELASTIC_URL")
			?? throw new InvalidOperationException("Elasticsearch URL not configured");

		var elasticsearchApiKey = userSecretsConfig["Parameters:DocumentationElasticApiKey"]
			?? configuration["Parameters:DocumentationElasticApiKey"]
			?? Environment.GetEnvironmentVariable("DOCUMENTATION_ELASTIC_APIKEY")
			?? throw new InvalidOperationException("Elasticsearch API key not configured");

		// Create a test parameter provider with the configuration values
		var parameterProvider = new TestParameterProvider(elasticsearchUrl, elasticsearchApiKey, "semantic-docs-dev-latest");
		var options = new ElasticsearchOptions(parameterProvider);

		return new ElasticsearchGateway(options, loggerFactory.CreateLogger<ElasticsearchGateway>());
	}

	/// <summary>
	/// Simple test implementation of IParameterProvider that returns configured values.
	/// </summary>
	private sealed class TestParameterProvider(string url, string apiKey, string indexName) : IParameterProvider
	{
		public Task<string> GetParam(string name, bool withDecryption = true, Cancel ctx = default) =>
			name switch
			{
				"docs-elasticsearch-url" => Task.FromResult(url),
				"docs-elasticsearch-apikey" => Task.FromResult(apiKey),
				"docs-elasticsearch-index" => Task.FromResult(indexName),
				_ => throw new ArgumentException($"Parameter '{name}' not configured in test provider")
			};
	}
}
