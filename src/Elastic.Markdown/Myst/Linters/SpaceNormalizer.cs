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

public static class SpaceNormalizerBuilderExtensions
{
	public static MarkdownPipelineBuilder UseSpaceNormalizer(this MarkdownPipelineBuilder pipeline)
	{
		pipeline.Extensions.AddIfNotAlready<SpaceNormalizerBuilderExtension>();
		return pipeline;
	}
}

public class SpaceNormalizerBuilderExtension : IMarkdownExtension
{
	public void Setup(MarkdownPipelineBuilder pipeline) =>
		pipeline.InlineParsers.InsertBefore<EmphasisInlineParser>(new SpaceNormalizerParser());

	public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer) =>
		renderer.ObjectRenderers.InsertAfter<EmphasisInlineRenderer>(new SpaceNormalizerRenderer());
}

public class SpaceNormalizerParser : InlineParser
{
	// Collection of irregular space characters that may impair Markdown rendering
	private static readonly char[] IrregularSpaceChars =
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
	private static readonly SearchValues<char> SpaceSearchValues = SearchValues.Create(IrregularSpaceChars);

	// Track which files have already had the hint emitted to avoid duplicates
	private static readonly HashSet<string> FilesWithHintEmitted = [];

	public SpaceNormalizerParser() => OpeningCharacters = IrregularSpaceChars;

	public override bool Match(InlineProcessor processor, ref StringSlice slice)
	{
		var span = slice.AsSpan().Slice(0, 1);
		if (span.IndexOfAny(SpaceSearchValues) == -1)
			return false;

		processor.Inline = IrregularSpace.Instance;

		// Emit a single hint per file on first detection
		var context = processor.GetContext();
		var filePath = context.MarkdownSourcePath.FullName;

		lock (FilesWithHintEmitted)
		{
			if (!FilesWithHintEmitted.Contains(filePath))
			{
				_ = FilesWithHintEmitted.Add(filePath);
				processor.EmitHint(processor.Inline, 1, "Irregular space detected. Run 'docs-builder format --write' to automatically fix all instances.");
			}
		}

		slice.SkipChar();
		return true;
	}
}

public class IrregularSpace : LeafInline
{
	public static readonly IrregularSpace Instance = new();
};

public class SpaceNormalizerRenderer : HtmlObjectRenderer<IrregularSpace>
{
	protected override void Write(HtmlRenderer renderer, IrregularSpace obj) =>
		renderer.Write(' ');
}
