// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Helpers;
using Markdig;
using Markdig.Extensions.SmartyPants;
using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Parsers.Inlines;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Renderers.Html.Inlines;
using Markdig.Syntax.Inlines;

namespace Elastic.Markdown.Myst.InlineParsers;

public static class InlineAnchorBuilderExtensions
{
	public static MarkdownPipelineBuilder UseInlineAnchors(this MarkdownPipelineBuilder pipeline)
	{
		pipeline.Extensions.AddIfNotAlready<InlineAnchorBuilderExtension>();
		return pipeline;
	}
}

public class InlineAnchorBuilderExtension : IMarkdownExtension
{
	public void Setup(MarkdownPipelineBuilder pipeline) =>
		pipeline.InlineParsers.InsertAfter<EmphasisInlineParser>(new InlineAnchorParser());

	public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer) =>
		renderer.ObjectRenderers.InsertAfter<EmphasisInlineRenderer>(new InlineAnchorRenderer());
}

public class InlineAnchorParser : InlineParser
{
	public InlineAnchorParser()
	{
		OpeningCharacters = ['$'];
	}

	public override bool Match(InlineProcessor processor, ref StringSlice slice)
	{
		var startPosition = processor.GetSourcePosition(slice.Start, out var line, out var column);
		var c = slice.CurrentChar;

		var span = slice.AsSpan();
		if (!span.StartsWith("$$$"))
			return false;

		var closingStart = span[3..].IndexOf('$');
		if (closingStart <= 0)
			return false;

		//not ending with three dollar signs
		if (!span[(closingStart + 3)..].StartsWith("$$$"))
			return false;

		processor.Inline = new InlineAnchor { Anchor = span[3..(closingStart + 3)].ToString().Slugify() };

		var sliceEnd = slice.Start + closingStart + 6;
		while (slice.Start != sliceEnd)
			slice.SkipChar();

		return true;
	}


}

public class InlineAnchor : LeafInline
{
	public required string Anchor { get; init; }
}

public class InlineAnchorRenderer : HtmlObjectRenderer<InlineAnchor>
{
	protected override void Write(HtmlRenderer renderer, InlineAnchor obj) =>
		renderer.Write("<a id=\"").Write(obj.Anchor).Write("\"></a>");
}
