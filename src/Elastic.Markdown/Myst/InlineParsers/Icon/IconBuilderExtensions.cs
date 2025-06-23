// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Markdig;
using Markdig.Renderers;
using Markdig.Renderers.Html;

namespace Elastic.Markdown.Myst.InlineParsers.Icon;

public static class IconBuilderExtensions
{
	public static MarkdownPipelineBuilder UseIcons(this MarkdownPipelineBuilder pipeline)
	{
		pipeline.Extensions.AddIfNotAlready<IconMarkdownExtension>();
		return pipeline;
	}
}

public class IconMarkdownExtension : IMarkdownExtension
{
	public void Setup(MarkdownPipelineBuilder pipeline)
	{
		if (!pipeline.InlineParsers.Contains<IconParser>())
		{
			// Insert the parser before any other parsers
			pipeline.InlineParsers.Insert(0, new IconParser());
		}
	}

	public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
	{
		if (renderer is HtmlRenderer && !renderer.ObjectRenderers.Contains<IconRenderer>())
			renderer.ObjectRenderers.Insert(0, new IconRenderer());
	}
}
