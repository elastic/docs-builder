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
		await ExactSearchAsync(query, pageNumber, pageSize, ctx);

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
							sh => sh.Prefix(p => p
								.Field("title.keyword")
								.Value(searchQuery)
								.CaseInsensitive(true)
								.Boost(300.0f)
							),
							sh => sh.Match(m => m
								.Field(f => f.Title)
								.Query(searchQuery)
								.Fuzziness("AUTO")
								.Boost(250.0f)
							),
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
							sh => sh.Match(m => m
								.Field("parents.title")
								.Query(searchQuery)
								.Boost(75.0f)
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
