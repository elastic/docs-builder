// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

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
