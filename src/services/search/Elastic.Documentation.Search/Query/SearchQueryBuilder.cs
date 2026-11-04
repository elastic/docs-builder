// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Search.Contract;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Elastic.Clients.Elasticsearch.Core.Search;

#pragma warning disable IDE0130 // 'Query' subfolder would shadow the ES client's Query type
namespace Elastic.Documentation.Search;

/// <summary>
/// Shared query-building utilities. Field names are string literals (see <see cref="QueryFieldNames"/>)
/// so the builder is decoupled from any specific TDocument and reusable across docs-only and
/// unified indices.
/// </summary>
public static class SearchQueryBuilder
{
	public const string ApiPropertyHits = "api_properties";
	public const string ApiNameHits = "api_name";
	public const string ApiChildrenHits = "api_children";

	/// <summary>Elasticsearch needs one inner-hits name per nested query, so each reading of a query gets its own.</summary>
	public static bool IsApiPropertyHits(string innerHitsName) =>
		innerHitsName == ApiPropertyHits || innerHitsName.StartsWith(ApiPropertyHits + "_", StringComparison.Ordinal);

	private static string InnerHitsName(int reading) => reading == 0 ? ApiPropertyHits : $"{ApiPropertyHits}_{reading}";

	private static readonly HashSet<string> HttpMethods = new(
		["get", "post", "put", "patch", "delete", "head", "options"],
		StringComparer.OrdinalIgnoreCase
	);
	private static readonly string[] ApiPropertySourceIncludes = [$"{QueryFieldNames.ApiProperties}.*"];

	/// <summary>
	/// Rank-feature and term boosts applied as <c>should</c> clauses on top of the base query.
	/// </summary>
	public static Query[] ScoringQueries { get; } =
	[
		new RankFeatureQuery { Field = QueryFieldNames.NavigationDepth, Boost = 0.8f },
		new RankFeatureQuery { Field = QueryFieldNames.NavigationTableOfContents, Boost = 0.8f },
		new TermQuery { Field = QueryFieldNames.Section, Value = "reference", Boost = 0.15f },
		new TermQuery { Field = QueryFieldNames.Section, Value = "getting-started", Boost = 0.1f }
	];

	/// <summary>Excludes hidden documents and bare root-URL placeholders.</summary>
	public static Query DocumentFilter { get; } = new BoolQuery
	{
		MustNot =
		[
			new TermsQuery(QueryFieldNames.PathKeyword, new TermsQueryField(["/docs", "/docs/", "/docs/404", "/docs/404/"])),
			new TermQuery { Field = QueryFieldNames.Hidden, Value = true }
		]
	};

	/// <summary>The nested field query for the query's first reading, see <see cref="ParseApiPropertyQuery"/>.</summary>
	public static Query? BuildApiPropertyQuery(string searchQuery, string? typeFilter) =>
		ParseApiPropertyQuery(searchQuery, typeFilter).Readings is [var first, ..]
			? BuildNestedPropertyQuery(first.Property, ApiPropertyHits, allowPrefix: typeFilter == "api")
			: null;

	/// <summary>A trailing dot on an API-filtered field, as in <c>retriever.</c>, asks for more of its children.</summary>
	public static bool ExpandsChildren(string searchQuery, string? typeFilter) =>
		typeFilter == "api" && ParseApiPropertyQuery(searchQuery, typeFilter).ExpandsChildren;

	public static Query ApplyApiPropertyQuery(Query query, string searchQuery, string? typeFilter)
	{
		var parsed = ParseApiPropertyQuery(searchQuery, typeFilter);
		var branches = parsed
			.Readings
			.Select((r, i) => BuildReadingQuery(r, InnerHitsName(i), parsed.EndpointRequired, allowPrefix: typeFilter == "api"))
			.OfType<Query>()
			.ToList();
		if (branches.Count == 0)
			return query;

		var properties = branches.Count == 1 ? branches[0] : new BoolQuery { Should = branches, MinimumShouldMatch = 1 };
		return new BoolQuery { Should = [query, properties], MinimumShouldMatch = 1, Filter = [DocumentFilter] };
	}

