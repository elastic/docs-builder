// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using System.Text.Json.Serialization;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Aggregations;
using Elastic.Clients.Elasticsearch.Core.Explain;
using Elastic.Clients.Elasticsearch.Core.Search;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Elastic.Clients.Elasticsearch.Serialization;
using Elastic.Documentation.Api.Core;
using Elastic.Documentation.Api.Core.Search;
using Elastic.Documentation.Configuration.Search;
using Elastic.Documentation.Search;
using Elastic.Transport;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Api.Infrastructure.Adapters.Search;

public partial class ElasticsearchGateway : ISearchGateway
{
	private readonly ElasticsearchClient _client;
	private readonly ElasticsearchOptions _elasticsearchOptions;
	private readonly SearchConfiguration _searchConfiguration;
	private readonly ILogger<ElasticsearchGateway> _logger;
	private readonly IReadOnlyCollection<string> _diminishTerms;
	private readonly string? _rulesetName;

	public ElasticsearchGateway(ElasticsearchOptions elasticsearchOptions, SearchConfiguration searchConfiguration, ILogger<ElasticsearchGateway> logger)
	{
		_logger = logger;
		_elasticsearchOptions = elasticsearchOptions;
		_searchConfiguration = searchConfiguration;
		_rulesetName = searchConfiguration.Rules.Count > 0 ? ExtractRulesetName(elasticsearchOptions.IndexName) : null;
		var nodePool = new SingleNodePool(new Uri(elasticsearchOptions.Url.Trim()));
		var clientSettings = new ElasticsearchClientSettings(
				nodePool,
				sourceSerializer: (_, settings) => new DefaultSourceSerializer(settings, EsJsonContext.Default)
			)
			.DefaultIndex(elasticsearchOptions.IndexName)
			.Authentication(new ApiKey(elasticsearchOptions.ApiKey));

		_client = new ElasticsearchClient(clientSettings);

		_diminishTerms = searchConfiguration.DiminishTerms;
		DiminishTermsQuery = new MultiMatchQuery
		{
			Query = string.Join(' ', _diminishTerms),
			Operator = Operator.Or,
			Fields = new[] { "search_title", "url.match" }
		};
	}

	public async Task<bool> CanConnect(Cancel ctx) => (await _client.PingAsync(ctx)).IsValidResponse;

	public async Task<SearchResult> SearchAsync(string query, int pageNumber, int pageSize, string? filter = null, Cancel ctx = default) =>
		await SearchImplementation(query, pageNumber, pageSize, filter, ctx);

	/// <summary>
	/// Extracts the ruleset name from the index name.
	/// Index name format: "semantic-docs-{namespace}-latest" → ruleset: "docs-ruleset-{namespace}"
	/// </summary>
	private static string? ExtractRulesetName(string indexName)
	{
		// Index name format: semantic-docs-{namespace}-latest
		var parts = indexName.Split('-');
		if (parts is ["semantic", "docs", _, ..])
			return $"docs-ruleset-{parts[2]}";
		return null;
	}

	/// <summary>
	/// Wraps a query with a RuleQuery if a ruleset is configured.
	/// </summary>
	private Query WrapWithRuleQuery(Query query, string searchQuery)
	{
		if (_rulesetName is null)
			return query;

		return new RuleQuery
		{
			Organic = query,
			RulesetIds = [_rulesetName],
			MatchCriteria = new RuleQueryMatchCriteria { QueryString = searchQuery }
		};
	}

	private Query? GenerateTitleKeywordQuery(string searchQuery)
	{
		var q = searchQuery.ToLowerInvariant();
		// This is a known content issue
		// /docs/reference/apm/agents/dotnet/setup-elasticsearch has 'Elasticsearch' as title.
		// We need to address this at the source
		if (q is "elasticsearch")
			return null;
		Query query = new TermQuery { Field = "title.keyword", Value = q };
		if (q.Contains(' '))
			return query;
		// this ensures all synonyms get boosted if the title matches fully e.g
		// k8s and kubernetes would match page names either k8s or kubernetes
		if (!_searchConfiguration.SynonymBiDirectional.TryGetValue(q, out var synonyms))
			return query;
		foreach (var synonym in synonyms)
			query |= new TermQuery { Field = "title.keyword", Value = synonym };
		return query;
	}

