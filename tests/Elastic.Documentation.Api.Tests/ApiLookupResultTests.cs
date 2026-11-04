// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using AwesomeAssertions;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Search;
using Elastic.Documentation.Search;
using Elastic.Documentation.Search.Common;
using Elastic.Documentation.Search.Contract;
using Elastic.Documentation.Search.Highlighting;

namespace Elastic.Documentation.Api.Tests;

public class ApiLookupResultTests
{
	private static readonly HighlightOptions ApiLookup = new() { ApiLookup = true };

	[Test]
	public void ProcessHit_TokenWonByEndpointKey_HidesItsFieldRows()
	{
		var json = SearchResponseJson(
			"""{"key_0":6.0,"field_0":0.6,"title_0":1.0,"field_1":1.0}""",
			("api_properties_0", InnerHit("req-search_type", 0.6, "search_type")),
			("api_properties_1", InnerHit("req-retriever", 1.0, "retriever"))
		);

		var result = SearchResultProcessor.ProcessHit(
			ParseResponse(json).Hits.Single(),
			"search retriever",
			new Dictionary<string, string[]>(),
			ApiLookup
		);

		result.Properties.Select(p => p.Anchor).Should().Equal("req-retriever");
	}

	[Test]
	public void ProcessHit_TokenTiedBetweenFieldAndTitle_ShowsItsRows()
	{
		var json = SearchResponseJson(
			"""{"field_0":1.0,"title_0":1.0,"body_0":0.1}""",
			("api_properties_0", InnerHit("req-retriever", 1.0, "retriever"))
		);

		var result = SearchResultProcessor.ProcessHit(
			ParseResponse(json).Hits.Single(),
			"retriever",
			new Dictionary<string, string[]>(),
			ApiLookup
		);

		result.Properties.Select(p => p.Anchor).Should().Equal("req-retriever");
	}

	[Test]
	public void ProcessHit_TokenWithoutFieldMatch_KeepsNoRows()
	{
		var json = SearchResponseJson("""{"id_0":2.0,"title_0":1.0}""", ("api_properties_0", InnerHit("req-cluster", 1.0, "cluster")));

		var result = SearchResultProcessor.ProcessHit(
			ParseResponse(json).Hits.Single(),
			"cluster",
			new Dictionary<string, string[]>(),
			ApiLookup
		);

		result.Properties.Should().BeEmpty();
	}

	[Test]
	public void ProcessHit_DocsWideReadingNames_AreNotApiLookupRows()
	{
		var json = SearchResponseJson(
			"""["api_properties"]""",
			("api_properties", InnerHit("req-track_total_hits", 1.0, "track_total_hits"))
		);

		var result = SearchResultProcessor.ProcessHit(
			ParseResponse(json).Hits.Single(),
			"track_total_hits",
			new Dictionary<string, string[]>(),
			ApiLookup
		);

		result.Properties.Should().BeEmpty();
	}

	private static string SearchResponseJson(string matchedQueries, params (string Name, string Hit)[] groups)
	{
		var innerHits = string.Join(
			",",
			groups.Select(
				g =>
					$$$"""
				"{{{g.Name}}}":{"hits":{"total":{"value":1,"relation":"eq"},"max_score":1,"hits":[{{{g.Hit}}}]}}
				"""
			)
		);
		return $$$"""
			{"took":1,"timed_out":false,"_shards":{"total":1,"successful":1,"failed":0},"hits":{"total":{"value":1,"relation":"eq"},"hits":[
			{"_index":"test","_id":"search","_score":1,"matched_queries":{{{matchedQueries}}},"_source":{"title":"Search","search_title":"Search","path":"/search"},
			"inner_hits":{{{{innerHits}}}}}]}}
			""";
	}

	private static string InnerHit(string anchor, double score, string path) =>
		$$$"""
		{"_index":"test","_id":"search","_score":{{{score.ToString(CultureInfo.InvariantCulture)}}},"_nested":{"field":"api.properties","offset":0},
		"_source":{"name":"{{{path}}}","path":"{{{path}}}","type":"boolean","anchor":"{{{anchor}}}","location":"request","description":"","parent":""}}
		""";

	private static Clients.Elasticsearch.SearchResponse<DocumentationDocument> ParseResponse(string json)
	{
		using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
		using var accessor = new ElasticsearchClientAccessor(
			new DocumentationEndpoints
			{
				Elasticsearch = new ElasticsearchEndpoint { Uri = new Uri("https://localhost:9200"), ApiKey = "test" }
			},
			new SearchConfiguration { Synonyms = [], Rules = [], DiminishTerms = [] }
		);
		return accessor.Client.RequestResponseSerializer.Deserialize<Clients.Elasticsearch.SearchResponse<DocumentationDocument>>(stream);
	}
}
