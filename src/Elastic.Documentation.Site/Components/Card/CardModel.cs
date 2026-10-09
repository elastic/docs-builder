// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.Components.Card;

public sealed record CardModel
{
	public required string Title { get; init; }
	public required string Description { get; init; }
	public required string Href { get; init; }
	public string? Icon { get; init; }
	public string CtaLabel { get; init; } = "Learn more";

	/// <summary>Puts the icon beside the title and tightens the padding.</summary>
	public bool Compact { get; init; }

	public string CssClass => Compact ? "card card--compact" : "card";
}
