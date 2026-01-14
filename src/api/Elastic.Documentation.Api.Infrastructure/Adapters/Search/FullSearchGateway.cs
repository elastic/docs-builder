// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.RegularExpressions;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Elastic.Documentation.Api.Core.Search;
using Elastic.Documentation.Api.Infrastructure.Adapters.Search.Common;
using Elastic.Documentation.Search;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Api.Infrastructure.Adapters.Search;

/// <summary>
/// Full-page search gateway implementation.
/// Uses hybrid RRF search for semantic queries, lexical-only for keyword queries.
/// </summary>
public partial class FullSearchGateway(
	ElasticsearchClientAccessor clientAccessor,
	ILogger<FullSearchGateway> logger) : IFullSearchGateway
{
	/// <summary>
	/// Regex pattern to detect semantic/question queries.
	/// </summary>
	private static readonly Regex SemanticKeywords = SemanticKeywordsRegex();

	[GeneratedRegex(@"^(how|why|what|when|where|can|should|is it|do i|does|will|would|could)", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
	private static partial Regex SemanticKeywordsRegex();

	/// <summary>
	/// Detects if a query is a semantic/question query.
	/// </summary>
	private static bool IsSemanticQuery(string query)
	{
		var trimmed = query.Trim();
		var wordCount = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
		return SemanticKeywords.IsMatch(trimmed) ||
			trimmed.EndsWith('?') ||
			wordCount > 3;
	}

	public async Task<FullSearchResult> SearchAsync(FullSearchRequest request, Cancel ctx = default)
	{
		var isSemantic = IsSemanticQuery(request.Query);

		logger.LogDebug("Full search for query '{Query}' - semantic: {IsSemantic}", request.Query, isSemantic);

		return isSemantic
			? await SearchWithHybridRrf(request, ctx)
			: await SearchLexicalOnly(request, ctx);
	}

	/// <summary>
	/// Performs hybrid RRF search combining lexical and semantic queries.
	/// Used when the query is detected as a semantic/question query.
	/// </summary>
	private async Task<FullSearchResult> SearchWithHybridRrf(FullSearchRequest request, Cancel ctx)
	{
		const string preTag = "<mark>";
		const string postTag = "</mark>";

		var lexicalQuery = BuildFullSearchLexicalQuery(request.Query);
		var semanticQuery = SearchQueryBuilder.BuildSemanticQuery(request.Query);

		// Combine lexical and semantic with bool should for hybrid search
		// In a production setup, you would use RRF retriever, but for compatibility
		// we'll use a combined bool query with semantic boosting
		var hybridQuery = new BoolQuery
		{
			Should = [lexicalQuery, semanticQuery],
			MinimumShouldMatch = 1
		};

		// Apply filters
		var filteredQuery = ApplyFilters(hybridQuery, request);

		try
		{
			var response = await clientAccessor.Client.SearchAsync<DocumentationDocument>(s =>
			{
				_ = s
					.Indices(clientAccessor.Options.IndexName)
					.From(Math.Max(request.PageNumber - 1, 0) * request.PageSize)
					.Size(request.PageSize)
					.Query(filteredQuery)
					.Aggregations(agg => agg
						.Add("type", a => a.Terms(t => t.Field(f => f.Type)))
						.Add("navigation_section", a => a.Terms(t => t.Field(f => f.NavigationSection)))
					)
					.Source(sf => sf
						.Filter(f => f
							.Includes(
								e => e.Type,
								e => e.Title,
								e => e.SearchTitle,
								e => e.Url,
								e => e.Description,
								e => e.Parents,
								e => e.Headings,
								e => e.NavigationSection,
								e => e.AiShortSummary,
								e => e.AiRagOptimizedSummary,
								e => e.LastUpdated
							)
						)
					)
					.Highlight(h => h
						.Fields(f => f
							.Add(Infer.Field<DocumentationDocument>(d => d.Title), hf => hf
								.FragmentSize(150)
								.NumberOfFragments(3)
								.NoMatchSize(150)
								.HighlightQuery(q => q.Match(m => m
									.Field(d => d.Title)
									.Query(request.Query)
									.Analyzer("highlight_analyzer")
								))
								.PreTags(preTag)
								.PostTags(postTag))
							.Add(Infer.Field<DocumentationDocument>(d => d.StrippedBody), hf => hf
								.FragmentSize(150)
								.NumberOfFragments(3)
								.NoMatchSize(150)
								.PreTags(preTag)
								.PostTags(postTag))
						)
					);

				ApplySorting(s, request.SortBy);
			}, ctx);

			if (!response.IsValidResponse)
			{
				logger.LogWarning("Elasticsearch response is not valid. Reason: {Reason}",
					response.ElasticsearchServerError?.Error.Reason ?? "Unknown");
			}

			return ProcessSearchResponse(response, request.Query, isSemanticQuery: true);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Error occurred during hybrid RRF search");
			throw;
		}
	}

	/// <summary>
	/// Performs lexical-only search.
	/// Used when the query is a simple keyword query.
	/// </summary>
	private async Task<FullSearchResult> SearchLexicalOnly(FullSearchRequest request, Cancel ctx)
	{
		const string preTag = "<mark>";
		const string postTag = "</mark>";

		var lexicalQuery = BuildFullSearchLexicalQuery(request.Query);
		var filteredQuery = ApplyFilters(lexicalQuery, request);

		try
		{
			var response = await clientAccessor.Client.SearchAsync<DocumentationDocument>(s =>
			{
				_ = s
					.Indices(clientAccessor.Options.IndexName)
					.From(Math.Max(request.PageNumber - 1, 0) * request.PageSize)
					.Size(request.PageSize)
					.Query(filteredQuery)
					.Aggregations(agg => agg
						.Add("type", a => a.Terms(t => t.Field(f => f.Type)))
						.Add("navigation_section", a => a.Terms(t => t.Field(f => f.NavigationSection)))
					)
					.Source(sf => sf
						.Filter(f => f
							.Includes(
								e => e.Type,
								e => e.Title,
								e => e.SearchTitle,
								e => e.Url,
								e => e.Description,
								e => e.Parents,
								e => e.Headings,
								e => e.NavigationSection,
								e => e.AiShortSummary,
								e => e.AiRagOptimizedSummary,
								e => e.LastUpdated
							)
						)
					)
					.Highlight(h => h
						.Fields(f => f
							.Add(Infer.Field<DocumentationDocument>(d => d.Title), hf => hf
								.FragmentSize(150)
								.NumberOfFragments(3)
								.NoMatchSize(150)
								.HighlightQuery(q => q.Match(m => m
									.Field(d => d.Title)
									.Query(request.Query)
									.Analyzer("highlight_analyzer")
								))
								.PreTags(preTag)
								.PostTags(postTag))
							.Add(Infer.Field<DocumentationDocument>(d => d.StrippedBody), hf => hf
								.FragmentSize(150)
								.NumberOfFragments(3)
								.NoMatchSize(150)
								.PreTags(preTag)
								.PostTags(postTag))
						)
					);

				ApplySorting(s, request.SortBy);
			}, ctx);

			if (!response.IsValidResponse)
			{
				logger.LogWarning("Elasticsearch response is not valid. Reason: {Reason}",
					response.ElasticsearchServerError?.Error.Reason ?? "Unknown");
			}

			return ProcessSearchResponse(response, request.Query, isSemanticQuery: false);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Error occurred during lexical search");
			throw;
		}
	}

	/// <summary>
	/// Builds the lexical search query for full-page search.
	/// Uses regular match on search_title (NOT prefix completion like autocomplete).
	/// </summary>
	private Query BuildFullSearchLexicalQuery(string searchQuery)
	{
		var tokens = searchQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries);

		// Regular match query on search_title (different from autocomplete which uses completion prefix)
		var query =
			(Query)new MultiMatchQuery
			{
				Query = searchQuery,
				Operator = Operator.And,
				Type = TextQueryType.BestFields,
				Analyzer = "synonyms_analyzer",
				Fields = new[] { "search_title^3", "stripped_body^0.1" }
			};

		// Add title keyword boost with synonyms
		var titleKeywordQuery = SearchQueryBuilder.GenerateTitleKeywordQuery(searchQuery, clientAccessor.SynonymBiDirectional);
		if (titleKeywordQuery is not null)
			query |= titleKeywordQuery;

		// Add title starts-with for short queries
		var startsWithQuery = SearchQueryBuilder.BuildTitleStartsWithQuery(searchQuery);
		if (startsWithQuery is not null)
			query |= startsWithQuery;

		// URL match for single tokens
		if (tokens.Length == 1)
			query |= SearchQueryBuilder.BuildUrlMatchQuery(searchQuery);

		// Phrase match for multi-token queries
		if (tokens.Length > 2)
			query |= SearchQueryBuilder.BuildPhraseMatchQuery(searchQuery);

		// Build positive query with filters and scoring
		var positiveQuery = new BoolQuery
		{
			Must = [query],
			Filter = [SearchQueryBuilder.DocumentFilter],
			Should = SearchQueryBuilder.ScoringQueries
		};

		// Apply diminish boost if configured
		var diminishQuery = SearchQueryBuilder.BuildDiminishQuery(clientAccessor.DiminishTerms);
		var baseQuery = SearchQueryBuilder.ApplyDiminishBoost(positiveQuery, diminishQuery);

		// Wrap with rule query if configured
		return SearchQueryBuilder.WrapWithRuleQuery(baseQuery, searchQuery, clientAccessor.RulesetName);
	}

	/// <summary>
	/// Applies filters to the query based on request parameters.
	/// Filters are placed in the Filter clause while the base query stays in Must to preserve scoring.
	/// </summary>
	private static Query ApplyFilters(Query baseQuery, FullSearchRequest request)
	{
		var filters = new List<Query>();

		// Type filter
		if (request.TypeFilter is { Length: > 0 })
		{
			filters.Add(new TermsQuery(
				Infer.Field<DocumentationDocument>(f => f.Type),
				new TermsQueryField(request.TypeFilter.Select(t => (FieldValue)t).ToArray())));
		}

		// Navigation section filter
		if (request.SectionFilter is { Length: > 0 })
		{
			filters.Add(new TermsQuery(
				Infer.Field<DocumentationDocument>(f => f.NavigationSection),
				new TermsQueryField(request.SectionFilter.Select(s => (FieldValue)s).ToArray())));
		}

		// TODO: Add nested applies_to filters when deployment/version filters are provided
		// This requires nested queries for the applies_to field

		if (filters.Count == 0)
			return baseQuery;

		// Keep baseQuery in Must to preserve scoring, only put actual filters in Filter
		return new BoolQuery
		{
			Must = [baseQuery],
			Filter = filters
		};
	}

	/// <summary>
	/// Applies sorting to the search request.
	/// </summary>
	private static void ApplySorting(SearchRequestDescriptor<DocumentationDocument> descriptor, string sortBy)
	{
		switch (sortBy.ToLowerInvariant())
		{
			case "recent":
				_ = descriptor.Sort(s => s.Field(f => f.LastUpdated, sf => sf.Order(SortOrder.Desc)));
				break;
			case "alpha":
				_ = descriptor.Sort(s => s.Field("title.keyword", sf => sf.Order(SortOrder.Asc)));
				break;
			default:
				// "relevance" is default - use _score
				break;
		}
	}

	/// <summary>
	/// Processes the search response into a FullSearchResult.
	/// </summary>
	private FullSearchResult ProcessSearchResponse(
		SearchResponse<DocumentationDocument> response,
		string searchQuery,
		bool isSemanticQuery)
	{
		var totalHits = (int)response.Total;

		var results = response.Hits.Select(hit =>
		{
			var item = SearchResultProcessor.ProcessHit(hit, searchQuery, clientAccessor.SynonymBiDirectional);
			return new FullSearchResultItem
			{
				Type = item.Type,
				Url = item.Url,
				Title = item.Title,
				Description = item.Description,
				Parents = item.Parents.Select(p => new FullSearchResultParent
				{
					Title = p.Title,
					Url = p.Url
				}).ToArray(),
				Score = item.Score,
				AiShortSummary = item.AiShortSummary,
				AiRagOptimizedSummary = item.AiRagOptimizedSummary,
				NavigationSection = item.NavigationSection,
				LastUpdated = item.LastUpdated
			};
		}).ToList();

		var aggregations = new FullSearchAggregations
		{
			Type = SearchResultProcessor.ExtractTypeAggregations(response),
			NavigationSection = SearchResultProcessor.ExtractNavigationSectionAggregations(response),
			DeploymentType = SearchResultProcessor.ExtractDeploymentTypeAggregations(response)
		};

		return new FullSearchResult
		{
			TotalHits = totalHits,
			Results = results,
			Aggregations = aggregations,
			IsSemanticQuery = isSemanticQuery
		};
	}
}
