// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Elastic.Clients.Elasticsearch.Serialization;
using Elastic.Documentation.Api.Core.Search;
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

	[JsonPropertyName("abstract")]
	public required string Abstract { get; init; }

	[JsonPropertyName("url_segment_count")]
	public int UrlSegmentCount { get; init; }

	[JsonPropertyName("parents")]
	public ParentDocumentDto[] Parents { get; init; } = [];
}

internal sealed record ParentDocumentDto
{
	[JsonPropertyName("title")]
	public required string Title { get; init; }

	[JsonPropertyName("url")]
	public required string Url { get; init; }
}

public class ElasticsearchGateway : ISearchGateway
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

		var searchQuery = query.Replace("dotnet", "net", StringComparison.InvariantCultureIgnoreCase);

		var lexicalSearchRetriever =
			((Query)new PrefixQuery(Infer.Field<DocumentDto>(f => f.Title.Suffix("keyword")), searchQuery) { Boost = 10.0f, CaseInsensitive = true }
				|| new MatchQuery(Infer.Field<DocumentDto>(f => f.Title), searchQuery) { Operator = Operator.And, Boost = 8.0f }
				|| new MatchBoolPrefixQuery(Infer.Field<DocumentDto>(f => f.Title), searchQuery) { Boost = 6.0f }
				|| new MatchQuery(Infer.Field<DocumentDto>(f => f.Abstract), searchQuery) { Boost = 4.0f }
				|| new MatchQuery(Infer.Field<DocumentDto>(f => f.Parents.First().Title), searchQuery) { Boost = 2.0f }
				|| new MatchQuery(Infer.Field<DocumentDto>(f => f.Title), searchQuery) { Fuzziness = 1, Boost = 1.0f }
			)
				&& !(Query)new TermsQuery(Infer.Field<DocumentDto>(f => f.Url.Suffix("keyword")), new TermsQueryField(["/docs", "/docs/", "/docs/404", "/docs/404/"]))
			;
		var semanticSearchRetriever =
				((Query)new SemanticQuery("title.semantic_text", searchQuery) { Boost = 5.0f }
				 || new SemanticQuery("abstract", searchQuery) { Boost = 3.0f }
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
					)
				)
				.From((pageNumber - 1) * pageSize)
				.Size(pageSize), ctx);

			if (!response.IsValidResponse)
			{
				_logger.LogWarning("Elasticsearch RRF search response was not valid. Reason: {Reason}",
					response.ElasticsearchServerError?.Error?.Reason ?? "Unknown");
			}
			else
			{
				_logger.LogInformation("RRF search completed for '{Query}'. Total hits: {TotalHits}", query, response.Total);
			}

			return ProcessSearchResponse(response);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error occurred during Elasticsearch RRF search for '{Query}'", query);
			throw;
		}
	}

	public async Task<(int TotalHits, List<SearchResultItem> Results)> ExactSearchAsync(string query, int pageNumber, int pageSize, Cancel ctx = default)
	{
		_logger.LogInformation("Starting search for '{Query}' with pageNumber={PageNumber}, pageSize={PageSize}", query, pageNumber, pageSize);

		var searchQuery = query.Replace("dotnet", "net", StringComparison.InvariantCultureIgnoreCase);

		try
		{
			var response = await _client.SearchAsync<DocumentDto>(s => s
				.Indices(_elasticsearchOptions.IndexName)
				.Query(q => q
					.Bool(b => b
						.Should(
							// Tier 1: Exact/Prefix matches (highest boost)
							sh => sh.Prefix(p => p
								.Field("title.keyword")
								.Value(searchQuery)
								.CaseInsensitive(true)
								.Boost(300.0f)
							),

							// Tier 2: Semantic search (combined into one clause)
							sh => sh.DisMax(dm => dm
								.Queries(
									dq => dq.Semantic(sem => sem
										.Field("title.semantic_text")
										.Query(searchQuery)
									),
									dq => dq.Semantic(sem => sem
										.Field("abstract")
										.Query(searchQuery)
									)
								)
								.Boost(200.0f)
							),

							// Tier 3: Standard text matching
							sh => sh.DisMax(dm => dm
								.Queries(
									dq => dq.MatchBoolPrefix(m => m
										.Field(f => f.Title)
										.Query(searchQuery)
									),
									dq => dq.Match(m => m
										.Field(f => f.Title)
										.Query(searchQuery)
										.Operator(Operator.And)
									),
									dq => dq.Match(m => m
										.Field(f => f.Abstract)
										.Query(searchQuery)
									)
								)
								.Boost(100.0f)
							),

							// Tier 4: Parent matching
							sh => sh.Match(m => m
								.Field("parents.title")
								.Query(searchQuery)
								.Boost(75.0f)
							),

							// Tier 5: Fuzzy fallback
							sh => sh.Match(m => m
								.Field(f => f.Title)
								.Query(searchQuery)
								.Fuzziness(1) // Reduced from 2
								.Boost(25.0f)
							)
						)
						.MustNot(mn => mn.Terms(t => t
							.Field("url.keyword")
							.Terms(factory => factory.Value("/docs", "/docs/", "/docs/404", "/docs/404/"))
						))
						.MinimumShouldMatch(1)
					)
				)
				.From((pageNumber - 1) * pageSize)
				.Size(pageSize), ctx);

			if (!response.IsValidResponse)
			{
				_logger.LogWarning("Elasticsearch search response was not valid. Reason: {Reason}",
					response.ElasticsearchServerError?.Error?.Reason ?? "Unknown");
			}
			else
			{
				_logger.LogInformation("Search completed for '{Query}'. Total hits: {TotalHits}", query, response.Total);
			}

			return ProcessSearchResponse(response);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error occurred during Elasticsearch search for '{Query}'", query);
			throw;
		}
	}


	private static (int TotalHits, List<SearchResultItem> Results) ProcessSearchResponse(SearchResponse<DocumentDto> response)
	{
		var totalHits = (int)response.Total;

		var results = response.Documents.Select((doc, index) => new SearchResultItem
		{
			Url = doc.Url,
			Title = doc.Title,
			Description = doc.Description ?? string.Empty,
			Parents = doc.Parents.Select(parent => new SearchResultItemParent
			{
				Title = parent.Title,
				Url = parent.Url
			}).ToArray(),
			Score = (float)(response.Hits.ElementAtOrDefault(index)?.Score ?? 0.0)
		}).ToList();

		return (totalHits, results);
	}
}

[JsonSerializable(typeof(DocumentDto))]
[JsonSerializable(typeof(ParentDocumentDto))]
internal sealed partial class EsJsonContext : JsonSerializerContext;
