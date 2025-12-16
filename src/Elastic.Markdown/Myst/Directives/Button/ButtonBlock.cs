// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Diagnostics;

namespace Elastic.Markdown.Myst.Directives.Button;

/// <summary>
/// Container block for grouping multiple buttons in a row.
/// </summary>
public class ButtonGroupBlock(DirectiveBlockParser parser, ParserContext context) : DirectiveBlock(parser, context)
{
	public override string Directive => "button-group";

	/// <summary>
	/// Horizontal alignment of the button group.
	/// </summary>
	public string Align { get; private set; } = "left";

	public override void FinalizeAndValidate(ParserContext context) =>
		Align = Prop("align") ?? "left";
}

/// <summary>
/// A button directive that wraps a link with button styling.
/// The link inside is processed by the standard link parser and renderer,
/// including cross-link resolution and htmx attributes.
/// </summary>
/// <example>
/// :::{button}
/// [Get Started](/get-started)
/// :::
/// </example>
public class ButtonBlock(DirectiveBlockParser parser, ParserContext context) : DirectiveBlock(parser, context)
{
	public override string Directive => "button";

	private static readonly HashSet<string> ValidTypes = ["primary", "secondary"];
	private static readonly HashSet<string> ValidAligns = ["left", "center", "right"];

	/// <summary>
	/// Button variant: "primary" (filled) or "secondary" (outlined).
	/// </summary>
	public string Type { get; private set; } = "primary";

	/// <summary>
	/// Horizontal alignment for standalone buttons: "left", "center", or "right".
	/// </summary>
	public string Align { get; private set; } = "left";

	/// <summary>
	/// Whether this button is inside a button group.
	/// </summary>
	public bool IsInGroup { get; private set; }

	public override void FinalizeAndValidate(ParserContext context)
	{
		// Get and validate type
		var type = Prop("type", "variant")?.ToLowerInvariant();
		if (type != null && !ValidTypes.Contains(type))
		{
			this.EmitWarning($"Invalid button type '{type}'. Valid types are: primary, secondary. Defaulting to 'primary'.");
			type = "primary";
		}
		Type = type ?? "primary";

		// Get and validate alignment
		var align = Prop("align")?.ToLowerInvariant();
		if (align != null && !ValidAligns.Contains(align))
		{
			this.EmitWarning($"Invalid button alignment '{align}'. Valid alignments are: left, center, right. Defaulting to 'left'.");
			align = "left";
		}
		Align = align ?? "left";

		// Check if inside a button group
		IsInGroup = Parent is ButtonGroupBlock;
	}
}
