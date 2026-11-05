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

namespace Elastic.Documentation.Api.Tests;

public class ApiPropertySearchTests
{
	private static readonly ElasticsearchClient Client = new(new ElasticsearchClientSettings(new InMemoryRequestInvoker()));

	private static JsonDocument Json(Query? query) => JsonDocument.Parse(Client.RequestResponseSerializer.SerializeToString(query));

	[Test]
	[Arguments("track_total", null, 5)]
	[Arguments("knn_field search", null, 5)]
	public void BuildApiPropertyQuery_Identifier_ReturnsNestedInnerHits(string input, string? filter, int size)
	{
		using var json = Json(SearchQueryBuilder.BuildApiPropertyQuery(input, filter));
		var nested = json.RootElement.GetProperty("nested");
		nested.GetProperty("path").GetString().Should().Be("api.properties");
		nested.GetProperty("inner_hits").GetProperty("size").GetInt32().Should().Be(size);
	}

	[Test]
	[Arguments(null, false)]
	[Arguments("all", false)]
	public void BuildApiPropertyQuery_PrefixMatch_OnlyInTheApiFilteredLookup(string? filter, bool prefix)
	{
		using var json = Json(SearchQueryBuilder.BuildApiPropertyQuery("track_total", filter));
		var options = json.RootElement.GetProperty("nested").GetProperty("query").GetProperty("dis_max").GetProperty("queries");
		options.GetArrayLength().Should().Be(1);
		var name = options[0].TryGetProperty("dis_max", out var names)
			? names.GetProperty("queries").EnumerateArray().ToArray()
			: [options[0]];
		name.Should().HaveCount(prefix ? 2 : 1);
		name[0].GetRawText().Should().Contain("\"term\"");
	}

