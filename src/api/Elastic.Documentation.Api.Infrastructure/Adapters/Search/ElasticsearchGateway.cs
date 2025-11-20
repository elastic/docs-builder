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

	public async Task<(int TotalHits, List<SearchResultItem> Results)> HybridSearchWithRrfAsync(string query, int pageNumber, int pageSize, Cancel ctx = default)
	{
		_logger.LogInformation("Starting RRF hybrid search for '{Query}' with pageNumber={PageNumber}, pageSize={PageSize}", query, pageNumber, pageSize);

		const string preTag = "<mark>";
		const string postTag = "</mark>";

		var searchQuery = query.Replace("dotnet", "net", StringComparison.InvariantCultureIgnoreCase);

		var lexicalSearchRetriever =
			((Query)new PrefixQuery(Infer.Field<DocumentDto>(f => f.Title), searchQuery) { Boost = 10.0f, CaseInsensitive = true }
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
			;
		var semanticSearchRetriever =
				((Query)new SemanticQuery("title.semantic_text", searchQuery) { Boost = 5.0f }
				 || new SemanticQuery("abstract.semantic_text", searchQuery) { Boost = 3.0f }
				)
				&& !(Query)new TermsQuery(Infer.Field<DocumentDto>(f => f.Url.Suffix("keyword")),
					new TermsQueryField(["/docs", "/docs/", "/docs/404", "/docs/404/"]))
			;

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
}

[JsonSerializable(typeof(DocumentDto))]
[JsonSerializable(typeof(ParentDocumentDto))]
internal sealed partial class EsJsonContext : JsonSerializerContext;
