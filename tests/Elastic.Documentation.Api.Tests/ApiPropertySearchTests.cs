// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using AwesomeAssertions;
using Elastic.Clients.Elasticsearch;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Search;
using Elastic.Documentation.Search;
using Elastic.Documentation.Search.Common;
using Elastic.Documentation.Search.Contract;
using Elastic.Documentation.Search.Highlighting;
using Elastic.Transport;
using Elastic.Transport.Extensions;

namespace Elastic.Documentation.Api.Tests;

public class ApiPropertySearchTests
{
	private static readonly ElasticsearchClient Client = new(new ElasticsearchClientSettings(new InMemoryRequestInvoker()));

	[Test]
	[Arguments("travel_agency", "api")]
	[Arguments("trav", "api")]
	[Arguments("flight.travel", "api")]
	[Arguments("track_total", null)]
	[Arguments("knn_field search", null)]
	public void BuildApiPropertyQuery_Identifier_ReturnsNestedInnerHits(string input, string? filter)
	{
		using var json = JsonDocument.Parse(
			Client.RequestResponseSerializer.SerializeToString(SearchQueryBuilder.BuildApiPropertyQuery(input, filter))
		);
		var nested = json.RootElement.GetProperty("nested");
		nested.GetProperty("path").GetString().Should().Be("api.properties");
		nested.GetProperty("inner_hits").GetProperty("size").GetInt32().Should().Be(5);
	}

	[Test]
	[Arguments("knn", "knn")]
	[Arguments("_search knn", "knn")]
	[Arguments("GET /_search knn", "knn")]
	[Arguments("knn /_search", "knn")]
	[Arguments("knn GET /_search", "knn")]
	[Arguments("options", "options")]
	[Arguments("_bulk delete", "delete")]
	[Arguments("track_total", "track_total")]
	[Arguments("trav", "trav")]
	public void BuildApiPropertyQuery_FieldTerm_MatchesOnlyItsOwnNameWithBoundedScores(string input, string name)
	{
		using var json = JsonDocument.Parse(
			Client.RequestResponseSerializer.SerializeToString(SearchQueryBuilder.BuildApiPropertyQuery(input, "api"))
		);
		var query = json.RootElement.GetProperty("nested").GetProperty("query");
		var matches = query.GetProperty("dis_max").GetProperty("queries");
		var exact = matches[0].GetProperty("constant_score");
		exact
			.GetProperty("filter")
			.GetProperty("term")
			.GetProperty("api.properties.name")
			.GetProperty("value")
			.GetString()
			.Should()
			.Be(name);
		query.GetRawText().Should().NotContain("api.properties.description").And.NotContain("api.properties.path");
		matches.GetArrayLength().Should().Be(2);
		matches[1]
			.GetProperty("constant_score")
			.GetProperty("filter")
			.GetProperty("prefix")
			.GetProperty("api.properties.name")
			.GetProperty("value")
			.GetString()
			.Should()
			.Be(name);
		var scores = matches.EnumerateArray().Select(q => q.GetProperty("constant_score").GetProperty("boost").GetSingle()).ToArray();
		scores.Should().OnlyContain(score => score > 0 && score <= 1);
		scores[0].Should().BeGreaterThan(scores[1], "an exact field name ranks ahead of a prefix");
	}

	[Test]
	[Arguments(null, 1)]
	[Arguments("all", 1)]
	[Arguments("api", 2)]
	public void BuildApiPropertyQuery_PrefixMatch_OnlyInTheApiFilteredLookup(string? filter, int nameClauses)
	{
		using var json = JsonDocument.Parse(
			Client.RequestResponseSerializer.SerializeToString(SearchQueryBuilder.BuildApiPropertyQuery("track_total", filter))
		);
		var clauses = json.RootElement.GetProperty("nested").GetProperty("query").GetProperty("dis_max").GetProperty("queries");
		clauses.GetArrayLength().Should().Be(nameClauses);
		clauses[0].GetRawText().Should().Contain("\"term\"");
	}

