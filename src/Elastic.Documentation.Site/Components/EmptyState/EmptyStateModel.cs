// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.Components.EmptyState;

/// <summary>What to show when a list, search or page has nothing in it, and what to do next.</summary>
public sealed record EmptyStateModel
{
	public required string Title { get; init; }
	public required string Description { get; init; }
	public string Icon { get; init; } = "search";
	public string? ActionLabel { get; init; }
	public string? ActionHref { get; init; }
}
