// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Reflection;
using Elastic.Documentation.Extensions;
using Elastic.Documentation.Site;
using Elastic.Documentation.Site.Navigation;
using Elastic.Markdown.IO;
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
			if (link.GetData(nameof(ParserContext.CurrentUrlPath)) is not string currentUrl)
			{
				base.Write(renderer, link);
				return;
			}

			var url = link.GetDynamicUrl?.Invoke() ?? link.Url;

			var isCrossLink = (link.GetData("isCrossLink") as bool?) == true;
			var isHttpLink = url?.StartsWith("http") ?? false;

			_ = renderer.Write("<a href=\"");
			_ = renderer.WriteEscapeUrl(url);
			_ = renderer.Write('"');
			_ = renderer.WriteAttributes(link);

			if (link.Url?.StartsWith('/') == true || isCrossLink)
			{
				var currentRootNavigation = link.GetData(nameof(MarkdownFile.NavigationRoot)) as INodeNavigationItem<INavigationModel, INavigationItem>;
				var targetRootNavigation = link.GetData($"Target{nameof(MarkdownFile.NavigationRoot)}") as INodeNavigationItem<INavigationModel, INavigationItem>;
				var hasSameTopLevelGroup = !isCrossLink && (currentRootNavigation?.Id == targetRootNavigation?.Id);
				_ = renderer.Write($" hx-select-oob=\"{Htmx.GetHxSelectOob(hasSameTopLevelGroup)}\"");
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
		else if (link.IsImage)
		{
			// Handle inline images with ALT override logic
			WriteImage(renderer, link);
		}
		else
			base.Write(renderer, link);
	}

	private static void WriteImage(HtmlRenderer renderer, LinkInline link)
	{
		_ = renderer.Write("<img src=\"");
		_ = renderer.WriteEscapeUrl(link.GetDynamicUrl?.Invoke() ?? link.Url);
		_ = renderer.Write('"');

		// Write alt text using WriteChildren to ensure substitutions are processed
		if (link.FirstChild != null)
		{
			_ = renderer.Write(" alt=\"");
			renderer.WriteChildren(link);
			_ = renderer.Write('"');
		}

		// Write any additional attributes (like width/height from styling instructions)
		_ = renderer.WriteAttributes(link);

		// Set title to alt text for inline images (after any substitutions are processed)
		if (link.FirstChild != null)
		{
			_ = renderer.Write(" title=\"");
			renderer.WriteChildren(link);
			_ = renderer.Write('"');
		}

		_ = renderer.Write(" />");
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