	[Test]
	public void BuildApiPropertyQuery_QualifiedPath_RequiresMatchingLeafAndPath()
	{
		using var json = JsonDocument.Parse(
			Client.RequestResponseSerializer.SerializeToString(SearchQueryBuilder.BuildApiPropertyQuery("_search knn.field", "api"))
		);
		var qualified = json
			.RootElement
			.GetProperty("nested")
			.GetProperty("query")
			.GetProperty("dis_max")
			.GetProperty("queries")[1]
			.GetProperty("bool");
		var leaf = qualified
			.GetProperty("must")
			.GetProperty("dis_max")
			.GetProperty("queries")[0]
			.GetProperty("constant_score")
			.GetProperty("filter");
		leaf.GetProperty("term").GetProperty("api.properties.name").GetProperty("value").GetString().Should().Be("field");
		var path = qualified.GetProperty("filter").GetProperty("match_bool_prefix").GetProperty("api.properties.path.match");
		path.GetProperty("query").GetString().Should().Be("knn.field");
		path.GetProperty("operator").GetString().Should().Be("and");
	}

	[Test]
	[Arguments("GET /_search knn")]
	[Arguments("knn /_search")]
	[Arguments("knn GET /_search")]
	public void ApplyApiPropertyQuery_MarkedEndpointAndField_RequiresFieldMatchBeforeEndpointBoost(string input)
	{
		using var json = JsonDocument.Parse(
			Client.RequestResponseSerializer.SerializeToString(
				SearchQueryBuilder.ApplyApiPropertyQuery(new Elastic.Clients.Elasticsearch.QueryDsl.MatchNoneQuery(), input, "api")
			)
		);
		var fieldBranch = json.RootElement.GetProperty("bool").GetProperty("should")[1].GetProperty("bool");
		fieldBranch.GetProperty("must").GetProperty("nested").GetProperty("query").GetRawText().Should().Contain("\"value\":\"knn\"");
		var endpoint = fieldBranch.GetProperty("should").GetProperty("dis_max").GetProperty("queries")[0].GetProperty("constant_score");
		endpoint.GetProperty("filter").GetProperty("terms").GetProperty("api.endpoints")[0].GetString().Should().Be("_search");
	}

	[Test]
	[Arguments("track_total")]
	[Arguments("/_search knn")]
	public void ApplyApiPropertyQuery_SingleReading_IsNamedAfterItsInnerHits(string input)
	{
		using var json = JsonDocument.Parse(
			Client.RequestResponseSerializer.SerializeToString(
				SearchQueryBuilder.ApplyApiPropertyQuery(new Elastic.Clients.Elasticsearch.QueryDsl.MatchNoneQuery(), input, "api")
			)
		);
		var reading = json.RootElement.GetProperty("bool").GetProperty("should")[1].GetProperty("bool");
		reading.GetProperty("_name").GetString().Should().Be("api_properties");
		reading
			.GetProperty("must")
			.GetProperty("nested")
			.GetProperty("inner_hits")
			.GetProperty("name")
			.GetString()
			.Should()
			.Be("api_properties");
	}

	[Test]
	[Arguments("_search knn")]
	[Arguments("search knn")]
	[Arguments("knn search")]
	[Arguments("knn _search")]
	public void ApplyApiPropertyQuery_TwoPlainWords_TriesBothReadingsWithTheEndpointRequired(string input)
	{
		using var json = JsonDocument.Parse(
			Client.RequestResponseSerializer.SerializeToString(
				SearchQueryBuilder.ApplyApiPropertyQuery(new Elastic.Clients.Elasticsearch.QueryDsl.MatchNoneQuery(), input, "api")
			)
		);
		var readings = json.RootElement.GetProperty("bool").GetProperty("should")[1].GetProperty("bool").GetProperty("should");
		readings.GetArrayLength().Should().Be(2);
		var knn = readings.EnumerateArray().Single(r => r.GetRawText().Contains("\"value\":\"knn\"")).GetProperty("bool");
		var must = knn.GetProperty("must");
		must.GetArrayLength().Should().Be(2);
		must[0].GetProperty("nested").GetProperty("query").GetRawText().Should().Contain("\"value\":\"knn\"");
		must[1].GetRawText().Should().Contain("\"api.endpoints\":[\"_search\"");
		knn.TryGetProperty("should", out _).Should().BeFalse();
		readings.EnumerateArray().Should().AllSatisfy(r => r.GetProperty("bool").TryGetProperty("_name", out _).Should().BeTrue());
		var names = System
			.Text
			.RegularExpressions
			.Regex
			.Matches(readings.GetRawText(), "\"name\":\"([^\"]+)\"")
			.Select(m => m.Groups[1].Value)
			.ToArray();
		names
			.Should()
			.HaveCount(2)
			.And
			.OnlyHaveUniqueItems()
			.And
			.AllSatisfy(n => SearchQueryBuilder.IsApiPropertyHits(n).Should().BeTrue());
	}

