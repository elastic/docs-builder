// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Components.PageCard;

namespace Elastic.Markdown.Components.Listing;

public sealed record ListingGroup(string? Key, string? Title, IReadOnlyList<PageCardModel> Cards);

/// <summary>A filterable list of page cards, optionally grouped. Script adds the filter bar; the cards are plain links without it.</summary>
public sealed record ListingModel
{
	public required IReadOnlyList<ListingGroup> Groups { get; init; }

	public int Total => Groups.Sum(g => g.Cards.Count);

	public IReadOnlyList<ListingGroup> Chips => [.. Groups.Where(g => g.Key is not null && g.Title is not null)];

	public string Placeholder => $"Filter {Total} page{(Total == 1 ? "" : "s")}…";
}
