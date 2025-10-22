// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Diagnostics;

namespace Elastic.Markdown.Myst.Directives.Math;

/// <summary>
/// Represents a math directive block for rendering mathematical expressions using LaTeX syntax.
/// Follows MyST specification: https://mystmd.org/guide/directives#directive-math
/// </summary>
public class MathBlock(DirectiveBlockParser parser, ParserContext context) : DirectiveBlock(parser, context)
{
	public override string Directive => "math";

	/// <summary>
	/// The LaTeX mathematical expression content
	/// </summary>
	public string? Content { get; private set; }

	/// <summary>
	/// Whether this is display math (block-level) or inline math
	/// </summary>
	public bool IsDisplayMath { get; private set; }

	/// <summary>
	/// Label for cross-referencing
	/// </summary>
	public string? Label { get; private set; }

	public override void FinalizeAndValidate(ParserContext context)
	{
		// Extract content from the directive body
		Content = ExtractContent();

		if (string.IsNullOrWhiteSpace(Content))
		{
			this.EmitError("Math directive requires content.");
			return;
		}

		// Determine if this is display math based on content analysis
		// Display math typically starts with \[ or $$ and contains block-level expressions
		IsDisplayMath = DetermineDisplayMath(Content);

		// Extract label for cross-referencing
		Label = Prop("label", "name");
	}

	private string? ExtractContent()
	{
		if (!this.Any())
			return null;

		var lines = new List<string>();
		foreach (var block in this)
		{
			if (block is Markdig.Syntax.LeafBlock leafBlock)
			{
				var content = leafBlock.Lines.ToString();
				if (!string.IsNullOrWhiteSpace(content))
					lines.Add(content);
			}
		}

		return lines.Count > 0 ? string.Join("\n", lines) : null;
	}

	private static bool DetermineDisplayMath(string content)
	{
		// Check for common display math indicators
		var trimmed = content.Trim();

		// LaTeX display math delimiters
		if (trimmed.StartsWith("\\[") && trimmed.EndsWith("\\]"))
			return true;
		if (trimmed.StartsWith("\\begin{") && trimmed.Contains("\\end{"))
			return true;

		// TeX display math delimiters
		if (trimmed.StartsWith("$$") && trimmed.EndsWith("$$"))
			return true;

		// Check for block-level math expressions (heuristics)
		// If content contains line breaks or complex expressions, likely display math
		if (content.Contains('\n') || content.Contains("\\frac") || content.Contains("\\sum") ||
			content.Contains("\\int") || content.Contains("\\lim") || content.Contains("\\begin"))
			return true;

		return false;
	}
}
