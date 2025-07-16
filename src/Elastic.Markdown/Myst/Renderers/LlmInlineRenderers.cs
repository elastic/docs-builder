// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Linq;
using System.Text;
using Elastic.Markdown.Myst.InlineParsers.Substitution;
using Elastic.Markdown.Myst.Roles;
using Elastic.Markdown.Myst.Roles.Kbd;
using Markdig.Renderers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Elastic.Markdown.Myst.Renderers;

/// <summary>
/// Renders links as standard CommonMark links, resolving cross-references for LLM consumption
/// </summary>
public class LlmLinkInlineRenderer : MarkdownObjectRenderer<LlmMarkdownRenderer, LinkInline>
{
	protected override void Write(LlmMarkdownRenderer renderer, LinkInline obj)
	{
		if (obj.IsImage)
		{
			// Render image as markdown image
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
			// Render link as markdown link
			renderer.Writer.Write("[");
			renderer.WriteChildren(obj);
			renderer.Writer.Write("](");

			// Use resolved URL if available, otherwise use original, then make absolute
			var url = obj.GetDynamicUrl?.Invoke() ?? obj.Url;
			var absoluteUrl = LlmRenderingHelpers.MakeAbsoluteUrl(renderer, url);
			renderer.Writer.Write(absoluteUrl ?? string.Empty);
		}

		if (!string.IsNullOrEmpty(obj.Title))
		{
			renderer.Writer.Write(" \"");
			renderer.Writer.Write(obj.Title);
			renderer.Writer.Write("\"");
		}

		renderer.Writer.Write(")");
	}
}

/// <summary>
/// Renders emphasis (bold/italic) as standard CommonMark emphasis
/// </summary>
public class LlmEmphasisInlineRenderer : MarkdownObjectRenderer<LlmMarkdownRenderer, EmphasisInline>
{
	protected override void Write(LlmMarkdownRenderer renderer, EmphasisInline obj)
	{
		var delimiter = obj.DelimiterChar == '*' ? "*" : "_";
		var markers = new string(obj.DelimiterChar, obj.DelimiterCount);

		renderer.Writer.Write(markers);
		renderer.WriteChildren(obj);
		renderer.Writer.Write(markers);
	}
}

/// <summary>
/// Renders inline code as standard CommonMark code spans
/// </summary>
public class LlmCodeInlineRenderer : MarkdownObjectRenderer<LlmMarkdownRenderer, CodeInline>
{
	protected override void Write(LlmMarkdownRenderer renderer, CodeInline obj)
	{
		renderer.Writer.Write("`");
		renderer.Writer.Write(obj.Content);
		renderer.Writer.Write("`");
	}
}

/// <summary>
/// Renders literal text content
/// </summary>
public class LlmLiteralInlineRenderer : MarkdownObjectRenderer<LlmMarkdownRenderer, LiteralInline>
{
	protected override void Write(LlmMarkdownRenderer renderer, LiteralInline obj) => renderer.Writer.Write(obj.Content.ToString());
}

/// <summary>
/// Renders line breaks as appropriate for the context
/// </summary>
public class LlmLineBreakInlineRenderer : MarkdownObjectRenderer<LlmMarkdownRenderer, LineBreakInline>
{
	protected override void Write(LlmMarkdownRenderer renderer, LineBreakInline obj)
	{
		if (obj.IsHard)
		{
			renderer.Writer.Write("  "); // Two spaces for hard break
			renderer.WriteLine();
		}
		else
		{
			renderer.Writer.Write(" "); // Soft break becomes space
		}
	}
}

/// <summary>
/// Renders MyST roles as structured text for LLM understanding
/// </summary>
public class LlmRoleRenderer : MarkdownObjectRenderer<LlmMarkdownRenderer, RoleLeaf>
{
	protected override void Write(LlmMarkdownRenderer renderer, RoleLeaf obj)
	{
		// Convert role to a format LLMs can understand
		// For example: :doc:`page` becomes [page](page) or just "page" depending on role type

		// RoleLeaf has a Role property and inherits Content from CodeInline
		var roleName = obj.Role ?? "unknown";
		var content = obj.Content;


		switch (obj)
		{
			case KbdRole kbd:
				{
					var shortcut = kbd.KeyboardShortcut;
					var output = KeyboardShortcut.RenderLlm(shortcut);
					renderer.Writer.Write(output);
					break;
				}
			default:
				{
					new LlmCodeInlineRenderer().Write(renderer, obj);
					break;
				}
		}
	}

	private static string ExtractRoleContent(Role role) =>
		// Extract text content from role's children
		role.Descendants()
			.OfType<LiteralInline>()
			.Select(l => l.Content.ToString())
			.Aggregate(string.Empty, (current, text) => current + text);
}

/// <summary>
/// Renders MyST substitutions by expanding them to their replacement text
/// </summary>
public class LlmSubstitutionRenderer : MarkdownObjectRenderer<LlmMarkdownRenderer, SubstitutionLeaf>
{
	protected override void Write(LlmMarkdownRenderer renderer, SubstitutionLeaf obj)
	{
		// Include substitution info as comment for LLM understanding
		renderer.Writer.Write("<!-- SUBSTITUTION: ");
		renderer.Writer.Write(obj.Content);
		renderer.Writer.Write(" = ");
		renderer.Writer.Write(obj.Replacement);
		renderer.Writer.Write(" -->");

		// Output the replacement text for LLM consumption
		renderer.Writer.Write(obj.Found ? obj.Replacement : obj.Content);
	}
}

/// <summary>
/// Renders container inlines by processing their children
/// </summary>
public class LlmContainerInlineRenderer : MarkdownObjectRenderer<LlmMarkdownRenderer, ContainerInline>
{
	protected override void Write(LlmMarkdownRenderer renderer, ContainerInline obj) => renderer.WriteChildren(obj);
}

// Note: LlmHtmlInlineRenderer was removed since HTML is disabled in the base pipeline (.DisableHtml())
// HTML elements are not parsed into the AST, making HTML renderers unnecessary dead code
