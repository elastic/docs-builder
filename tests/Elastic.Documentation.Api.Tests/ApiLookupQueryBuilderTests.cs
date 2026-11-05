// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
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

	/// <summary>The <c>key_0</c> option, whether the word offers other roles or, as a path, only this one.</summary>
	private static JsonElement KeyOption(JsonDocument json)
	{
		var clause = TokenClause(json, 0).GetProperty("dis_max");
		return clause.TryGetProperty("_name", out var name) && name.GetString() == "key_0"
			? clause
			: OptionsByName(TokenClause(json, 0))["key_0"];
	}

	/// <summary>The exact endpoint lookups, each a <c>dis_max</c> of <c>terms</c> templates, in option order.</summary>
	private static IEnumerable<JsonProperty> TermsOptions(JsonElement key) =>
		key
			.GetProperty("queries")
			.EnumerateArray()
			.Where(q => q.TryGetProperty("dis_max", out _))
			.Select(q => q.GetProperty("dis_max").GetProperty("queries")[0].GetProperty("constant_score").GetProperty("filter"))
			.Where(f => f.TryGetProperty("terms", out _))
			.Select(f => f.GetProperty("terms").EnumerateObject().Single());

	/// <summary>The most literal templates of the two exact endpoint options, by the field each reads.</summary>
	private static Dictionary<string, string?[]> ExactOptions(JsonElement key) =>
		TermsOptions(key).Take(2).ToDictionary(p => p.Name, p => p.Value.EnumerateArray().Select(v => v.GetString()).ToArray());

	/// <summary>
	/// Each routes-under alternation as <c>field pattern boost</c>, in option order, checked to carry no name of its own.
	/// </summary>
	private static string[] RoutesUnder(JsonElement key) =>
		key
			.GetProperty("queries")
			.EnumerateArray()
			.Where(q => q.TryGetProperty("dis_max", out _))
			.SelectMany(q => q.GetProperty("dis_max").GetProperty("queries").EnumerateArray())
			.Select(q => q.GetProperty("constant_score"))
			.Where(cs => cs.GetProperty("filter").TryGetProperty("regexp", out _))
			.Select(cs =>
			{
				cs.TryGetProperty("_name", out _).Should().BeFalse();
				var regexp = cs.GetProperty("filter").GetProperty("regexp").EnumerateObject().Single();
				var boost = cs.GetProperty("boost").GetSingle().ToString("0.###", CultureInfo.InvariantCulture);
				return $"{regexp.Name} {regexp.Value.GetProperty("value").GetString()} {boost}";
			})
			.ToArray();

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
	public void Build_PlainWord_KeyOptionWeighsTheEndpointAtSixOnBothViews()
	{
		using var json = Json(ApiLookupQueryBuilder.Build("search"));
		var key = KeyOption(json);

		var exact = ExactOptions(key);
		exact.Keys.Should().BeEquivalentTo("api.endpoints", "api.endpoints_bare");
		exact.Values.Should().AllSatisfy(terms => terms.Should().Equal("search"));
		key
			.GetProperty("queries")
			.EnumerateArray()
			.Where(q => q.TryGetProperty("dis_max", out _))
			.SelectMany(q => q.GetProperty("dis_max").GetProperty("queries").EnumerateArray())
			.Select(o => o.GetProperty("constant_score"))
			.Where(cs => cs.GetProperty("filter").TryGetProperty("terms", out _))
			.Select(cs => cs.GetProperty("boost").GetSingle())
			.Should()
			.AllSatisfy(b => b.Should().Be(6.0f));
	}

	[Test]
	[Arguments("snapshot", "snapshot", "snapshot")]
	[Arguments("Snapshot", "snapshot", "snapshot")]
	[Arguments("_snapshot", "_snapshot", "snapshot")]
	public void Build_PlainWord_KeyOptionCoversOnlyTheRoutesUnderItsSystemRouteAtFiveAndAHalf(string word, string route, string bare)
	{
		using var json = Json(ApiLookupQueryBuilder.Build(word));
		var key = KeyOption(json);

		ExactOptions(key)
			.Should()
			.BeEquivalentTo(new Dictionary<string, string?[]> { ["api.endpoints"] = [route], ["api.endpoints_bare"] = [bare] });
		RoutesUnder(key).Should().Equal("api.endpoints (_snapshot)/.+ 5.5");
	}

	[Test]
	public void Build_PlainWord_DoesNotCoverTheRoutesUnderItsOwnName()
	{
		using var json = Json(ApiLookupQueryBuilder.Build("api"));

		RoutesUnder(KeyOption(json)).Should().Equal("api.endpoints (_api)/.+ 5.5");
	}

	[Test]
	public void Build_PlainWord_KeyOptionKeepsTheUnderscoreAliasOnTheExactFieldForIndexesWithoutTheBareView()
	{
		using var json = Json(ApiLookupQueryBuilder.Build("snapshot"));
		var options = KeyOption(json).GetProperty("queries").EnumerateArray().Skip(2).ToList();

		options.Should().HaveCount(2);
		var terms = options[0].GetProperty("dis_max").GetProperty("queries")[0].GetProperty("constant_score");
		terms.GetProperty("boost").GetSingle().Should().Be(6.0f);
		var field = terms.GetProperty("filter").GetProperty("terms").EnumerateObject().Single();
		field.Name.Should().Be("api.endpoints");
		field.Value.EnumerateArray().Select(v => v.GetString()).Should().Contain("_snapshot");
		RoutesUnder(KeyOption(json)).Should().Equal("api.endpoints (_snapshot)/.+ 5.5");
	}

	[Test]
	[Arguments("_snapshot")]
	[Arguments("/api/fleet")]
	public void Build_NonBareKey_HasNoUnderscoreAlias(string typed)
	{
		using var json = Json(ApiLookupQueryBuilder.Build(typed));

		TermsOptions(KeyOption(json)).Where(p => p.Name == "api.endpoints").Should().ContainSingle();
	}

	[Test]
	[Arguments("/api/fleet")]
	[Arguments("/api/fleet/")]
	[Arguments("GET /api/fleet")]
	public void Build_TypedRoute_KeyOptionCoversTheRoutesUnderIt(string typed)
	{
		using var json = Json(ApiLookupQueryBuilder.Build(typed));
		var key = KeyOption(json);

		var exact = ExactOptions(key);
		exact.Keys.Should().BeEquivalentTo("api.endpoints", "api.endpoints_bare");
		exact.Values.Should().AllSatisfy(terms => terms.Should().Contain("api/fleet"));
		RoutesUnder(key)
			.Should()
			.Equal(
				"api.endpoints (api/fleet)/.+ 5.5",
				@"api.endpoints (\*/fleet|api/\*)/.+ 3.667",
				"api.endpoints_bare (api/fleet)/.+ 5.5",
				@"api.endpoints_bare (\*/fleet|api/\*)/.+ 3.667"
			);
	}

	[Test]
	public void Build_TypedRouteWithAParameter_CoversTheRoutesUnderTheTemplate()
	{
		using var json = Json(ApiLookupQueryBuilder.Build("/api/fleet/{id}"));

		RoutesUnder(KeyOption(json))
			.Should()
			.Equal(
				@"api.endpoints (api/fleet/\*)/.+ 5.5",
				@"api.endpoints (\*/fleet/\*|api/\*/\*)/.+ 3.667",
				@"api.endpoints_bare (api/fleet/\*)/.+ 5.5",
				@"api.endpoints_bare (\*/fleet/\*|api/\*/\*)/.+ 3.667"
			);
	}

	[Test]
	public void Build_TypedRouteWithAParameterValue_CoversTheDescendantsOfEveryTemplateByLiteralCount()
	{
		using var json = Json(ApiLookupQueryBuilder.Build("/_security/user/alice"));

		RoutesUnder(KeyOption(json))
			.Where(r => r.StartsWith("api.endpoints ", StringComparison.Ordinal))
			.Should()
			.Equal(
				"api.endpoints (_security/user/alice)/.+ 5.5",
				@"api.endpoints (\*/user/alice|_security/\*/alice|_security/user/\*)/.+ 4.125",
				@"api.endpoints (\*/\*/alice|\*/user/\*|_security/\*/\*)/.+ 2.75"
			);
	}

	[Test]
	public void Build_TypedSingleSegmentRoute_CoversTheRoutesUnderItAtFiveAndAHalf()
	{
		using var json = Json(ApiLookupQueryBuilder.Build("/search"));

		RoutesUnder(KeyOption(json))
			.Where(r => r.StartsWith("api.endpoints ", StringComparison.Ordinal))
			.Should()
			.Equal("api.endpoints (search)/.+ 5.5", "api.endpoints (_search)/.+ 5.5");
	}

	[Test]
	public void Build_LongTypedRoute_KeepsEveryRegexpUnderTheIndexLengthLimit()
	{
		using var json = Json(
			ApiLookupQueryBuilder.Build("/" + string.Join('/', Enumerable.Range(0, 8).Select(i => $"segment-number-{i}")))
		);
		var routes = RoutesUnder(KeyOption(json));

		routes.Should().HaveCountGreaterThan(16, "eight template groups on two fields need splitting");
		routes.Should().OnlyContain(r => r.Split(' ')[1].Length <= 1000);
	}

	[Test]
	[Arguments("/_snapshot", "_snapshot", "snapshot")]
	[Arguments("/snapshot", "snapshot", "snapshot")]
	public void Build_TypedSystemRoute_StripsTheTypedUnderscoreForTheBareKeys(string typed, string route, string bare)
	{
		using var json = Json(ApiLookupQueryBuilder.Build(typed));
		var key = KeyOption(json);

		ExactOptions(key)
			.Should()
			.BeEquivalentTo(new Dictionary<string, string?[]> { ["api.endpoints"] = [route], ["api.endpoints_bare"] = [bare] });
		RoutesUnder(key).Should().Contain([$"api.endpoints ({route})/.+ 5.5", $"api.endpoints_bare ({bare})/.+ 5.5"]);
	}

	[Test]
	public void Build_TypedSystemRouteWithSegments_StripsTheUnderscoreForTheBareKeys()
	{
		using var json = Json(ApiLookupQueryBuilder.Build("/_cluster/health"));
		var exact = ExactOptions(KeyOption(json));

		exact["api.endpoints"].Should().Contain("_cluster/health");
		exact["api.endpoints_bare"].Should().Contain("cluster/health");
	}

	[Test]
	public void Build_RouteOfOnlyAnUnderscore_LooksUpOnlyTheExactKeys()
	{
		using var json = Json(ApiLookupQueryBuilder.Build("/_"));
		var key = KeyOption(json);

		ExactOptions(key).Should().BeEquivalentTo(new Dictionary<string, string?[]> { ["api.endpoints"] = ["_"] });
		RoutesUnder(key).Should().Equal("api.endpoints (_)/.+ 5.5");
	}

	[Test]
	[Arguments("/")]
	[Arguments("GET /")]
	[Arguments("/{index}")]
	public void Build_RouteWithoutALiteralSegment_MatchesNothing(string typed)
	{
		using var json = Json(ApiLookupQueryBuilder.Build(typed));

		json.RootElement.TryGetProperty("match_none", out _).Should().BeTrue();
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
	[Arguments("knn", 0, "knn")]
	[Arguments("GET /_search knn", 1, "knn")]
	[Arguments("knn /_search", 0, "knn")]
	[Arguments("options", 0, "options")]
	[Arguments("track_total", 0, "track_total")]
	public void Build_FieldWord_MatchesOnlyItsOwnNameWithBoundedScores(string query, int index, string name)
	{
		using var json = Json(ApiLookupQueryBuilder.Build(query));
		var fieldQuery = OptionsByName(TokenClause(json, index))[$"field_{index}"].GetProperty("query");
		var options = fieldQuery.GetProperty("dis_max").GetProperty("queries");
		options.GetArrayLength().Should().Be(1);
		var matches = options[0].GetProperty("dis_max").GetProperty("queries");
		matches.GetArrayLength().Should().Be(2);
		matches[0]
			.GetProperty("constant_score")
			.GetProperty("filter")
			.GetProperty("term")
			.GetProperty("api.properties.name")
			.GetProperty("value")
			.GetString()
			.Should()
			.Be(name);
		matches[1]
			.GetProperty("constant_score")
			.GetProperty("filter")
			.GetProperty("prefix")
			.GetProperty("api.properties.name")
			.GetProperty("value")
			.GetString()
			.Should()
			.Be(name);
		fieldQuery.GetRawText().Should().NotContain("api.properties.description").And.NotContain("api.properties.path");
		var scores = matches.EnumerateArray().Select(q => q.GetProperty("constant_score").GetProperty("boost").GetSingle()).ToArray();
		scores.Should().OnlyContain(score => score > 0 && score <= 1);
		scores[0].Should().BeGreaterThan(scores[1], "an exact field name ranks ahead of a prefix");
	}

	[Test]
	[Arguments("_search knn.field", "field", "knn field")]
	[Arguments("search rrf.min_score", "min_score", "rrf min_score")]
	public void Build_QualifiedPath_FieldOptionRequiresMatchingLeafAndPathWords(string query, string name, string phrase)
	{
		using var json = Json(ApiLookupQueryBuilder.Build(query));
		var qualified = OptionsByName(TokenClause(json, 1))["field_1"]
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
		leaf.GetProperty("term").GetProperty("api.properties.name").GetProperty("value").GetString().Should().Be(name);
		qualified
			.GetProperty("filter")
			.GetProperty("match_phrase_prefix")
			.GetProperty("api.properties.path_words")
			.GetProperty("query")
			.GetString()
			.Should()
			.Be(phrase);
	}

	[Test]
	[Arguments("retriever.", "retriever")]
	[Arguments("pets[].owner.", "pets.owner")]
	[Arguments("labels.<string>.", "labels")]
	public void Build_TrailingDot_ChildrenOptionNamesTheParentKey(string query, string parent)
	{
		using var json = Json(ApiLookupQueryBuilder.Build(query));
		var term = OptionsByName(TokenClause(json, 0))["field_0"]
			.GetProperty("query")
			.GetProperty("dis_max")
			.GetProperty("queries")
			.EnumerateArray()
			.Select(o => o.EnumerateObject().Single().Value)
			.Single(o => o.TryGetProperty("_name", out var name) && name.GetString() == "api_children")
			.GetProperty("filter")
			.GetProperty("term")
			.GetProperty("api.properties.parent");
		term.GetProperty("value").GetString().Should().Be(parent);
		term.GetProperty("case_insensitive").GetBoolean().Should().BeTrue();
	}

	[Test]
	[Arguments("retriever")]
	[Arguments("retriever.standard")]
	public void Build_TrailingDot_KeepsTheNameOptions(string path)
	{
		static string[] Options(string query)
		{
			using var json = Json(ApiLookupQueryBuilder.Build(query));
			return OptionsByName(TokenClause(json, 0))["field_0"]
				.GetProperty("query")
				.GetProperty("dis_max")
				.GetProperty("queries")
				.EnumerateArray()
				.Select(q => q.GetRawText())
				.ToArray();
		}

		Options(path + ".")[..^1].Should().Equal(Options(path)).And.NotBeEmpty();
	}

	[Test]
	public void Build_NoTrailingDot_FieldOptionHasNoChildrenOption()
	{
		using var json = Json(ApiLookupQueryBuilder.Build("retriever"));
		var field = OptionsByName(TokenClause(json, 0))["field_0"];

		field.GetProperty("query").GetRawText().Should().NotContain("api_children").And.NotContain("api.properties.parent");
	}

	[Test]
	[Arguments("retriever", "retriever")]
	[Arguments("retriever.standard.", "retriever.standard")]
	[Arguments("GET /_search retriever", "retriever")]
	[Arguments("pets[].owner.", "pets.owner")]
	[Arguments("labels.<string>.", "labels")]
	public void Build_PlainWord_LiftsOperationsThatListChildrenOfTheTypedPath(string query, string field)
	{
		using var json = Json(ApiLookupQueryBuilder.Build(query));
		var lift = json
			.RootElement
			.GetProperty("bool")
			.GetProperty("should")
			.EnumerateArray()
			.Where(l => l.TryGetProperty("nested", out _))
			.Select(l => l.GetProperty("nested"))
			.Single(
				l => l
					.GetProperty("query")
					.GetProperty("constant_score")
					.GetProperty("filter")
					.GetProperty("term")
					.GetProperty("api.properties.parent")
					.GetProperty("value")
					.GetString() == field
			);
		lift.GetProperty("path").GetString().Should().Be("api.properties");
		lift.GetProperty("score_mode").GetString().Should().Be("max");
		lift.TryGetProperty("inner_hits", out _).Should().BeFalse();
		var constantScore = lift.GetProperty("query").GetProperty("constant_score");
		constantScore.GetProperty("boost").GetSingle().Should().Be(0.5f);
		constantScore
			.GetProperty("filter")
			.GetProperty("term")
			.GetProperty("api.properties.parent")
			.GetProperty("case_insensitive")
			.GetBoolean()
			.Should()
			.BeTrue();
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
