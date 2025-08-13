// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Markdig;
using Markdig.Parsers.Inlines;
using Markdig.Renderers;
using Markdig.Renderers.Html.Inlines;

namespace Elastic.Markdown.Myst.InlineParsers.SubstitutionInlineCode;

public static class SubstitutionInlineCodeBuilderExtensions
{
	public static MarkdownPipelineBuilder UseSubstitutionInlineCode(this MarkdownPipelineBuilder pipeline)
	{
		pipeline.Extensions.AddIfNotAlready<SubstitutionInlineCodeMarkdownExtension>();
		return pipeline;
	}
}

public class SubstitutionInlineCodeMarkdownExtension : IMarkdownExtension
{
	public void Setup(MarkdownPipelineBuilder pipeline)
	{
		if (!pipeline.InlineParsers.Contains<SubstitutionInlineCodeParser>())
		{
			// Insert before CodeInlineParser to intercept {subs=true}`...` patterns
			_ = pipeline.InlineParsers.InsertBefore<CodeInlineParser>(new SubstitutionInlineCodeParser());
		}
	}

	public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
	{
		if (!renderer.ObjectRenderers.Contains<SubstitutionInlineCodeRenderer>())
			_ = renderer.ObjectRenderers.InsertBefore<CodeInlineRenderer>(new SubstitutionInlineCodeRenderer());
	}
}
