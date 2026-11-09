// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.Globalization;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Elastic.Documentation.Search.Contract;

#pragma warning disable IDE0130 // 'Query' subfolder would shadow the ES client's Query type
namespace Elastic.Documentation.Search;

/// <summary>
/// The query behind the API navigation search. Each word of the query must be explained by one role on an operation:
/// its endpoint, a word of its operation id, its group, a word of its title, its HTTP method, a field name, or a word of
/// its description. An HTTP method typed right before a path joins that path's word and only lifts operations with that
/// method. The best role counts per word and word order does not matter. Every role option is named
/// <c>&lt;role&gt;_&lt;index&gt;</c> so the result processor can tell, from the named query scores, which role won each word.
/// </summary>
public static class ApiLookupQueryBuilder
{
	private const string SynonymsAnalyzer = "synonyms_analyzer";
	private const string FieldHitsPrefix = SearchQueryBuilder.ApiPropertyHits + "_";
	private const string WholeTitleQueryName = "title_all";

	// Up to two words every word must be explained; from three on, most of them.
	private const string TokensMinimumShouldMatch = "2<75%";

	// Each word adds a dis_max with nested inner hits to a per-keystroke request; same bound as ApiEndpointPath's path segments.
	private const int MaxTokens = 8;

	// index.max_regex_length defaults to 1,000 characters, and a long typed route has up to 70 templates in one group.
	private const int MaxRegexpLength = 900;

	private static readonly FrozenSet<string> HttpMethods = new[] { "GET", "POST", "PUT", "DELETE", "HEAD", "PATCH" }.ToFrozenSet(
		StringComparer.OrdinalIgnoreCase
	);

	private static readonly Field[] TitleCompletionFields =
	[
		QueryFieldNames.SearchTitleCompletion,
		QueryFieldNames.SearchTitleCompletion2Gram,
		QueryFieldNames.SearchTitleCompletion3Gram
	];

	private static readonly Field[] OperationWordFields =
	[
		QueryFieldNames.ApiWordsCompletion,
		QueryFieldNames.ApiWordsCompletion2Gram,
		QueryFieldNames.ApiWordsCompletion3Gram
	];

	public static Query Build(string query)
	{
		var tokens = Tokenize(query);
		var tokenWords = tokens.Select(t => t.Kind == ApiLookupTokenKind.Plain ? ApiPropertyPath.IdentifierWords(t.Text) : []).ToArray();
		var clauses = tokens.Select((token, index) => TokenClause(token, index, OtherWords(tokenWords, index))).OfType<Query>().ToList();
		if (clauses.Count == 0)
			return new MatchNoneQuery();

		var typed = query.Trim();
		return new BoolQuery
		{
			Must = [new BoolQuery { Should = clauses, MinimumShouldMatch = TokensMinimumShouldMatch }],
			Should = Lifts(typed, tokens)
		};
	}

	/// <summary>The words of every plain word but the one at <paramref name="index"/>, which a field row may name besides it.</summary>
	private static string[] OtherWords(string[][] tokenWords, int index) => [.. tokenWords.Where((_, i) => i != index).SelectMany(w => w)];

	/// <summary>A word typed with a trailing dot, as in <c>retriever.</c>, asks for the children of the field it names.</summary>
	public static bool ExpandsChildren(string query) => Tokenize(query).Any(t => t.ExpandChildren);

	public static IReadOnlyList<ApiLookupToken> Tokenize(string query)
	{
		var words = query.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		var tokens = new List<ApiLookupToken>();
		for (var i = 0; i < words.Length && tokens.Count < MaxTokens; i++)
		{
			if (i + 1 < words.Length && HttpMethods.Contains(words[i]) && words[i + 1].StartsWith('/'))
			{
				tokens.Add(
					new ApiLookupToken(ApiLookupTokenKind.Path, $"{words[i]} {words[i + 1]}", ExpandChildren: false, Method: words[i])
				);
				i++;
			}
			else if (ToToken(words[i]) is { } token)
				tokens.Add(token);
		}
		return tokens;
	}

