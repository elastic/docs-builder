// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Markdig;
using Markdig.Parsers;
using Markdig.Parsers.Inlines;
using Markdig.Renderers;
using Markdig.Renderers.Html;

namespace Elastic.Markdown.Myst.Directives;

public static class DirectiveMarkdownBuilderExtensions
{
	public static MarkdownPipelineBuilder UseDirectives(this MarkdownPipelineBuilder pipeline)
	{
		pipeline.Extensions.AddIfNotAlready<DirectiveMarkdownExtension>();
		return pipeline;
	}
}

/// <summary>
/// Extension to allow custom containers.
/// </summary>
/// <seealso cref="IMarkdownExtension" />
public class DirectiveMarkdownExtension : IMarkdownExtension
{
	public void Setup(MarkdownPipelineBuilder pipeline)
	{
		if (!pipeline.BlockParsers.Contains<DirectiveBlockParser>())
		{
			// Insert the parser before any other parsers
			_ = pipeline.BlockParsers.InsertBefore<ThematicBreakParser>(new DirectiveBlockParser());
		}
		_ = pipeline.BlockParsers.Replace<ParagraphBlockParser>(new DirectiveParagraphParser());

		// Plug the inline parser for CustomContainerInline
		var inlineParser = pipeline.InlineParsers.Find<EmphasisInlineParser>();
		if (inlineParser != null && !inlineParser.HasEmphasisChar(':'))
		{
			inlineParser.EmphasisDescriptors.Add(new EmphasisDescriptor(':', 2, 2, true));
			inlineParser.TryCreateEmphasisInlineList.Add((emphasisChar, delimiterCount) =>
				delimiterCount != 2 || emphasisChar != ':'
				? null
				: (Markdig.Syntax.Inlines.EmphasisInline)new Role { DelimiterChar = ':', DelimiterCount = 2 }
			);
		}
	}

	public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
	{
		if (!renderer.ObjectRenderers.Contains<DirectiveHtmlRenderer>())
		{
			// Must be inserted before CodeBlockRenderer
			_ = renderer.ObjectRenderers.InsertBefore<CodeBlockRenderer>(new DirectiveHtmlRenderer());
		}

		_ = renderer.ObjectRenderers.Replace<HeadingRenderer>(new SectionedHeadingRenderer());
	}
}
