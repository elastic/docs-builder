// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using AwesomeAssertions;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Elastic.Documentation.Search;
using Elastic.Transport;
using Elastic.Transport.Extensions;

namespace Elastic.Documentation.Api.Tests;

public class ApiLookupQueryBuilderTests
{
	private static readonly ElasticsearchClient Client = new(new ElasticsearchClientSettings(new InMemoryRequestInvoker()));

	private static JsonDocument Json(Query query) => JsonDocument.Parse(Client.RequestResponseSerializer.SerializeToString(query));

	/// <summary>The client writes a one-clause list as a bare object.</summary>
	private static JsonElement[] Clauses(JsonElement clauses) =>
		clauses.ValueKind == JsonValueKind.Array ? [.. clauses.EnumerateArray()] : [clauses];

	private static JsonElement TokensBool(JsonDocument json) =>
		Clauses(json.RootElement.GetProperty("bool").GetProperty("must"))[0].GetProperty("bool");

	private static JsonElement TokenClause(JsonDocument json, int index) => Clauses(TokensBool(json).GetProperty("should"))[index];

	/// <summary>Each option is one query object, such as <c>{"constant_score": {...}}</c>, carrying its <c>_name</c>.</summary>
	private static Dictionary<string, JsonElement> OptionsByName(JsonElement tokenClause) =>
		tokenClause
			.GetProperty("dis_max")
			.GetProperty("queries")
			.EnumerateArray()
			.Select(o => o.EnumerateObject().Single().Value)
			.ToDictionary(o => o.GetProperty("_name").GetString()!);

	[Test]
	public void Tokenize_TwoPlainWords_ReturnsTwoPlainTokens()
	{
		var tokens = ApiLookupQueryBuilder.Tokenize("search retriever");

		tokens.Should().Equal(
			new ApiLookupToken(ApiLookupTokenKind.Plain, "search", ExpandChildren: false),
			new ApiLookupToken(ApiLookupTokenKind.Plain, "retriever", ExpandChildren: false)
		);
	}

	[Test]
	public void Tokenize_MethodPathAndWord_FoldsTheMethodIntoThePath()
	{
		var tokens = ApiLookupQueryBuilder.Tokenize("GET /_search knn");

		tokens.Should().Equal(
			new ApiLookupToken(ApiLookupTokenKind.Path, "GET /_search", ExpandChildren: false, Method: "GET"),
			new ApiLookupToken(ApiLookupTokenKind.Plain, "knn", ExpandChildren: false)
		);
	}

	[Test]
	public void Build_MethodBeforePath_KeysTheEndpointAndLiftsTheMethod()
	{
		using var json = Json(ApiLookupQueryBuilder.Build("post /_count"));

		Clauses(TokensBool(json).GetProperty("should")).Should().ContainSingle();
		var key = TokenClause(json, 0).GetProperty("dis_max");
		key.GetProperty("_name").GetString().Should().Be("key_0");
		key.GetRawText().Should().Contain("\"_count\"");
		var methodLift = Clauses(json.RootElement.GetProperty("bool").GetProperty("should"))
			.Where(q => q.TryGetProperty("term", out var term) && term.TryGetProperty("api.method", out _))
			.Select(q => q.GetProperty("term").GetProperty("api.method"))
			.Should()
			.ContainSingle()
			.Subject;
		methodLift.GetProperty("value").GetString().Should().Be("POST");
		methodLift.GetProperty("boost").GetSingle().Should().Be(0.5f);
	}

	[Test]
	public void Tokenize_TrailingDot_ExpandsChildrenOfTheStrippedPath() =>
		ApiLookupQueryBuilder
			.Tokenize("retriever.standard.")
			.Should()
			.Equal(new ApiLookupToken(ApiLookupTokenKind.Plain, "retriever.standard", ExpandChildren: true));

	[Test]
	public void Tokenize_ExtraWhitespaceAndDotOnlyWords_AreDropped() =>
		ApiLookupQueryBuilder
			.Tokenize("  cluster   ..  health ")
			.Should()
			.Equal(
				new ApiLookupToken(ApiLookupTokenKind.Plain, "cluster", ExpandChildren: false),
				new ApiLookupToken(ApiLookupTokenKind.Plain, "health", ExpandChildren: false)
			);

	[Test]
	public void Tokenize_TwelveWords_KeepsTheFirstEight() =>
		ApiLookupQueryBuilder.Tokenize("a b c d e f g h i j k l").Select(t => t.Text).Should().Equal("a", "b", "c", "d", "e", "f", "g", "h");