	/// <summary>The inner hits of the field option of the word at <paramref name="index"/>.</summary>
	public static string FieldHitsName(int index) => FieldHitsPrefix + index.ToString(CultureInfo.InvariantCulture);

	public static bool TryTokenIndex(string innerHitsName, out int index)
	{
		index = -1;
		return innerHitsName.StartsWith(FieldHitsPrefix, StringComparison.Ordinal)
			&& int.TryParse(innerHitsName.AsSpan(FieldHitsPrefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out index);
	}

	private static ApiLookupToken? ToToken(string word)
	{
		if (word.StartsWith('/'))
			return new ApiLookupToken(ApiLookupTokenKind.Path, word, ExpandChildren: false);

		var text = word.TrimEnd('.');
		return text.Length == 0 ? null : new ApiLookupToken(ApiLookupTokenKind.Plain, text, ExpandChildren: text.Length < word.Length);
	}

	/// <summary>The roles a word may play, best one wins. A path that names no endpoint explains nothing, so it drops out.</summary>
	private static Query? TokenClause(ApiLookupToken token, int index, IReadOnlyList<string> otherWords)
	{
		var key = KeyOption(token.Text, index);
		if (token.Kind == ApiLookupTokenKind.Path)
			return key;

		Query?[] options =
		[
			key,
			Constant(OperationWordQuery(token.Text), ApiLookupWeights.OperationWord, ApiLookupRoles.QueryName(ApiLookupRole.Id, index)),
			Constant(
				SynonymsMatch(QueryFieldNames.ApiGroup, token.Text),
				ApiLookupWeights.GroupWord,
				ApiLookupRoles.QueryName(ApiLookupRole.Group, index)
			),
			Constant(TitleQuery(token.Text), ApiLookupWeights.TitleWord, ApiLookupRoles.QueryName(ApiLookupRole.Title, index)),
			MethodOption(token.Text, index),
			FieldOption(token, index, otherWords),
			Constant(
				SynonymsMatch(QueryFieldNames.Body, token.Text),
				ApiLookupWeights.BodyWord,
				ApiLookupRoles.QueryName(ApiLookupRole.Body, index)
			)
		];
		return new DisMaxQuery { Queries = options.OfType<Query>().ToList(), TieBreaker = 0 };
	}

	/// <summary>
	/// A word or a typed route matches its endpoint exactly, or covers the routes under it just below. A typed route covers
	/// the routes under it; a bare word covers only the routes under its system route, because words such as <c>api</c> are the
	/// first segment of every Kibana route. The bare keys and the typed route both drop the underscore that starts an
	/// Elasticsearch system route, so <c>snapshot</c>, <c>/snapshot</c> and <c>/_snapshot</c> all cover <c>_snapshot/*/*/_restore</c>.
	/// A typed route that stops inside a segment completes it just below the full keys, so <c>/_sear</c> finds <c>_search</c>;
	/// a trailing slash asks for the routes under it instead. The root route names only its own operations.
	/// </summary>
	private static Query? KeyOption(string text, int index)
	{
		var name = ApiLookupRoles.QueryName(ApiLookupRole.Key, index);
		var route = ApiEndpointPath.NormalizeQuery(text);
		if (SearchQueryBuilder.BuildApiEndpointQuery(route, ApiLookupWeights.EndpointKey, QueryFieldNames.ApiEndpoints) is not { } exact)
			return null;
		// A lone slash is the first keystroke of every route, so it names the root operations rather than listing every route.
		if (route == ApiEndpointPath.Root)
			return new DisMaxQuery { Queries = [exact], QueryName = name };

		var typedRoute = text.Contains('/');
		var completes = typedRoute && !text.EndsWith('/');
		var bare = ApiEndpointPath.StripRouteUnderscores(route);
		var bareExact = SearchQueryBuilder.BuildApiEndpointQuery(bare, ApiLookupWeights.EndpointKey, QueryFieldNames.ApiEndpointsBare);
		Query?[] options =
		[
			exact,
			bareExact,
			typedRoute ? RoutesUnder(route, QueryFieldNames.ApiEndpoints) : null,
			typedRoute && bareExact is not null ? RoutesUnder(bare, QueryFieldNames.ApiEndpointsBare) : null,
			.. completes ? Completions(route, QueryFieldNames.ApiEndpoints) : [],
			.. completes && bareExact is not null ? Completions(bare, QueryFieldNames.ApiEndpointsBare) : [],
			.. UnderscoreAliasOptions(route, typedRoute: typedRoute)
		];
		return new DisMaxQuery { Queries = options.OfType<Query>().ToList(), QueryName = name };
	}

	/// <summary>
	/// An index written before the bare keys exist has no <c>api.endpoints_bare</c> and the service deploys on its own, so a
	/// bare word keeps its underscore alias on the exact field until every index carries the bare keys.
	/// </summary>
	private static IEnumerable<Query> UnderscoreAliasOptions(string route, bool typedRoute)
	{
		if (SearchQueryBuilder.BuildBareWordEndpointQuery(route, ApiLookupWeights.EndpointKey) is { } alias)
			yield return alias;
		if (SearchQueryBuilder.EndpointName(route) is not { } name || (typedRoute && name == route))
			yield break;
		if (RoutesUnder(name, QueryFieldNames.ApiEndpoints) is { } nameRoutes)
			yield return nameRoutes;
	}

	private static Query? RoutesUnder(string route, Field field) =>
		RoutesMatching(ApiEndpointPath.QueryCandidates(route), field, RouteTier.Under);

	/// <summary>
	/// The keys that finish the last typed segment and the routes under them. A <c>*</c> is a complete parameter value, so
	/// templates ending in one complete nothing, while the full-literal group still sets the weights.
	/// </summary>
	private static IEnumerable<Query> Completions(string route, Field field)
	{
		var groups = ApiEndpointPath
			.QueryCandidates(route)
			.Select(g => g with { Terms = g.Terms.Where(t => t.Split('/')[^1] != "*").ToList() })
			.ToList();
		return new[] { RouteTier.Completion, RouteTier.Deeper }.Select(tier => RoutesMatching(groups, field, tier)).OfType<Query>();
	}

	/// <summary>
	/// The keys that relate to every template <see cref="ApiEndpointPath.QueryCandidates"/> reads the route as the way
	/// <paramref name="tier"/> says, so <c>/_security/user/alice</c> covers <c>_security/user/*/_password</c>. Like the exact
	/// lookup, a template scores by the literal segments it keeps.
	/// </summary>
	private static Query? RoutesMatching(IReadOnlyList<EndpointCandidateGroup> groups, Field field, RouteTier tier)
	{
		var total = groups[0].LiteralSegments;
		var queries = groups.SelectMany(
			g => RegexpBatches(g.Terms).Select(
				pattern =>
					(Query)new ConstantScoreQuery
					{
						Filter = new RegexpQuery { Field = field, Value = pattern + tier.Suffix },
						Boost = tier.Weight * (1 + g.LiteralSegments) / (1 + total)
					}
			)
		).ToList();
		if (queries.Count == 0)
			return null;
		return new DisMaxQuery { Queries = queries };
	}

	/// <summary>How a key relates to a typed route R: under it (R/…), finishing its last segment (Rx), or under that (Rx/…).</summary>
	private sealed record RouteTier(string Suffix, float Weight)
	{
		public static readonly RouteTier Under = new("/.+", ApiLookupWeights.NamespaceKey);
		public static readonly RouteTier Completion = new("[^/]+", ApiLookupWeights.EndpointCompletion);
		public static readonly RouteTier Deeper = new("[^/]+/.+", ApiLookupWeights.NamespaceCompletion);
	}

	// A route segment that long names nothing, and one oversized pattern would fail the whole request.
	private static IEnumerable<string> RegexpBatches(IReadOnlyList<string> terms)
	{
		var batch = new List<string>();
		foreach (var term in terms.Where(t => ApiEndpointPath.RegexpAlternation([t]).Length <= MaxRegexpLength))
		{
			if (batch.Count > 0 && ApiEndpointPath.RegexpAlternation([.. batch, term]).Length > MaxRegexpLength)
			{
				yield return ApiEndpointPath.RegexpAlternation(batch);
				batch = [];
			}
			batch.Add(term);
		}
		if (batch.Count > 0)
			yield return ApiEndpointPath.RegexpAlternation(batch);
	}

	/// <summary>A method word, such as <c>get</c>, also stays an operation, title and description word.</summary>
	private static Query? MethodOption(string text, int index) =>
		HttpMethods.Contains(text)
			? Constant(
				new TermQuery { Field = QueryFieldNames.ApiMethod, Value = text.ToUpperInvariant() },
				ApiLookupWeights.Method,
				ApiLookupRoles.QueryName(ApiLookupRole.Method, index)
			)
			: null;

	private static Query FieldOption(ApiLookupToken token, int index, IReadOnlyList<string> otherWords) =>
		SearchQueryBuilder.BuildNestedPropertyQuery(
			token.Text,
			FieldHitsName(index),
			new SearchQueryBuilder.PropertyLookup(AllowPrefix: true, token.ExpandChildren, otherWords),
			ApiLookupRoles.QueryName(ApiLookupRole.Field, index)
		);

	private static List<Query> Lifts(string typed, IReadOnlyList<ApiLookupToken> tokens)
	{
		List<Query> lifts =
		[
			Constant(TitleQuery(typed), ApiLookupWeights.WholeTitle, WholeTitleQueryName),
			new TermQuery { Field = QueryFieldNames.TitleKeyword, Value = typed.ToLowerInvariant() }
		];
		if (SearchQueryBuilder.BuildTitleStartsWithQuery(typed) is { } startsWith)
			lifts.Add(startsWith);
		lifts.AddRange(
			tokens.Where(t => t.Kind == ApiLookupTokenKind.Plain).Select(
				t => SearchQueryBuilder.BuildListsFieldsUnderTypedPathBoost(t.Text)
			)
		);
		// The spec gives one canonical verb, so a typed method before a path lifts its operation rather than requiring it.
		lifts.AddRange(tokens.Select(t => t.Method).OfType<string>().Select(MethodLift));
		lifts.AddRange(SearchQueryBuilder.ScoringQueries);
		// An operation id of fewer words is more specific, so `esql query` prefers `esql-query` over `esql-async-query-stop`.
		lifts.Add(new RankFeatureQuery { Field = QueryFieldNames.ApiIdWords, Boost = ApiLookupWeights.Specificity });
		lifts.Add(new TermQuery { Field = QueryFieldNames.ApiDeprecated, Value = false, Boost = ApiLookupWeights.NotDeprecated });
		return lifts;
	}

	private static Query MethodLift(string method) =>
		new TermQuery { Field = QueryFieldNames.ApiMethod, Value = method.ToUpperInvariant(), Boost = ApiLookupWeights.Method };

	private static Query OperationWordQuery(string text) => BoolPrefix(text, OperationWordFields);

	private static Query TitleQuery(string text) => BoolPrefix(text, TitleCompletionFields);

	private static Query BoolPrefix(string text, Field[] fields) =>
		new MultiMatchQuery
		{
			Query = text,
			Operator = Operator.And,
			Type = TextQueryType.BoolPrefix,
			Analyzer = SynonymsAnalyzer,
			Fields = fields
		};

	private static Query SynonymsMatch(Field field, string text) =>
		new MatchQuery { Field = field, Query = text, Analyzer = SynonymsAnalyzer };

	private static Query Constant(Query filter, float boost, string name) =>
		new ConstantScoreQuery { Filter = filter, Boost = boost, QueryName = name };
}
