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
/// method; it is not a word of any title. The best role counts per word and word order does not matter. Every role option is named
/// <c>&lt;role&gt;_&lt;index&gt;</c> so the result processor can tell, from the named query scores, which role won each word.
/// </summary>
public static class ApiLookupQueryBuilder
{
	private const string SynonymsAnalyzer = "synonyms_analyzer";
	private const string FieldHitsPrefix = SearchQueryBuilder.ApiPropertyHits + "_";
	private const string WholeTitleQueryName = "title_all";
	private const string TitleNamedQueryName = "title_named";
	private const string LandingNameQueryName = "landing_name";
	private const string NotDeprecatedQueryName = "not_deprecated";

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

	// An operation's search_title also holds its route, id and group, so a word names an operation through its display title or
	// through a parent title (the API's name, its group) instead.
	private static readonly Field[] TitleOrParentFields = [QueryFieldNames.TitleCompletion, QueryFieldNames.ParentsTitle];
	private static readonly Field[] DisplayTitleOnly = [QueryFieldNames.TitleCompletion];

	// The titles are indexed as written, but the synonyms analyzer stems the typed word. A word matches as stemmed, which finds a title
	// written in base form, and as written, which finds `policies` in "Lifecycle policies" and `ES|QL` (`es`, `ql`) in "Run an ES|QL query".
	private static readonly string[] TitleWordAnalyzers = [SynonymsAnalyzer, "standard"];

	private const int TitleWordFixedLetters = 5;
	private const string RootParentTitle = "API";

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

