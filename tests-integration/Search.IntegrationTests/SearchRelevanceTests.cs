// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using System.IO.Abstractions;
using AwesomeAssertions;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Loading;
using Elastic.Documentation.FileSystems;
using Elastic.Documentation.Search;
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
	public static IEnumerable<Func<(string, string, string[]?)>> SearchQueryTestCases()
	{
		//TODO these results reflect today's result, we still have some work to do to improve the relevance of the search results

		// Elasticsearch specific queries
		yield return () => ("elasticsearch get started", "/docs/solutions/search/get-started", null);
		yield return () => ("elasticsearch getting started", "/docs/solutions/search/get-started", null);
		yield return () => ("elastic common schema", "/docs/reference/ecs", null);
		yield return () => ("ecs", "/docs/reference/ecs", null);
		yield return () =>
			("c# client", "/docs/reference/elasticsearch/clients/dotnet/installation", ["/docs/reference/elasticsearch/clients/dotnet"]);
		yield return () =>
			("dotnet client", "/docs/reference/elasticsearch/clients/dotnet/installation", ["/docs/reference/elasticsearch/clients/dotnet"]);
		yield return () =>
			("runscript", "/docs/api/kibana/operation/operation-runscriptaction", ["/docs/solutions/security/endpoint-response-actions"]);
		yield return () => ("_search", "/docs/api/elasticsearch/operation/operation-search", null);
		yield return () => ("/_search", "/docs/api/elasticsearch/operation/operation-search", null);
		yield return () => ("GET /_search", "/docs/api/elasticsearch/operation/operation-search", null);
		yield return () => ("index/_search", "/docs/api/elasticsearch/operation/operation-search", null);
		yield return () => ("my-index/_search", "/docs/api/elasticsearch/operation/operation-search", null);
		yield return () => ("_cat/indices/logs-2026", "/docs/api/elasticsearch/operation/operation-cat-indices", null);
		yield return () => ("_bulk", "/docs/api/elasticsearch/operation/operation-bulk", null);
		yield return () => ("_security/user/alice/_password", "/docs/api/elasticsearch/operation/operation-security-change-password", null);
		yield return () => ("data-streams", "/docs/manage-data/data-store/data-streams", null);
		yield return () => ("datastream", "/docs/manage-data/data-store/data-streams", null);
		yield return () => ("data stream", "/docs/manage-data/data-store/data-streams", null);
		yield return () =>
			("saml sso", "/docs/deploy-manage/users-roles/cloud-organization/configure-saml-authentication", [
				"/docs/deploy-manage/users-roles/cloud-organization/configure-saml-authentication"
			]);
		yield return () => ("templates", "/docs/manage-data/data-store/templates", null);
		// different results because of the exact match on title, QueryDSL needs to be normalized in the content
		yield return () =>
			("query dsl", "/docs/explore-analyze/query-filter/languages/querydsl", [
				"/docs/explore-analyze/query-filter/languages/querydsl"
			]);
		yield return () =>
			("querydsl", "/docs/reference/query-languages/querydsl", ["/docs/explore-analyze/query-filter/languages/querydsl"]);
		yield return () => ("Agent policy", "/docs/reference/fleet/agent-policy", null);
		yield return () => ("aliases", "/docs/manage-data/data-store/aliases", null);
		yield return () => ("Kibana privilege", "/docs/deploy-manage/users-roles/cluster-or-deployment-auth/kibana-privileges", null);
		yield return () => ("lens", "/docs/explore-analyze/visualize/lens", null);
		yield return () => ("machine learning node", "/docs/deploy-manage/autoscaling/autoscaling-in-ece-and-ech", null);
		yield return () => ("machine learning", "/docs/reference/machine-learning", null);
		yield return () => ("ml", "/docs/reference/machine-learning", null);
		yield return () => ("elasticsearch", "/docs/reference/elasticsearch", null);
		yield return () => ("kibana", "/docs/reference/kibana", null);
		yield return () => ("cloud", "/docs/reference/cloud", null);
		yield return () => ("logstash", "/docs/reference/logstash", null);
		yield return () => ("logstash release", "/docs/release-notes/logstash", null);
		yield return () => ("esql", "/docs/reference/query-languages/esql", null);
		yield return () => ("ES|QL", "/docs/reference/query-languages/esql", null);
		yield return () => ("Output plugins for Logstash", "/docs/reference/logstash/plugins/output-plugins", null);
		// exact match on title wins but with variations we prefer the more general topic page
		yield return () =>
			("Sending data to Elastic Cloud Hosted", "/docs/reference/logstash/connecting-to-cloud", [
				"/docs/solutions/observability/get-started/quickstart-elastic-cloud-otel-endpoint"
			]);
		yield return () =>
			("Send data to Elastic Cloud Hosted", "/docs/solutions/observability/get-started/quickstart-elastic-cloud-otel-endpoint", [
				"/docs/reference/logstash/connecting-to-cloud"
			]);
		yield return () => ("universal profiling", "/docs/solutions/observability/infra-and-hosts/universal-profiling", null);
		yield return () => ("agg", "/docs/explore-analyze/query-filter/aggregations", null);
		yield return () => ("a", "/docs/reference/apm/observability/apm", null);
		yield return () => ("index.number_of_replicas", "/docs/reference/elasticsearch/index-settings/index-modules", null);
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

	public static IEnumerable<(string, string)> ApiSearchQueryTestCases()
	{
		yield return ("search", "operation-search");
		yield return ("_search", "operation-search");
		yield return ("bulk", "operation-bulk");
		yield return ("ml flush", "operation-ml-flush-job");
		yield return ("flush ml", "operation-ml-flush-job");
		yield return ("flush ML job", "operation-ml-flush-job");
		yield return ("ml flush job", "operation-ml-flush-job");
		yield return ("_ml _flush", "operation-ml-flush-job");
		yield return ("flush", "operation-indices-flush");
		yield return ("search retriever", "operation-search");
		yield return ("es|ql drop_null_columns", "operation-esql-query");
		yield return ("search knn", "operation-search");
		yield return ("knn search", "operation-search");
		yield return ("cluster health", "operation-cluster-health");
		yield return ("health cluster", "operation-cluster-health");
		// One typo in a word of four letters or more still names the operation the correct spelling does.
		yield return ("clustr health", "operation-cluster-health");
		yield return ("cat health", "operation-cat-health");
		yield return ("indices refresh", "operation-indices-refresh");
		yield return ("cluster state", "operation-cluster-state");
		yield return ("get cluster settings", "operation-cluster-get-settings");
		yield return ("update cluster settings", "operation-cluster-put-settings");
		yield return ("cluster get flat_settings", "operation-cluster-get-settings");
		yield return ("esql query drop_null_columns", "operation-esql-query");
		yield return ("run a search", "operation-search");
		yield return ("GET /_search knn", "operation-search");
		yield return ("POST /_count", "operation-count");
		// The route decides: a method that differs from the operation's canonical one never lets a title that starts with the
		// same verb, such as "Get the search shards", lead the operation the route names.
		yield return ("GET /_search", "operation-search");
		yield return ("POST /_search", "operation-search");
		yield return ("/_search", "operation-search");
		yield return ("/_search knn", "operation-search");
		yield return ("GET /_cluster/health", "operation-cluster-health");
		yield return ("GET /_search_shards", "operation-search-shards");
		yield return ("snapshot restore", "operation-snapshot-restore");
		yield return ("search template", "operation-search-template");
		yield return ("/_cluster health", "operation-cluster-health");
		yield return ("/cluster health", "operation-cluster-health");
		yield return ("/search", "operation-search");
		yield return ("/search knn", "operation-search");
		yield return ("/count", "operation-count");
		yield return ("/snapshot restore", "operation-snapshot-restore");
		yield return ("/_security role", "operation-security-put-role");
		yield return ("HEAD /", "operation-ping");
		yield return ("/_sear", "operation-search");
		yield return ("/_sea", "operation-search");
		yield return ("/sear", "operation-search");
		yield return ("/_cat/ind", "operation-cat-indices");
		yield return ("/{index}/_sear", "operation-search");
		yield return ("/_cluster/hea", "operation-cluster-health");
		// The words are the whole title once its stopwords and plurals are ignored, so the operation leads the ones that only
		// mention them, such as the `_create` endpoint of a document.
		yield return ("create index", "operation-indices-create");
		yield return ("index create", "operation-indices-create");
		yield return ("delete index", "operation-indices-delete");
		// The titles are plain words and the operation id carries the same words: the whole-title lift must not move them.
		yield return ("create an index", "operation-indices-create");
		yield return ("create data stream", "operation-indices-create-data-stream");
		// The word is an endpoint of four operations, and the current one outranks the three deprecated legacy ones.
		yield return ("template", "operation-indices-exists-template");
		yield return ("esql query", "operation-esql-query");
		// A deprecated twin or a sibling operation never leads the one the words name.
		yield return ("isolate endpoint", "operation-endpointisolateaction");
		yield return ("release isolated endpoint", "operation-endpointunisolateaction");
		yield return ("agent status", "operation-get-fleet-agent-status");
		yield return ("service token", "operation-security-create-service-token");
		// Words people type most. Each leads by more than a stray point over the next operation, and the Serverless twin of an
		// operation ties with the stack one.
		yield return ("reindex", "operation-reindex");
		yield return ("msearch", "operation-msearch");
		yield return ("mget", "operation-mget");
		yield return ("count", "operation-count");
		yield return ("refresh", "operation-indices-refresh");
		yield return ("rollover", "operation-indices-rollover");
		yield return ("analyze", "operation-indices-analyze");
		yield return ("force merge", "operation-indices-forcemerge");
		yield return ("put mapping", "operation-indices-put-mapping");
		yield return ("get mapping", "operation-indices-get-mapping");
		yield return ("cat indices", "operation-cat-indices");
		yield return ("nodes stats", "operation-nodes-stats");
		yield return ("snapshot create", "operation-snapshot-create");
		yield return ("allocation explain", "operation-cluster-allocation-explain");
		yield return ("field capabilities", "operation-field-caps");
		// One typo in a word of four letters or more names the operation the correct spelling does. A word with no typo of its
		// own in the title still reaches the operation through its id.
		yield return ("cluser health", "operation-cluster-health");
		yield return ("clustr state", "operation-cluster-state");
		yield return ("snaphot restore", "operation-snapshot-restore");
		yield return ("snapshot restor", "operation-snapshot-restore");
		yield return ("snapshto restore", "operation-snapshot-restore");
		yield return ("reindx", "operation-reindex");
		yield return ("bulck", "operation-bulk");
		yield return ("indx refresh", "operation-indices-refresh");
		// `stats` stems to `stat`, which begins `state`: a word typed whole beats one that only starts another word.
		yield return ("cluster stats", "operation-cluster-stats");
		yield return ("clustr stats", "operation-cluster-stats");
		// A typo in a word only the title carries: the typed words are the whole title within one edit.
		yield return ("creat index", "operation-indices-create");
		yield return ("delte index", "operation-indices-delete");
		yield return ("refesh index", "operation-indices-refresh");
		yield return ("get cluser health status", "operation-cluster-health");
	}

	public static IEnumerable<(string, string, string, string)> ApiSearchFieldTestCases()
	{
		yield return ("cluster flat_settings", "operation-cluster-get-settings", "flat_settings", "query");
		yield return ("flat_settings cluster", "operation-cluster-get-settings", "flat_settings", "query");
		yield return ("es|ql drop_null_columns", "operation-esql-query", "drop_null_columns", "query");
		yield return ("search retriever", "operation-search", "retriever", "request");
		yield return ("GET /_search knn", "operation-search", "knn", "request");
		yield return ("rrf.min_score", "operation-search", "min_score", "request");
		yield return ("[].retriever.standard", "operation-msearch", "standard", "request");
		yield return ("search rrf.min_score", "operation-search", "min_score", "request");
		// 18,419 field names start with s on the live index, more than the 50 terms a phrase prefix expands to on a plain text field.
		yield return ("retriever.s", "operation-search", "standard", "request");
		yield return ("search retriever.sta", "operation-search", "standard", "request");
		yield return ("unrated docs", "operation-rank-eval", "unrated_docs", "response");
		yield return ("docs unrated", "operation-rank-eval", "unrated_docs", "response");
		yield return ("unreated docs", "operation-rank-eval", "unrated_docs", "response");
		yield return ("_uri", "operation-cluster-info", "last_uri", "response");
	}

	/// <summary>A typed word that names a field shared with other rows, as <c>search</c> in <c>search_type</c>, next to one that singles it out.</summary>
	public static IEnumerable<(string, string, string)> ApiSearchFieldOrderTestCases()
	{
		yield return ("search after", "operation-search", "search_after");
		yield return ("scroll id", "operation-scroll", "scroll_id");
	}

	// The index keeps "data streams" as plain words, so the singular finds a plural-only page only through the
	// search-time "data streams" expansion in search.yml. The pin keeps the overview first either way.
	[Test]
	[Arguments("data stream")]
	[Arguments("datastream")]
	public async Task SearchSingular_FindsAPageThatOnlyUsesThePluralWords(string query)
	{
		var (gateway, _) = CreateFindPageGateway();
		Skip.Unless(gateway is not null, "Elasticsearch is not connected");
		var canConnect = await gateway.CanConnect(TestContext.Current!.Execution.CancellationToken);
		Skip.Unless(canConnect, "Elasticsearch is not connected");

		var searchResult = await gateway.NavigationSearchAsync(
			new NavigationSearchRequest { Query = query, PageNumber = 1, PageSize = 20 },
			TestContext.Current!.Execution.CancellationToken
		);

		searchResult.Results.Select(r => r.Url).Should().Contain("/docs/reference/fleet/data-streams");
	}

	[Test]
	[MethodDataSource(nameof(ApiSearchQueryTestCases))]
	public async Task ApiSearchReturnsExpectedFirstResult(string query, string expectedOperation)
	{
		var (gateway, _) = CreateFindPageGateway();
		Skip.Unless(gateway is not null, "Elasticsearch is not connected");
		var canConnect = await gateway.CanConnect(TestContext.Current!.Execution.CancellationToken);
		Skip.Unless(canConnect, "Elasticsearch is not connected");

		var searchResult = await gateway.NavigationSearchAsync(
			new NavigationSearchRequest { Query = query, PageNumber = 1, PageSize = 5, TypeFilter = "api" },
			TestContext.Current!.Execution.CancellationToken
		);

		var results = searchResult.Results.ToList();
		results.Should().NotBeEmpty($"API search for '{query}' should return results");
		results.Should().OnlyContain(r => r.Type == "api");
		// The Serverless twin of an operation ties with the stack one, so match the operation, not the API.
		results[0].Url.Should().EndWith($"/operation/{expectedOperation}");
	}

	[Test]
	[MethodDataSource(nameof(ApiSearchFieldTestCases))]
	public async Task ApiSearchListsTheFieldUnderTheOperation(string query, string expectedOperation, string field, string location)
	{
		var (gateway, _) = CreateFindPageGateway();
		Skip.Unless(gateway is not null, "Elasticsearch is not connected");
		var canConnect = await gateway.CanConnect(TestContext.Current!.Execution.CancellationToken);
		Skip.Unless(canConnect, "Elasticsearch is not connected");

		var searchResult = await gateway.NavigationSearchAsync(
			new NavigationSearchRequest { Query = query, PageNumber = 1, PageSize = 20, TypeFilter = "api" },
			TestContext.Current!.Execution.CancellationToken
		);

		var results = searchResult.Results.ToList();
		results.Should().NotBeEmpty($"API search for '{query}' should return results");
		results.Should().OnlyContain(r => r.Type == "api");
		results[0].Properties.Should().Contain(p => p.Name == field, $"the first result for '{query}' lists the field it names");
		// The Serverless twin of an operation ties with the stack one, so every copy of the operation must list the field.
		var operations = results.Where(r => r.Url.EndsWith($"/operation/{expectedOperation}", StringComparison.Ordinal)).ToList();
		operations.Should().NotBeEmpty($"'{query}' should find {expectedOperation}");
		operations.Should().AllSatisfy(r => r.Properties.Should().Contain(p => p.Name == field && p.Location == location));
	}

	[Test]
	[MethodDataSource(nameof(ApiSearchFieldOrderTestCases))]
	public async Task ApiSearchListsTheRowNamingEveryTypedWordFirst(string query, string expectedOperation, string field)
	{
		var (gateway, _) = CreateFindPageGateway();
		Skip.Unless(gateway is not null, "Elasticsearch is not connected");
		var canConnect = await gateway.CanConnect(TestContext.Current!.Execution.CancellationToken);
		Skip.Unless(canConnect, "Elasticsearch is not connected");

		var searchResult = await gateway.NavigationSearchAsync(
			new NavigationSearchRequest { Query = query, PageNumber = 1, PageSize = 5, TypeFilter = "api" },
			TestContext.Current!.Execution.CancellationToken
		);

		var results = searchResult.Results.ToList();
		results.Should().NotBeEmpty($"API search for '{query}' should return results");
		results[0].Url.Should().EndWith($"/operation/{expectedOperation}");
		results[0].Properties.Should().NotBeEmpty().And.Subject.First().Name.Should().Be(field, $"'{query}' names every word of {field}");
	}

	[Test]
	public async Task ApiSearchTypedRouteListsTheRoutesUnderIt()
	{
		var (gateway, _) = CreateFindPageGateway();
		Skip.Unless(gateway is not null, "Elasticsearch is not connected");
		var canConnect = await gateway.CanConnect(TestContext.Current!.Execution.CancellationToken);
		Skip.Unless(canConnect, "Elasticsearch is not connected");

		var searchResult = await gateway.NavigationSearchAsync(
			new NavigationSearchRequest { Query = "/api/fleet", PageNumber = 1, PageSize = 20, TypeFilter = "api" },
			TestContext.Current!.Execution.CancellationToken
		);

		var results = searchResult.Results.ToList();
		results.Should().NotBeEmpty("'/api/fleet' should list the Fleet operations");
		results.Should().AllSatisfy(
			r => r
				.Routes
				.Should()
				.Contain(route => route.Path.StartsWith("/api/fleet", StringComparison.Ordinal), $"{r.Url} has a Fleet route")
		);
	}

	[Test]
	public async Task ApiSearchTypedRouteWithAParameterValueListsItsDescendantRoutes()
	{
		var (gateway, _) = CreateFindPageGateway();
		Skip.Unless(gateway is not null, "Elasticsearch is not connected");
		var canConnect = await gateway.CanConnect(TestContext.Current!.Execution.CancellationToken);
		Skip.Unless(canConnect, "Elasticsearch is not connected");

		var searchResult = await gateway.NavigationSearchAsync(
			new NavigationSearchRequest { Query = "/_security/user/alice", PageNumber = 1, PageSize = 20, TypeFilter = "api" },
			TestContext.Current!.Execution.CancellationToken
		);

		var results = searchResult.Results.ToList();
		TestContext.Current?.Output.WriteLine(string.Join("\n", results.Select(r => r.Url)));
		results.Should().NotBeEmpty();
		// Get, put and delete user share the route the value fills, so they tie and only the route is asserted.
		results[0].Routes.Should().Contain(route => route.Path == "/_security/user/{username}", "the template the value fills ranks first");
		results.Should().Contain(r => r.Url.EndsWith("/operation/operation-security-change-password", StringComparison.Ordinal));
	}

	[Test]
	public async Task ApiSearchRootRouteListsOnlyTheRootOperations()
	{
		var (gateway, _) = CreateFindPageGateway();
		Skip.Unless(gateway is not null, "Elasticsearch is not connected");
		var canConnect = await gateway.CanConnect(TestContext.Current!.Execution.CancellationToken);
		Skip.Unless(canConnect, "Elasticsearch is not connected");

		var searchResult = await gateway.NavigationSearchAsync(
			new NavigationSearchRequest { Query = "/", PageNumber = 1, PageSize = 20, TypeFilter = "api" },
			TestContext.Current!.Execution.CancellationToken
		);

		var results = searchResult.Results.ToList();
		TestContext.Current?.Output.WriteLine(string.Join("\n", results.Select(r => r.Url)));
		results.Should().NotBeEmpty("'/' names the root route");
		results.Should().AllSatisfy(r => r.Routes.Should().Contain(route => route.Path == "/", $"{r.Url} is on the root route"));
		results.Should().Contain(r => r.Url.EndsWith("/elasticsearch/operation/operation-ping", StringComparison.Ordinal));
		results.Should().Contain(r => r.Url.EndsWith("/elasticsearch/operation/operation-info", StringComparison.Ordinal));
	}

	[Test]
	public async Task ApiSearchTypedRouteWithATrailingSlashListsOnlyTheRoutesUnderIt()
	{
		var (gateway, _) = CreateFindPageGateway();
		Skip.Unless(gateway is not null, "Elasticsearch is not connected");
		var canConnect = await gateway.CanConnect(TestContext.Current!.Execution.CancellationToken);
		Skip.Unless(canConnect, "Elasticsearch is not connected");

		var searchResult = await gateway.NavigationSearchAsync(
			new NavigationSearchRequest { Query = "/_search/", PageNumber = 1, PageSize = 20, TypeFilter = "api" },
			TestContext.Current!.Execution.CancellationToken
		);

		var results = searchResult.Results.ToList();
		TestContext.Current?.Output.WriteLine(string.Join("\n", results.Select(r => r.Url)));
		results.Should().NotBeEmpty("'/_search/' lists /_search and the routes under it");
		results.Should().AllSatisfy(
			r => r
				.Routes
				.Should()
				.Contain(
					route => route.Path == "/_search" || route.Path.StartsWith("/_search/", StringComparison.Ordinal),
					$"{r.Url} is /_search or a route under it"
				)
		);
		results.Should().NotContain(
			r => r.Url.EndsWith("/operation/operation-search-shards", StringComparison.Ordinal),
			"the trailing slash completes nothing"
		);
	}

	[Test]
	public async Task ApiSearchPlainWordDoesNotCoverTheRoutesUnderItsOwnName()
	{
		var (gateway, _) = CreateFindPageGateway();
		Skip.Unless(gateway is not null, "Elasticsearch is not connected");
		var canConnect = await gateway.CanConnect(TestContext.Current!.Execution.CancellationToken);
		Skip.Unless(canConnect, "Elasticsearch is not connected");

		var searchResult = await gateway.NavigationSearchAsync(
			new NavigationSearchRequest { Query = "api key", PageNumber = 1, PageSize = 5, TypeFilter = "api" },
			TestContext.Current!.Execution.CancellationToken
		);

		var urls = searchResult.Results.Select(r => r.Url).ToList();
		TestContext.Current?.Output.WriteLine(string.Join("\n", urls));
		// Every Kibana route starts with api/, so the word must not lift Kibana operations above the API-key ones.
		urls
			.Should()
			.NotBeEmpty()
			.And
			.OnlyContain(u => u.Contains("api-key", StringComparison.Ordinal) && !u.Contains("/kibana", StringComparison.Ordinal));
	}

	[Test]
	// The deprecated Kibana "Create a new monitored user" came first, and the Elasticsearch operation was 10th, until the title roles
	// read the display title (within one edit), a title's "X or Y" verbs joined api.words, and a word the operation names itself
	// stopped listing a same-named request object as a path.
	[Arguments("create user", "/operation/operation-create-user", 1)]
	[Arguments("create user", "/operation/operation-security-put-user", 2)]
	// A word that names the API still counts: the title match once read search_title, which holds "Cloud Enterprise API".
	[Arguments("cloud create user", "/operation/operation-create-user", 1)]
	// The group page is the title typed, whichever way the word is spelled: stemmed words, and the British spelling through a synonym.
	[Arguments("behavioral analytics", "/group/endpoint-analytics", 1)]
	[Arguments("behavior analytics", "/group/endpoint-analytics", 1)]
	[Arguments("Behaviour analytics", "/group/endpoint-analytics", 1)]
	public async Task ApiSearchPlacesAResultAtItsExpectedPosition(string query, string expectedUrlEnd, int expectedPosition)
	{
		var (gateway, _) = CreateFindPageGateway();
		Skip.Unless(gateway is not null, "Elasticsearch is not connected");
		var canConnect = await gateway.CanConnect(TestContext.Current!.Execution.CancellationToken);
		Skip.Unless(canConnect, "Elasticsearch is not connected");

		var searchResult = await gateway.NavigationSearchAsync(
			new NavigationSearchRequest { Query = query, PageNumber = 1, PageSize = 5, TypeFilter = "api" },
			TestContext.Current!.Execution.CancellationToken
		);

		// Positions count from 1, as the search telemetry does.
		var urls = searchResult.Results.Select(r => r.Url).ToList();
		var position = urls.FindIndex(u => u.EndsWith(expectedUrlEnd, StringComparison.Ordinal)) + 1;
		position.Should().Be(expectedPosition, $"'{query}' listed {string.Join(", ", urls)}");
	}

	[Test]
	[Arguments("run an ES|QL query", "/operation/operation-esql-query")]
	[Arguments("Get lifecycle policies", "/operation/operation-ilm-get-lifecycle")]
	public async Task ApiSearchTitleTypedAsWrittenEarnsTheWholeTitleLift(string title, string expectedUrlEnd)
	{
		var (gateway, clientAccessor) = CreateFindPageGateway();
		Skip.Unless(gateway is not null, "Elasticsearch is not connected");
		var ct = TestContext.Current!.Execution.CancellationToken;
		Skip.Unless(await gateway.CanConnect(ct), "Elasticsearch is not connected");

		// The titles are indexed as written, while the synonyms analyzer turns `policies` into `policy` and `ES|QL` into `esql`.
		var response = await clientAccessor.Client.SearchAsync<DocumentationDocument>(
			s => s
				.Indices(clientAccessor.SearchIndex)
				.Size(50)
				.Query(SearchQueryBuilder.FilterDocuments(ApiLookupQueryBuilder.Build(title), page: null))
				.PostFilter(new TermQuery { Field = QueryFieldNames.ContentType, Value = "api" })
				.Source(sf => sf.Filter(f => f.Includes(ReportSourceIncludes)))
				.IncludeNamedQueriesScore(true),
			ct
		);

		var hit = response.Hits.FirstOrDefault(h => h.Source?.Path?.EndsWith(expectedUrlEnd, StringComparison.Ordinal) == true);
		hit.Should().NotBeNull($"'{title}' lists {expectedUrlEnd}");
		var scored = hit!.MatchedQueries?.Match(_ => null, scores => scores) ?? new Dictionary<string, double>();
		scored
			.TryGetValue("title_all", out var lift)
			.Should()
			.BeTrue($"the title '{title}' names its own operation, which scored {string.Join(", ", scored)}");
		lift.Should().Be(ApiLookupWeights.WholeTitle);
	}

	[Test]
	public async Task ApiSearchWordsOfAnApisNameListItsLandingPageFirst()
	{
		var (gateway, _) = CreateFindPageGateway();
		Skip.Unless(gateway is not null, "Elasticsearch is not connected");
		var canConnect = await gateway.CanConnect(TestContext.Current!.Execution.CancellationToken);
		Skip.Unless(canConnect, "Elasticsearch is not connected");

		var searchResult = await gateway.NavigationSearchAsync(
			new NavigationSearchRequest { Query = "kibana api", PageNumber = 1, PageSize = 5, TypeFilter = "api" },
			TestContext.Current!.Execution.CancellationToken
		);

		searchResult.Results.First().Url.Should().Be("/docs/api/kibana");
	}

	// DocumentationDocument requires title and search_title; the full source also carries nullable dates it cannot read.
	private static readonly string[] ReportSourceIncludes = ["path", "title", "search_title"];

	/// <summary>
	/// Queries the report lists with no first-result case, so a weight change shows what it moves beyond the judged cases. Those that
	/// <see cref="ApiSearchPlacesAResultAtItsExpectedPosition"/> judges by position stay here: the report prints their scores and winning roles.
	/// </summary>
	public static IEnumerable<string> ApiSearchUnjudgedReportQueries() =>
		[
			"get index",
			"create index template",
			"close index",
			"open index",
			"delete data stream",
			"refresh index",
			"index document",
			"delete document",
			"get document",
			"put mapping",
			"create alias",
			"clear cache",
			"force merge",
			"create snapshot",
			"get cluster stats",
			"create pipeline",
			"search templates",
			"create role",
			"create user",
			"cloud create user",
			"behavior analytics",
			"Behaviour analytics",
			"reindex",
			"legacy index template",
			"install package",
			"reassign agent",
			"enrollment api key",
			"clustr helth",
			"serch knn",
			// Typos the fuzzy id word does not fix yet: a route key is not fuzzed, and a title-only word misses the whole-title lift.
			"serch",
			"clustr settings",
			"pipline"
		];

	/// <summary>
	/// Not an assertion: prints the top three operations of every judged API query, with each word's winning role, for
	/// whoever tunes <see cref="ApiLookupWeights"/> next. With <c>API_SEARCH_REPORT_PATH</c> set it also writes the report
	/// there, so a run before and a run after a change can be diffed.
	/// </summary>
	[Test]
	[Explicit]
	public async Task ApiSearchRelevanceReport()
	{
		var (gateway, clientAccessor) = CreateFindPageGateway();
		Skip.Unless(gateway is not null, "Elasticsearch is not connected");
		var ct = TestContext.Current!.Execution.CancellationToken;
		Skip.Unless(await gateway.CanConnect(ct), "Elasticsearch is not connected");

		var judged = ApiSearchQueryTestCases()
			.Concat(ApiSearchFieldTestCases().Select(c => (c.Item1, c.Item2)))
			.Concat(ApiSearchUnjudgedReportQueries().Select(q => (q, "unjudged")))
			.Distinct();
		var lines = new List<string>();

		void Report(string line)
		{
			lines.Add(line);
			TestContext.Current?.Output.WriteLine(line);
		}

		foreach (var (query, expectedOperation) in judged)
		{
			var tokens = ApiLookupQueryBuilder.Tokenize(query);
			var response = await clientAccessor.Client.SearchAsync<DocumentationDocument>(
				s => s
					.Indices(clientAccessor.SearchIndex)
					.Size(3)
					.Query(SearchQueryBuilder.FilterDocuments(ApiLookupQueryBuilder.Build(query), page: null))
					.PostFilter(new TermQuery { Field = QueryFieldNames.ContentType, Value = "api" })
					.Source(sf => sf.Filter(f => f.Includes(ReportSourceIncludes)))
					.IncludeNamedQueriesScore(true),
				ct
			);
			Report($"\n{query}  (expected {expectedOperation})");
			foreach (var hit in response.Hits)
			{
				var scored = hit.MatchedQueries?.Match(_ => null, scores => scores) ?? new Dictionary<string, double>();
				var roles = ApiLookupRoles.Resolve(scored).Select(r => $"{tokens[r.Index].Text}={r.Role.ToString().ToLowerInvariant()}");
				var score = (hit.Score ?? 0).ToString("F2", CultureInfo.InvariantCulture);
				Report($"  {score,7}  {hit.Source?.Path}  [{string.Join(", ", roles)}]");
			}
		}

		if (Environment.GetEnvironmentVariable("API_SEARCH_REPORT_PATH") is { Length: > 0 } reportPath)
			await File.WriteAllLinesAsync(reportPath, lines, ct);
	}

	[Test]
	public async Task ApiSearchWithThreeWordsMatchesOperationsThatContainTwoOfThem()
	{
		var (gateway, _) = CreateFindPageGateway();
		Skip.Unless(gateway is not null, "Elasticsearch is not connected");
		var canConnect = await gateway.CanConnect(TestContext.Current!.Execution.CancellationToken);
		Skip.Unless(canConnect, "Elasticsearch is not connected");

		var searchResult = await gateway.NavigationSearchAsync(
			new NavigationSearchRequest { Query = "create deployment zzzunknownword", PageNumber = 1, PageSize = 20, TypeFilter = "api" },
			TestContext.Current!.Execution.CancellationToken
		);

		var titles = searchResult.Results.Select(r => r.Title.Replace("<mark>", "").Replace("</mark>", "")).ToList();
		TestContext.Current?.Output.WriteLine(string.Join("\n", titles));

		// The unknown third word is ignored under the 2<75% rule, so an operation needs only the two real words.
		// A path word can match a query word the title lacks (kstem stems the path word "deployments" to "deployment").
		titles.Should().NotBeEmpty();
		titles[0].Should().ContainEquivalentOf("create").And.ContainEquivalentOf("deployment");
		titles.Take(3).Should().OnlyContain(t => t.Contains("create", StringComparison.OrdinalIgnoreCase));
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
		var searchConfig = SearchTuningLoader.Load(configProvider);

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
