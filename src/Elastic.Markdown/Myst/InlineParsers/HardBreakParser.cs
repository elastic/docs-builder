// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Markdig;
using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Parsers.Inlines;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Renderers.Html.Inlines;
using Markdig.Syntax.Inlines;

namespace Elastic.Markdown.Myst.InlineParsers;

public static class HardBreakBuilderExtensions
{
	public static MarkdownPipelineBuilder UseHardBreaks(this MarkdownPipelineBuilder pipeline)
	{
		pipeline.Extensions.AddIfNotAlready<HardBreakBuilderExtension>();
		return pipeline;
	}
}

public class HardBreakBuilderExtension : IMarkdownExtension
{
	public void Setup(MarkdownPipelineBuilder pipeline) =>
		pipeline.InlineParsers.InsertBefore<EmphasisInlineParser>(new HardBreakParser());

	public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer) =>
		renderer.ObjectRenderers.InsertAfter<EmphasisInlineRenderer>(new HardBreakRenderer());
}

public class HardBreakParser : InlineParser
{
	public HardBreakParser() => OpeningCharacters = ['<'];

	public override bool Match(InlineProcessor processor, ref StringSlice slice)
	{
		var span = slice.AsSpan();
		if (!span.StartsWith("<br"))
			return false;

		var closingStart = span[3..].IndexOf('>');
		// we allow
		if (closingStart != 0)
			return false;

		processor.Inline = new HardBreak();

		var sliceEnd = slice.Start + 4; //<br + >
		while (slice.Start != sliceEnd)
			slice.SkipChar();

		return true;
	}
}

public class HardBreak : LeafInline;

public class HardBreakRenderer : HtmlObjectRenderer<HardBreak>
{
	protected override void Write(HtmlRenderer renderer, HardBreak obj) =>
		renderer.Write("<br>");
}