	[Test]
	[Arguments("")]
	[Arguments("   ")]
	[Arguments("...")]
	public void Build_NoTokens_MatchesNothing(string query)
	{
		using var json = Json(ApiLookupQueryBuilder.Build(query));

		json.RootElement.TryGetProperty("match_none", out _).Should().BeTrue();
	}

	[Test]
	public void Build_PlainWord_OffersEveryRoleButMethodWithItsWeight()
	{
		using var json = Json(ApiLookupQueryBuilder.Build("cluster flat_settings"));
		var options = OptionsByName(TokenClause(json, 0));

		options.Keys.Should().BeEquivalentTo("key_0", "id_0", "group_0", "title_0", "field_0", "body_0");
		options["id_0"].GetProperty("boost").GetSingle().Should().Be(2.0f);
		options["group_0"].GetProperty("boost").GetSingle().Should().Be(1.0f);
		options["title_0"].GetProperty("boost").GetSingle().Should().Be(1.0f);
		options["body_0"].GetProperty("boost").GetSingle().Should().Be(0.1f);
		TokenClause(json, 0).GetProperty("dis_max").GetProperty("tie_breaker").GetDouble().Should().Be(0);
	}

	[Test]
	public void Build_PlainWord_KeyOptionWeighsTheEndpointAtSix()
	{
		using var json = Json(ApiLookupQueryBuilder.Build("search"));
		var key = OptionsByName(TokenClause(json, 0))["key_0"];

		var boosts = key
			.GetProperty("queries")
			.EnumerateArray()
			.Where(endpoint => endpoint.TryGetProperty("dis_max", out _))
			.SelectMany(endpoint => endpoint.GetProperty("dis_max").GetProperty("queries").EnumerateArray())
			.Select(option => option.GetProperty("constant_score").GetProperty("boost").GetSingle());
		boosts.Should().AllSatisfy(b => b.Should().Be(6.0f));
		key.GetRawText().Should().Contain("\"_search\"");
	}

	[Test]
	[Arguments("snapshot")]
	[Arguments("_snapshot")]
	[Arguments("Snapshot")]
	public void Build_PlainWord_KeyOptionCoversTheRoutesUnderTheEndpointAtFiveAndAHalf(string word)
	{
		using var json = Json(ApiLookupQueryBuilder.Build(word));
		var queries = OptionsByName(TokenClause(json, 0))["key_0"].GetProperty("queries").EnumerateArray().ToArray();

		queries.Count(q => q.TryGetProperty("dis_max", out _)).Should().Be(word.StartsWith('_') ? 1 : 2);
		var ns = queries.Single(q => q.TryGetProperty("constant_score", out _)).GetProperty("constant_score");
		ns.GetProperty("boost").GetSingle().Should().Be(5.5f);
		ns
			.GetProperty("filter")
			.GetProperty("prefix")
			.GetProperty("api.endpoints")
			.GetProperty("value")
			.GetString()
			.Should()
			.Be("_snapshot/");
		ns.TryGetProperty("_name", out _).Should().BeFalse();
	}

	[Test]
	[Arguments("/_snapshot")]
	[Arguments("_snapshot/_status")]
	public void Build_PathOrSlashedWord_KeyOptionHasNoNamespacePrefix(string word)
	{
		using var json = Json(ApiLookupQueryBuilder.Build(word));

		json.RootElement.GetRawText().Should().NotContain("\"prefix\":{\"api.endpoints\"");
	}

	[Test]
	public void Build_PlainWord_IdTitleGroupAndBodyReadTheirFields()
	{
		using var json = Json(ApiLookupQueryBuilder.Build("cluster"));
		var options = OptionsByName(TokenClause(json, 0));

		var id = options["id_0"].GetProperty("filter").GetProperty("multi_match");
		id.GetProperty("type").GetString().Should().Be("bool_prefix");
		id.GetProperty("operator").GetString().Should().Be("and");
		id.GetProperty("analyzer").GetString().Should().Be("synonyms_analyzer");
		id
			.GetProperty("fields")
			.EnumerateArray()
			.Select(f => f.GetString())
			.Should()
			.Equal("api.words.completion", "api.words.completion._2gram", "api.words.completion._3gram");
		options["title_0"]
			.GetProperty("filter")
			.GetProperty("multi_match")
			.GetProperty("fields")
			.EnumerateArray()
			.Select(f => f.GetString())
			.Should()
			.Equal("search_title.completion", "search_title.completion._2gram", "search_title.completion._3gram");
		options["group_0"]
			.GetProperty("filter")
			.GetProperty("match")
			.GetProperty("api.group")
			.GetProperty("query")
			.GetString()
			.Should()
			.Be("cluster");
		options["body_0"]
			.GetProperty("filter")
			.GetProperty("match")
			.GetProperty("body")
			.GetProperty("analyzer")
			.GetString()
			.Should()
			.Be("synonyms_analyzer");
	}

