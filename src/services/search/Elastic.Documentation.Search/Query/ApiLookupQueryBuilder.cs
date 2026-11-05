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
		var clauses = tokens.Select(TokenClause).OfType<Query>().ToList();
		if (clauses.Count == 0)
			return new MatchNoneQuery();

		var typed = query.Trim();
		return new BoolQuery
		{
			Must = [new BoolQuery { Should = clauses, MinimumShouldMatch = TokensMinimumShouldMatch }],
			Filter = [SearchQueryBuilder.DocumentFilter],
			Should = Lifts(typed, tokens)
		};
	}

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
	private static Query? TokenClause(ApiLookupToken token, int index)
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
			FieldOption(token, index),
			Constant(
				SynonymsMatch(QueryFieldNames.Body, token.Text),
				ApiLookupWeights.BodyWord,
				ApiLookupRoles.QueryName(ApiLookupRole.Body, index)
			)
		];
		return new DisMaxQuery { Queries = options.OfType<Query>().ToList(), TieBreaker = 0 };
	}

	/// <summary>
	/// A word or a typed route matches its endpoint exactly, or covers the routes under it just below. The bare view of the
	/// keys makes the underscore that starts an Elasticsearch system route optional, so <c>snapshot</c>, <c>/snapshot</c> and
	/// <c>/_snapshot</c> all cover <c>_snapshot/*/*/_restore</c>.
	/// </summary>
	private static Query? KeyOption(string text, int index)
	{
		var route = ApiEndpointPath.NormalizeQuery(text).TrimEnd('/');
		if (SearchQueryBuilder.BuildApiEndpointQuery(route, ApiLookupWeights.EndpointKey, QueryFieldNames.ApiEndpoints) is not { } exact)
			return null;

		List<Query> options =
		[
			exact,
			SearchQueryBuilder.BuildApiEndpointQuery(route, ApiLookupWeights.EndpointKey, QueryFieldNames.ApiEndpointsBare)!,
			RoutesUnder(route, QueryFieldNames.ApiEndpoints),
			RoutesUnder(route, QueryFieldNames.ApiEndpointsBare)
		];

		// An index written before the bare view exists has no api.endpoints.bare and the service deploys on its own,
		// so a bare word keeps its underscore alias on the exact field until every index carries the bare view.
		if (SearchQueryBuilder.BuildBareWordEndpointQuery(route, ApiLookupWeights.EndpointKey) is { } alias)
			options.Add(alias);
		if (SearchQueryBuilder.EndpointName(route) is { } name && name != route)
			options.Add(RoutesUnder(name, QueryFieldNames.ApiEndpoints));

		return new DisMaxQuery { Queries = options, QueryName = ApiLookupRoles.QueryName(ApiLookupRole.Key, index) };
	}

	private static Query RoutesUnder(string route, Field field) =>
		new ConstantScoreQuery { Filter = new PrefixQuery { Field = field, Value = route + "/" }, Boost = ApiLookupWeights.NamespaceKey };

	/// <summary>A method word, such as <c>get</c>, also stays an operation, title and description word.</summary>
	private static Query? MethodOption(string text, int index) =>
		HttpMethods.Contains(text)
			? Constant(
				new TermQuery { Field = QueryFieldNames.ApiMethod, Value = text.ToUpperInvariant() },
				ApiLookupWeights.Method,
				ApiLookupRoles.QueryName(ApiLookupRole.Method, index)
			)
			: null;

	private static Query FieldOption(ApiLookupToken token, int index) =>
		SearchQueryBuilder.BuildNestedPropertyQuery(
			token.Text,
			FieldHitsName(index),
			new SearchQueryBuilder.PropertyLookup(AllowPrefix: true, token.ExpandChildren),
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
