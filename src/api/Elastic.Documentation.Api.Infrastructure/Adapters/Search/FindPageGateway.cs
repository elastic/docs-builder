// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using System.Text.Json.Serialization;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Core.Explain;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Elastic.Documentation.Api.Core.Search;
using Elastic.Documentation.Api.Infrastructure.Adapters.Search.Common;
using Elastic.Documentation.Search;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Api.Infrastructure.Adapters.Search;

/// <summary>
/// Elasticsearch gateway for FindPage (autocomplete/navigation search).
/// Uses shared lexical query optimized for autocomplete.
/// </summary>
public class FindPageGateway(
	ElasticsearchClientAccessor clientAccessor,
	ILogger<FindPageGateway> logger) : IFindPageGateway
{
	public async Task<bool> CanConnect(Cancel ctx) => await clientAccessor.CanConnect(ctx);

	public async Task<FindPageResult> FindPageAsync(string query, int pageNumber, int pageSize, string? filter = null, Cancel ctx = default) =>
		await SearchImplementation(query, pageNumber, pageSize, filter, ctx);

	public async Task<FindPageResult> SearchImplementation(string query, int pageNumber, int pageSize, string? filter = null, Cancel ctx = default)
	{
		const string preTag = "<mark>";
		const string postTag = "</mark>";

		var searchQuery = query;
		var lexicalQuery = SearchQueryBuilder.BuildLexicalQuery(
			searchQuery,
			clientAccessor.SynonymBiDirectional,
			clientAccessor.DiminishTerms,
			clientAccessor.RulesetName);

		// Build post_filter for type filtering (applied after aggregations are computed)
		Query? postFilter = null;
		if (!string.IsNullOrWhiteSpace(filter))
			postFilter = new TermQuery { Field = Infer.Field<DocumentationDocument>(f => f.Type), Value = filter };

		try
		{
			var response = await clientAccessor.Client.SearchAsync<DocumentationDocument>(s =>
			{
				_ = s
					.Indices(clientAccessor.Options.IndexName)
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
						.Fields(f => f
							.Add(Infer.Field<DocumentationDocument>(d => d.Title), hf => hf
								.FragmentSize(150)
								.NumberOfFragments(3)
								.NoMatchSize(150)
								.HighlightQuery(q => q.Match(m => m
									.Field(d => d.Title)
									.Query(searchQuery)
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

				// Apply post_filter if a filter is specified
				if (postFilter is not null)
					_ = s.PostFilter(postFilter);
			}, ctx);

			if (!response.IsValidResponse)
			{
				logger.LogWarning("Elasticsearch response is not valid. Reason: {Reason}",
					response.ElasticsearchServerError?.Error.Reason ?? "Unknown");
			}

			return ProcessSearchResponse(response, searchQuery);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Error occurred during Elasticsearch search");
			throw;
		}
	}

	private FindPageResult ProcessSearchResponse(
		SearchResponse<DocumentationDocument> response,
		string searchQuery)
	{
		var totalHits = (int)response.Total;

		var results = response.Hits.Select(hit =>
		{
			var item = SearchResultProcessor.ProcessHit(hit, searchQuery, clientAccessor.SynonymBiDirectional);
			return new FindPageResultItem
			{
				Type = item.Type,
				Url = item.Url,
				Title = item.Title,
				Description = item.Description,
				Parents = item.Parents.Select(p => new FindPageResultItemParent
				{
					Title = p.Title,
					Url = p.Url
				}).ToArray(),
				Score = item.Score
			};
		}).ToList();

		// Extract aggregations
		var aggregations = SearchResultProcessor.ExtractTypeAggregations(response);

		return new FindPageResult
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
		var lexicalQuery = SearchQueryBuilder.BuildLexicalQuery(
			searchQuery,
			clientAccessor.SynonymBiDirectional,
			clientAccessor.DiminishTerms,
			clientAccessor.RulesetName);

		// Combine queries with bool should to match RRF behavior
		var combinedQuery = (Query)new BoolQuery
		{
			Should = [lexicalQuery],
			MinimumShouldMatch = 1
		};

		try
		{
			// First, find the document by URL
			var getDocResponse = await clientAccessor.Client.SearchAsync<DocumentationDocument>(s => s
				.Indices(clientAccessor.Options.IndexName)
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
			var explainResponse = await clientAccessor.Client.ExplainAsync<DocumentationDocument>(
				clientAccessor.Options.IndexName, documentId, e => e.Query(combinedQuery), ctx);

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
			logger.LogError(ex, "Error explaining document '{Url}' for query '{Query}'", documentUrl, query);
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
