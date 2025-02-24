// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Helpers;
using Markdig;
using Markdig.Renderers;
using Markdig.Renderers.Html.Inlines;
using Markdig.Syntax.Inlines;

namespace Elastic.Markdown.Myst.Renderers;

public class HtmxLinkInlineRenderer : LinkInlineRenderer
{
	protected override void Write(HtmlRenderer renderer, LinkInline link)
	{
		if (renderer.EnableHtmlForInline && !link.IsImage && link.Url?.StartsWith('/') == true)
		{
			_ = renderer.Write("<a href=\"");
			_ = renderer.WriteEscapeUrl(link.GetDynamicUrl != null ? link.GetDynamicUrl() ?? link.Url : link.Url);
			_ = renderer.Write('"');
			_ = renderer.WriteAttributes(link);
			_ = renderer.Write(" hx-get=\"");
			_ = renderer.WriteEscapeUrl(link.GetDynamicUrl != null ? link.GetDynamicUrl() ?? link.Url : link.Url);
			_ = renderer.Write('"');
			_ = renderer.Write($" hx-select-oob=\"{Htmx.GetHxSelectOob()}\"");
			_ = renderer.Write(" hx-swap=\"none\"");
			_ = renderer.Write(" hx-push-url=\"true\"");
			_ = renderer.Write(" hx-indicator=\"#htmx-indicator\"");
			_ = renderer.Write(" preload=\"mouseover\"");

			if (!string.IsNullOrEmpty(link.Title))
			{
				_ = renderer.Write(" title=\"");
				_ = renderer.WriteEscape(link.Title);
				_ = renderer.Write('"');
			}
			if (!string.IsNullOrWhiteSpace(Rel))
			{
				_ = renderer.Write(" rel=\"");
				_ = renderer.Write(Rel);
				_ = renderer.Write('"');
			}

			_ = renderer.Write('>');
			renderer.WriteChildren(link);

			_ = renderer.Write("</a>");
		}
		else
		{
			base.Write(renderer, link);
		}
	}
}

public static class CustomLinkInlineRendererExtensions
{
	public static MarkdownPipelineBuilder UseHtmxLinkInlineRenderer(this MarkdownPipelineBuilder pipeline)
	{
		pipeline.Extensions.AddIfNotAlready<HtmxLinkInlineRendererExtension>();
		return pipeline;
	}
}

public class HtmxLinkInlineRendererExtension : IMarkdownExtension
{
	public void Setup(MarkdownPipelineBuilder pipeline)
	{
		// No setup required for the pipeline
	}

	public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
	{
		if (renderer is HtmlRenderer htmlRenderer)
		{
			_ = htmlRenderer.ObjectRenderers.RemoveAll(x => x is LinkInlineRenderer);
			htmlRenderer.ObjectRenderers.Add(new HtmxLinkInlineRenderer());
		}
	}
}
