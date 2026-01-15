// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Elastic.Documentation.Search;

namespace Elastic.Documentation.Api.Infrastructure.Adapters.Search.Common;

/// <summary>
/// Shared query building utilities for search operations.
/// Used by both FindPage (autocomplete) and FullSearch gateways.
/// </summary>
public static class SearchQueryBuilder
{
	/// <summary>
	/// Shared scoring queries using rank features for relevance boosting.
	/// </summary>
	public static Query[] ScoringQueries { get; } =
	[
		new RankFeatureQuery
		{
			Field = Infer.Field<DocumentationDocument>(f => f.NavigationDepth),
			Boost = 0.8f
		},
		new RankFeatureQuery
		{
			Field = Infer.Field<DocumentationDocument>(f => f.NavigationTableOfContents),
			Boost = 0.8f
		},
		new TermQuery { Field = Infer.Field<DocumentationDocument>(f => f.NavigationSection), Value = "reference", Boost = 0.15f },
		new TermQuery { Field = Infer.Field<DocumentationDocument>(f => f.NavigationSection), Value = "getting-started", Boost = 0.1f }
	];

	/// <summary>
	/// Shared document filter that excludes hidden documents and root URLs.
	/// </summary>
	public static Query DocumentFilter { get; } =
		!(Query)new TermsQuery(
			Infer.Field<DocumentationDocument>(f => f.Url.Suffix("keyword")),
			new TermsQueryField(["/docs", "/docs/", "/docs/404", "/docs/404/"]))
		&& !(Query)new TermQuery { Field = Infer.Field<DocumentationDocument>(f => f.Hidden), Value = true };

	/// <summary>
	/// Builds a diminish terms query to reduce scoring for common/noisy terms.
	/// </summary>
	public static Query? BuildDiminishQuery(IReadOnlyCollection<string> diminishTerms)
	{
		if (diminishTerms.Count == 0)
			return null;

		return new MultiMatchQuery
		{
			Query = string.Join(' ', diminishTerms),
			Operator = Operator.Or,
			Fields = new[] { "search_title", "url.match" }
		};
	}

	/// <summary>
	/// Wraps a query with a RuleQuery if a ruleset is configured.
	/// </summary>
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

	/// <summary>
	/// Generates a title keyword query with synonym support.
	/// </summary>
	public static Query? GenerateTitleKeywordQuery(
		string searchQuery,
		IReadOnlyDictionary<string, string[]> synonymBiDirectional)
	{
		var q = searchQuery.ToLowerInvariant();

		// Known content issue workaround
		if (q is "elasticsearch")
			return null;

		Query query = new TermQuery { Field = "title.keyword", Value = q };

		if (q.Contains(' '))
			return query;

		// Boost synonyms if the title matches fully
		if (!synonymBiDirectional.TryGetValue(q, out var synonyms))
			return query;

		foreach (var synonym in synonyms)
			query |= new TermQuery { Field = "title.keyword", Value = synonym };

		return query;
	}

	/// <summary>
	/// Builds a semantic query using semantic_text fields.
	/// </summary>
	public static Query BuildSemanticQuery(string searchQuery) =>
		(Query)new SemanticQuery("title.semantic_text", searchQuery) { Boost = 5.0f }
		|| new SemanticQuery("abstract.semantic_text", searchQuery) { Boost = 3.0f }
		|| new SemanticQuery("ai_rag_optimized_summary.semantic_text", searchQuery) { Boost = 4.0f }
		|| new SemanticQuery("ai_questions.semantic_text", searchQuery) { Boost = 2.0f };

	/// <summary>
	/// Builds a URL match query for single-token searches.
	/// </summary>
	public static Query BuildUrlMatchQuery(string searchQuery) =>
		new ConstantScoreQuery
		{
			Filter = new MatchQuery
			{
				Field = Infer.Field<DocumentationDocument>(f => f.Url.Suffix("match")),
				Query = searchQuery
			},
			Boost = 0.3f
		};

	/// <summary>
	/// Builds a title starts-with query for short queries.
	/// </summary>
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
				Field = "title.starts_with",
				Value = searchQuery.ToLowerInvariant(),
				Boost = boost
			},
			Boost = boost
		};
	}

	/// <summary>
	/// Builds a phrase match query for multi-token searches.
	/// </summary>
	public static Query BuildPhraseMatchQuery(string searchQuery) =>
		new MultiMatchQuery
		{
			Query = searchQuery,
			Operator = Operator.And,
			Type = TextQueryType.Phrase,
			Analyzer = "synonyms_analyzer",
			Boost = 0.2f,
			Fields = new[] { "stripped_body" }
		};

	/// <summary>
	/// Builds the lexical search query optimized for autocomplete behavior.
	/// Uses prefix completion queries for fast as-you-type search.
	/// Shared between FindPage (autocomplete) and FullSearch gateways.
	/// </summary>
	public static Query BuildLexicalQuery(
		string searchQuery,
		IReadOnlyDictionary<string, string[]> synonymBiDirectional,
		IReadOnlyCollection<string> diminishTerms,
		string? rulesetName)
	{
		var tokens = searchQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries);

		// Prefix completion query for autocomplete behavior
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
						"search_title.completion",
						"search_title.completion._2gram",
						"search_title.completion._3gram"
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
				Fields = new[] { "stripped_body" }
			};

		// Add title keyword boost with synonyms
		var titleKeywordQuery = GenerateTitleKeywordQuery(searchQuery, synonymBiDirectional);
		if (titleKeywordQuery is not null)
			query |= titleKeywordQuery;

		// Add title starts-with for short queries
		var startsWithQuery = BuildTitleStartsWithQuery(searchQuery);
		if (startsWithQuery is not null)
			query |= startsWithQuery;

		// URL match for single tokens
		if (tokens.Length == 1)
			query |= BuildUrlMatchQuery(searchQuery);

		// Phrase match for multi-token queries
		if (tokens.Length > 2)
			query |= BuildPhraseMatchQuery(searchQuery);

		// Build positive query with shared filters and scoring
		var positiveQuery = new BoolQuery
		{
			Must = [query],
			Filter = [DocumentFilter],
			Should = ScoringQueries
		};

		// Apply diminish boost if configured
		var diminishQuery = BuildDiminishQuery(diminishTerms);
		var baseQuery = ApplyDiminishBoost(positiveQuery, diminishQuery);

		// Wrap with rule query if configured
		return WrapWithRuleQuery(baseQuery, searchQuery, rulesetName);
	}

	/// <summary>
	/// Wraps a positive query with boosting query if diminish terms are configured.
	/// </summary>
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

/// <summary>
/// Internal record for RuleQuery match criteria serialization.
/// </summary>
internal sealed record RuleQueryMatchCriteria
{
	[System.Text.Json.Serialization.JsonPropertyName("query_string")]
	public required string QueryString { get; init; }
}