	/// <summary>
	/// Builds the lexical search query for the given search term.
	/// </summary>
	private Query BuildLexicalQuery(string searchQuery)
	{
		var tokens = searchQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries);

		var query =
			(Query)new ConstantScoreQuery
			{
				Filter = new MultiMatchQuery
				{
					Query = searchQuery, Operator = Operator.And, Type = TextQueryType.BoolPrefix,
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
				Query = searchQuery, Operator = Operator.And, Type = TextQueryType.BestFields,
				Analyzer = "synonyms_analyzer",
				Boost = 0.1f,
				Fields = new[] { "stripped_body" }
			};

		var titleKeywordQuery = GenerateTitleKeywordQuery(searchQuery);
		if (titleKeywordQuery is not null)
			query |= titleKeywordQuery;

		if (searchQuery.Length <= 10)
		{
			float? boost = searchQuery.Length switch
			{
				1 => 100.0f,
				2 => 4.0f,
				_ => null
			};
			query |= new ConstantScoreQuery
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

		// If the search term is a single word, boost the URL match
		// This is to ensure that URLs that contain the search term are ranked higher than URLs that don't
		// We dampen the boost by wrapping it in a constant score query
		// This allows a query for `templates` which is an overloaded term to yield pages that contain `templates` in the URL
		if (tokens.Length == 1)
		{
			query |= new ConstantScoreQuery
			{
				Filter = new MatchQuery
				{
					Field = Infer.Field<DocumentationDocument>(f => f.Url.Suffix("match")),
					Query = searchQuery
				},
				Boost = 0.3f
			};
		}

		if (tokens.Length > 2)
		{
			query |= new MultiMatchQuery
			{
				Query = searchQuery, Operator = Operator.And, Type = TextQueryType.Phrase,
				Analyzer = "synonyms_analyzer",
				Boost = 0.2f,
				Fields = new[] { "stripped_body" }
			};
		}

		var positiveQuery = new BoolQuery
		{
			Must = [query],
			Filter = [DocumentFilter],
			Should = ScoringQueries
		};

		Query baseQuery = _diminishTerms.Count == 0
			? positiveQuery
			: new BoostingQuery
			{
				Positive = positiveQuery,
				NegativeBoost = 0.8,
				Negative = DiminishTermsQuery
			};

		return WrapWithRuleQuery(baseQuery, searchQuery);
	}

	/// <summary>
	/// Builds the semantic search query for the given search term.
	/// </summary>
	private static Query BuildSemanticQuery(string searchQuery) =>
		(Query)new SemanticQuery("title.semantic_text", searchQuery) { Boost = 5.0f }
		|| new SemanticQuery("abstract.semantic_text", searchQuery) { Boost = 3.0f };

	private Query DiminishTermsQuery { get; }

	private static Query[] ScoringQueries { get; } =
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

	private static Query DocumentFilter { get; } = !(Query)new TermsQuery(Infer.Field<DocumentationDocument>(f => f.Url.Suffix("keyword")),
		new TermsQueryField(["/docs", "/docs/", "/docs/404", "/docs/404/"]))
		&& !(Query)new TermQuery { Field = Infer.Field<DocumentationDocument>(f => f.Hidden), Value = true };

	public async Task<SearchResult> SearchImplementation(string query, int pageNumber, int pageSize, string? filter = null, Cancel ctx = default)
	{
		const string preTag = "<mark>";
		const string postTag = "</mark>";

		var searchQuery = query;
		var lexicalQuery = BuildLexicalQuery(searchQuery);

		// Build post_filter for type filtering (applied after aggregations are computed)
		Query? postFilter = null;
		if (!string.IsNullOrWhiteSpace(filter))
			postFilter = new TermQuery { Field = Infer.Field<DocumentationDocument>(f => f.Type), Value = filter };

		try
		{
			var response = await _client.SearchAsync<DocumentationDocument>(s =>
			{
				_ = s
					.Indices(_elasticsearchOptions.IndexName)
					.From(Math.Max(pageNumber - 1, 0) * pageSize)
					.Size(pageSize)
					.Query(lexicalQuery)
					.Aggregations(agg => agg
						.Add("type", a => a.Terms(t => t.Field(f => f.Type)))
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
								e => e.Headings
							)
						)
					)
					.Highlight(h => h
						.RequireFieldMatch(true)
						.Fields(f => f
							.Add(Infer.Field<DocumentationDocument>(d => d.SearchTitle.Suffix("completion")), hf => hf
								.FragmentSize(150)
								.NumberOfFragments(3)
								.NoMatchSize(150)
								.BoundaryChars(":.!?\t\n")
								.BoundaryScanner(BoundaryScanner.Sentence)
								.BoundaryMaxScan(15)
								.FragmentOffset(0)
								.HighlightQuery(q => q.Match(m => m
									.Field(d => d.SearchTitle.Suffix("completion"))
									.Query(searchQuery)
									.Analyzer("highlight_analyzer")
								))
								.PreTags(preTag)
								.PostTags(postTag))
							.Add(Infer.Field<DocumentationDocument>(d => d.StrippedBody), hf => hf
								.FragmentSize(150)
								.NumberOfFragments(3)
								.NoMatchSize(150)
								.BoundaryChars(":.!?\t\n")
								.BoundaryScanner(BoundaryScanner.Sentence)
								.BoundaryMaxScan(15)
								.FragmentOffset(0)
								.HighlightQuery(q => q.Match(m => m
									.Field(d => d.StrippedBody)
									.Query(searchQuery)
									.Analyzer("highlight_analyzer")
								))
								.PreTags(preTag)
								.PostTags(postTag))
						)
					);

				// Apply post_filter if a filter is specified
				if (postFilter is not null)
					_ = s.PostFilter(postFilter);
			}, ctx);

			if (!response.IsValidResponse)
			{
				_logger.LogWarning("Elasticsearch response is not valid. Reason: {Reason}",
					response.ElasticsearchServerError?.Error.Reason ?? "Unknown");
			}

			return ProcessSearchResponse(response);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error occurred during Elasticsearch search");
			throw;
		}
	}

	private static SearchResult ProcessSearchResponse(SearchResponse<DocumentationDocument> response)
	{
		var totalHits = (int)response.Total;

		var results = response.Documents.Select((doc, index) =>
		{
			var hit = response.Hits.ElementAtOrDefault(index);
			var highlights = hit?.Highlight;

			string? highlightedTitle = null;
			string? highlightedBody = null;

			if (highlights != null)
			{
				if (highlights.TryGetValue("stripped_body", out var bodyHighlights) && bodyHighlights.Count > 0)
					highlightedBody = string.Join(". ", bodyHighlights.Select(h => h.TrimEnd('.', ' ', '-')));

				if (highlights.TryGetValue("search_title.completion", out var titleHighlights) && titleHighlights.Count > 0)
					highlightedTitle = string.Join(". ", titleHighlights.Select(h => h.TrimEnd('.', ' ', '-')));
			}

			return new SearchResultItem
			{
				Url = doc.Url,
				Title = doc.Title,
				Type = doc.Type,
				Description = doc.Description ?? string.Empty,
				Headings = doc.Headings,
				Parents = doc.Parents.Select(parent => new SearchResultItemParent
				{
					Title = parent.Title,
					Url = parent.Url
				}).ToArray(),
				Score = (float)(hit?.Score ?? 0.0),
				HighlightedTitle = highlightedTitle,
				HighlightedBody = highlightedBody
			};
		}).ToList();

		// Extract aggregations
		var aggregations = new Dictionary<string, long>();
		if (response.Aggregations?.TryGetValue("type", out var typeAgg) == true && typeAgg is StringTermsAggregate stringTermsAgg)
		{
			foreach (var bucket in stringTermsAgg.Buckets)
				aggregations[bucket.Key.ToString()!] = bucket.DocCount;
		}

		return new SearchResult
		{
			TotalHits = totalHits,
			Results = results,
			Aggregations = aggregations
		};
	}

	/// <summary>
	/// Explains why a document did or didn't match for a given query.
	/// Returns detailed scoring information using Elasticsearch's _explain API.
	/// </summary>
	public async Task<ExplainResult> ExplainDocumentAsync(string query, string documentUrl, Cancel ctx = default)
	{
		var searchQuery = query;
		var lexicalQuery = BuildLexicalQuery(searchQuery);

		// Combine queries with bool should to match RRF behavior
		var combinedQuery = (Query)new BoolQuery
		{
			Should = [lexicalQuery],
			MinimumShouldMatch = 1
		};

		try
		{
			// First, find the document by URL
			var getDocResponse = await _client.SearchAsync<DocumentationDocument>(s => s
				.Indices(_elasticsearchOptions.IndexName)
				.Query(q => q.Term(t => t.Field(f => f.Url).Value(documentUrl)))
				.Size(1), ctx);

			if (!getDocResponse.IsValidResponse || getDocResponse.Documents.Count == 0)
			{
				return new ExplainResult
				{
					SearchTitle = "N/A",
					DocumentUrl = documentUrl,
					Found = false,
					Explanation = $"Document with URL '{documentUrl}' not found in index"
				};
			}

			var documentId = getDocResponse.Hits.First().Id;

			// Now explain why this document matches (or doesn't match) the query
			var explainResponse = await _client.ExplainAsync<DocumentationDocument>(_elasticsearchOptions.IndexName, documentId, e => e
				.Query(combinedQuery), ctx);

			if (!explainResponse.IsValidResponse)
			{
				return new ExplainResult
				{
					SearchTitle = "N/A",
					DocumentUrl = documentUrl,
					Found = true,
					Matched = false,
					Explanation = $"Error explaining document: {explainResponse.ElasticsearchServerError?.Error?.Reason ?? "Unknown error"}"
				};
			}

			return new ExplainResult
			{
				DocumentUrl = documentUrl,
				SearchTitle = getDocResponse.Documents.First().SearchTitle,
				Found = true,
				Matched = explainResponse.Matched,
				Score = explainResponse.Explanation?.Value ?? 0,
				Explanation = FormatExplanation(explainResponse.Explanation, 0)
			};
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error explaining document '{Url}' for query '{Query}'", documentUrl, query);
			return new ExplainResult
			{
				SearchTitle = "N/A",
				DocumentUrl = documentUrl,
				Found = false,
				Explanation = $"Exception during explain: {ex.Message}"
			};
		}
	}

	/// <summary>
	/// Formats the Elasticsearch explanation into a readable string with indentation.
	/// </summary>
	private static string FormatExplanation(ExplanationDetail? explanation, int indent)
	{
		if (explanation == null)
			return string.Empty;

		var indentStr = new string(' ', indent * 2);
		var value = explanation.Value.ToString("F4", CultureInfo.InvariantCulture);
		var desc = explanation.Description;
		var result = $"{indentStr}{value} - {desc}\n";

		if (explanation.Details != null && explanation.Details.Count > 0)
		{
			foreach (var detail in explanation.Details)
				result += FormatExplanation(detail, indent + 1);
		}

		return result;
	}

	/// <summary>
	/// Explains both the top search result and an expected document for comparison.
	/// Returns detailed scoring information for both documents.
	/// </summary>
	public async Task<(ExplainResult TopResult, ExplainResult ExpectedResult)> ExplainTopResultAndExpectedAsync(
		string query,
		string expectedDocumentUrl,
		Cancel ctx = default)
	{
		// First, get the top result
		var searchResults = await SearchImplementation(query, 1, 1, null, ctx);
		var topResultUrl = searchResults.Results.FirstOrDefault()?.Url;

		if (string.IsNullOrEmpty(topResultUrl))
		{
			var emptyResult = new ExplainResult
			{
				SearchTitle = "N/A",
				DocumentUrl = "N/A",
				Found = false,
				Explanation = "No search results returned"
			};
			return (emptyResult, await ExplainDocumentAsync(query, expectedDocumentUrl, ctx));
		}

		// Explain both documents
		var topResultExplain = await ExplainDocumentAsync(query, topResultUrl, ctx);
		var expectedResultExplain = await ExplainDocumentAsync(query, expectedDocumentUrl, ctx);

		return (topResultExplain, expectedResultExplain);
	}
}

/// <summary>
/// Result of explaining why a document matched or didn't match a query.
/// </summary>
public sealed record ExplainResult
{
	public required string SearchTitle { get; init; }
	public required string DocumentUrl { get; init; }
	public bool Found { get; init; }
	public bool Matched { get; init; }
	public double Score { get; init; }
	public string Explanation { get; init; } = string.Empty;
}

[JsonSerializable(typeof(DocumentationDocument))]
[JsonSerializable(typeof(ParentDocument))]
[JsonSerializable(typeof(RuleQueryMatchCriteria))]
internal sealed partial class EsJsonContext : JsonSerializerContext;

internal sealed record RuleQueryMatchCriteria
{
	[JsonPropertyName("query_string")]
	public required string QueryString { get; init; }
}
