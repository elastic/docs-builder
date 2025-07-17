// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO;
using Elastic.Documentation.Configuration;
using Elastic.Markdown.Exporters;
using Markdig.Renderers;
using Markdig.Syntax;

namespace Elastic.Markdown.Myst.Renderers;

/// <summary>
/// Base renderer that outputs CommonMark-compliant markdown optimized for LLM consumption
/// </summary>
public class LlmMarkdownRenderer : TextRendererBase
{
	/// <summary>
	/// BuildContext for accessing configuration like CanonicalBaseUrl for URL transformation
	/// </summary>
	public required BuildContext BuildContext { get; init; }

	private bool _isAtLineStart = true;

	/// <summary>
	/// Ensures that the output ends with a line break (only adds one if needed)
	/// </summary>
	public void EnsureLine()
	{
		if (_isAtLineStart)
			return;
		Writer.WriteLine();
		_isAtLineStart = true;
	}

	/// <summary>
	/// Ensures exactly one empty line before a block element (for consistent spacing)
	/// </summary>
	public void EnsureBlockSpacing()
	{
		EnsureLine(); // Ensure we're at start of line
		Writer.WriteLine(); // Add one empty line
		_isAtLineStart = true;
	}

	/// <summary>
	/// Writes a line of text to the output
	/// </summary>
	public void WriteLine(string text = "")
	{
		Writer.WriteLine(text);
		_isAtLineStart = true;
	}

	/// <summary>
	/// Writes text to the output (tracks line state for EnsureLine)
	/// </summary>
	public void Write(string text)
	{
		Writer.Write(text);
		_isAtLineStart = text.EndsWith(Environment.NewLine) || text.EndsWith('\n');
	}

	/// <summary>
	/// Writes the content of a leaf inline element
	/// </summary>
	public void WriteLeafInline(LeafBlock leafBlock)
	{
		if (leafBlock.Inline != null)
			Write(leafBlock.Inline);
	}

	public LlmMarkdownRenderer(TextWriter writer) : base(writer)
	{
		// Add renderer to skip YAML frontmatter blocks (prevents them from appearing as visible content)
		ObjectRenderers.Add(new LlmYamlFrontMatterRenderer());

		// Add inline renderers
		ObjectRenderers.Add(new LlmSubstituionLeafRenderer());
		ObjectRenderers.Add(new LlmRoleRenderer());
		ObjectRenderers.Add(new LlmLinkInlineRenderer());
		ObjectRenderers.Add(new LlmEmphasisInlineRenderer());
		ObjectRenderers.Add(new LlmCodeInlineRenderer());
		ObjectRenderers.Add(new LlmLiteralInlineRenderer());
		ObjectRenderers.Add(new LlmLineBreakInlineRenderer());

		// Add custom renderers for your MyST extensions
		ObjectRenderers.Add(new LlmDirectiveRenderer());
		ObjectRenderers.Add(new LlmEnhancedCodeBlockRenderer());

		// Add default object renderers for CommonMark elements
		ObjectRenderers.Add(new LlmHeadingRenderer());
		ObjectRenderers.Add(new LlmParagraphRenderer());
		ObjectRenderers.Add(new LlmDefinitionItemRenderer());
		ObjectRenderers.Add(new LlmDefinitionListRenderer());
		ObjectRenderers.Add(new LlmQuoteBlockRenderer());
		ObjectRenderers.Add(new LlmThematicBreakRenderer());
		ObjectRenderers.Add(new LlmTableRenderer());
		ObjectRenderers.Add(new LlmListRenderer());
	}


}
