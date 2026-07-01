// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Myst.Directives.Math;
using Markdig;
using Markdig.Parsers.Inlines;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Renderers.Html.Inlines;

namespace Elastic.Markdown.Myst.Roles.Math;

public class MathRoleHtmlRenderer : HtmlObjectRenderer<MathRole>
{
	protected override void Write(HtmlRenderer renderer, MathRole role) =>
		// Inline math is always rendered as a span, KaTeX renders it client-side.
		MathMarkup.WriteHtml(renderer, role.Content, isDisplayMath: false);
}

public static class InlineMathExtensions
{
	public static MarkdownPipelineBuilder UseInlineMath(this MarkdownPipelineBuilder pipeline)
	{
		pipeline.Extensions.AddIfNotAlready<InlineMathExtension>();
		return pipeline;
	}
}

public class InlineMathExtension : IMarkdownExtension
{
	public void Setup(MarkdownPipelineBuilder pipeline) => _ = pipeline.InlineParsers.InsertBefore<CodeInlineParser>(new MathParser());

	public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer) =>
		renderer.ObjectRenderers.InsertBefore<CodeInlineRenderer>(new MathRoleHtmlRenderer());
}
