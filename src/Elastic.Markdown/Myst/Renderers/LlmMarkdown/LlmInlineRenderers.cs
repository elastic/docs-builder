// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.IO;
using Elastic.Markdown.Myst.InlineParsers.Substitution;
using Elastic.Markdown.Myst.Roles;
using Elastic.Markdown.Myst.Roles.Kbd;
using Markdig.Renderers;
using Markdig.Syntax.Inlines;

namespace Elastic.Markdown.Myst.Renderers.LlmMarkdown;

public class LlmLinkInlineRenderer : MarkdownObjectRenderer<LlmMarkdownRenderer, LinkInline>
{
	protected override void Write(LlmMarkdownRenderer renderer, LinkInline obj)
	{
		if (obj.IsImage)
		{
			renderer.Writer.Write("!");
			renderer.Writer.Write("[");
			renderer.WriteChildren(obj);
			renderer.Writer.Write("]");
			renderer.Writer.Write("(");

			// Make image URL absolute for better LLM consumption
			var imageUrl = LlmRenderingHelpers.MakeAbsoluteUrl(renderer, obj.Url);
			renderer.Writer.Write(imageUrl ?? string.Empty);
		}
		else
		{
			renderer.Writer.Write("[");
			renderer.WriteChildren(obj);
			renderer.Writer.Write("](");
			var url = obj.GetDynamicUrl?.Invoke() ?? obj.Url;

			// Check if this is an internal link to a markdown page
			var isCrossLink = (obj.GetData("isCrossLink") as bool?) == true;
			var hasTargetNavigationRoot = obj.GetData($"Target{nameof(MarkdownFile.NavigationRoot)}") != null;
			var originalCrossLinkUrl = obj.GetData("originalCrossLinkUrl") as string;
			var isInternalMarkdownLink = !isCrossLink && hasTargetNavigationRoot;
			var isCrossLinkToMarkdown = isCrossLink && originalCrossLinkUrl is not null && IsCrossLinkToMarkdown(originalCrossLinkUrl);

			if (isInternalMarkdownLink)
			{
				// For internal markdown links, preserve the .md extension
				renderer.Writer.Write(EnsureMarkdownExtension(url) ?? string.Empty);
			}
			else if (isCrossLinkToMarkdown)
			{
				// For cross-links to markdown files, use absolute URL with .md extension
				var absoluteUrl = LlmRenderingHelpers.MakeAbsoluteUrl(renderer, url);
				var urlWithMdExtension = EnsureMarkdownExtension(absoluteUrl);
				renderer.Writer.Write(urlWithMdExtension ?? string.Empty);
			}
			else
			{
				// For external links and non-markdown cross-links, make absolute
				var absoluteUrl = LlmRenderingHelpers.MakeAbsoluteUrl(renderer, url);
				renderer.Writer.Write(absoluteUrl ?? string.Empty);
			}
		}
		if (!string.IsNullOrEmpty(obj.Title))
		{
			renderer.Writer.Write(" \"");
			renderer.Writer.Write(obj.Title);
			renderer.Writer.Write("\"");
		}
		renderer.Writer.Write(")");
	}

	/// <summary>
	/// Ensures the URL ends with .md extension for markdown links
	/// </summary>
	private static string? EnsureMarkdownExtension(string? url)
	{
		if (string.IsNullOrEmpty(url))
			return url;

		// If it already has .md extension, return as-is
		if (url.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
			return url;

		// Convert absolute paths to relative paths for markdown links
		var processedUrl = url.StartsWith('/') ? url.TrimStart('/') : url;

		// Add .md extension to internal markdown links
		return processedUrl + ".md";
	}

	/// <summary>
	/// Checks if a cross-link URL points to a markdown file
	/// </summary>
	private static bool IsCrossLinkToMarkdown(string originalCrossLinkUrl)
	{
		if (string.IsNullOrEmpty(originalCrossLinkUrl))
			return false;

		// Parse the cross-link URI to extract the path
		if (Uri.TryCreate(originalCrossLinkUrl, UriKind.Absolute, out var uri))
		{
			var path = uri.AbsolutePath;
			return path.EndsWith(".md", StringComparison.OrdinalIgnoreCase);
		}

		return false;
	}
}

public class LlmEmphasisInlineRenderer : MarkdownObjectRenderer<LlmMarkdownRenderer, EmphasisInline>
{
	protected override void Write(LlmMarkdownRenderer renderer, EmphasisInline obj)
	{
		var markers = new string(obj.DelimiterChar, obj.DelimiterCount);
		renderer.Writer.Write(markers);
		renderer.WriteChildren(obj);
		renderer.Writer.Write(markers);
	}
}

public class LlmSubstitutionLeafRenderer : MarkdownObjectRenderer<LlmMarkdownRenderer, SubstitutionLeaf>
{
	protected override void Write(LlmMarkdownRenderer renderer, SubstitutionLeaf obj)
		=> renderer.Writer.Write(obj.Found ? obj.Replacement : obj.Content);
}

public class LlmCodeInlineRenderer : MarkdownObjectRenderer<LlmMarkdownRenderer, CodeInline>
{
	protected override void Write(LlmMarkdownRenderer renderer, CodeInline obj)
	{
		renderer.Writer.Write("`");
		renderer.Writer.Write(obj.Content);
		renderer.Writer.Write("`");
	}
}

public class LlmLiteralInlineRenderer : MarkdownObjectRenderer<LlmMarkdownRenderer, LiteralInline>
{
	protected override void Write(LlmMarkdownRenderer renderer, LiteralInline obj) => renderer.Writer.Write(obj.Content);
}

public class LlmLineBreakInlineRenderer : MarkdownObjectRenderer<LlmMarkdownRenderer, LineBreakInline>
{
	protected override void Write(LlmMarkdownRenderer renderer, LineBreakInline obj)
	{
		if (obj.IsHard)
			renderer.WriteLine();
		renderer.WriteLine();
	}
}

public class LlmRoleRenderer : MarkdownObjectRenderer<LlmMarkdownRenderer, RoleLeaf>
{
	protected override void Write(LlmMarkdownRenderer renderer, RoleLeaf obj)
	{
		switch (obj)
		{
			case KbdRole kbd:
				{
					var shortcut = kbd.KeyboardShortcut;
					var output = KeyboardShortcut.RenderLlm(shortcut);
					renderer.Writer.Write(output);
					break;
				}
			// TODO: Add support for applies_to role
			default:
				{
					new LlmCodeInlineRenderer().Write(renderer, obj);
					break;
				}
		}
	}
}
