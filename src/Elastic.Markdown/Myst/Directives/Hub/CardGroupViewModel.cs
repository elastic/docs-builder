// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Markdown.Myst.Directives.Hub;

public class CardGroupViewModel : DirectiveViewModel
{
	public required string? Title { get; init; }
	public required string? Intro { get; init; }
	public required string? Anchor { get; init; }
	public required string? Variant { get; init; }

	/// <summary>Rendered as a collapsible accordion group inside an {explore} section.</summary>
	public bool IsAccordion { get; init; }

	/// <summary>The accordion is expanded by default (the first group in an Explore stack).</summary>
	public bool IsOpen { get; init; }

	/// <summary>
	/// Shared &lt;details name&gt; group so the accordions open exclusively: expanding one
	/// collapses the others in the same Explore section.
	/// </summary>
	public string? AccordionGroup { get; init; }
}
