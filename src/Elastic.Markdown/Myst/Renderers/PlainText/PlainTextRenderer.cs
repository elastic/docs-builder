// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration;
using Markdig.Renderers;
using Markdig.Syntax;

namespace Elastic.Markdown.Myst.Renderers.PlainText;

/// <summary>
/// Renderer that outputs plain text optimized for search indexing.
/// Strips all markdown formatting while preserving readable content with newlines.
/// </summary>
public class PlainTextRenderer : TextRendererBase
{
	public required IDocumentationConfigurationContext BuildContext { get; set; }
	private bool _isAtLineStart = true;

	/// <summary>
	/// Resets internal state for pooled reuse
	/// </summary>
	public void Reset() => _isAtLineStart = true;

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
		EnsureLine();
		Writer.WriteLine();
		_isAtLineStart = true;
	}

	public void WriteLine(string text = "")
	{
		Writer.WriteLine(text);
		_isAtLineStart = true;
	}

	public void Write(string text)
	{
		if (string.IsNullOrEmpty(text))
			return;

		Writer.Write(text);
		_isAtLineStart = text.EndsWith(Environment.NewLine) || text.EndsWith('\n');
	}

	public void WriteLeafInline(LeafBlock leafBlock)
	{
		if (leafBlock.Inline != null)
			Write(leafBlock.Inline);
	}

	public PlainTextRenderer(TextWriter writer) : base(writer)
	{
		// Skip YAML frontmatter
		ObjectRenderers.Add(new PlainTextYamlFrontMatterRenderer());

		// Inline renderers - strip formatting, keep text
		ObjectRenderers.Add(new PlainTextSubstitutionRenderer());
		ObjectRenderers.Add(new PlainTextRoleRenderer());
		ObjectRenderers.Add(new PlainTextLinkRenderer());
		ObjectRenderers.Add(new PlainTextEmphasisRenderer());
		ObjectRenderers.Add(new PlainTextCodeInlineRenderer());
		ObjectRenderers.Add(new PlainTextLiteralRenderer());
		ObjectRenderers.Add(new PlainTextLineBreakRenderer());

		// Block renderers
		ObjectRenderers.Add(new PlainTextDirectiveRenderer());
		ObjectRenderers.Add(new PlainTextCodeBlockRenderer());
		ObjectRenderers.Add(new PlainTextHeadingRenderer());
		ObjectRenderers.Add(new PlainTextParagraphRenderer());
		ObjectRenderers.Add(new PlainTextListRenderer());
		ObjectRenderers.Add(new PlainTextQuoteBlockRenderer());
		ObjectRenderers.Add(new PlainTextThematicBreakRenderer());
		ObjectRenderers.Add(new PlainTextTableRenderer());
		ObjectRenderers.Add(new PlainTextDefinitionListRenderer());
		ObjectRenderers.Add(new PlainTextDefinitionItemRenderer());
	}
}
