// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Helpers;
using Elastic.Markdown.IO.Configuration;
using Markdig;
using Markdig.Renderers;
using Markdig.Renderers.Html.Inlines;
using Markdig.Syntax.Inlines;

namespace Elastic.Markdown.Myst.Renderers;

public class HtmxLinkInlineRenderer(BuildContext build) : LinkInlineRenderer
{
	protected override void Write(HtmlRenderer renderer, LinkInline link)
	{
		if (renderer.EnableHtmlForInline && !link.IsImage && link.Url?.StartsWith('/') == true)
		{
			// ReSharper disable once UnusedVariable
			if (link.GetData(nameof(ParserContext.CurrentUrlPath)) is not string currentUrl)
			{
				base.Write(renderer, link);
				return;
			}

			_ = renderer.Write("<a href=\"");
			_ = renderer.WriteEscapeUrl(link.GetDynamicUrl != null ? link.GetDynamicUrl() ?? link.Url : link.Url);
			_ = renderer.Write('"');
			_ = renderer.WriteAttributes(link);
			_ = renderer.Write(" hx-get=\"");
			_ = renderer.WriteEscapeUrl(link.GetDynamicUrl != null ? link.GetDynamicUrl() ?? link.Url : link.Url);
			_ = renderer.Write('"');
			_ = renderer.Write($" hx-select-oob=\"{Htmx.GetHxSelectOob(build.Configuration, build.UrlPathPrefix, currentUrl, link.Url)}\"");
			_ = renderer.Write(" hx-swap=\"none\"");
			_ = renderer.Write(" hx-push-url=\"true\"");
			_ = renderer.Write(" hx-indicator=\"#htmx-indicator\"");
			_ = renderer.Write($" preload=\"{Htmx.GetPreload()}\"");

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
			base.Write(renderer, link);
	}
}

public static class CustomLinkInlineRendererExtensions
{
	public static MarkdownPipelineBuilder UseHtmxLinkInlineRenderer(this MarkdownPipelineBuilder pipeline, BuildContext build)
	{
		pipeline.Extensions.AddIfNotAlready(new HtmxLinkInlineRendererExtension(build));
		return pipeline;
	}
}

public class HtmxLinkInlineRendererExtension(BuildContext build) : IMarkdownExtension
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
			htmlRenderer.ObjectRenderers.Add(new HtmxLinkInlineRenderer(build));
		}
	}
}
