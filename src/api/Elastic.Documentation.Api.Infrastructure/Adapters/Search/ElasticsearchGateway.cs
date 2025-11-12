// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using System.Text.Json.Serialization;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Core.Search;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Elastic.Clients.Elasticsearch.Serialization;
using Elastic.Documentation.Api.Core.Search;
using Elastic.Documentation.AppliesTo;
using Elastic.Transport;
using Elastic.Transport.Extensions;
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

	public async Task<(int TotalHits, List<SearchResultItem> Results)> SearchAsync(Activity? parentActivity, string query, int pageNumber, int pageSize,
		Cancel ctx = default)
	{
		_ = parentActivity?.SetTag("db.operation.name", $"search {_elasticsearchOptions.IndexName}");
		_ = parentActivity?.SetTag("db.system.name", "elasticsearch");
		var elasticsearchUri = new Uri(_elasticsearchOptions.Url);
		_ = parentActivity?.SetTag("server.address", elasticsearchUri.Host);
		_ = parentActivity?.SetTag("server.port", elasticsearchUri.Port);
		var subdomain = elasticsearchUri.Host.Split('.').FirstOrDefault(); // the name of the ES project
		if (!string.IsNullOrEmpty(subdomain))
			_ = parentActivity?.SetTag("db.namespace", subdomain);

		return await HybridSearchWithRrfAsync(query, pageNumber, pageSize, parentActivity: parentActivity, ctx: ctx);
	}

	public async Task<(int TotalHits, List<SearchResultItem> Results)> HybridSearchWithRrfAsync(string query, int pageNumber, int pageSize, Activity? parentActivity = null, Cancel ctx = default)
	{
		_logger.LogInformation("Starting RRF hybrid search for '{Query}' with pageNumber={PageNumber}, pageSize={PageSize}", query, pageNumber, pageSize);

		const string preTag = "<mark>";
		const string postTag = "</mark>";

		var searchQuery = query.Replace("dotnet", "net", StringComparison.InvariantCultureIgnoreCase);

		var lexicalSearchRetriever =
			((Query)new PrefixQuery(Infer.Field<DocumentDto>(f => f.Title.Suffix("keyword")), searchQuery) { Boost = 10.0f, CaseInsensitive = true }
				|| new MatchQuery(Infer.Field<DocumentDto>(f => f.Title), searchQuery) { Operator = Operator.And, Boost = 8.0f }
				|| new MatchBoolPrefixQuery(Infer.Field<DocumentDto>(f => f.Title), searchQuery) { Boost = 6.0f }
				|| new MatchQuery(Infer.Field<DocumentDto>(f => f.Abstract), searchQuery) { Boost = 4.0f }
				|| new MatchQuery(Infer.Field<DocumentDto>(f => f.StrippedBody), searchQuery) { Boost = 3.0f }
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

		var searchDescriptor = new SearchRequestDescriptor<DocumentDto>()
			.Indices(_elasticsearchOptions.IndexName)
			.Retriever(r => r
				.Rrf(rrf => rrf
					.Retrievers(
						ret => ret.Standard(std => std.Query(lexicalSearchRetriever)),
						ret => ret.Standard(std => std.Query(semanticSearchRetriever))
					)
					.RankConstant(60)
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
						e => e.Parents
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
			);

		var requestJson = _client.RequestResponseSerializer.SerializeToString(searchDescriptor);
		_ = parentActivity?.SetTag("db.query.text", requestJson);
		_ = parentActivity?.SetTag("db.operation.name", "hybrid_search_rrf");

		try
		{
			var response = await _client.SearchAsync<DocumentDto>(searchDescriptor, ctx);
			if (!response.IsValidResponse)
			{
				var reason = response.ElasticsearchServerError?.Error.Reason ?? "Unknown";
				_logger.LogError("Elasticsearch RRF search failed for '{Query}'. Reason: {Reason}", query, reason);
				_ = parentActivity?.SetTag("db.response.status_code", response.ElasticsearchServerError?.Status);
				_ = parentActivity?.SetStatus(ActivityStatusCode.Error, reason);
			}
			else
			{
				_logger.LogInformation("RRF search completed for '{Query}'. Total hits: {TotalHits}", query, response.Total);
				_ = parentActivity?.SetTag("db.response.status_code", 200);
				_ = parentActivity?.SetStatus(ActivityStatusCode.Ok);
			}

			return ProcessSearchResponse(response);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error occurred during Elasticsearch RRF search for '{Query}'", query);
			throw;
		}
	}

	private (int TotalHits, List<SearchResultItem> Results) ProcessSearchResponse(SearchResponse<DocumentDto> response)
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
				Parents = doc.Parents.Select(parent => new SearchResultItemParent
				{
					Title = parent.Title,
					Url = parent.Url
				}).ToArray(),
				Score = (float)(hit?.Score ?? 0.0),
				HighlightedBody = highlightedBody
			};
		}).ToList();
		_logger.LogInformation("Search results processed. Returning {search.result.count} results: [{search.result.paths}]", results.Count, results.Select(i => i.Url).ToArray());
		return (totalHits, results);
	}
}

[JsonSerializable(typeof(DocumentDto))]
[JsonSerializable(typeof(ParentDocumentDto))]
internal sealed partial class EsJsonContext : JsonSerializerContext;