		var typed = TitleText(query);
		return new BoolQuery
		{
			Must = [new BoolQuery { Should = clauses, MinimumShouldMatch = TokensMinimumShouldMatch }],
			MustNot = [PageWithoutName(typed, tokens)],
			Should = Lifts(typed, tokens)
		};
	}

	/// <summary>
	/// An API's landing, group, type or topic page has no operation to explain a word, so it qualifies only when every typed word
	/// names it through its title or, on a type page, through a field it lists. Otherwise the words of its body would list it for
	/// queries about an operation.
	/// </summary>
	private static Query PageWithoutName(string typed, IReadOnlyList<ApiLookupToken> tokens) =>
		new BoolQuery { Filter = [LandingOrPage()], MustNot = [TitleQuery(typed), EveryWordNamesTitleOrField(tokens)] };

	private static readonly SearchQueryBuilder.PropertyLookup PageFieldLookup = new(AllowPrefix: true, ExpandChildren: false);

	/// <summary>A typed route names no title or field, so a query with one never names a page this way.</summary>
	private static Query EveryWordNamesTitleOrField(IReadOnlyList<ApiLookupToken> tokens) =>
		tokens.Any(t => t.Kind != ApiLookupTokenKind.Plain)
			? new MatchNoneQuery()
			: new BoolQuery
			{
				Must =
				[
					.. tokens.Select(
						t =>
							(Query)new BoolQuery
							{
								Should =
								[
									TitleQuery(t.Text),
									SearchQueryBuilder.BuildNestedPropertyQuery(t.Text, innerHitsName: null, PageFieldLookup)
								],
								MinimumShouldMatch = 1
							}
					)
				]
			};

	private static Query LandingNamed(string typed) => new BoolQuery { Filter = [LandingTerm(), TitleQuery(typed)] };

	private static Query LandingTerm() => new TermQuery { Field = QueryFieldNames.ApiLanding, Value = true };

	private static Query LandingOrPage() =>
		new BoolQuery { Should = [LandingTerm(), new TermQuery { Field = QueryFieldNames.ApiPage, Value = true }], MinimumShouldMatch = 1 };

	/// <summary>The words of every plain word but the one at <paramref name="index"/>, which a field row may name besides it.</summary>
	private static string[] OtherWords(string[][] tokenWords, int index) => [.. tokenWords.Where((_, i) => i != index).SelectMany(w => w)];

	/// <summary>
	/// The typed words a title can answer. A method before a path names the verb of the request, so <c>GET /_search</c> asks
	/// for the route and never for a title that starts with <c>Get</c>.
	/// </summary>
	private static string TitleText(string query)
	{
		var words = Words(query);
		return string.Join(' ', words.Where((_, i) => !IsMethodBeforePath(words, i)));
	}

	private static string[] Words(string query) =>
		query.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

	private static bool IsMethodBeforePath(string[] words, int index) =>
		index + 1 < words.Length && HttpMethods.Contains(words[index]) && words[index + 1].StartsWith('/');

	/// <summary>A word typed with a trailing dot, as in <c>retriever.</c>, asks for the children of the field it names.</summary>
	public static bool ExpandsChildren(string query) => Tokenize(query).Any(t => t.ExpandChildren);

	public static IReadOnlyList<ApiLookupToken> Tokenize(string query)
	{
		var words = Words(query);
		var tokens = new List<ApiLookupToken>();
		for (var i = 0; i < words.Length && tokens.Count < MaxTokens; i++)
		{
			if (IsMethodBeforePath(words, i))
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
			OperationWordOption(token.Text, index),
			Constant(
				SynonymsMatch(QueryFieldNames.ApiGroup, token.Text),
				ApiLookupWeights.GroupWord,
				ApiLookupRoles.QueryName(ApiLookupRole.Group, index)
			),
			Constant(
				WordNamesTitleOrParents(token.Text, completes: true),
				ApiLookupWeights.TitleWord,
				ApiLookupRoles.QueryName(ApiLookupRole.Title, index)
			),
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
			Constant(EveryWordNamesTitleOrParents(typed), ApiLookupWeights.WholeTitle, WholeTitleQueryName),
			TitleNamedLift(typed)
		];
		if (SearchQueryBuilder.BuildTitleStartsWithQuery(typed) is { } startsWith)
			lifts.Add(startsWith);
		lifts.Add(Constant(LandingNamed(typed), ApiLookupWeights.LandingName, LandingNameQueryName));
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
		// Current operations carry no flag or an explicit false, so the lift tests that the flag is not true.
		lifts.Add(
			Constant(
				new BoolQuery { MustNot = [new TermQuery { Field = QueryFieldNames.ApiDeprecated, Value = true }] },
				ApiLookupWeights.NotDeprecated,
				NotDeprecatedQueryName
			)
		);
		return lifts;
	}

	/// <summary>
	/// The match takes no analyzer because the field carries its own, and an index without the field then matches nothing
	/// instead of failing. The exact lowercase title keeps working on such an index.
	/// </summary>
	private static Query TitleNamed(string typed) =>
		new DisMaxQuery
		{
			Queries =
			[
				new TermQuery { Field = QueryFieldNames.TitleKeyword, Value = typed.ToLowerInvariant() },
				new MatchQuery { Field = QueryFieldNames.ApiTitleFingerprint, Query = typed }
			]
		};

	/// <summary>
	/// The fingerprint is one token, so the edit distance spans the whole title. The larger of the two applies, so an exact title
	/// never scores less than before and a typo only lifts what the exact match misses.
	/// </summary>
	private static Query TitleNamedLift(string typed) =>
		new DisMaxQuery
		{
			QueryName = TitleNamedQueryName,
			Queries =
			[
				Constant(TitleNamed(typed), ApiLookupWeights.TitleNamed),
				Constant(SearchQueryBuilder.FuzzyWordMatch(QueryFieldNames.ApiTitleFingerprint, typed), ApiLookupWeights.TitleNamedTypo)
			]
		};

	private static Query MethodLift(string method) =>
		new TermQuery { Field = QueryFieldNames.ApiMethod, Value = method.ToUpperInvariant(), Boost = ApiLookupWeights.Method };

	/// <summary>
	/// A word of the operation id typed whole, as its beginning, or one edit away. The three options share the role name. The
	/// beginning scores below the whole word because stemming makes <c>stats</c> the beginning of <c>state</c>, and a word too
	/// short for an edit has no typo option.
	/// </summary>
	private static Query OperationWordOption(string text, int index)
	{
		List<Query> options =
		[
			Constant(SynonymsMatch(QueryFieldNames.ApiWords, text), ApiLookupWeights.OperationWord),
			Constant(BoolPrefix(text, OperationWordFields), ApiLookupWeights.OperationWordPrefix)
		];
		if (text.Length >= SearchQueryBuilder.FuzzyMinimumLength)
			options.Add(
				Constant(
					SearchQueryBuilder.FuzzyWordMatch(QueryFieldNames.ApiWords, text, SynonymsAnalyzer),
					ApiLookupWeights.OperationWordTypo
				)
			);
		return new DisMaxQuery { QueryName = ApiLookupRoles.QueryName(ApiLookupRole.Id, index), Queries = options };
	}

	private static Query TitleQuery(string text) => BoolPrefix(text, TitleCompletionFields);

	/// <summary>
	/// The typed words are all in the display title or the parent titles, such as <c>cloud</c> in <c>Cloud Enterprise API</c>. A landing,
	/// group, type or topic page also names itself through <c>search_title</c>, which for such a page is its title alone and, unlike the
	/// display title, is stemmed as the typed words are: <c>behavior analytics</c> names <c>Behavioral analytics</c>.
	/// </summary>
	private static Query EveryWordNamesTitleOrParents(string typed)
	{
		var words = Words(typed).Take(MaxTokens).ToArray();
		return words.Length == 0
			? new MatchNoneQuery()
			: new BoolQuery { Must = [.. words.Select((word, i) => WordNamesTitleOrItsPage(word, completes: i == words.Length - 1))] };
	}

	private static Query WordNamesTitleOrItsPage(string word, bool completes) =>
		new BoolQuery
		{
			Should = [WordNamesTitleOrParents(word, completes), PageWordNamesItsTitle(word, completes)],
			MinimumShouldMatch = 1
		};

	private static Query PageWordNamesItsTitle(string word, bool completes) =>
		new BoolQuery
		{
			Filter = [LandingOrPage()],
			Must =
			[
				completes
					? CompletedWord(QueryFieldNames.SearchTitleCompletion, word)
					: SearchQueryBuilder.FuzzyWordMatch(QueryFieldNames.SearchTitle, word, SynonymsAnalyzer)
			]
		};

	/// <summary>
	/// The last word is still being typed, so it completes. An earlier word that differs only after its first five letters still names a
	/// title: <c>create</c> finds <c>Creates a new user</c>, while <c>delte</c> does not become <c>Delete</c> and lift every title that has
	/// it. A word that analyzes to several tokens, such as a typed route, needs all of them. Every API operation has the root parent titled
	/// <c>API</c>, which names no operation, so <c>api</c> reads the display title only.
	/// </summary>
	private static Query WordNamesTitleOrParents(string word, bool completes)
	{
		var fields = word.Equals(RootParentTitle, StringComparison.OrdinalIgnoreCase) ? DisplayTitleOnly : TitleOrParentFields;
		return new BoolQuery
		{
			Should =
			[
				.. fields.SelectMany(
					field => TitleWordAnalyzers.Select(
						analyzer => (Query)(completes ? CompletedWord(field, word, analyzer) : EditedWord(field, word, analyzer))
					)
				)
			],
			MinimumShouldMatch = 1
		};
	}

	private static MatchBoolPrefixQuery CompletedWord(Field field, string word, string analyzer = SynonymsAnalyzer) =>
		new() { Field = field, Query = word, Analyzer = analyzer, Operator = Operator.And };

	private static MatchQuery EditedWord(Field field, string word, string analyzer)
	{
		var match = SearchQueryBuilder.FuzzyWordMatch(field, word, analyzer, TitleWordFixedLetters);
		match.Operator = Operator.And;
		return match;
	}

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

	private static Query Constant(Query filter, float boost, string? name = null) =>
		new ConstantScoreQuery { Filter = filter, Boost = boost, QueryName = name };
}
