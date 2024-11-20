// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

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
			pipeline.BlockParsers.InsertBefore<ThematicBreakParser>(new DirectiveBlockParser());
		}
		pipeline.BlockParsers.Replace<ParagraphBlockParser>(new DirectiveParagraphParser());

		// Plug the inline parser for CustomContainerInline
		var inlineParser = pipeline.InlineParsers.Find<EmphasisInlineParser>();
		if (inlineParser != null && !inlineParser.HasEmphasisChar(':'))
		{
			inlineParser.EmphasisDescriptors.Add(new EmphasisDescriptor(':', 2, 2, true));
			inlineParser.TryCreateEmphasisInlineList.Add((emphasisChar, delimiterCount) =>
			{
				if (delimiterCount == 2 && emphasisChar == ':')
					return new Role();

				return null;
			});
		}
	}

	public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
	{
		if (!renderer.ObjectRenderers.Contains<DirectiveHtmlRenderer>())
		{
			// Must be inserted before CodeBlockRenderer
			renderer.ObjectRenderers.InsertBefore<CodeBlockRenderer>(new DirectiveHtmlRenderer());
		}

		renderer.ObjectRenderers.Replace<HeadingRenderer>(new SectionedHeadingRenderer());
	}
}
