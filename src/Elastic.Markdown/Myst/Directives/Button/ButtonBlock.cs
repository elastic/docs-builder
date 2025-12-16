// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.RegularExpressions;
using Elastic.Markdown.Diagnostics;
using Markdig.Syntax;

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
public partial class ButtonBlock(DirectiveBlockParser parser, ParserContext context) : DirectiveBlock(parser, context)
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

	// Regex to match a Markdown link: [text](url)
	[GeneratedRegex(@"^\s*\[[^\]]+\]\([^\)]+\)\s*$", RegexOptions.Singleline)]
	private static partial Regex LinkPattern();

	public override void FinalizeAndValidate(ParserContext context)
	{
		// Validate that the content is a single link
		ValidateContent();

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

	private void ValidateContent()
	{
		// Extract raw content from child blocks
		var content = ExtractContent();

		if (string.IsNullOrWhiteSpace(content))
		{
			this.EmitError("Button directive requires a link. Use: :::{button}\n[text](url)\n:::");
			return;
		}

		// Check if content matches the link pattern
		if (!LinkPattern().IsMatch(content))
		{
			this.EmitError("Button directive must contain only a single Markdown link. Use: :::{button}\n[text](url)\n:::");
		}
	}

	private string? ExtractContent()
	{
		var lines = new List<string>();
		foreach (var block in this)
		{
			if (block is LeafBlock leafBlock)
			{
				var content = leafBlock.Lines.ToString();
				if (!string.IsNullOrWhiteSpace(content))
					lines.Add(content);
			}
			else if (block is ContainerBlock)
			{
				// Nested directive or container - this is invalid
				this.EmitError("Button directive cannot contain nested directives or blocks. Use only a single Markdown link.");
				return null;
			}
		}

		return lines.Count > 0 ? string.Join("\n", lines) : null;
	}
}
