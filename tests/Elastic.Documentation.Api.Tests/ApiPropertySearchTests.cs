// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using AwesomeAssertions;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Search;
using Elastic.Documentation.Search;
using Elastic.Documentation.Search.Common;
using Elastic.Documentation.Search.Contract;
using Elastic.Documentation.Search.Highlighting;
using Elastic.Transport;
using Elastic.Transport.Extensions;
using Nullean.Snapshots.Json;

namespace Elastic.Documentation.Api.Tests;

public class ApiPropertySearchTests
{
	private static readonly ElasticsearchClient Client = new(new ElasticsearchClientSettings(new InMemoryRequestInvoker()));

	private static string Json(Query? query) => Client.RequestResponseSerializer.SerializeToString(query);

	[Test]
	[Arguments("track_total", null)]
	[Arguments("knn_field search", null)]
	public void BuildApiPropertyQuery_Identifier_ReturnsNestedInnerHits(string input, string? filter) =>
		Json(SearchQueryBuilder.BuildApiPropertyQuery(input, filter)).Should().MatchJsonSnapshot();

	[Test]
	[Arguments("retriever.rrf.min_score")]
	[Arguments("rrf.min_score")]
	[Arguments("[].retriever.standard")]
	[Arguments("labels.<string>.value")]
	public void BuildApiPropertyQuery_QualifiedPath_RequiresMatchingLeafAndPathWords(string input) =>
		Json(SearchQueryBuilder.BuildApiPropertyQuery(input, null)).Should().MatchJsonSnapshot();

	[Test]
	public void BuildApiPropertyQuery_MarkedSingleWord_MatchesTheBareName() =>
		Json(SearchQueryBuilder.BuildApiPropertyQuery("[].retriever", null)).Should().MatchJsonSnapshot();

	[Test]
	[Arguments(null)]
	[Arguments("all")]
	public void BuildApiPropertyQuery_PrefixMatch_OnlyInTheApiFilteredLookup(string? filter) =>
		Json(SearchQueryBuilder.BuildApiPropertyQuery("track_total", filter)).Should().MatchJsonSnapshot();

	[Test]
	[Arguments("track_total")]
	[Arguments("/_search track_total")]
	public void ApplyApiPropertyQuery_SingleReading_IsNamedAfterItsInnerHits(string input) =>
		Json(SearchQueryBuilder.ApplyApiPropertyQuery(new MatchNoneQuery(), input, null)).Should().MatchJsonSnapshot();

	[Test]
	public void ApplyApiPropertyQuery_TwoPlainWordsDocsWide_KeepsTheEndpointRequiredForTheSurvivingReading() =>
		Json(SearchQueryBuilder.ApplyApiPropertyQuery(new MatchNoneQuery(), "bulk track_total_hits", null)).Should().MatchJsonSnapshot();

	[Test]
	[Arguments("GET /_search track_total")]
	[Arguments("/_search track_total")]
	public void ApplyApiPropertyQuery_DocsWideMarkedEndpointAndField_RequiresFieldMatchBeforeEndpointBoost(string input) =>
		Json(SearchQueryBuilder.ApplyApiPropertyQuery(new MatchNoneQuery(), input, null)).Should().MatchJsonSnapshot();

	[Test]
	public void ApplyApiPropertyQuery_DocsWideTwoIdentifierWords_TriesBothReadingsWithUniqueInnerHitNames() =>
		Json(SearchQueryBuilder.ApplyApiPropertyQuery(new MatchNoneQuery(), "knn_field track_total", null)).Should().MatchJsonSnapshot();

	[Test]
	public void ApplyApiPropertyQuery_DocsWide_HasNoChildrenLift() =>
		Json(SearchQueryBuilder.ApplyApiPropertyQuery(new MatchNoneQuery(), "retriever.standard", null)).Should().MatchJsonSnapshot();

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
	[Arguments("", null)]
	[Arguments("/_search", null)]
	[Arguments("GET /_search", null)]
	public void BuildApiPropertyQuery_ExcludedQuery_ReturnsNull(string input, string? filter) =>
		SearchQueryBuilder.BuildApiPropertyQuery(input, filter).Should().BeNull();

	/// <summary>
	/// The api.* mapping, for the lexical and the semantic variant: boolean deprecated, synonym text words and group,
	/// negative-impact rank_feature id_words, plain keyword endpoints, nested properties with keyword names and parent
	/// (and no children field), and unindexed keyword routes.
	/// </summary>
	[Test]
	[Arguments(false)]
	[Arguments(true)]
	public void Mapping_Api(bool semantic) => ApiMapping(semantic).GetRawText().Should().MatchJsonSnapshot();

	[Test]
	public void BuildApiPropertyQuery_OnlyADot_ReturnsNull() => SearchQueryBuilder.BuildApiPropertyQuery(".", null).Should().BeNull();

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

	[Test]
	public void BuildApiPropertyQuery_DocsWide_HasNoChildrenOption() =>
		Json(SearchQueryBuilder.BuildApiPropertyQuery("retriever.standard", null)).Should().MatchJsonSnapshot();

	[Test]
	public void ProcessHit_InnerHitsWithoutMatchedQueries_AreAllNameMatches()
	{
		var innerHits = string.Join(",", InnerHit("req-b", 4, "b"), InnerHit("req-a", 5, "a"), InnerHit("req-c", 3, "c"));

		var result = SearchResultProcessor.ProcessHit(
			ParseResponse(SearchResponseJson(innerHits)).Hits.Single(),
			"a",
			new Dictionary<string, string[]>()
		);

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

	private static string InnerHit(string anchor, double score, string path, string parent = "", string location = "request") =>
		$$$"""
		{"_index":"test","_id":"search","_score":{{{score.ToString(System.Globalization.CultureInfo.InvariantCulture)}}},"_nested":{"field":"api.properties","offset":0},
		"_source":{"name":"{{{path.Split('.')[^1]}}}","path":"{{{path}}}","type":"boolean","anchor":"{{{anchor}}}","location":"{{{location}}}","description":"Count hits","parent":"{{{parent}}}"}}
		""";

	private static Elastic.Clients.Elasticsearch.SearchResponse<DocumentationDocument> ParseResponse(string json)
	{
		using var stream = new ReadOnlyMemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
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

	private static JsonElement ApiMapping(bool semantic)
	{
		var context = semantic
			? DocumentationMappingContext.DocumentationDocumentSemantic.CreateContext(type: "assembler", env: "test")
			: DocumentationMappingContext.DocumentationDocument.CreateContext(type: "assembler", env: "test");
		using var json = JsonDocument.Parse(context.GetMappingsJson());
		return json.RootElement.GetProperty("properties").GetProperty("api").GetProperty("properties").Clone();
	}
}
