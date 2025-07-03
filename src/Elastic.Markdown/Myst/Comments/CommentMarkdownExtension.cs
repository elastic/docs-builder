// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Myst.Renderers;
using Markdig;
using Markdig.Parsers;
using Markdig.Renderers;

namespace Elastic.Markdown.Myst.Comments;

public static class CommentBuilderExtensions
{
	public static MarkdownPipelineBuilder UseComments(this MarkdownPipelineBuilder pipeline)
	{
		pipeline.Extensions.AddIfNotAlready<CommentMarkdownExtension>();
		return pipeline;
	}
}

public class CommentMarkdownExtension : IMarkdownExtension
{
	public void Setup(MarkdownPipelineBuilder pipeline)
	{
		if (!pipeline.BlockParsers.Contains<CommentBlockParser>())
			_ = pipeline.BlockParsers.InsertBefore<ThematicBreakParser>(new CommentBlockParser());
		if (!pipeline.BlockParsers.Contains<MultipleLineCommentBlockParser>())
			_ = pipeline.BlockParsers.InsertBefore<ThematicBreakParser>(new MultipleLineCommentBlockParser());
	}

	public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
	{
		if (!renderer.ObjectRenderers.Contains<CommentRenderer>())
			_ = renderer.ObjectRenderers.InsertBefore<SectionedHeadingRenderer>(new CommentRenderer());
	}
}
