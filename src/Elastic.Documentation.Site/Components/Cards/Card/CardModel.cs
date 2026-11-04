// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.Components.Cards.Card;

public sealed record CardModel
{
	public required string Title { get; init; }
	public required string Description { get; init; }
	public required string Href { get; init; }
	public string? Icon { get; init; }
	public string CtaLabel { get; init; } = "Learn more";

	/// <summary>Puts the icon beside the title and tightens the padding.</summary>
	public bool Compact { get; init; }

	/// <summary>The dark icon box used for the main entries on a landing page.</summary>
	public bool Primary { get; init; }

	/// <summary>Keeps the slab shadow on at rest, for the one card a page wants to lead with.</summary>
	public bool Featured { get; init; }

	/// <summary>A short mono label above the title, such as "New".</summary>
	public string? Eyebrow { get; init; }

	public string CssClass =>
		string.Join(
			' ',
			new[] { "card", Compact ? "card--compact" : null, Primary ? "card--primary" : null, Featured ? "card--featured" : null }.Where(
				c => c is not null
			)
		);
}
