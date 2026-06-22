// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Clients.Elasticsearch.Core.Search;
using Elastic.Internal.Search;

namespace Elastic.Documentation.Search.Highlighting;

/// <summary>
/// Builds <see cref="SearchResultItem{TDocument}"/> values from raw ES hits — merges ES-side
/// highlight fragments with a post-pass <c>&lt;mark&gt;</c> over any unhighlighted occurrences
/// of the query tokens (and their synonyms).
/// </summary>
public static class SearchResultProcessor
{
	public static SearchResultItem<TDocument> ProcessHit<TDocument>(
		Hit<TDocument> hit,
		string searchQuery,
		IReadOnlyDictionary<string, string[]> synonyms,
		HighlightOptions? highlightOptions = null)
		where TDocument : SearchDocumentBase
	{
		var options = highlightOptions ?? HighlightOptions.Default;
		var doc = hit.Source!;
		var highlights = hit.Highlight;
		var searchTokens = searchQuery
			.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
			.Where(token => token.Length >= options.MinTokenLength)
			.Where(token => options.ExcludePattern is null || !options.ExcludePattern.IsMatch(token))
			.ToArray();

		string? highlightedTitle = null;
		string? highlightedBody = null;

		if (highlights != null)
		{
			if (highlights.TryGetValue("stripped_body", out var bodyHighlights) && bodyHighlights.Count > 0)
				highlightedBody = string.Join(". ", bodyHighlights.Select(h => h.Trim(['|', ' ', '.', '-'])));

			if (highlights.TryGetValue("title", out var titleHighlights) && titleHighlights.Count > 0)
				highlightedTitle = string.Join(". ", titleHighlights.Select(h => h.Trim(['|', ' ', '.', '-'])));
		}

		var title = (highlightedTitle ?? doc.Title).HighlightTokens(searchTokens, synonyms, options.WholeWordOnly);
		var description = (!string.IsNullOrWhiteSpace(highlightedBody) ? highlightedBody : doc.Description ?? string.Empty)
			.Replace("\r\n", " ")
			.Replace("\n", " ")
			.Replace("\r", " ")
			.Trim(['|', ' '])
			.HighlightTokens(searchTokens, synonyms, options.WholeWordOnly);

		return new SearchResultItem<TDocument>
		{
			Document = doc,
			Title = title,
			Description = description,
			Score = (float)(hit.Score ?? 0.0)
		};
	}

	public static IReadOnlyDictionary<string, long> ExtractTermsAggregation<TDocument>(
		Clients.Elasticsearch.SearchResponse<TDocument> response,
		string aggregationName)
	{
		var aggregations = new Dictionary<string, long>();
		var terms = response.Aggregations?.GetStringTerms(aggregationName);
		if (terms is not null)
		{
			foreach (var bucket in terms.Buckets)
				aggregations[bucket.Key.ToString()] = bucket.DocCount;
		}
		return aggregations;
	}

	public static IReadOnlyDictionary<string, long> ExtractNestedTermsAggregation<TDocument>(
		Clients.Elasticsearch.SearchResponse<TDocument> response,
		string nestedAggregationName,
		string innerTermsAggregationName)
	{
		var aggregations = new Dictionary<string, long>();
		var nested = response.Aggregations?.GetNested(nestedAggregationName);
		if (nested?.Aggregations is not null)
		{
			var terms = nested.Aggregations.GetStringTerms(innerTermsAggregationName);
			if (terms is not null)
			{
				foreach (var bucket in terms.Buckets)
					aggregations[bucket.Key.ToString()] = bucket.DocCount;
			}
		}
		return aggregations;
	}
}