	[Test]
	public void Build_MethodWord_AddsTheMethodOptionInUpperCase()
	{
		using var json = Json(ApiLookupQueryBuilder.Build("get retriever"));
		var options = OptionsByName(TokenClause(json, 0));

		options.Keys.Should().Contain(["method_0", "id_0", "title_0", "body_0"]);
		options["method_0"].GetProperty("boost").GetSingle().Should().Be(0.5f);
		options["method_0"]
			.GetProperty("filter")
			.GetProperty("term")
			.GetProperty("api.method")
			.GetProperty("value")
			.GetString()
			.Should()
			.Be("GET");
		OptionsByName(TokenClause(json, 1)).Keys.Should().NotContain("method_1");
	}

	[Test]
	public void Build_PlainWord_FieldOptionIsNamedNestedWithItsOwnInnerHits()
	{
		using var json = Json(ApiLookupQueryBuilder.Build("cluster flat_settings"));
		var field = OptionsByName(TokenClause(json, 1))["field_1"];

		field.GetProperty("path").GetString().Should().Be("api.properties");
		field.GetProperty("score_mode").GetString().Should().Be("max");
		field.GetProperty("ignore_unmapped").GetBoolean().Should().BeTrue();
		var innerHits = field.GetProperty("inner_hits");
		innerHits.GetProperty("name").GetString().Should().Be("api_properties_1");
		innerHits.GetProperty("size").GetInt32().Should().Be(20);
		var names = field
			.GetProperty("query")
			.GetProperty("dis_max")
			.GetProperty("queries")[0]
			.GetProperty("dis_max")
			.GetProperty("queries");
		names[0].GetProperty("constant_score").GetProperty("boost").GetSingle().Should().Be(1.0f);
		names[1].GetProperty("constant_score").GetProperty("boost").GetSingle().Should().Be(0.6f);
		names[1].GetProperty("constant_score").GetProperty("_name").GetString().Should().Be("api_name");
	}

	[Test]
	public void Build_TrailingDot_AddsTheChildrenOptionAndWidensInnerHits()
	{
		using var json = Json(ApiLookupQueryBuilder.Build("retriever.standard."));
		var field = OptionsByName(TokenClause(json, 0))["field_0"];

		field.GetProperty("inner_hits").GetProperty("size").GetInt32().Should().Be(100);
		var children = field
			.GetProperty("query")
			.GetProperty("dis_max")
			.GetProperty("queries")
			.EnumerateArray()
			.Select(o => o.EnumerateObject().Single().Value)
			.Single(o => o.TryGetProperty("_name", out var name) && name.GetString() == "api_children");
		children.GetProperty("boost").GetSingle().Should().Be(0.4f);
		children
			.GetProperty("filter")
			.GetProperty("term")
			.GetProperty("api.properties.parent")
			.GetProperty("value")
			.GetString()
			.Should()
			.Be("retriever.standard");
		ApiLookupQueryBuilder.ExpandsChildren("retriever.standard.").Should().BeTrue();
		ApiLookupQueryBuilder.ExpandsChildren("retriever.standard").Should().BeFalse();
	}

	[Test]
	public void Build_PathToken_OffersOnlyTheEndpointKey()
	{
		using var json = Json(ApiLookupQueryBuilder.Build("GET /_search knn"));
		var path = TokenClause(json, 0).GetProperty("dis_max");

		path.GetProperty("_name").GetString().Should().Be("key_0");
		path.GetRawText().Should().Contain("\"_search\"").And.NotContain("api.properties").And.NotContain("search_title");
	}

	[Test]
	public void Build_Tokens_AllMustMatchUpToTwoThenThreeQuarters()
	{
		using var json = Json(ApiLookupQueryBuilder.Build("cluster flat_settings"));

		TokensBool(json).GetProperty("minimum_should_match").GetString().Should().Be("2<75%");
		Clauses(TokensBool(json).GetProperty("should")).Should().HaveCount(2);
	}