	private static Query? BuildReadingQuery(
		(string Operation, string Property) reading,
		string innerHitsName,
		bool endpointRequired,
		bool allowPrefix
	)
	{
		var properties = BuildNestedPropertyQuery(reading.Property, innerHitsName, allowPrefix);
		var endpoint = reading.Operation.Length == 0 ? null : BuildEndpointHalfQuery(reading.Operation);
		if (endpoint is null && endpointRequired)
			return null;
		// Named after its inner hits so the result processor can tell which reading a hit matched: inner hits are
		// computed from the nested query alone, even for a reading whose endpoint did not hold, and a hit the lexical
		// branch alone matched carries no name. The name sits on the whole reading, as Elasticsearch reports a named
		// clause that matched even when its parent did not.
		List<Query> must = endpoint is not null && endpointRequired ? [properties, endpoint] : [properties];
		List<Query> should = endpoint is not null && !endpointRequired ? [endpoint] : [];
		if (allowPrefix)
			should.Add(BuildChildrenLiftQuery(reading.Property));
		return new BoolQuery { Must = must, Should = should.Count == 0 ? null : should, QueryName = innerHitsName };
	}

	/// <summary>
	/// Exact name matches all score alike, so Elasticsearch breaks their ties arbitrarily; a reader typing a path wants
	/// the operation that lists what comes next.
	/// </summary>
	private static Query BuildChildrenLiftQuery(string property) =>
		new NestedQuery
		{
			Path = QueryFieldNames.ApiProperties,
			IgnoreUnmapped = true,
			ScoreMode = ChildScoreMode.Max,
			Query = new ConstantScoreQuery
			{
				Filter = new TermQuery { Field = QueryFieldNames.ApiPropertiesParent, Value = property, CaseInsensitive = true },
				Boost = 0.5f
			}
		};

	/// <summary>
	/// The endpoint half as typed and, for a bare word, as its underscore alias too: `deployments` is a route of
	/// its own while `search` means `_search`, and a required endpoint must accept either.
	/// </summary>
	private static Query? BuildEndpointHalfQuery(string operation)
	{
		var candidates = new[] { BuildApiEndpointQuery(operation), BuildBareWordEndpointQuery(operation) }.OfType<Query>().ToList();
		return candidates.Count switch
		{
			0 => null,
			1 => candidates[0],
			_ => new DisMaxQuery { Queries = candidates }
		};
	}

