// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.Text.Json;
using Elastic.Clients.Elasticsearch.Core.Search;
using Elastic.Documentation.Search.Contract;

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
		HighlightOptions? highlightOptions = null
	) where TDocument : SearchDocumentBase
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
			if (highlights.TryGetValue(QueryFieldNames.Body, out var bodyHighlights) && bodyHighlights.Count > 0)
				highlightedBody = string.Join(". ", bodyHighlights.Select(h => h.Trim(['|', ' ', '.', '-'])));

			if (highlights.TryGetValue(QueryFieldNames.Title, out var titleHighlights) && titleHighlights.Count > 0)
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
			Score = (float)(hit.Score ?? 0.0),
			Properties = MatchedProperties(hit, options.MaxChildProperties)
		};
	}

	private const int MaxMatchedProperties = 3;
	public const int MaxChildProperties = 5;
	public const int MaxChildPropertiesExpanded = 20;

	/// <summary>
	/// The properties matching the typed field by name, best first, with the typed path's direct children in page order right after
	/// the row they are listed under; see <see cref="SearchQueryBuilder.ApiPropertyHits"/> and <see cref="SearchQueryBuilder.ApiChildrenHits"/>.
	/// </summary>
	private static IReadOnlyList<ApiPropertyEntry> MatchedProperties<TDocument>(Hit<TDocument> hit, int maxChildProperties)
	{
		if (hit.InnerHits is null)
			return [];

		// A reading's inner hits are computed from its nested query alone, so a reading the document did not match
		// still carries them, and Elasticsearch omits matched_queries when no named query matched at all; only a
		// reading this hit matched counts.
		var matched = MatchedQueryNames(hit);
		var innerHits = hit
			.InnerHits
			.Where(group => SearchQueryBuilder.IsApiPropertyHits(group.Key) && matched.Contains(group.Key))
			.SelectMany(group => group.Value.Hits.Hits)
			.ToLookup(IsChildOnly);

		var named = innerHits[false]
			.OrderByDescending(h => h.Score ?? 0)
			.Select(Entry)
			.OfType<ApiPropertyEntry>()
			.DistinctBy(p => p.Anchor)
			.ToArray();
		var namedAnchors = named.Select(p => p.Anchor).ToHashSet(StringComparer.Ordinal);
		var children = innerHits[true]
			.OrderBy(h => h.Nested?.Offset ?? 0)
			.Select(Entry)
			.OfType<ApiPropertyEntry>()
			.Where(p => !namedAnchors.Contains(p.Anchor))
			.DistinctBy(p => p.Anchor)
			.Take(maxChildProperties)
			.ToArray();

		var parent = children.Length == 0
			? null
			: Array.Find(named, p => string.Equals(ApiPropertyPath.ParentKey(p.Path), children[0].Parent, StringComparison.Ordinal));
		if (parent is null)
			return [.. named.Take(MaxMatchedProperties), .. children];
		return [parent, .. children, .. named.Where(p => p != parent).Take(MaxMatchedProperties - 1)];
	}

	private static bool IsChildOnly<TDocument>(Hit<TDocument> innerHit) =>
		MatchedQueryNames(innerHit) is { Count: 1 } names && names.Contains(SearchQueryBuilder.ApiChildrenHits);

	private static ApiPropertyEntry? Entry<TDocument>(Hit<TDocument> innerHit) =>
		innerHit.Source is JsonElement source ? source.Deserialize(Contract.SourceGenerationContext.Default.ApiPropertyEntry) : null;

	/// <summary>The named queries this hit matched; empty when none did, as Elasticsearch then omits the field.</summary>
	private static IReadOnlySet<string> MatchedQueryNames<TDocument>(Hit<TDocument> hit) =>
		hit.MatchedQueries?.Match<IReadOnlySet<string>>(
			names => (names ?? []).ToHashSet(StringComparer.Ordinal),
			scored => (scored?.Keys ?? []).ToHashSet(StringComparer.Ordinal)
		)
			?? FrozenSet<string>.Empty;

	public static IReadOnlyDictionary<string, long> ExtractTermsAggregation<TDocument>(
		Clients.Elasticsearch.SearchResponse<TDocument> response,
		string aggregationName
	)
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
		string innerTermsAggregationName
	)
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
