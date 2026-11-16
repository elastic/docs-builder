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
	private static readonly HighlightOptions Expanded = ApiLookup with { ListChildren = true };

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

	[Test]
	public void ProcessHit_ChildrenInnerHits_FollowTheMatchedRowsInPageOrder()
	{
		var innerHits = string.Join(
			",",
			Matched(InnerHit("req-c7", 0.4, "a.c7", parent: "a"), "api_children", offset: 7),
			Matched(InnerHit("req-a", 1, "a"), "api_properties", offset: 1),
			Matched(InnerHit("req-c3", 0.4, "a.c3", parent: "a"), "api_children", offset: 3),
			Matched(InnerHit("req-c5", 0.4, "a.c5", parent: "a"), "api_children", offset: 5)
		);

		var result = SearchResultProcessor.ProcessHit(
			ParseResponse(FieldRows(innerHits)).Hits.Single(),
			"a",
			new Dictionary<string, string[]>(),
			Expanded
		);

		result.Properties.Select(p => p.Path).Should().Equal("a", "a.c3", "a.c5", "a.c7");
	}

	[Test]
	public void ProcessHit_ChildrenFollowTheirParentRow()
	{
		var innerHits = string.Join(
			",",
			InnerHit("req-b", 1, "b"),
			InnerHit("req-a", 1, "a"),
			InnerHit("req-a-x-a", 1, "a.x.a", parent: "a.x"),
			Matched(InnerHit("req-a-c1", 0.4, "a.c1", parent: "a"), "api_children", offset: 1),
			Matched(InnerHit("req-a-c2", 0.4, "a.c2", parent: "a"), "api_children", offset: 2)
		);

		var result = SearchResultProcessor.ProcessHit(
			ParseResponse(FieldRows(innerHits)).Hits.Single(),
			"a",
			new Dictionary<string, string[]>(),
			Expanded
		);

		result.Properties.Select(p => p.Path).Should().Equal("a", "a.c1", "a.c2", "b", "a.x.a");
	}

	[Test]
	public void ProcessHit_ParameterAndBodyFieldOfTheSameName_ChildrenFollowTheBodyRow()
	{
		var innerHits = string.Join(
			",",
			InnerHit("query-size", 2, "size", location: "query"),
			InnerHit("req-size", 1, "size"),
			Matched(InnerHit("req-size-a", 0.4, "size.a", parent: "size"), "api_children", offset: 1),
			Matched(InnerHit("req-size-b", 0.4, "size.b", parent: "size"), "api_children", offset: 2)
		);

		var result = SearchResultProcessor.ProcessHit(
			ParseResponse(FieldRows(innerHits)).Hits.Single(),
			"size.",
			new Dictionary<string, string[]>(),
			Expanded
		);

		result.Properties.Select(p => p.Anchor).Should().Equal("req-size", "req-size-a", "req-size-b", "query-size");
	}

	[Test]
	public void ProcessHit_RequestAndResponseChildrenOfTheSameParentKey_EachFollowTheirOwnRow()
	{
		var innerHits = string.Join(
			",",
			InnerHit("req-hits", 1, "hits"),
			InnerHit("res-hits", 1, "hits", location: "response"),
			Matched(InnerHit("res-hits-total", 0.4, "hits.total", parent: "hits", location: "response"), "api_children", offset: 10),
			Matched(InnerHit("req-hits-a", 0.4, "hits.a", parent: "hits"), "api_children", offset: 1)
		);

		var result = SearchResultProcessor.ProcessHit(
			ParseResponse(FieldRows(innerHits)).Hits.Single(),
			"hits.",
			new Dictionary<string, string[]>(),
			Expanded
		);

		result.Properties.Select(p => p.Anchor).Should().Equal("req-hits", "req-hits-a", "res-hits", "res-hits-total");
	}

	[Test]
	public void ProcessHit_ParentOutsideTheTopThree_IsStillListedFirst()
	{
		var innerHits = string.Join(
			",",
			InnerHit("req-w", 4, "w"),
			InnerHit("req-x", 3, "x"),
			InnerHit("req-y", 2, "y"),
			InnerHit("req-a", 1, "a"),
			Matched(InnerHit("req-a-c1", 0.4, "a.c1", parent: "a"), "api_children", offset: 1),
			Matched(InnerHit("req-a-c2", 0.4, "a.c2", parent: "a"), "api_children", offset: 2)
		);

		var result = SearchResultProcessor.ProcessHit(
			ParseResponse(FieldRows(innerHits)).Hits.Single(),
			"a",
			new Dictionary<string, string[]>(),
			Expanded
		);

		result.Properties.Select(p => p.Path).Should().Equal("a", "a.c1", "a.c2", "w", "x");
	}

	[Test]
	public void ProcessHit_ArrayBodyParent_MatchesItsTypedPath()
	{
		var innerHits = string.Join(
			",",
			InnerHit("req-retriever", 2, "[].retriever"),
			InnerHit("req-retriever-rule-retriever", 1, "[].retriever.rule.retriever", parent: "retriever.rule"),
			Matched(InnerHit("req-retriever-standard", 0.4, "[].retriever.standard", parent: "retriever"), "api_children", offset: 1)
		);

		var result = SearchResultProcessor.ProcessHit(
			ParseResponse(FieldRows(innerHits)).Hits.Single(),
			"retriever",
			new Dictionary<string, string[]>(),
			Expanded
		);

		result.Properties.Select(p => p.Path).Should().Equal("[].retriever", "[].retriever.standard", "[].retriever.rule.retriever");
	}

	[Test]
	public void ProcessHit_ChildrenWithoutTheirParent_FollowTheNameMatches()
	{
		var innerHits = string.Join(
			",",
			InnerHit("req-a", 2, "a"),
			InnerHit("req-b", 1, "b"),
			Matched(InnerHit("req-z-c1", 0.4, "z.c1", parent: "z"), "api_children", offset: 1),
			Matched(InnerHit("req-z-c2", 0.4, "z.c2", parent: "z"), "api_children", offset: 2)
		);

		var result = SearchResultProcessor.ProcessHit(
			ParseResponse(FieldRows(innerHits)).Hits.Single(),
			"z",
			new Dictionary<string, string[]>(),
			Expanded
		);

		result.Properties.Select(p => p.Path).Should().Equal("a", "b", "z.c1", "z.c2");
	}

	[Test]
	public void ProcessHit_WithoutExpansion_ListsNoChildren()
	{
		var innerHits = string.Join(
			",",
			Matched(InnerHit("req-a", 2, "a"), "api_name", offset: 0),
			Matched(InnerHit("req-b", 1, "b"), "api_name", offset: 4),
			Matched(InnerHit("req-a-c1", 0.4, "a.c1", parent: "a"), "api_children", offset: 1),
			Matched(InnerHit("req-a-c2", 0.4, "a.c2", parent: "a"), "api_children", offset: 2)
		);

		var result = SearchResultProcessor.ProcessHit(
			ParseResponse(FieldRows(innerHits)).Hits.Single(),
			"a",
			new Dictionary<string, string[]>(),
			ApiLookup
		);

		result.Properties.Select(p => p.Path).Should().Equal("a", "b");
	}

	[Test]
	public void ProcessHit_ExpandedChildren_KeepsAllUpToTheWindow()
	{
		var innerHits = string.Join(
			",",
			Enumerable
				.Range(0, 25)
				.Select(i => Matched(InnerHit($"req-c{i}", 0.4, $"a.c{i}", parent: "a"), "api_children", offset: i))
				.Prepend(Matched(InnerHit("req-a", 1, "a"), "api_name", offset: 0))
		);

		var result = SearchResultProcessor.ProcessHit(
			ParseResponse(FieldRows(innerHits)).Hits.Single(),
			"a.",
			new Dictionary<string, string[]>(),
			Expanded
		);

		result.Properties.Select(p => p.Path).Should().Equal(Enumerable.Range(0, 25).Select(i => $"a.c{i}").Prepend("a"));
	}

	[Test]
	public void ProcessHit_RowMatchedByNameAndAsChild_IsANameMatch()
	{
		var innerHits = string.Join(
			",",
			Matched(InnerHit("req-aggs", 1, "aggs"), "api_name", offset: 0),
			Matched(InnerHit("req-aggs-string-aggs", 1, "aggs.<string>.aggs"), "api_name\",\"api_children", offset: 2),
			Matched(InnerHit("req-aggs-string-terms", 0.4, "aggs.<string>.terms", parent: "aggs"), "api_children", offset: 1)
		);

		var result = SearchResultProcessor.ProcessHit(
			ParseResponse(FieldRows(innerHits)).Hits.Single(),
			"aggs",
			new Dictionary<string, string[]>(),
			Expanded
		);

		result.Properties.Select(p => p.Path).Should().Equal("aggs", "aggs.<string>.terms", "aggs.<string>.aggs");
	}

	/// <summary>Rows of the first word's field option, with that word resolved to the field role.</summary>
	private static string FieldRows(string innerHits) => SearchResponseJson("""{"field_0":1.0}""", ("api_properties_0", innerHits));

	private static string Matched(string innerHit, string matchedQuery, int offset) =>
		innerHit.Replace(
			"\"offset\":0},",
			$$"""
			"offset":{{offset}}},"matched_queries":["{{matchedQuery}}"],
			"""
		);

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

	private static string InnerHit(string anchor, double score, string path, string parent = "", string location = "request") =>
		$$$"""
		{"_index":"test","_id":"search","_score":{{{score.ToString(CultureInfo.InvariantCulture)}}},"_nested":{"field":"api.properties","offset":0},
		"_source":{"name":"{{{path}}}","path":"{{{path}}}","type":"boolean","anchor":"{{{anchor}}}","location":"{{{location}}}","description":"","parent":"{{{parent}}}"}}
		""";

	private static Clients.Elasticsearch.SearchResponse<DocumentationDocument> ParseResponse(string json)
	{
		using var stream = new ReadOnlyMemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
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
