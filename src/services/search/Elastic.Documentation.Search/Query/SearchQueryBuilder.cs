// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Clients.Elasticsearch.QueryDsl;

#pragma warning disable IDE0130 // 'Query' subfolder would shadow the ES client's Query type
namespace Elastic.Documentation.Search;

/// <summary>
/// Shared query-building utilities. Field names are string literals (see <see cref="QueryFieldNames"/>)
/// so the builder is decoupled from any specific TDocument and reusable across docs-only and
/// unified indices.
/// </summary>
public static class SearchQueryBuilder
{
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
	public static Query DocumentFilter { get; } =
		new BoolQuery
		{
			MustNot =
			[
				new TermsQuery(
					QueryFieldNames.PathKeyword,
					new TermsQueryField(["/docs", "/docs/", "/docs/404", "/docs/404/"])),
				new TermQuery { Field = QueryFieldNames.Hidden, Value = true }
			]
		};

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

	public static Query? GenerateTitleKeywordQuery(
		string searchQuery,
		IReadOnlyDictionary<string, string[]> synonymBiDirectional)
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

	public static Query BuildSemanticQuery(string searchQuery) =>
		(Query)new SemanticQuery(QueryFieldNames.TitleSemanticText, searchQuery) { Boost = 5.0f }
		|| new SemanticQuery(QueryFieldNames.SummarySemanticText, searchQuery) { Boost = 3.0f }
		|| new SemanticQuery(QueryFieldNames.AiRagSummarySemanticText, searchQuery) { Boost = 4.0f }
		|| new SemanticQuery(QueryFieldNames.AiQuestionsSemanticText, searchQuery) { Boost = 2.0f };

	// NOTE: BuildSemanticQueryProbe / BuildLexicalQueryProbe use SearchQueryComponents, now available
	// from the in-repo contract — restore these in a follow-up.

	public static Query BuildUrlMatchQuery(string searchQuery) =>
		new ConstantScoreQuery
		{
			Filter = new MatchQuery { Field = QueryFieldNames.PathMatch, Query = searchQuery },
			Boost = 0.3f
		};

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
			Filter = new TermQuery
			{
				Field = QueryFieldNames.TitleStartsWith,
				Value = searchQuery.ToLowerInvariant(),
				Boost = boost
			},
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
		string? rulesetName)
	{
		var tokens = searchQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries);

		var query =
			(Query)new ConstantScoreQuery
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

		var startsWithQuery = BuildTitleStartsWithQuery(searchQuery);
		if (startsWithQuery is not null)
			query |= startsWithQuery;

		if (tokens.Length == 1)
			query |= BuildUrlMatchQuery(searchQuery);

		if (tokens.Length > 2)
			query |= BuildPhraseMatchQuery(searchQuery);

		var positiveQuery = new BoolQuery
		{
			Must = [query],
			Filter = [DocumentFilter],
			Should = ScoringQueries
		};

		var diminishQuery = BuildDiminishQuery(diminishTerms);
		var baseQuery = ApplyDiminishBoost(positiveQuery, diminishQuery);

		return WrapWithRuleQuery(baseQuery, searchQuery, rulesetName);
	}

	public static Query ApplyDiminishBoost(Query positiveQuery, Query? diminishQuery)
	{
		if (diminishQuery is null)
			return positiveQuery;

		return new BoostingQuery
		{
			Positive = positiveQuery,
			NegativeBoost = 0.8,
			Negative = diminishQuery
		};
	}
}

/// <summary>Serialization shape for <c>rule_query.match_criteria.query_string</c>.</summary>
public sealed record RuleQueryMatchCriteria
{
	[System.Text.Json.Serialization.JsonPropertyName("query_string")]
	public required string QueryString { get; init; }
}
