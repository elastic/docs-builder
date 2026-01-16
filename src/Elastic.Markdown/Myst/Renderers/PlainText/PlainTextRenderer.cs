// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration;
using Markdig.Renderers;
using Markdig.Syntax;

namespace Elastic.Markdown.Myst.Renderers.PlainText;

/// <summary>
/// Renderer that outputs plain text optimized for search indexing.
/// Strips all markdown formatting and produces single-line output
/// with bullet separators between blocks.
/// </summary>
public class PlainTextRenderer : TextRendererBase
{
	private const string BlockSeparator = " • ";

	public required IDocumentationConfigurationContext BuildContext { get; set; }
	private bool _needsBlockSeparator;
	private bool _hasContent;

	/// <summary>
	/// Resets internal state for pooled reuse
	/// </summary>
	public void Reset()
	{
		_needsBlockSeparator = false;
		_hasContent = false;
	}

	/// <summary>
	/// Ensures a block separator will be added before the next content
	/// </summary>
	public void EnsureLine()
	{
		if (_hasContent)
			_needsBlockSeparator = true;
	}

	/// <summary>
	/// Ensures a block separator will be added before the next content
	/// </summary>
	public void EnsureBlockSpacing()
	{
		if (_hasContent)
			_needsBlockSeparator = true;
	}

	public void WriteLine(string text = "")
	{
		if (!string.IsNullOrEmpty(text))
			Write(text);
		EnsureLine();
	}

	public void Write(string text)
	{
		if (string.IsNullOrEmpty(text))
			return;

		if (_needsBlockSeparator && _hasContent)
		{
			Writer.Write(BlockSeparator);
			_needsBlockSeparator = false;
		}

		Writer.Write(text);
		_hasContent = true;
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
