// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Buffers;
using Elastic.Markdown.Diagnostics;
using Markdig;
using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Parsers.Inlines;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Renderers.Html.Inlines;
using Markdig.Syntax.Inlines;

namespace Elastic.Markdown.Myst.Linters;

public static class WhiteSpaceNormalizerBuilderExtensions
{
	public static MarkdownPipelineBuilder UseWhiteSpaceNormalizer(this MarkdownPipelineBuilder pipeline)
	{
		pipeline.Extensions.AddIfNotAlready<WhiteSpaceNormalizerBuilderExtension>();
		return pipeline;
	}
}

public class WhiteSpaceNormalizerBuilderExtension : IMarkdownExtension
{
	public void Setup(MarkdownPipelineBuilder pipeline) =>
		pipeline.InlineParsers.InsertBefore<EmphasisInlineParser>(new WhiteSpaceNormalizerParser());

	public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer) =>
		renderer.ObjectRenderers.InsertAfter<EmphasisInlineRenderer>(new WhiteSpaceNormalizerRenderer());
}

public class WhiteSpaceNormalizerParser : InlineParser
{
	// Collection of irregular whitespace characters that may impair Markdown rendering
	private static readonly char[] IrregularWhitespaceChars =
	[
		'\u000B', // Line Tabulation (\v) - <VT>
		'\u000C', // Form Feed (\f) - <FF>
		'\u00A0', // No-Break Space - <NBSP>
		'\u0085', // Next Line
		'\u1680', // Ogham Space Mark
		'\u180E', // Mongolian Vowel Separator - <MVS>
		'\ufeff', // Zero Width No-Break Space - <BOM>
		'\u2000', // En Quad
		'\u2001', // Em Quad
		'\u2002', // En Space - <ENSP>
		'\u2003', // Em Space - <EMSP>
		'\u2004', // Tree-Per-Em
		'\u2005', // Four-Per-Em
		'\u2006', // Six-Per-Em
		'\u2007', // Figure Space
		'\u2008', // Punctuation Space - <PUNCSP>
		'\u2009', // Thin Space
		'\u200A', // Hair Space
		'\u200B', // Zero Width Space - <ZWSP>
		'\u2028', // Line Separator
		'\u2029', // Paragraph Separator
		'\u202F', // Narrow No-Break Space
		'\u205F', // Medium Mathematical Space
		'\u3000'  // Ideographic Space
	];
	private static readonly SearchValues<char> WhiteSpaceSearchValues = SearchValues.Create(IrregularWhitespaceChars);

	public WhiteSpaceNormalizerParser() => OpeningCharacters = IrregularWhitespaceChars;

	public override bool Match(InlineProcessor processor, ref StringSlice slice)
	{
		var span = slice.AsSpan().Slice(0, 1);
		if (span.IndexOfAny(WhiteSpaceSearchValues) == -1)
			return false;

		processor.Inline = IrregularWhiteSpace.Instance;

		var c = span[0];
		var charName = GetCharacterName(c);

		processor.EmitHint(processor.Inline, 1, $"Irregular whitespace character detected: U+{(int)c:X4} ({charName}). This may impair Markdown rendering.");

		slice.SkipChar();
		return true;
	}

	// Helper to get a friendly name for the whitespace character
	private static string GetCharacterName(char c) => c switch
	{
		'\u000B' => "Line Tabulation (VT)",
		'\u000C' => "Form Feed (FF)",
		'\u00A0' => "No-Break Space (NBSP)",
		'\u0085' => "Next Line",
		'\u1680' => "Ogham Space Mark",
		'\u180E' => "Mongolian Vowel Separator (MVS)",
		'\ufeff' => "Zero Width No-Break Space (BOM)",
		'\u2000' => "En Quad",
		'\u2001' => "Em Quad",
		'\u2002' => "En Space (ENSP)",
		'\u2003' => "Em Space (EMSP)",
		'\u2004' => "Tree-Per-Em",
		'\u2005' => "Four-Per-Em",
		'\u2006' => "Six-Per-Em",
		'\u2007' => "Figure Space",
		'\u2008' => "Punctuation Space (PUNCSP)",
		'\u2009' => "Thin Space",
		'\u200A' => "Hair Space",
		'\u200B' => "Zero Width Space (ZWSP)",
		'\u2028' => "Line Separator",
		'\u2029' => "Paragraph Separator",
		'\u202F' => "Narrow No-Break Space",
		'\u205F' => "Medium Mathematical Space",
		'\u3000' => "Ideographic Space",
		_ => "Unknown"
	};
}

public class IrregularWhiteSpace : LeafInline
{
	public static readonly IrregularWhiteSpace Instance = new();
};

public class WhiteSpaceNormalizerRenderer : HtmlObjectRenderer<IrregularWhiteSpace>
{
	protected override void Write(HtmlRenderer renderer, IrregularWhiteSpace obj) =>
		renderer.Write(' ');
}