	[Test]
	[Arguments("track_total")]
	[Arguments("/_search track_total")]
	public void ApplyApiPropertyQuery_SingleReading_IsNamedAfterItsInnerHits(string input)
	{
		using var json = Json(SearchQueryBuilder.ApplyApiPropertyQuery(new MatchNoneQuery(), input, null));
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
	public void ApplyApiPropertyQuery_TwoPlainWordsDocsWide_KeepsTheEndpointRequiredForTheSurvivingReading()
	{
		using var json = Json(SearchQueryBuilder.ApplyApiPropertyQuery(new MatchNoneQuery(), "bulk track_total_hits", null));
		var reading = json.RootElement.GetProperty("bool").GetProperty("should")[1].GetProperty("bool");
		var must = reading.GetProperty("must");
		must.GetArrayLength().Should().Be(2);
		must[0].GetProperty("nested").GetProperty("query").GetRawText().Should().Contain("\"value\":\"track_total_hits\"");
		must[1].GetRawText().Should().Contain("_bulk");
		reading
			.TryGetProperty("should", out _)
			.Should()
			.BeFalse("the plain word names no field docs-wide, and the reading left must still hold its endpoint");
	}

	[Test]
	[Arguments("GET /_search track_total")]
	[Arguments("/_search track_total")]
	public void ApplyApiPropertyQuery_DocsWideMarkedEndpointAndField_RequiresFieldMatchBeforeEndpointBoost(string input)
	{
		using var json = Json(SearchQueryBuilder.ApplyApiPropertyQuery(new MatchNoneQuery(), input, null));
		var fieldBranch = json.RootElement.GetProperty("bool").GetProperty("should")[1].GetProperty("bool");
		fieldBranch
			.GetProperty("must")
			.GetProperty("nested")
			.GetProperty("query")
			.GetRawText()
			.Should()
			.Contain("\"value\":\"track_total\"");
		var endpoint = fieldBranch.GetProperty("should").GetProperty("dis_max").GetProperty("queries")[0].GetProperty("constant_score");
		endpoint.GetProperty("filter").GetProperty("terms").GetProperty("api.endpoints")[0].GetString().Should().Be("_search");
	}

	[Test]
	public void ApplyApiPropertyQuery_DocsWideTwoIdentifierWords_TriesBothReadingsWithUniqueInnerHitNames()
	{
		using var json = Json(SearchQueryBuilder.ApplyApiPropertyQuery(new MatchNoneQuery(), "knn_field track_total", null));
		var readings = json.RootElement.GetProperty("bool").GetProperty("should")[1].GetProperty("bool").GetProperty("should");
		readings.GetArrayLength().Should().Be(2);
		var trackTotal = readings.EnumerateArray().Single(r => r.GetRawText().Contains("\"value\":\"track_total\"")).GetProperty("bool");
		trackTotal.GetProperty("must")[1].GetRawText().Should().Contain("\"api.endpoints\":[\"_knn_field\"");
		var readingNames = readings.EnumerateArray().Select(r => r.GetProperty("bool").GetProperty("_name").GetString()).ToArray();
		var innerHitNames = readings
			.EnumerateArray()
			.Select(
				r => r
					.GetProperty("bool")
					.GetProperty("must")[0]
					.GetProperty("nested")
					.GetProperty("inner_hits")
					.GetProperty("name")
					.GetString()
			)
			.ToArray();
		readingNames.Should().OnlyHaveUniqueItems();
		innerHitNames.Should().OnlyHaveUniqueItems();
		readingNames.Concat(innerHitNames).Should().AllSatisfy(n => SearchQueryBuilder.IsApiPropertyHits(n).Should().BeTrue());
	}

	[Test]
	public void ApplyApiPropertyQuery_DocsWide_HasNoChildrenLift()
	{
		var json = Client.RequestResponseSerializer.SerializeToString(
			SearchQueryBuilder.ApplyApiPropertyQuery(new MatchNoneQuery(), "retriever.standard", null)
		);
		json.Should().Contain("\"api.properties.name\"").And.NotContain("api.properties.parent");
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
	[Arguments("", null)]
	[Arguments("/_search", null)]
	[Arguments("GET /_search", null)]
	public void BuildApiPropertyQuery_ExcludedQuery_ReturnsNull(string input, string? filter) =>
		SearchQueryBuilder.BuildApiPropertyQuery(input, filter).Should().BeNull();

	[Test]
	[Arguments(false)]
	[Arguments(true)]
	public void Mapping_ApiDeprecated_IsBoolean(bool semantic)
	{
		var api = ApiMapping(semantic);
		api.GetProperty("deprecated").GetProperty("type").GetString().Should().Be("boolean");
	}

	[Test]
	[Arguments(false)]
	[Arguments(true)]
	public void Mapping_ApiWordsAndGroup_AreSynonymText(bool semantic)
	{
		var api = ApiMapping(semantic);
		var words = api.GetProperty("words");
		words.GetProperty("type").GetString().Should().Be("text");
		words.GetProperty("analyzer").GetString().Should().Be("synonyms_fixed_analyzer");
		words.GetProperty("search_analyzer").GetString().Should().Be("synonyms_analyzer");
		var completion = words.GetProperty("fields").GetProperty("completion");
		completion.GetProperty("type").GetString().Should().Be("search_as_you_type");
		completion.GetProperty("analyzer").GetString().Should().Be("synonyms_fixed_analyzer");
		completion.GetProperty("search_analyzer").GetString().Should().Be("synonyms_analyzer");
		completion.TryGetProperty("index_options", out _).Should().BeFalse();

		var group = api.GetProperty("group");
		group.GetProperty("type").GetString().Should().Be("text");
		group.GetProperty("analyzer").GetString().Should().Be("synonyms_fixed_analyzer");
		group.GetProperty("search_analyzer").GetString().Should().Be("synonyms_analyzer");
	}

	[Test]
	[Arguments(false)]
	[Arguments(true)]
	public void Mapping_ApiIdWords_IsRankFeatureWithNegativeImpact(bool semantic)
	{
		var idWords = ApiMapping(semantic).GetProperty("id_words");
		idWords.GetProperty("type").GetString().Should().Be("rank_feature");
		idWords.GetProperty("positive_score_impact").GetBoolean().Should().BeFalse();
	}

	[Test]
	[Arguments(false)]
	[Arguments(true)]
	public void Mapping_ApiEndpointsBare_IsKeyword(bool semantic)
	{
		var api = ApiMapping(semantic);
		var endpoints = api.GetProperty("endpoints");
		endpoints.GetProperty("type").GetString().Should().Be("keyword");
		endpoints.TryGetProperty("fields", out _).Should().BeFalse();
		api.GetProperty("endpoints_bare").GetProperty("type").GetString().Should().Be("keyword");
	}

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
		fields.GetProperty("path_words").GetProperty("type").GetString().Should().Be("text");

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
	[Arguments(false)]
	[Arguments(true)]
	public void Mapping_ApiProperties_HasNoChildrenField(bool semantic)
	{
		var context = semantic
			? DocumentationMappingContext.DocumentationDocumentSemantic.CreateContext(type: "assembler", env: "test")
			: DocumentationMappingContext.DocumentationDocument.CreateContext(type: "assembler", env: "test");
		using var json = JsonDocument.Parse(context.GetMappingsJson());
		var fields = json
			.RootElement
			.GetProperty("properties")
			.GetProperty("api")
			.GetProperty("properties")
			.GetProperty("properties")
			.GetProperty("properties");
		fields.TryGetProperty("children", out _).Should().BeFalse();
	}

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
		Client
			.RequestResponseSerializer
			.SerializeToString(SearchQueryBuilder.BuildApiPropertyQuery("retriever.standard", null))
			.Should()
			.NotContain("api.properties.parent")
			.And
			.NotContain("_name");

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

	[Test]
	[Arguments(false)]
	[Arguments(true)]
	public void Mapping_ApiPropertiesParent_IsKeyword(bool semantic)
	{
		var context = semantic
			? DocumentationMappingContext.DocumentationDocumentSemantic.CreateContext(type: "assembler", env: "test")
			: DocumentationMappingContext.DocumentationDocument.CreateContext(type: "assembler", env: "test");
		using var json = JsonDocument.Parse(context.GetMappingsJson());
		var parent = json
			.RootElement
			.GetProperty("properties")
			.GetProperty("api")
			.GetProperty("properties")
			.GetProperty("properties")
			.GetProperty("properties")
			.GetProperty("parent");
		parent.GetProperty("type").GetString().Should().Be("keyword");
		parent.TryGetProperty("index", out _).Should().BeFalse();
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

	private static JsonElement ApiMapping(bool semantic)
	{
		var context = semantic
			? DocumentationMappingContext.DocumentationDocumentSemantic.CreateContext(type: "assembler", env: "test")
			: DocumentationMappingContext.DocumentationDocument.CreateContext(type: "assembler", env: "test");
		using var json = JsonDocument.Parse(context.GetMappingsJson());
		return json.RootElement.GetProperty("properties").GetProperty("api").GetProperty("properties").Clone();
	}
}
