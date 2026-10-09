// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.Components.EmptyState;

/// <summary>What to show when a list, search or page has nothing in it, and what to do next.</summary>
public sealed record EmptyStateModel
{
	public required string Title { get; init; }
	public required string Description { get; init; }
	/// <summary>A short mono label above the title, such as "Nothing yet" or "404".</summary>
	public string? Eyebrow { get; init; }

	/// <summary>Draws the state as a bordered card instead of floating on the page.</summary>
	public bool Boxed { get; init; }

	public string CssClass => Boxed ? "empty-state empty-state--boxed" : "empty-state";

	public string Icon { get; init; } = "search";
	public string? ActionLabel { get; init; }
	public string? ActionHref { get; init; }
}
