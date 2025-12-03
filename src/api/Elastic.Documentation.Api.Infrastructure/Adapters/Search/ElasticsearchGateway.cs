// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Core.Search;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Elastic.Clients.Elasticsearch.Serialization;
using Elastic.Documentation.Api.Core.Search;
using Elastic.Documentation.AppliesTo;
using Elastic.Transport;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Api.Infrastructure.Adapters.Search;

internal sealed record DocumentDto
{
	[JsonPropertyName("title")]
	public required string Title { get; init; }

	[JsonPropertyName("search_title")]
	public required string SearchTitle { get; init; }

	[JsonPropertyName("url")]
	public required string Url { get; init; }

	[JsonPropertyName("navigation_depth")]
	public int NavigationDepth { get; init; }

	[JsonPropertyName("navigation_table_of_contents")]
	public int NavigationTableOfContents { get; init; }

	[JsonPropertyName("navigation_section")]
	public string? NavigationSection { get; init; }

	[JsonPropertyName("description")]
	public string? Description { get; init; }

	[JsonPropertyName("type")]
	public string Type { get; init; } = "doc";

	[JsonPropertyName("body")]
	public string? Body { get; init; }

	[JsonPropertyName("stripped_body")]
	public string? StrippedBody { get; init; }

	[JsonPropertyName("abstract")]
	public string? Abstract { get; init; }

	[JsonPropertyName("url_segment_count")]
	public int UrlSegmentCount { get; init; }

	[JsonPropertyName("headings")]
	public string[] Headings { get; init; } = [];

	[JsonPropertyName("parents")]
	public ParentDocumentDto[] Parents { get; init; } = [];

	[JsonPropertyName("applies_to")]
	public ApplicableTo? Applies { get; init; }

	[JsonPropertyName("hidden")]
	public bool Hidden { get; init; } = false;
}

internal sealed record ParentDocumentDto
{
	[JsonPropertyName("title")]
	public required string Title { get; init; }

	[JsonPropertyName("url")]
	public required string Url { get; init; }
}

public partial class ElasticsearchGateway : ISearchGateway
{
	private readonly ElasticsearchClient _client;
	private readonly ElasticsearchOptions _elasticsearchOptions;
	private readonly ILogger<ElasticsearchGateway> _logger;

	public ElasticsearchGateway(ElasticsearchOptions elasticsearchOptions, ILogger<ElasticsearchGateway> logger)
	{
		_logger = logger;
		_elasticsearchOptions = elasticsearchOptions;
		var nodePool = new SingleNodePool(new Uri(elasticsearchOptions.Url.Trim()));
		var clientSettings = new ElasticsearchClientSettings(
				nodePool,
				sourceSerializer: (_, settings) => new DefaultSourceSerializer(settings, EsJsonContext.Default)
			)
			.DefaultIndex(elasticsearchOptions.IndexName)
			.Authentication(new ApiKey(elasticsearchOptions.ApiKey));

		_client = new ElasticsearchClient(clientSettings);
	}

	public async Task<bool> CanConnect(Cancel ctx) => (await _client.PingAsync(ctx)).IsValidResponse;

	public async Task<(int TotalHits, List<SearchResultItem> Results)> SearchAsync(string query, int pageNumber, int pageSize, Cancel ctx = default) =>
		await HybridSearchWithRrfAsync(query, pageNumber, pageSize, ctx);

	/// <summary>
	/// Builds the lexical search query for the given search term.
	/// </summary>
	private static Query BuildLexicalQuery(string searchQuery)
	{
		var tokens = searchQuery.Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries);

		var query =
			(Query)new ConstantScoreQuery
			{
				Filter = new MultiMatchQuery
				{
					Query = searchQuery, Operator = Operator.And, Type = TextQueryType.BoolPrefix,
					Analyzer = "synonyms_analyzer",
					Boost = 1.0f,
					Fields = new[]
					{
						"search_title.completion",
						"search_title.completion._2gram",
						"search_title.completion._3gram"
					}
				},
				Boost = 6.0f
			}
			|| new MultiMatchQuery
			{
				Query = searchQuery, Operator = Operator.And, Type = TextQueryType.BestFields,
				Analyzer = "synonyms_analyzer",
				Boost = 0.1f,
				Fields = new[] { "stripped_body" }
			};
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
					Field = Infer.Field<DocumentDto>(f => f.Url.Suffix("match")),
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