	[Test]
	public void ApplyApiPropertyQuery_EndpointHalf_MatchesTheLiteralPathAndItsUnderscoreAlias()
	{
		using var json = JsonDocument.Parse(
			Client.RequestResponseSerializer.SerializeToString(
				SearchQueryBuilder.ApplyApiPropertyQuery(
					new Elastic.Clients.Elasticsearch.QueryDsl.MatchNoneQuery(),
					"deployments status",
					"api"
				)
			)
		);
		var readings = json.RootElement.GetProperty("bool").GetProperty("should")[1].GetProperty("bool").GetProperty("should");
		var status = readings.EnumerateArray().Single(r => r.GetRawText().Contains("\"value\":\"status\"")).GetProperty("bool");
		var endpoint = status.GetProperty("must")[1].GetRawText();
		endpoint.Should().Contain("\"deployments\"").And.Contain("\"_deployments\"");
	}

	[Test]
	public void ProcessHit_InnerHitsFromBothReadings_AreMerged()
	{
		var second =
			$$$""","api_properties_1":{"hits":{"total":{"value":1,"relation":"eq"},"max_score":4,"hits":[{{{InnerHit("req-b", 4, "b")}}}]}}""";
		var json = SearchResponseJson(InnerHit("req-a", 5, "a"), second, "\"matched_queries\":[\"api_properties\",\"api_properties_1\"],");

		var result = SearchResultProcessor.ProcessHit(ParseResponse(json).Hits.Single(), "a", new Dictionary<string, string[]>());

		result.Properties.Select(p => p.Anchor).Should().Equal("req-a", "req-b");
	}

	[Test]
	public void ProcessHit_InnerHitsOfAReadingTheDocumentDidNotMatch_AreDropped()
	{
		var second =
			$$$""","api_properties_1":{"hits":{"total":{"value":1,"relation":"eq"},"max_score":4,"hits":[{{{InnerHit("req-b", 4, "b")}}}]}}""";
		var json = SearchResponseJson(InnerHit("req-a", 5, "a"), second, "\"matched_queries\":[\"api_properties\"],");

		var result = SearchResultProcessor.ProcessHit(ParseResponse(json).Hits.Single(), "a", new Dictionary<string, string[]>());

		result.Properties.Select(p => p.Anchor).Should().Equal("req-a");
	}

	[Test]
	public void ProcessHit_HitThatMatchedNoReading_KeepsNoProperties()
	{
		var json = SearchResponseJson(InnerHit("req-index", 5, "index"), matchedQueries: "");

		var result = SearchResultProcessor.ProcessHit(ParseResponse(json).Hits.Single(), "bulk index", new Dictionary<string, string[]>());

		result.Properties.Should().BeEmpty();
	}

	[Test]
	[Arguments("how to search", null)]
	[Arguments("trav", "docs")]
	[Arguments("trav", null)]
	[Arguments("index", "all")]
	[Arguments("elasticsearch index_name settings", null)]
	[Arguments("/_search knn", null)]
	[Arguments("", "api")]
	[Arguments("/_search", "api")]
	[Arguments("GET /_search", "api")]
	public void BuildApiPropertyQuery_ExcludedQuery_ReturnsNull(string input, string? filter) =>
		SearchQueryBuilder.BuildApiPropertyQuery(input, filter).Should().BeNull();

