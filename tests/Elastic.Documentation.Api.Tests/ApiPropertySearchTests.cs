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
	[Arguments("trav", null)]
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
	[Arguments("_search knn")]
	[Arguments("GET /_search knn")]
	[Arguments("search knn")]
	public void ApplyApiPropertyQuery_OperationAndField_RequiresFieldMatchBeforeEndpointBoost(string input)
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
	[Arguments("how to search", null)]
	[Arguments("trav", "docs")]
	[Arguments("", "api")]
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
	}

	[Test]
	public void ProcessHit_NestedPropertySource_ReturnsMatchingProperty()
	{
		const string json =
			"""
		{"took":1,"timed_out":false,"_shards":{"total":1,"successful":1,"failed":0},"hits":{"total":{"value":1,"relation":"eq"},"hits":[
		{"_index":"test","_id":"search","_score":1,"_source":{"title":"Search","search_title":"Search","path":"/search"},
		"inner_hits":{"api_properties":{"hits":{"total":{"value":1,"relation":"eq"},"max_score":5,"hits":[
		{"_index":"test","_id":"search","_score":5,"_nested":{"field":"api.properties","offset":0},
		"_source":{"name":"track_total_hits","path":"track_total_hits","type":"boolean","anchor":"req-track_total_hits","location":"request","description":"Count hits"}}
		]}}}}]}}
		""";
		using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
		using var accessor = new ElasticsearchClientAccessor(
			new DocumentationEndpoints
			{
				Elasticsearch = new ElasticsearchEndpoint { Uri = new Uri("https://localhost:9200"), ApiKey = "test" }
			},
			new SearchConfiguration { Synonyms = [], Rules = [], DiminishTerms = [] }
		);
		var response = accessor
			.Client
			.RequestResponseSerializer
			.Deserialize<Elastic.Clients.Elasticsearch.SearchResponse<DocumentationDocument>>(stream);
		var result = SearchResultProcessor.ProcessHit(response.Hits.Single(), "track_total", new Dictionary<string, string[]>());
		result.Properties.Should().ContainSingle().Which.Anchor.Should().Be("req-track_total_hits");
	}
}
