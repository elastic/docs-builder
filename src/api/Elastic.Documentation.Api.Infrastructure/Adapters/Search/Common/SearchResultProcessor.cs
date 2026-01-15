// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Clients.Elasticsearch.Core.Search;
using Elastic.Documentation.Search;

namespace Elastic.Documentation.Api.Infrastructure.Adapters.Search.Common;

/// <summary>
/// Shared result processing utilities for search operations.
/// Used by both FindPage (autocomplete) and FullSearch gateways.
/// </summary>
public static class SearchResultProcessor
{
	/// <summary>
	/// Processes a search hit into a common result item.
	/// </summary>
	public static SearchResultItem ProcessHit(
		Hit<DocumentationDocument> hit,
		string searchQuery,
		IReadOnlyDictionary<string, string[]> synonyms)
	{
		var doc = hit.Source!;
		var highlights = hit.Highlight;
		var searchTokens = searchQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

		string? highlightedTitle = null;
		string? highlightedBody = null;

		if (highlights != null)
		{
			if (highlights.TryGetValue("stripped_body", out var bodyHighlights) && bodyHighlights.Count > 0)
				highlightedBody = string.Join(". ", bodyHighlights.Select(h => h.Trim(['|', ' ', '.', '-'])));

			if (highlights.TryGetValue("title", out var titleHighlights) && titleHighlights.Count > 0)
				highlightedTitle = string.Join(". ", titleHighlights.Select(h => h.Trim(['|', ' ', '.', '-'])));
		}

		var title = (highlightedTitle ?? doc.Title).HighlightTokens(searchTokens, synonyms);
		var description = (!string.IsNullOrWhiteSpace(highlightedBody) ? highlightedBody : doc.Description ?? string.Empty)
			.Replace("\r\n", " ")
			.Replace("\n", " ")
			.Replace("\r", " ")
			.Trim(['|', ' '])
			.HighlightTokens(searchTokens, synonyms);

		return new SearchResultItem
		{
			Url = doc.Url,
			Title = title,
			Type = doc.Type,
			Description = description,
			Parents = doc.Parents.Select(parent => new SearchResultParent
			{
				Title = parent.Title,
				Url = parent.Url
			}).ToArray(),
			Score = (float)(hit.Score ?? 0.0),
			AiShortSummary = doc.AiShortSummary,
			AiRagOptimizedSummary = doc.AiRagOptimizedSummary,
			NavigationSection = doc.NavigationSection,
			LastUpdated = doc.LastUpdated
		};
	}

	/// <summary>
	/// Extracts type aggregations from search response.
	/// </summary>
	public static IReadOnlyDictionary<string, long> ExtractTypeAggregations(
		Elastic.Clients.Elasticsearch.SearchResponse<DocumentationDocument> response)
	{
		var aggregations = new Dictionary<string, long>();
		var terms = response.Aggregations?.GetStringTerms("type");
		if (terms is not null)
		{
			foreach (var bucket in terms.Buckets)
				aggregations[bucket.Key.ToString()] = bucket.DocCount;
		}
		return aggregations;
	}

	/// <summary>
	/// Extracts navigation section aggregations from search response.
	/// </summary>
	public static IReadOnlyDictionary<string, long> ExtractNavigationSectionAggregations(
		Elastic.Clients.Elasticsearch.SearchResponse<DocumentationDocument> response)
	{
		var aggregations = new Dictionary<string, long>();
		var terms = response.Aggregations?.GetStringTerms("navigation_section");
		if (terms is not null)
		{
			foreach (var bucket in terms.Buckets)
				aggregations[bucket.Key.ToString()] = bucket.DocCount;
		}
		return aggregations;
	}

	/// <summary>
	/// Extracts nested deployment type aggregations from search response.
	/// </summary>
	public static IReadOnlyDictionary<string, long> ExtractDeploymentTypeAggregations(
		Elastic.Clients.Elasticsearch.SearchResponse<DocumentationDocument> response)
	{
		var aggregations = new Dictionary<string, long>();
		var nested = response.Aggregations?.GetNested("applies_to_type");
		if (nested?.Aggregations is not null)
		{
			var terms = nested.Aggregations.GetStringTerms("types");
			if (terms is not null)
			{
				foreach (var bucket in terms.Buckets)
					aggregations[bucket.Key.ToString()] = bucket.DocCount;
			}
		}
		return aggregations;
	}
}

/// <summary>
/// Common search result item used by both FindPage and FullSearch.
/// </summary>
public record SearchResultItem
{
	public required string Type { get; init; }
	public required string Url { get; init; }
	public required string Title { get; init; }
	public required string Description { get; init; }
	public required SearchResultParent[] Parents { get; init; }
	public float Score { get; init; }
	public string? AiShortSummary { get; init; }
	public string? AiRagOptimizedSummary { get; init; }
	public string? NavigationSection { get; init; }
	public DateTimeOffset? LastUpdated { get; init; }
}

/// <summary>
/// Parent document reference in breadcrumb trail.
/// </summary>
public record SearchResultParent
{
	public required string Title { get; init; }
	public required string Url { get; init; }
}
