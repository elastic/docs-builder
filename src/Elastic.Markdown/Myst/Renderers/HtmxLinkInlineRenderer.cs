// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Site;
using Elastic.Documentation.Site.Navigation;
using Elastic.Markdown.Helpers;
using Elastic.Markdown.IO;
using Elastic.Markdown.IO.Navigation;
using Markdig;
using Markdig.Renderers;
using Markdig.Renderers.Html.Inlines;
using Markdig.Syntax.Inlines;

namespace Elastic.Markdown.Myst.Renderers;

public class HtmxLinkInlineRenderer : LinkInlineRenderer
{
	protected override void Write(HtmlRenderer renderer, LinkInline link)
	{
		if (renderer.EnableHtmlForInline && !link.IsImage)
		{
			// ReSharper disable once UnusedVariable
			if (link.GetData(nameof(ParserContext.CurrentUrlPath)) is not string currentUrl)
			{
				base.Write(renderer, link);
				return;
			}

			var url = link.GetDynamicUrl != null ? link.GetDynamicUrl() : link.Url;

			var isCrossLink = (link.GetData("isCrossLink") as bool?) == true;
			var isHttpLink = url?.StartsWith("http") ?? false;

			_ = renderer.Write("<a href=\"");
			_ = renderer.WriteEscapeUrl(url);
			_ = renderer.Write('"');
			_ = renderer.WriteAttributes(link);

			if (link.Url?.StartsWith('/') == true)
			{
				var currentRootNavigation = link.GetData(nameof(MarkdownFile.NavigationRoot)) as INodeNavigationItem<INavigationModel, INavigationItem>;
				var targetRootNavigation = link.GetData($"Target{nameof(MarkdownFile.NavigationRoot)}") as INodeNavigationItem<INavigationModel, INavigationItem>;
				var hasSameTopLevelGroup = !isCrossLink && (currentRootNavigation?.Id == targetRootNavigation?.Id);
				_ = renderer.Write(" hx-get=\"");
				_ = renderer.WriteEscapeUrl(url);
				_ = renderer.Write('"');
				_ = renderer.Write($" hx-select-oob=\"{Htmx.GetHxSelectOob(hasSameTopLevelGroup)}\"");
				_ = renderer.Write($" hx-swap=\"{Htmx.HxSwap}\"");
				_ = renderer.Write($" hx-push-url=\"{Htmx.HxPushUrl}\"");
				_ = renderer.Write($" hx-indicator=\"{Htmx.HxIndicator}\"");
				_ = renderer.Write($" preload=\"{Htmx.Preload}\"");
			}
			if (isHttpLink && !isCrossLink)
			{
				_ = renderer.Write(" target=\"_blank\"");
				_ = renderer.Write(" rel=\"noopener noreferrer\"");
			}

			if (!string.IsNullOrEmpty(link.Title))
			{
				_ = renderer.Write(" title=\"");
				_ = renderer.WriteEscape(link.Title);
				_ = renderer.Write('"');
			}

			if (!string.IsNullOrWhiteSpace(Rel) && link.Url?.StartsWith('/') == false)
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
	public static MarkdownPipelineBuilder UseHtmxLinkInlineRenderer(this MarkdownPipelineBuilder pipeline)
	{
		pipeline.Extensions.AddIfNotAlready(new HtmxLinkInlineRendererExtension());
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
