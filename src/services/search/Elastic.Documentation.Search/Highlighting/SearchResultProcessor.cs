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
			Properties = MatchedProperties(hit, options)
		};
	}

	private const int MaxMatchedProperties = 3;

	/// <summary>
	/// The properties matching the typed field by name, best first, with the typed path's direct children in page order right after
	/// the row they are listed under; see <see cref="SearchQueryBuilder.ApiPropertyHits"/> and <see cref="SearchQueryBuilder.ApiChildrenHits"/>.
	/// </summary>
	private static IReadOnlyList<ApiPropertyEntry> MatchedProperties<TDocument>(Hit<TDocument> hit, HighlightOptions options)
	{
		if (hit.InnerHits is null)
			return [];

		var keep = options.ApiLookup ? FieldWordHits(hit) : MatchedReadingHits(hit);
		var innerHits = hit.InnerHits.Where(group => keep(group.Key)).SelectMany(group => group.Value.Hits.Hits).ToLookup(IsChildOnly);

		var named = innerHits[false]
			.OrderByDescending(h => h.Score ?? 0)
			.Select(Entry)
			.OfType<ApiPropertyEntry>()
			.DistinctBy(p => p.Anchor)
			.ToArray();
		var namedAnchors = named.Select(p => p.Anchor).ToHashSet(StringComparer.Ordinal);
		var children = !options.ListChildren
			? []
			: innerHits[true]
				.OrderBy(h => h.Nested?.Offset ?? 0)
				.Select(Entry)
				.OfType<ApiPropertyEntry>()
				.Where(p => !namedAnchors.Contains(p.Anchor))
				.DistinctBy(p => p.Anchor)
				.ToArray();

		// A query parameter and a body field may share a path, so a row lists only the children of its own location.
		var childrenByParent = children.ToLookup(c => (c.Parent, c.Location));
		var parents = named.Where(p => childrenByParent.Contains(ParentGroup(p))).DistinctBy(ParentGroup).ToArray();
		var listedGroups = parents.Select(ParentGroup).ToHashSet();
		var others = OnePerSection(named, parents);
		var orphans = children.Where(c => !listedGroups.Contains((c.Parent, c.Location)));
		return [.. parents.SelectMany(p => childrenByParent[ParentGroup(p)].Prepend(p)), .. others, .. orphans];
	}

	/// <summary>
	/// The best named row of every section no parent lists, then the next best rows up to <see cref="MaxMatchedProperties"/>,
	/// in score order. A field repeated within the request body must not hide the same field in the response, so a section
	/// is listed even past that limit.
	/// </summary>
	private static IEnumerable<ApiPropertyEntry> OnePerSection(ApiPropertyEntry[] named, ApiPropertyEntry[] parents)
	{
		var covered = parents.Select(p => p.Location).ToHashSet(StringComparer.Ordinal);
		var candidates = named.Except(parents).ToArray();
		var picked = candidates.Where(p => !covered.Contains(p.Location)).DistinctBy(p => p.Location).ToHashSet();
		picked.UnionWith(candidates.Where(p => !picked.Contains(p)).Take(MaxMatchedProperties - parents.Length - picked.Count));
		return candidates.Where(picked.Contains);
	}

	/// <summary>
	/// A reading's inner hits are computed from its nested query alone, so a reading the document did not match
	/// still carries them, and Elasticsearch omits matched_queries when no named query matched at all; only a
	/// reading this hit matched counts.
	/// </summary>
	private static Func<string, bool> MatchedReadingHits<TDocument>(Hit<TDocument> hit)
	{
		var matched = MatchedQueryNames(hit);
		return name => SearchQueryBuilder.IsApiPropertyHits(name) && matched.Contains(name);
	}

	/// <summary>
	/// A word's field rows list only when the word named a field at least as well as anything else, so in
	/// <c>search retriever</c> the endpoint <c>search</c> lists no <c>search_type</c> row.
	/// </summary>
	private static Func<string, bool> FieldWordHits<TDocument>(Hit<TDocument> hit)
	{
		var scored = hit.MatchedQueries?.Match(_ => null, scores => scores) ?? new Dictionary<string, double>();
		var fieldWords = ApiLookupRoles.Resolve(scored).Where(t => t.Role == ApiLookupRole.Field).Select(t => t.Index).ToHashSet();
		return name => ApiLookupQueryBuilder.TryTokenIndex(name, out var index) && fieldWords.Contains(index);
	}

	private static (string Parent, string Location) ParentGroup(ApiPropertyEntry row) =>
		(ApiPropertyPath.ParentKey(row.Path), row.Location);

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
