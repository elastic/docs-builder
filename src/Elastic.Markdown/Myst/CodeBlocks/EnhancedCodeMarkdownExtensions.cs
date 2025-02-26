// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Markdig;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Renderers.Html;

namespace Elastic.Markdown.Myst.CodeBlocks;

public static class EnhancedCodeBuilderExtensions
{
	public static MarkdownPipelineBuilder UseEnhancedCodeBlocks(this MarkdownPipelineBuilder pipeline)
	{
		pipeline.Extensions.AddIfNotAlready<EnhancedCodeBlockExtension>();
		return pipeline;
	}
}

/// <summary>
/// Extension to allow custom containers.
/// </summary>
/// <seealso cref="IMarkdownExtension" />
public class EnhancedCodeBlockExtension : IMarkdownExtension
{
	public void Setup(MarkdownPipelineBuilder pipeline) =>
		pipeline.BlockParsers.Replace<FencedCodeBlockParser>(new EnhancedCodeBlockParser());

	public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer) =>
		renderer.ObjectRenderers.Replace<CodeBlockRenderer>(new EnhancedCodeBlockHtmlRenderer());
}