	[Test]
	[Arguments(false)]
	[Arguments(true)]
	public void Mapping_ApiProperties_IsNestedWithKeywordNames(bool semantic)
	{
		var context = semantic
			? DocumentationMappingContext.DocumentationDocumentSemantic.CreateContext(type: "assembler", env: "test")
			: DocumentationMappingContext.DocumentationDocument.CreateContext(type: "assembler", env: "test");
		using var json = JsonDocument.Parse(context.GetMappingsJson());
		var field = json.RootElement.GetProperty("properties").GetProperty("api").GetProperty("properties").GetProperty("properties");
		field.GetProperty("type").GetString().Should().Be("nested");
		var fields = field.GetProperty("properties");
		fields.GetProperty("name").GetProperty("type").GetString().Should().Be("keyword");
		fields.GetProperty("anchor").GetProperty("type").GetString().Should().Be("keyword");

		var routes = json
			.RootElement
			.GetProperty("properties")
			.GetProperty("api")
			.GetProperty("properties")
			.GetProperty("routes")
			.GetProperty("properties");
		routes.GetProperty("path").GetProperty("type").GetString().Should().Be("keyword");
		routes.GetProperty("path").GetProperty("index").GetBoolean().Should().BeFalse();
		routes.GetProperty("methods").GetProperty("type").GetString().Should().Be("keyword");
	}

	[Test]
	public void ProcessHit_ManyInnerHits_KeepsTheThreeBestDistinctAnchorsInScoreOrder()
	{
		var innerHits = string.Join(
			",",
			InnerHit("req-b", 4, "b"),
			InnerHit("req-a", 5, "a"),
			InnerHit("req-d", 2, "d"),
			InnerHit("req-a", 4.5, "other.a"),
			InnerHit("req-c", 3, "c")
		);
		var result = SearchResultProcessor.ProcessHit(
			ParseResponse(SearchResponseJson(innerHits)).Hits.Single(),
			"a",
			new Dictionary<string, string[]>()
		);

		result.Properties.Select(p => p.Anchor).Should().Equal("req-a", "req-b", "req-c");
		result.Properties.Select(p => p.Path).Should().Equal("a", "b", "c");
	}

	private static string SearchResponseJson(
		string innerHits,
		string moreInnerHitGroups = "",
		string matchedQueries = "\"matched_queries\":[\"api_properties\"],"
	) =>
		"""
		{"took":1,"timed_out":false,"_shards":{"total":1,"successful":1,"failed":0},"hits":{"total":{"value":1,"relation":"eq"},"hits":[
		{"_index":"test","_id":"search","_score":1,__MATCHED__"_source":{"title":"Search","search_title":"Search","path":"/search"},
		"inner_hits":{"api_properties":{"hits":{"total":{"value":5,"relation":"eq"},"max_score":5,"hits":[__INNER_HITS__]}}__MORE_GROUPS__}}]}}
		"""
			.Replace("__INNER_HITS__", innerHits)
			.Replace("__MORE_GROUPS__", moreInnerHitGroups)
			.Replace("__MATCHED__", matchedQueries);

	private static string InnerHit(string anchor, double score, string path) =>
		$$$"""
		{"_index":"test","_id":"search","_score":{{{score.ToString(System.Globalization.CultureInfo.InvariantCulture)}}},"_nested":{"field":"api.properties","offset":0},
		"_source":{"name":"{{{path.Split('.')[^1]}}}","path":"{{{path}}}","type":"boolean","anchor":"{{{anchor}}}","location":"request","description":"Count hits"}}
		""";

	private static Elastic.Clients.Elasticsearch.SearchResponse<DocumentationDocument> ParseResponse(string json)
	{
		using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
		using var accessor = new ElasticsearchClientAccessor(
			new DocumentationEndpoints
			{
				Elasticsearch = new ElasticsearchEndpoint { Uri = new Uri("https://localhost:9200"), ApiKey = "test" }
			},
			new SearchConfiguration { Synonyms = [], Rules = [], DiminishTerms = [] }
		);
		return accessor
			.Client
			.RequestResponseSerializer
			.Deserialize<Elastic.Clients.Elasticsearch.SearchResponse<DocumentationDocument>>(stream);
	}

	[Test]
	public void ProcessHit_NestedPropertySource_ReturnsMatchingProperty()
	{
		var json = SearchResponseJson(InnerHit("req-track_total_hits", 5, "track_total_hits"));

		var result = SearchResultProcessor.ProcessHit(ParseResponse(json).Hits.Single(), "track_total", new Dictionary<string, string[]>());

		result.Properties.Should().ContainSingle().Which.Anchor.Should().Be("req-track_total_hits");
	}
}
