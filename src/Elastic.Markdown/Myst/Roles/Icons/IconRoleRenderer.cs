// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Markdig;
using Markdig.Parsers.Inlines;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Renderers.Html.Inlines;

namespace Elastic.Markdown.Myst.Roles.Icons;

public class IconRoleHtmlRenderer : HtmlObjectRenderer<IconsRole>
{

	protected override void Write(HtmlRenderer renderer, IconsRole role)
	{
		_ = renderer.Write($"<span aria-label=\"Icon for {role.Name}\" class=\"icon icon-{role.Name}\">");
		_ = renderer.Write(role.Svg);
		_ = renderer.Write("</span>");
	}
}

public static class InlineAppliesToExtensions
{
	public static MarkdownPipelineBuilder UseInlineIcons(this MarkdownPipelineBuilder pipeline)
	{
		pipeline.Extensions.AddIfNotAlready<InlineIconExtension>();
		return pipeline;
	}
}

public class InlineIconExtension : IMarkdownExtension
{
	public void Setup(MarkdownPipelineBuilder pipeline) => _ = pipeline.InlineParsers.InsertBefore<CodeInlineParser>(new IconParser());

	public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer) =>
		renderer.ObjectRenderers.InsertBefore<CodeInlineRenderer>(new IconRoleHtmlRenderer());
}
