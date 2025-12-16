// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Markdown.Myst.Directives.Button;

/// <summary>
/// View model for a single button.
/// </summary>
public class ButtonViewModel : DirectiveViewModel
{
	/// <summary>
	/// The button text to display.
	/// </summary>
	public required string Text { get; init; }

	/// <summary>
	/// The resolved URL the button links to.
	/// </summary>
	public required string? Link { get; init; }

	/// <summary>
	/// Button variant: "primary" (filled) or "secondary" (outlined).
	/// </summary>
	public required string Type { get; init; }

	/// <summary>
	/// Horizontal alignment for standalone buttons: "left", "center", or "right".
	/// </summary>
	public required string Align { get; init; }

	/// <summary>
	/// If true, the link opens in a new tab with appropriate security attributes.
	/// </summary>
	public required bool External { get; init; }

	/// <summary>
	/// Whether this button is inside a button group.
	/// </summary>
	public required bool IsInGroup { get; init; }

	/// <summary>
	/// Whether the link is a cross-repository link.
	/// </summary>
	public required bool IsCrossLink { get; init; }

	/// <summary>
	/// Whether the link requires htmx attributes for client-side navigation.
	/// </summary>
	public required bool RequiresHtmx { get; init; }

	/// <summary>
	/// Gets the CSS class for the button type.
	/// </summary>
	public string TypeClass => Type == "secondary" ? "doc-button-secondary" : "doc-button-primary";

	/// <summary>
	/// Gets the CSS class for the button alignment (only used for standalone buttons).
	/// </summary>
	public string AlignClass => Align switch
	{
		"center" => "doc-button-center",
		"right" => "doc-button-right",
		_ => "doc-button-left"
	};
}

/// <summary>
/// View model for a button group container.
/// </summary>
public class ButtonGroupViewModel : DirectiveViewModel
{
	/// <summary>
	/// Horizontal alignment of the button group.
	/// </summary>
	public required string Align { get; init; }

	/// <summary>
	/// Gets the CSS class for the button group alignment.
	/// </summary>
	public string AlignClass => Align switch
	{
		"center" => "doc-button-group-center",
		"right" => "doc-button-group-right",
		_ => "doc-button-group-left"
	};
}