	/// <summary>
	/// The ways a query may split into an endpoint half and a field half; empty when it never names a field.
	/// A word that starts with a slash, or an HTTP method right before one, can only be endpoint, so the field is the
	/// last word that is neither; on its own, `get` or `options` may name a field. Two plain words are ambiguous, so
	/// both orders are returned, the last word as the field first.
	/// </summary>
	private static ApiPropertyReadings ParseApiPropertyQuery(string searchQuery, string? typeFilter)
	{
		var input = searchQuery.Trim();
		if (input.Length == 0 || typeFilter is not (null or "" or "all" or "api"))
			return ApiPropertyReadings.None;
		var parsed = WithoutTrailingDots(ParseReadings(input.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)));
		// The docs-wide search only tries a field word that looks like an identifier; a plain word would lift every
		// API operation that happens to have a field of that name above the pages about it. Dropping a reading
		// leaves the others as demanding as the query made them.
		return typeFilter == "api"
			? parsed
			: parsed with { Readings = parsed.Readings.Where(r => r.Property.Contains('.') || r.Property.Contains('_')).ToArray() };
	}

	private static ApiPropertyReadings ParseReadings(string[] words)
	{
		var endpointWords = words.Select(
			(w, i) => w.StartsWith('/') || (HttpMethods.Contains(w) && i + 1 < words.Length && words[i + 1].StartsWith('/'))
		).ToArray();
		// With one reading the endpoint only lifts the operation. With two, the index decides which reading holds,
		// so each requires its endpoint: a bare word that names no endpoint, such as `_knn`, matches nothing.
		if (!endpointWords.Any(e => e) && words.Length == 2)
			return new ApiPropertyReadings([(words[0], words[1]), (words[1], words[0])], EndpointRequired: true);

		var field = Array.LastIndexOf(endpointWords, false);
		if (field < 0)
			return ApiPropertyReadings.None;
		return new ApiPropertyReadings([(string.Join(' ', words.Where((_, i) => i != field)), words[field])], EndpointRequired: false);
	}

	/// <summary>A trailing dot is a child field not yet typed, so <c>retriever.standard.</c> still names <c>retriever.standard</c>.</summary>
	private static ApiPropertyReadings WithoutTrailingDots(ApiPropertyReadings parsed)
	{
		var readings = parsed
			.Readings
			.Select(r => r with { Property = r.Property.TrimEnd('.') })
			.Where(r => r.Property.Length > 0)
			.ToArray();
		return readings.Length == 0
			? ApiPropertyReadings.None
			: parsed with { Readings = readings, ExpandsChildren = parsed.Readings.Any(r => r.Property.EndsWith('.')) };
	}

	/// <summary>The readings of a query, and whether each must match its endpoint rather than only be lifted by it.</summary>
	private sealed record ApiPropertyReadings(IReadOnlyList<(string Operation, string Property)> Readings, bool EndpointRequired)
	{
		public static readonly ApiPropertyReadings None = new([], EndpointRequired: false);

		public bool ExpandsChildren { get; init; }
	}

	private static Query BuildNestedPropertyQuery(string property, string innerHitsName, bool allowPrefix) =>
		new NestedQuery
		{
			Path = QueryFieldNames.ApiProperties,
			IgnoreUnmapped = true,
			ScoreMode = ChildScoreMode.Max,
			Query = BuildPropertyMatchQuery(property, allowPrefix),
			InnerHits = new InnerHits
			{
				Name = innerHitsName,
				Size = 20,
				Source = new SourceConfig(new SourceFilter { Includes = ApiPropertySourceIncludes })
			}
		};

	/// <summary>
	/// An API-filtered lookup also lists the direct children of the typed path. They score below every name match,
	/// and a document never matches through them alone: a row naming the input as its parent implies that row exists.
	/// </summary>
	private static Query BuildPropertyMatchQuery(string input, bool allowPrefix)
	{
		var separator = input.LastIndexOf('.');
		var options = separator < 0
			? BuildPropertyNameOptions(input, allowPrefix)
			: BuildPropertyPathOptions(input, separator, allowPrefix);
		if (allowPrefix)
		{
			options.Add(new ConstantScoreQuery
			{
				Filter = new TermQuery { Field = QueryFieldNames.ApiPropertiesParent, Value = input, CaseInsensitive = true },
				Boost = 0.4f,
				QueryName = ApiChildrenHits
			});
		}
		return new DisMaxQuery { Queries = options };
	}

	private static List<Query> BuildPropertyPathOptions(string input, int separator, bool allowPrefix) =>
		[
			new DisMaxQuery { Queries = BuildPropertyNameOptions(input, allowPrefix) },
			new BoolQuery
			{
				Must = [new DisMaxQuery { Queries = BuildPropertyNameOptions(input[(separator + 1)..], allowPrefix) }],
				Filter =
				[
					new MatchBoolPrefixQuery { Field = QueryFieldNames.ApiPropertiesPathMatch, Query = input, Operator = Operator.And }
				]
			}
		];

	/// <summary>
	/// Bound field scores below operation matches, regardless of term rarity in nested documents. Only an
	/// API-filtered lookup matches by prefix: in the docs-wide search a prefix such as `s` would lift hundreds of operations.
	/// It alone lists children, so it alone names its name matches apart from them.
	/// </summary>
	private static List<Query> BuildPropertyNameOptions(string name, bool allowPrefix)
	{
		var exact = new ConstantScoreQuery
		{
			Filter = new TermQuery { Field = QueryFieldNames.ApiPropertiesName, Value = name, CaseInsensitive = true },
			Boost = 1,
			QueryName = allowPrefix ? ApiNameHits : null
		};
		if (!allowPrefix)
			return [exact];

		// A keyword prefix preserves underscores and field names longer than the edge-ngram limit.
		var prefix = new ConstantScoreQuery
		{
			Filter = new PrefixQuery { Field = QueryFieldNames.ApiPropertiesName, Value = name, CaseInsensitive = true },
			Boost = 0.6f,
			QueryName = ApiNameHits
		};
		return [exact, prefix];
	}

	public static Query? BuildDiminishQuery(IReadOnlyCollection<string> diminishTerms)
	{
		if (diminishTerms.Count == 0)
			return null;

		return new MultiMatchQuery
		{
			Query = string.Join(' ', diminishTerms),
			Operator = Operator.Or,
			Fields = new[] { QueryFieldNames.SearchTitle, QueryFieldNames.PathMatch }
		};
	}

	public static Query WrapWithRuleQuery(Query query, string searchQuery, string? rulesetName)
	{
		if (rulesetName is null)
			return query;

		return new RuleQuery
		{
			Organic = query,
			RulesetIds = [rulesetName],
			MatchCriteria = new RuleQueryMatchCriteria { QueryString = searchQuery }
		};
	}

	public static Query? GenerateTitleKeywordQuery(string searchQuery, IReadOnlyDictionary<string, string[]> synonymBiDirectional)
	{
		var q = searchQuery.ToLowerInvariant();

		// Known content issue workaround — "elasticsearch" matches too many titles to be useful as a keyword boost.
		if (q is "elasticsearch")
			return null;

		Query query = new TermQuery { Field = QueryFieldNames.TitleKeyword, Value = q };

		if (q.Contains(' '))
			return query;

		if (!synonymBiDirectional.TryGetValue(q, out var synonyms))
			return query;

		foreach (var synonym in synonyms)
			query |= new TermQuery { Field = QueryFieldNames.TitleKeyword, Value = synonym };

		return query;
	}

	/// <summary>
	/// Exact match on <c>api.endpoints</c> so typing an endpoint path (<c>_search</c>, <c>GET /_search</c>,
	/// <c>my-index/_search</c>) ranks that operation above every other API whose search title merely starts
	/// with the same token. A typed value matches the <c>*</c> of a parameter in any position. One <c>terms</c>
	/// clause per number of literal segments kept, weighted <c>5 * (1 + literals) / (1 + total)</c>, and
	/// <c>dis_max</c> keeps the best.
	/// </summary>
	public static Query? BuildApiEndpointQuery(string searchQuery)
	{
		var groups = ApiEndpointPath.QueryCandidates(ApiEndpointPath.NormalizeQuery(searchQuery));
		if (groups.Count == 0)
			return null;

		var total = groups[0].LiteralSegments;
		return new DisMaxQuery
		{
			Queries = groups.Select(
				g =>
					(Query)new ConstantScoreQuery
					{
						Filter = new TermsQuery(
							QueryFieldNames.ApiEndpoints,
							new TermsQueryField(g.Terms.Select(t => (FieldValue)t).ToArray())
						),
						Boost = 5.0f * (1 + g.LiteralSegments) / (1 + total)
					}
			).ToList()
		};
	}

	/// <summary>
	/// Elasticsearch endpoints are underscore-prefixed (<c>_search</c>, <c>_bulk</c>), so a bare word like
	/// <c>search</c> is scored as the endpoint <c>_search</c> too. Only meant for API-filtered lookups: in the
	/// unfiltered docs search it would put <c>/_aliases</c> above the "Aliases" docs page.
	/// </summary>
	public static Query? BuildBareWordEndpointQuery(string searchQuery)
	{
		var word = searchQuery.Trim();
		if (word.Length == 0 || word.StartsWith('_') || word.Any(c => c is '/' or ' ' or '{'))
			return null;

		return BuildApiEndpointQuery("_" + word);
	}

	/// <summary>
	/// Partial-match fallback for API lookups. The strict lexical query needs every typed word, so
	/// <c>create deployment cloud</c> finds only operations that contain all three. From three words on, this
	/// clause also matches operations that contain 75% of them, at a lower weight than a full match.
	/// </summary>
	public static Query? BuildLoosePartialMatchQuery(string searchQuery)
	{
		if (searchQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length < 3)
			return null;

		return new BoolQuery
		{
			Must =
			[
				new MultiMatchQuery
				{
					Query = searchQuery,
					Operator = Operator.Or,
					MinimumShouldMatch = "75%",
					Type = TextQueryType.BestFields,
					Analyzer = "synonyms_analyzer",
					Boost = 0.5f,
					Fields = new[] { QueryFieldNames.SearchTitle, QueryFieldNames.Body }
				}
			],
			Filter = [DocumentFilter]
		};
	}

	/// <summary>
	/// API-filtered lookups only: widens the match set with <see cref="BuildLoosePartialMatchQuery"/> and lifts the
	/// endpoint of a bare word. Both are no-ops for queries they don't apply to.
	/// </summary>
	public static Query ApplyApiRanking(Query lexicalQuery, string searchQuery)
	{
		var query = BuildLoosePartialMatchQuery(searchQuery) is { } loose
			? new BoolQuery { Should = [lexicalQuery, loose], MinimumShouldMatch = 1 }
			: lexicalQuery;

		return BuildBareWordEndpointQuery(searchQuery) is { } bareWordEndpoint
			? new BoolQuery { Must = [query], Should = [bareWordEndpoint] }
			: query;
	}

	public static Query BuildSemanticQuery(string searchQuery) =>
		(Query)new SemanticQuery(QueryFieldNames.TitleSemanticText, searchQuery) { Boost = 5.0f }
			|| new SemanticQuery(QueryFieldNames.SummarySemanticText, searchQuery) { Boost = 3.0f }
			|| new SemanticQuery(QueryFieldNames.AiRagSummarySemanticText, searchQuery) { Boost = 4.0f }
			|| new SemanticQuery(QueryFieldNames.AiQuestionsSemanticText, searchQuery) { Boost = 2.0f };

	// NOTE: BuildSemanticQueryProbe / BuildLexicalQueryProbe use SearchQueryComponents, now available
	// from the in-repo contract — restore these in a follow-up.

	public static Query BuildUrlMatchQuery(string searchQuery) =>
		new ConstantScoreQuery { Filter = new MatchQuery { Field = QueryFieldNames.PathMatch, Query = searchQuery }, Boost = 0.3f };

	public static Query? BuildTitleStartsWithQuery(string searchQuery)
	{
		if (searchQuery.Length > 10)
			return null;

		float? boost = searchQuery.Length switch
		{
			1 => 100.0f,
			2 => 4.0f,
			_ => null
		};

		return new ConstantScoreQuery
		{
			Filter = new TermQuery { Field = QueryFieldNames.TitleStartsWith, Value = searchQuery.ToLowerInvariant(), Boost = boost },
			Boost = boost
		};
	}

	public static Query BuildPhraseMatchQuery(string searchQuery) =>
		new MultiMatchQuery
		{
			Query = searchQuery,
			Operator = Operator.And,
			Type = TextQueryType.Phrase,
			Analyzer = "synonyms_analyzer",
			Boost = 0.2f,
			Fields = new[] { QueryFieldNames.Body }
		};

	/// <summary>
	/// Builds the lexical search query: prefix completion on <c>search_title.completion</c> +
	/// best-fields on <c>stripped_body</c>, OR'ed with title-keyword, title-starts-with,
	/// URL match (single-token), and phrase match (multi-token). Wrapped with the document
	/// filter, scoring rank-features, optional diminish-boost, and optional rule query.
	/// </summary>
	public static Query BuildLexicalQuery(
		string searchQuery,
		IReadOnlyDictionary<string, string[]> synonymBiDirectional,
		IReadOnlyCollection<string> diminishTerms,
		string? rulesetName
	)
	{
		var tokens = searchQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries);

		var query = (Query)new ConstantScoreQuery
		{
			Filter = new MultiMatchQuery
			{
				Query = searchQuery,
				Operator = Operator.And,
				Type = TextQueryType.BoolPrefix,
				Analyzer = "synonyms_analyzer",
				Fields = new[]
				{
					QueryFieldNames.SearchTitleCompletion,
					QueryFieldNames.SearchTitleCompletion2Gram,
					QueryFieldNames.SearchTitleCompletion3Gram
				}
			},
			Boost = 3.0f
		}
			|| new MultiMatchQuery
			{
				Query = searchQuery,
				Operator = Operator.And,
				Type = TextQueryType.BestFields,
				Analyzer = "synonyms_analyzer",
				Boost = 0.1f,
				Fields = new[] { QueryFieldNames.Body }
			};

		var titleKeywordQuery = GenerateTitleKeywordQuery(searchQuery, synonymBiDirectional);
		if (titleKeywordQuery is not null)
			query |= titleKeywordQuery;

		var apiEndpointQuery = BuildApiEndpointQuery(searchQuery);
		if (apiEndpointQuery is not null)
			query |= apiEndpointQuery;

		var startsWithQuery = BuildTitleStartsWithQuery(searchQuery);
		if (startsWithQuery is not null)
			query |= startsWithQuery;

		if (tokens.Length == 1)
			query |= BuildUrlMatchQuery(searchQuery);

		if (tokens.Length > 2)
			query |= BuildPhraseMatchQuery(searchQuery);

		var positiveQuery = new BoolQuery { Must = [query], Filter = [DocumentFilter], Should = ScoringQueries };

		var diminishQuery = BuildDiminishQuery(diminishTerms);
		var baseQuery = ApplyDiminishBoost(positiveQuery, diminishQuery);

		return WrapWithRuleQuery(baseQuery, searchQuery, rulesetName);
	}

	public static Query ApplyDiminishBoost(Query positiveQuery, Query? diminishQuery)
	{
		if (diminishQuery is null)
			return positiveQuery;

		return new BoostingQuery { Positive = positiveQuery, NegativeBoost = 0.8, Negative = diminishQuery };
	}
}

/// <summary>Serialization shape for <c>rule_query.match_criteria.query_string</c>.</summary>
public sealed record RuleQueryMatchCriteria
{
	[System.Text.Json.Serialization.JsonPropertyName("query_string")]
	public required string QueryString { get; init; }
}
