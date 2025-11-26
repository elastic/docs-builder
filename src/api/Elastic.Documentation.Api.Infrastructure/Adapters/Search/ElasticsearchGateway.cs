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

	[JsonPropertyName("url")]
	public required string Url { get; init; }

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
	public bool Hidden { get; init; }
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

	public async Task<(int TotalHits, List<SearchResultItem> Results)> SearchAsync(string query, int pageNumber, int pageSize, Cancel ctx = default) =>
		await HybridSearchWithRrfAsync(query, pageNumber, pageSize, ctx);

	/// <summary>
	/// Builds the lexical search query for the given search term.
	/// </summary>
	private static Query BuildLexicalQuery(string searchQuery) =>
		((Query)new PrefixQuery(Infer.Field<DocumentDto>(f => f.Title.Suffix("keyword")), searchQuery) { Boost = 10.0f, CaseInsensitive = true }
		 || new MatchPhrasePrefixQuery(Infer.Field<DocumentDto>(f => f.Title), searchQuery) { Boost = 9.0f }
		 || new MatchQuery(Infer.Field<DocumentDto>(f => f.Title), searchQuery) { Operator = Operator.And, Boost = 8.0f }
		 || new MatchBoolPrefixQuery(Infer.Field<DocumentDto>(f => f.Title), searchQuery) { Boost = 6.0f }
		 || new MatchQuery(Infer.Field<DocumentDto>(f => f.Abstract), searchQuery) { Operator = Operator.And, Boost = 5.0f }
		 || new MatchQuery(Infer.Field<DocumentDto>(f => f.StrippedBody), searchQuery) { Operator = Operator.And, Boost = 4.5f }
		 || new MatchQuery(Infer.Field<DocumentDto>(f => f.Headings), searchQuery) { Operator = Operator.And, Boost = 4.5f }
		 || new MatchQuery(Infer.Field<DocumentDto>(f => f.Abstract), searchQuery) { Operator = Operator.Or, Boost = 4.0f }
		 || new MatchQuery(Infer.Field<DocumentDto>(f => f.StrippedBody), searchQuery) { Operator = Operator.Or, Boost = 3.0f }
		 || new MatchQuery(Infer.Field<DocumentDto>(f => f.Headings), searchQuery) { Operator = Operator.Or, Boost = 3.0f }
		 || new MatchQuery(Infer.Field<DocumentDto>(f => f.Parents.First().Title), searchQuery) { Boost = 2.0f }
		 || new MatchQuery(Infer.Field<DocumentDto>(f => f.Title), searchQuery) { Fuzziness = 1, Boost = 1.0f }
		)
		&& !(Query)new TermsQuery(Infer.Field<DocumentDto>(f => f.Url.Suffix("keyword")), new TermsQueryField(["/docs", "/docs/", "/docs/404", "/docs/404/"]))
		&& !(Query)new TermQuery { Field = Infer.Field<DocumentDto>(f => f.Hidden), Value = true };

	/// <summary>
	/// Builds the semantic search query for the given search term.
	/// </summary>
	private static Query BuildSemanticQuery(string searchQuery) =>
		((Query)new SemanticQuery("title.semantic_text", searchQuery) { Boost = 5.0f }
		 || new SemanticQuery("abstract.semantic_text", searchQuery) { Boost = 3.0f }
		)
		&& !(Query)new TermsQuery(Infer.Field<DocumentDto>(f => f.Url.Suffix("keyword")),
			new TermsQueryField(["/docs", "/docs/", "/docs/404", "/docs/404/"]))
		&& !(Query)new TermQuery { Field = Infer.Field<DocumentDto>(f => f.Hidden), Value = true };

	/// <summary>
	/// Normalizes the search query by replacing "dotnet" with "net".
	/// </summary>
	private static string NormalizeSearchQuery(string query) =>
		query.Replace("dotnet", "net", StringComparison.InvariantCultureIgnoreCase);

	public async Task<(int TotalHits, List<SearchResultItem> Results)> HybridSearchWithRrfAsync(string query, int pageNumber, int pageSize, Cancel ctx = default)
	{
		_logger.LogInformation("Starting RRF hybrid search for '{Query}' with pageNumber={PageNumber}, pageSize={PageSize}", query, pageNumber, pageSize);

		const string preTag = "<mark>";
		const string postTag = "</mark>";

		var searchQuery = NormalizeSearchQuery(query);
		var lexicalSearchRetriever = BuildLexicalQuery(searchQuery);
		var semanticSearchRetriever = BuildSemanticQuery(searchQuery);

		try
		{
			var response = await _client.SearchAsync<DocumentDto>(s => s
				.Indices(_elasticsearchOptions.IndexName)
				.Retriever(r => r
					.Rrf(rrf => rrf
						.Retrievers(
							// Lexical/Traditional search retriever
							ret => ret.Standard(std => std.Query(lexicalSearchRetriever)),
							// Semantic search retriever
							ret => ret.Standard(std => std.Query(semanticSearchRetriever))
						)
						.RankConstant(60) // Controls how much weight is given to document ranking
						.RankWindowSize(100)
					)
				)
		.From((pageNumber - 1) * pageSize)
		.Size(pageSize)
		.Source(sf => sf
			.Filter(f => f
				.Includes(
					e => e.Type,
					e => e.Title,
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

			string? highlightedBody = null;

			if (highlights != null)
			{
				if (highlights.TryGetValue("stripped_body", out var bodyHighlights) && bodyHighlights.Count > 0)
					highlightedBody = string.Join(". ", bodyHighlights.Select(h => h.TrimEnd('.')));
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
		var searchQuery = NormalizeSearchQuery(query);
		var lexicalQuery = BuildLexicalQuery(searchQuery);
		var semanticQuery = BuildSemanticQuery(searchQuery);

		// Combine queries with bool should to match RRF behavior
		var combinedQuery = (Query)new BoolQuery
		{
			Should = [lexicalQuery, semanticQuery],
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
					DocumentUrl = documentUrl,
					Found = true,
					Matched = false,
					Explanation = $"Error explaining document: {explainResponse.ElasticsearchServerError?.Error?.Reason ?? "Unknown error"}"
				};
			}

			return new ExplainResult
			{
				DocumentUrl = documentUrl,
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
	public required string DocumentUrl { get; init; }
	public bool Found { get; init; }
	public bool Matched { get; init; }
	public double Score { get; init; }
	public string Explanation { get; init; } = string.Empty;
}

[JsonSerializable(typeof(DocumentDto))]
[JsonSerializable(typeof(ParentDocumentDto))]
internal sealed partial class EsJsonContext : JsonSerializerContext;