	[Test]
	public void Build_WholeQuery_LiftsATitleOfEveryWordAtThree()
	{
		using var json = Json(ApiLookupQueryBuilder.Build("get cluster settings"));
		var lift = json
			.RootElement
			.GetProperty("bool")
			.GetProperty("should")
			.EnumerateArray()
			.Single(
				l => l.TryGetProperty("constant_score", out var cs) && cs.TryGetProperty("_name", out var n) && n.GetString() == "title_all"
			)
			.GetProperty("constant_score");

		lift.GetProperty("boost").GetSingle().Should().Be(3.0f);
		lift.GetProperty("filter").GetProperty("multi_match").GetProperty("query").GetString().Should().Be("get cluster settings");
	}

	[Test]
	public void Build_Lifts_FavourFewerIdWordsAndOperationsNotDeprecated()
	{
		using var json = Json(ApiLookupQueryBuilder.Build("esql query"));
		var lifts = json.RootElement.GetProperty("bool").GetProperty("should").GetRawText();

		lifts.Should().Contain("\"rank_feature\":{\"boost\":0.5,\"field\":\"api.id_words\"}");
		lifts.Should().Contain("\"api.deprecated\":{\"boost\":0.2,\"value\":false}");
		Clauses(json.RootElement.GetProperty("bool").GetProperty("filter")).Should().ContainSingle();
	}

	[Test]
	public void ApiLookupWeights_EndpointAndFieldBeatsWholeTitleOperationAndTitleWords() =>
		(ApiLookupWeights.EndpointKey + ApiLookupWeights.FieldName).Should().BeGreaterThan(
			ApiLookupWeights.WholeTitle + ApiLookupWeights.OperationWord + ApiLookupWeights.TitleWord
		);

	[Test]
	public void ApiLookupWeights_EndpointKeyBeatsNamespaceKey() =>
		ApiLookupWeights.EndpointKey.Should().BeGreaterThan(ApiLookupWeights.NamespaceKey);

	[Test]
	public void ApiLookupWeights_NamespaceAndOperationWordBeatEndpointAndGroupWord() =>
		(ApiLookupWeights.NamespaceKey + ApiLookupWeights.OperationWord).Should().BeGreaterThan(
			ApiLookupWeights.EndpointKey + ApiLookupWeights.GroupWord
		);

	[Test]
	public void ApiLookupWeights_OperationWordBeatsGroupWord() =>
		ApiLookupWeights.OperationWord.Should().BeGreaterThan(ApiLookupWeights.GroupWord);

	[Test]
	public void ApiLookupWeights_OperationWordBeatsFieldName() =>
		ApiLookupWeights.OperationWord.Should().BeGreaterThan(ApiLookupWeights.FieldName);

	[Test]
	public void FieldHitsName_RoundTripsThroughTryTokenIndex()
	{
		ApiLookupQueryBuilder.FieldHitsName(3).Should().Be("api_properties_3");
		ApiLookupQueryBuilder.TryTokenIndex("api_properties_3", out var index).Should().BeTrue();
		index.Should().Be(3);
		ApiLookupQueryBuilder.TryTokenIndex("api_properties", out _).Should().BeFalse();
	}

	[Test]
	public void Resolve_EndpointKeyOutscoresField_TokenIsKey()
	{
		var roles = ApiLookupRoles.Resolve(new Dictionary<string, double>
		{
			["key_0"] = 6.0,
			["field_0"] = 0.6,
			["title_0"] = 1.0,
			["field_1"] = 1.0,
			["title_all"] = 3.0
		});

		roles.Should().Equal(new ApiTokenRole(0, ApiLookupRole.Key, 6.0), new ApiTokenRole(1, ApiLookupRole.Field, 1.0));
	}

	[Test]
	public void Resolve_FieldTiesWithTitle_TokenIsField() =>
		ApiLookupRoles
			.Resolve(new Dictionary<string, double> { ["title_0"] = 1.0, ["field_0"] = 1.0, ["body_0"] = 0.1 })
			.Should()
			.Equal(new ApiTokenRole(0, ApiLookupRole.Field, 1.0));

	[Test]
	public void Resolve_NamesThatAreNotRoles_AreIgnored() =>
		ApiLookupRoles
			.Resolve(new Dictionary<string, double> { ["api_name"] = 1.0, ["title_all"] = 3.0, ["api_children"] = 0.4 })
			.Should()
			.BeEmpty();
}
