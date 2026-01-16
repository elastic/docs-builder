// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Myst.InlineParsers.Substitution;
using Elastic.Markdown.Myst.Renderers.LlmMarkdown;
using Elastic.Markdown.Myst.Roles;
using Elastic.Markdown.Myst.Roles.AppliesTo;
using Elastic.Markdown.Myst.Roles.Kbd;
using Markdig.Renderers;
using Markdig.Syntax.Inlines;

namespace Elastic.Markdown.Myst.Renderers.PlainText;

/// <summary>
/// Renders links as plain text (just the link text, no URL)
/// </summary>
public class PlainTextLinkRenderer : MarkdownObjectRenderer<PlainTextRenderer, LinkInline>
{
	protected override void Write(PlainTextRenderer renderer, LinkInline obj) =>
		// For both images and links, just output the text content
		renderer.WriteChildren(obj);
}

/// <summary>
/// Renders emphasis as plain text (strips * and ** markers)
/// </summary>
public class PlainTextEmphasisRenderer : MarkdownObjectRenderer<PlainTextRenderer, EmphasisInline>
{
	protected override void Write(PlainTextRenderer renderer, EmphasisInline obj) =>
		// Just render children without emphasis markers
		renderer.WriteChildren(obj);
}

/// <summary>
/// Renders substitutions with their resolved value
/// </summary>
public class PlainTextSubstitutionRenderer : MarkdownObjectRenderer<PlainTextRenderer, SubstitutionLeaf>
{
	protected override void Write(PlainTextRenderer renderer, SubstitutionLeaf obj)
		=> renderer.Write(obj.Found ? obj.Replacement : obj.Content);
}

/// <summary>
/// Renders inline code as plain text (strips backticks)
/// </summary>
public class PlainTextCodeInlineRenderer : MarkdownObjectRenderer<PlainTextRenderer, CodeInline>
{
	protected override void Write(PlainTextRenderer renderer, CodeInline obj)
		=> renderer.Write(obj.Content);
}

/// <summary>
/// Renders literal text as-is
/// </summary>
public class PlainTextLiteralRenderer : MarkdownObjectRenderer<PlainTextRenderer, LiteralInline>
{
	protected override void Write(PlainTextRenderer renderer, LiteralInline obj)
		=> renderer.Write(obj.Content.ToString());
}

/// <summary>
/// Renders line breaks as spaces or newlines
/// </summary>
public class PlainTextLineBreakRenderer : MarkdownObjectRenderer<PlainTextRenderer, LineBreakInline>
{
	protected override void Write(PlainTextRenderer renderer, LineBreakInline obj)
	{
		if (obj.IsHard)
			renderer.WriteLine();
		else
			renderer.Write(" ");
	}
}

/// <summary>
/// Renders roles as plain text
/// </summary>
public class PlainTextRoleRenderer : MarkdownObjectRenderer<PlainTextRenderer, RoleLeaf>
{
	protected override void Write(PlainTextRenderer renderer, RoleLeaf obj)
	{
		switch (obj)
		{
			case KbdRole kbd:
				// Render keyboard shortcuts as plain text
				var shortcut = kbd.KeyboardShortcut;
				var output = KeyboardShortcut.RenderPlainText(shortcut);
				renderer.Write(output);
				break;
			case AppliesToRole appliesToRole:
				// Render applies-to as readable text (without XML tags)
				var appliesText = LlmApplicabilityHelper.RenderForLlm(
					appliesToRole.AppliesTo,
					appliesToRole.BuildContext.VersionsConfiguration,
					useInlineTag: false);
				if (!string.IsNullOrEmpty(appliesText))
					renderer.Write($"({appliesText})");
				break;
			default:
				// For other roles, just output the content without backticks
				renderer.Write(obj.Content);
				break;
		}
	}
}
