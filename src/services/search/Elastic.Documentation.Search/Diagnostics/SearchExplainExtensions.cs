// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using Elastic.Clients.Elasticsearch.Core.Explain;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Elastic.Documentation.Search.Contract;

namespace Elastic.Documentation.Search.Diagnostics;

/// <summary>
/// Diagnostic helpers on top of <see cref="DefaultSearchService{TDocument}"/> — invoke the ES
/// <c>_explain</c> API to break down why a document did (or didn't) match a query.
/// Intended for relevance tests; not part of <see cref="ISearchService{TDocument}"/>.
/// </summary>
public static class SearchExplainExtensions
{
	public static async Task<ExplainResult> ExplainDocumentAsync<TDocument>(
		this DefaultSearchService<TDocument> service,
		string query,
		string documentUrl,
		CancellationToken ct = default)
		where TDocument : SearchDocumentBase
	{
		var lexicalQuery = SearchQueryBuilder.BuildLexicalQuery(
			query,
			service.Configuration.SynonymBiDirectional,
			service.Configuration.DiminishTerms,
			service.Configuration.RulesetName);

		var combinedQuery = (Query)new BoolQuery
		{
			Should = [lexicalQuery],
			MinimumShouldMatch = 1
		};

		var getDocResponse = await service.Client.SearchAsync<TDocument>(s => s
			.Indices(service.IndexAlias)
			.Query(q => q.Term(t => t.Field("url").Value(documentUrl)))
			.Size(1), ct).ConfigureAwait(false);

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

		var explainResponse = await service.Client.ExplainAsync<TDocument>(
			service.IndexAlias, documentId, e => e.Query(combinedQuery), ct).ConfigureAwait(false);

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

	public static async Task<(ExplainResult TopResult, ExplainResult ExpectedResult)> ExplainTopResultAndExpectedAsync<TDocument>(
		this DefaultSearchService<TDocument> service,
		string query,
		string expectedDocumentUrl,
		CancellationToken ct = default)
		where TDocument : SearchDocumentBase
	{
		var top = await service.AutocompleteAsync(new AutocompleteRequest { Query = query, PageNumber = 1, PageSize = 1 }, ct).ConfigureAwait(false);
		var topResultUrl = top.Results.Count > 0 ? top.Results[0].Document.Url : null;

		if (string.IsNullOrEmpty(topResultUrl))
		{
			var emptyResult = new ExplainResult
			{
				SearchTitle = "N/A",
				DocumentUrl = "N/A",
				Found = false,
				Explanation = "No search results returned"
			};
			return (emptyResult, await service.ExplainDocumentAsync(query, expectedDocumentUrl, ct).ConfigureAwait(false));
		}

		var topResultExplain = await service.ExplainDocumentAsync(query, topResultUrl, ct).ConfigureAwait(false);
		var expectedResultExplain = await service.ExplainDocumentAsync(query, expectedDocumentUrl, ct).ConfigureAwait(false);

		return (topResultExplain, expectedResultExplain);
	}

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
}

public sealed record ExplainResult
{
	public required string SearchTitle { get; init; }
	public required string DocumentUrl { get; init; }
	public bool Found { get; init; }
	public bool Matched { get; init; }
	public double Score { get; init; }
	public string Explanation { get; init; } = string.Empty;
}