		return new BoostingQuery
		{
			Positive = new BoolQuery
			{
				Must = [query],
				Filter = [DocumentFilter],
				Should = [
					new RankFeatureQuery {
						Field = Infer.Field<DocumentDto>(f => f.NavigationDepth),
						Boost = 0.8f
					},
					new RankFeatureQuery {
						Field = Infer.Field<DocumentDto>(f => f.NavigationTableOfContents),
						Boost = 0.8f
					},
					new TermQuery { Field = Infer.Field<DocumentDto>(f => f.NavigationSection ), Value = "reference", Boost = 0.15f },
					new TermQuery { Field = Infer.Field<DocumentDto>(f => f.NavigationSection ), Value = "getting-started", Boost = 0.1f }
				]
			},
			NegativeBoost = 0.8,
			Negative = new MultiMatchQuery
			{
				Query = "plugin client integration glossary", Operator = Operator.Or, Fields = new[] { "search_title", "url.match" }
			},
		};
	}

	/// <summary>
	/// Builds the semantic search query for the given search term.
	/// </summary>
	private static Query BuildSemanticQuery(string searchQuery) =>
		(Query)new SemanticQuery("title.semantic_text", searchQuery) { Boost = 5.0f }
		|| new SemanticQuery("abstract.semantic_text", searchQuery) { Boost = 3.0f };

	private static Query DocumentFilter { get; } = !(Query)new TermsQuery(Infer.Field<DocumentDto>(f => f.Url.Suffix("keyword")),
		new TermsQueryField(["/docs", "/docs/", "/docs/404", "/docs/404/"]))
		&& !(Query)new TermQuery { Field = Infer.Field<DocumentDto>(f => f.Hidden), Value = true };

	public async Task<(int TotalHits, List<SearchResultItem> Results)> HybridSearchWithRrfAsync(string query, int pageNumber, int pageSize,
		Cancel ctx = default)
	{
		_logger.LogInformation("Starting RRF hybrid search for '{Query}' with pageNumber={PageNumber}, pageSize={PageSize}", query, pageNumber, pageSize);

		const string preTag = "<mark>";
		const string postTag = "</mark>";

		var searchQuery = query;
		var lexicalSearchRetriever = BuildLexicalQuery(searchQuery);
		var semanticSearchRetriever = BuildSemanticQuery(searchQuery);

		try
		{
			var response = await _client.SearchAsync<DocumentDto>(s => s
				.Indices(_elasticsearchOptions.IndexName)
				.From(Math.Max(pageNumber - 1, 0) * pageSize)
				.Size(pageSize)
				.Query(BuildLexicalQuery(query))
				// .Retriever(r => r
				// 	.Rrf(rrf => rrf
				// 		.Filter(BuildFilter())
				// 		.Retrievers(
				// 			// Lexical/Traditional search retriever
				// 			ret => ret.Standard(std => std.Query(lexicalSearchRetriever)),
				// 			// Semantic search retriever
				// 			ret => ret.Standard(std => std.Query(semanticSearchRetriever))
				// 		)
				// 		.RankConstant(60) // Controls how much weight is given to document ranking
				// 		.RankWindowSize(100)
				// 	)
				// )
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
						.Add(Infer.Field<DocumentDto>(d => d.SearchTitle.Suffix("completion")), hf => hf
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
						.Add(Infer.Field<DocumentDto>(d => d.StrippedBody), hf => hf
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
				), ctx);

			if (!response.IsValidResponse)
			{
				_logger.LogWarning("Elasticsearch RRF search response was not valid. Reason: {Reason}",
					response.ElasticsearchServerError?.Error?.Reason ?? "Unknown");
			}
			else
				_logger.LogInformation("RRF search completed for '{Query}'. Total hits: {TotalHits}", query, response.Total);

			return ProcessSearchResponse(response);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error occurred during Elasticsearch RRF search for '{Query}'", query);
			throw;
		}
	}

	private static (int TotalHits, List<SearchResultItem> Results) ProcessSearchResponse(SearchResponse<DocumentDto> response)
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

		return (totalHits, results);
	}

	/// <summary>
	/// Explains why a document did or didn't match for a given query.
	/// Returns detailed scoring information using Elasticsearch's _explain API.
	/// </summary>
	public async Task<ExplainResult> ExplainDocumentAsync(string query, string documentUrl, Cancel ctx = default)
	{
		var searchQuery = query;
		var lexicalQuery = BuildLexicalQuery(searchQuery);
		//var semanticQuery = BuildSemanticQuery(searchQuery);

		// Combine queries with bool should to match RRF behavior
		var combinedQuery = (Query)new BoolQuery
		{
			Should = [lexicalQuery],
			MinimumShouldMatch = 1
		};

		try
		{
			// First, find the document by URL
			var getDocResponse = await _client.SearchAsync<DocumentDto>(s => s
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
			var explainResponse = await _client.ExplainAsync<DocumentDto>(_elasticsearchOptions.IndexName, documentId, e => e
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
	private static string FormatExplanation(Elastic.Clients.Elasticsearch.Core.Explain.ExplanationDetail? explanation, int indent)
	{
		if (explanation == null)
			return string.Empty;

		var indentStr = new string(' ', indent * 2);
		var value = explanation.Value.ToString("F4", System.Globalization.CultureInfo.InvariantCulture);
		var desc = explanation.Description ?? "No description";
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
		var searchResults = await HybridSearchWithRrfAsync(query, 1, 1, ctx);
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

[JsonSerializable(typeof(DocumentDto))]
[JsonSerializable(typeof(ParentDocumentDto))]
internal sealed partial class EsJsonContext : JsonSerializerContext;
