// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.RegularExpressions;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Elastic.Documentation.Search.Highlighting;
using Elastic.Internal.Search;
using Microsoft.Extensions.Logging;
using InternalSearch = Elastic.Internal.Search;
using SearchRequest = Elastic.Internal.Search.SearchRequest;
using SortMode = Elastic.Internal.Search.SortMode;

namespace Elastic.Documentation.Search;

/// <summary>
/// Default Elasticsearch-backed implementation of <see cref="ISearchService{TDocument}"/>.
/// Both methods share the lexical query builder; <see cref="SearchAsync"/> additionally adds the
/// hybrid lex+semantic path, filters, aggregations and sorting, while
/// <see cref="AutocompleteAsync"/> stays lexical-only with a post-filter and a lean <c>_source</c>.
/// </summary>
public partial class DefaultSearchService<TDocument>(
	ElasticsearchClient client,
	string indexAlias,
	SearchQueryConfiguration searchConfig,
	ILogger<DefaultSearchService<TDocument>> logger,
	IProductNameLookup? productNameLookup = null)
	: ISearchService<TDocument>
	where TDocument : SearchDocumentBase
{
	private const string PreTag = "<mark>";
	private const string PostTag = "</mark>";

	private static readonly string[] AutocompleteSourceIncludes =
	[
		"content_type", "title", "search_title", "url", "description", "parents", "headings"
	];

	private static readonly string[] SearchSourceIncludes =
	[
		"content_type", "title", "search_title", "url", "description", "parents", "headings",
		"navigation_section", "ai_short_summary", "ai_rag_optimized_summary",
		"last_updated", "product", "related_products"
	];

	private static readonly Regex SemanticKeywordsRegex = BuildSemanticKeywordsRegex();
	private static readonly Regex ExcludeFromHighlightRegex = BuildExcludeFromHighlightRegex();

	[GeneratedRegex(@"^(how|why|what|when|where|can|should|is it|do i|does|will|would|could)", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
	private static partial Regex BuildSemanticKeywordsRegex();

	[GeneratedRegex(@"^(how|why|what|when|where|can|should|is|it|do|i|does|will|would|could)$", RegexOptions.IgnoreCase)]
	private static partial Regex BuildExcludeFromHighlightRegex();

	private static readonly HighlightOptions FullPageHighlightOptions = new()
	{
		WholeWordOnly = true,
		MinTokenLength = 2,
		ExcludePattern = ExcludeFromHighlightRegex
	};

	/// <summary>Underlying client — exposed for diagnostic extensions (Explain etc.).</summary>
	public ElasticsearchClient Client => client;

	/// <summary>The read target (index name or alias) this service queries.</summary>
	public string IndexAlias => indexAlias;

	/// <summary>The active search configuration (synonyms, rules, diminish terms).</summary>
	public SearchQueryConfiguration Configuration => searchConfig;

	/// <summary>Health check — pings the underlying cluster.</summary>
	public async Task<bool> CanConnectAsync(CancellationToken ct = default)
	{
		try
		{
			var response = await client.PingAsync(ct);
			return response.IsValidResponse;
		}
		catch (Exception ex)
		{
			logger.LogWarning(ex, "Elasticsearch ping failed");
			return false;
		}
	}

	public async Task<AutocompleteResponse<TDocument>> AutocompleteAsync(AutocompleteRequest request, CancellationToken ct = default)
	{
		var lexicalQuery = SearchQueryBuilder.BuildLexicalQuery(
			request.Query,
			searchConfig.SynonymBiDirectional,
			searchConfig.DiminishTerms,
			searchConfig.RulesetName);

		Query? postFilter = null;
		if (!string.IsNullOrWhiteSpace(request.TypeFilter))
			postFilter = new TermQuery { Field = QueryFieldNames.ContentType, Value = request.TypeFilter };

		var response = await client.SearchAsync<TDocument>(s =>
		{
			_ = s
				.Indices(indexAlias)
				.From(Math.Max(request.PageNumber - 1, 0) * request.PageSize)
				.Size(request.PageSize)
				.Query(lexicalQuery)
				.Aggregations(agg => agg
					.Add("type", a => a.Terms(t => t.Field(QueryFieldNames.ContentType))))
				.Source(sf => sf.Filter(f => f.Includes(AutocompleteSourceIncludes)))
				.Highlight(h => h
					.Fields(f => f
						.Add(QueryFieldNames.Title, hf => hf
							.FragmentSize(150)
							.NumberOfFragments(3)
							.NoMatchSize(150)
							.HighlightQuery(q => q.Match(m => m
								.Field(QueryFieldNames.Title)
								.Query(request.Query)
								.Analyzer("highlight_analyzer")))
							.PreTags(PreTag)
							.PostTags(PostTag))
						.Add(QueryFieldNames.StrippedBody, hf => hf
							.FragmentSize(150)
							.NumberOfFragments(3)
							.NoMatchSize(150)
							.PreTags(PreTag)
							.PostTags(PostTag))));

			if (postFilter is not null)
				_ = s.PostFilter(postFilter);
		}, ct);

		if (!response.IsValidResponse)
			LogInvalidResponse(response.ElasticsearchServerError?.Error?.Reason ?? "Unknown");

		var results = response.Hits.Select(hit => SearchResultProcessor
			.ProcessHit(hit, request.Query, searchConfig.SynonymBiDirectional)).ToList();

		var typeAgg = SearchResultProcessor.ExtractTermsAggregation<TDocument>(response, "type");

		LogAutocompleteResults(request.PageSize, request.PageNumber, request.Query,
			results.Select(r => r.Document.Url).ToArray());

		// NOTE: ElasticsearchTookMs and IsValidResponse require a Contract version > 0.9.2 — restore when published.
		return new AutocompleteResponse<TDocument>
		{
			Results = results,
			TotalResults = response.Total,
			PageNumber = request.PageNumber,
			PageSize = request.PageSize,
			Aggregations = new AutocompleteAggregations { Type = typeAgg }
		};
	}

	// NOTE: probe-mode SearchAsync branch (request.Components bitmask path) requires
	// SearchQueryComponents from Elastic.Internal.Search.Contract — restore when that type is published.

	public async Task<InternalSearch.SearchResponse<TDocument>> SearchAsync(SearchRequest request, CancellationToken ct = default)
	{
		var isSemantic = searchConfig.SemanticEnabled && IsSemanticQuery(request.Query);

		var lexicalQuery = SearchQueryBuilder.BuildLexicalQuery(
			request.Query,
			searchConfig.SynonymBiDirectional,
			searchConfig.DiminishTerms,
			searchConfig.RulesetName);

		var baseQuery = isSemantic
			? new BoolQuery
			{
				Should = [lexicalQuery, SearchQueryBuilder.BuildSemanticQuery(request.Query)],
				MinimumShouldMatch = 1
			}
			: lexicalQuery;

		var filteredQuery = ApplyFilters(baseQuery, request);

		var response2 = await client.SearchAsync<TDocument>(s =>
		{
			_ = s
				.Indices(indexAlias)
				.From(Math.Max(request.PageNumber - 1, 0) * request.PageSize)
				.Size(request.PageSize)
				.Query(filteredQuery)
				.Aggregations(agg => agg
					.Add("type", a => a.Terms(t => t.Field(QueryFieldNames.ContentType)))
					.Add("navigation_section", a => a.Terms(t => t.Field(QueryFieldNames.NavigationSection)))
					.Add("product", a => a.Terms(t => t.Field(QueryFieldNames.RelatedProductsId).Size(100))))
				.Source(sf => sf.Filter(f => f.Includes(SearchSourceIncludes)));

			if (request.IncludeHighlighting)
			{
				_ = s.Highlight(h => h
					.Fields(f => f
						.Add(QueryFieldNames.Title, hf => hf
							.FragmentSize(150)
							.NumberOfFragments(3)
							.NoMatchSize(150)
							.HighlightQuery(q => q.Match(m => m
								.Field(QueryFieldNames.Title)
								.Query(request.Query)
								.Analyzer("highlight_analyzer")))
							.PreTags(PreTag)
							.PostTags(PostTag))
						.Add(QueryFieldNames.StrippedBody, hf => hf
							.FragmentSize(150)
							.NumberOfFragments(3)
							.NoMatchSize(150)
							.PreTags(PreTag)
							.PostTags(PostTag))));
			}

			ApplySorting(s, request.SortBy);
		}, ct);

		if (!response2.IsValidResponse)
			LogInvalidResponse(response2.ElasticsearchServerError?.Error?.Reason ?? "Unknown");

		var highlightOptions = request.IncludeHighlighting ? FullPageHighlightOptions : null;
		var results2 = response2.Hits.Select(hit => SearchResultProcessor
			.ProcessHit(hit, request.IncludeHighlighting ? request.Query : string.Empty,
				searchConfig.SynonymBiDirectional, highlightOptions)).ToList();

		var aggregations2 = new SearchAggregations
		{
			Type = SearchResultProcessor.ExtractTermsAggregation<TDocument>(response2, "type"),
			NavigationSection = SearchResultProcessor.ExtractTermsAggregation<TDocument>(response2, "navigation_section"),
			Product = SearchResultProcessor.ExtractTermsAggregation<TDocument>(response2, "product")
				.ToDictionary(kvp => kvp.Key, kvp => new InternalSearch.ProductAggregationBucket
				{
					Count = kvp.Value,
					DisplayName = productNameLookup is not null && productNameLookup.TryGetProductName(kvp.Key, out var name)
						? name
						: null
				})
		};

		LogSearchResults(request.PageSize, request.PageNumber, request.Query, isSemantic,
			results2.Select(r => r.Document.Url).ToArray());

		// NOTE: ElasticsearchTookMs and IsValidResponse require a Contract version > 0.9.2 — restore when published.
		return new InternalSearch.SearchResponse<TDocument>
		{
			Results = results2,
			TotalResults = response2.Total,
			PageNumber = request.PageNumber,
			PageSize = request.PageSize,
			Aggregations = aggregations2,
			IsSemanticQuery = isSemantic
		};
	}

	private static bool IsSemanticQuery(string query)
	{
		var trimmed = query.Trim();
		var wordCount = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
		return SemanticKeywordsRegex.IsMatch(trimmed) || trimmed.EndsWith('?') || wordCount > 3;
	}

	private static Query ApplyFilters(Query baseQuery, SearchRequest request)
	{
		var filters = new List<Query>();

		if (request.TypeFilter is { Length: > 0 })
		{
			filters.Add(new TermsQuery(
				QueryFieldNames.ContentType,
				new TermsQueryField(request.TypeFilter.Select(t => (FieldValue)t).ToArray())));
		}

		if (request.SectionFilter is { Length: > 0 })
		{
			filters.Add(new TermsQuery(
				QueryFieldNames.NavigationSection,
				new TermsQueryField(request.SectionFilter.Select(s => (FieldValue)s).ToArray())));
		}

		// AND semantics — each requested product must match.
		if (request.ProductFilter is { Length: > 0 })
		{
			foreach (var productId in request.ProductFilter)
				filters.Add(new TermQuery { Field = QueryFieldNames.RelatedProductsId, Value = productId });
		}

		// TODO: applies_to nested filters for DeploymentFilter / VersionFilter.

		if (filters.Count == 0)
			return baseQuery;

		return new BoolQuery
		{
			Must = [baseQuery],
			Filter = filters
		};
	}

	private static void ApplySorting(SearchRequestDescriptor<TDocument> descriptor, SortMode sortBy)
	{
		switch (sortBy)
		{
			case SortMode.Recent:
				_ = descriptor.Sort(s => s.Field(QueryFieldNames.LastUpdated, sf => sf.Order(SortOrder.Desc)));
				break;
			case SortMode.Alpha:
				_ = descriptor.Sort(s => s.Field(QueryFieldNames.TitleKeyword, sf => sf.Order(SortOrder.Asc)));
				break;
			case SortMode.Relevance:
			default:
				break;
		}
	}

	[LoggerMessage(Level = LogLevel.Warning, Message = "Elasticsearch response is not valid. Reason: {Reason}")]
	private partial void LogInvalidResponse(string reason);

	[LoggerMessage(Level = LogLevel.Debug, Message = "Autocomplete completed with {PageSize} (page {PageNumber}) results for '{SearchQuery}': {Urls}")]
	private partial void LogAutocompleteResults(int pageSize, int pageNumber, string searchQuery, string[] urls);

	[LoggerMessage(Level = LogLevel.Debug, Message = "Search completed with {PageSize} (page {PageNumber}) results for '{SearchQuery}' (semantic: {IsSemantic}): {Urls}")]
	private partial void LogSearchResults(int pageSize, int pageNumber, string searchQuery, bool isSemantic, string[] urls);
}
