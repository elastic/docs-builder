// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.RegularExpressions;
using Markdig;
using Markdig.Extensions.DefinitionLists;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Elastic.Markdown.Myst.Renderers;

/// <summary>
/// Overrides HtmlDefinitionListRenderer to add id attributes to code-quoted definition terms,
/// enabling fragment links to individual parameters/flags.
/// </summary>
public partial class DefinitionListAnchorRenderer : HtmlDefinitionListRenderer
{
	protected override void Write(HtmlRenderer renderer, DefinitionList list)
	{
		_ = renderer.EnsureLine();
		_ = renderer.Write("<dl").WriteAttributes(list).WriteLine('>');

		foreach (var item in list)
		{
			var definitionItem = (DefinitionItem)item;
			var hasOpendd = false;
			var countdd = 0;
			var lastWasSimpleParagraph = false;

			for (var i = 0; i < definitionItem.Count; i++)
			{
				var node = definitionItem[i];
				if (node is DefinitionTerm term)
				{
					if (hasOpendd)
					{
						if (!lastWasSimpleParagraph)
							_ = renderer.EnsureLine();
						_ = _ = renderer.WriteLine("</dd>");
						lastWasSimpleParagraph = false;
						hasOpendd = false;
						countdd = 0;
					}

					// Only add id when term contains inline code (backtick-quoted)
					var id = HasCodeInline(term) ? ExtractAnchorId(ExtractPlainText(term)) : null;
					_ = renderer.Write("<dt");
					if (!string.IsNullOrEmpty(id))
						_ = renderer.Write($" id=\"{id}\"");
					_ = renderer.WriteAttributes(term);
					_ = renderer.Write('>');
					_ = renderer.WriteLeafInline(term);
					if (!string.IsNullOrEmpty(id))
						_ = renderer.Write($"<a class=\"paramlink\" href=\"#{id}\" aria-hidden=\"true\">#</a>");
					_ = renderer.WriteLine("</dt>");
				}
				else
				{
					if (!hasOpendd)
					{
						_ = renderer.Write("<dd").WriteAttributes(definitionItem).Write('>');
						countdd = 0;
						hasOpendd = true;
					}

					var nextTerm = i + 1 < definitionItem.Count ? definitionItem[i + 1] : null;
					var isSimpleParagraph = (nextTerm is null || nextTerm is DefinitionItem) &&
											countdd == 0 && node is ParagraphBlock;
					var saveImplicit = renderer.ImplicitParagraph;
					if (isSimpleParagraph)
					{
						renderer.ImplicitParagraph = true;
						lastWasSimpleParagraph = true;
					}
					renderer.Write(node);
					renderer.ImplicitParagraph = saveImplicit;
					countdd++;
				}
			}

			if (hasOpendd)
			{
				if (!lastWasSimpleParagraph)
					_ = renderer.EnsureLine();
				_ = _ = renderer.WriteLine("</dd>");
			}
		}
		_ = renderer.WriteLine("</dl>");
	}

	private static bool HasCodeInline(DefinitionTerm term) =>
		term.Inline?.Any(i => i is CodeInline) ?? false;

	/// <summary>
	/// Extracts a clean anchor id from a parameter term:
	/// "-l --log-level" → "log-level", "--[no-]strict" → "strict", "&lt;source&gt;" → "source"
	/// </summary>
	internal static string ExtractAnchorId(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
			return string.Empty;

		var longFlagMatches = LongFlagRegex().Matches(text);
		if (longFlagMatches.Count > 0)
		{
			var name = longFlagMatches[^1].Groups[1].Value;
			name = NoFlagRegex().Replace(name, string.Empty);
			return name;
		}

		var positionalMatch = PositionalRegex().Match(text);
		if (positionalMatch.Success)
			return positionalMatch.Groups[1].Value;

		var shortFlagMatch = ShortFlagRegex().Match(text);
		if (shortFlagMatch.Success)
			return shortFlagMatch.Groups[1].Value;

		return string.Empty;
	}

	private static string ExtractPlainText(DefinitionTerm term)
	{
		var sb = new System.Text.StringBuilder();
		if (term.Inline == null)
			return string.Empty;
		foreach (var inline in term.Inline)
		{
			switch (inline)
			{
				case LiteralInline literal:
					_ = sb.Append(literal.Content);
					break;
				case CodeInline code:
					_ = sb.Append(code.Content);
					break;
			}
		}
		return sb.ToString().Trim();
	}

	[GeneratedRegex(@"--\[?(?:no-)?\]?([\w-]+)")]
	private static partial Regex LongFlagRegex();

	[GeneratedRegex(@"\[no-\]")]
	private static partial Regex NoFlagRegex();

	[GeneratedRegex(@"<([\w-]+)>")]
	private static partial Regex PositionalRegex();

	[GeneratedRegex(@"-([a-zA-Z])")]
	private static partial Regex ShortFlagRegex();
}

public static class DefinitionTermAnchorExtensions
{
	public static MarkdownPipelineBuilder UseDefinitionTermAnchors(this MarkdownPipelineBuilder pipeline)
	{
		pipeline.Extensions.AddIfNotAlready<DefinitionTermAnchorExtension>();
		return pipeline;
	}
}

public class DefinitionTermAnchorExtension : IMarkdownExtension
{
	public void Setup(MarkdownPipelineBuilder pipeline) { }

	public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
	{
		if (renderer is HtmlRenderer htmlRenderer)
		{
			// Replace the default HtmlDefinitionListRenderer with our anchor-aware subclass
			_ = htmlRenderer.ObjectRenderers.RemoveAll(x => x is HtmlDefinitionListRenderer);
			htmlRenderer.ObjectRenderers.Add(new DefinitionListAnchorRenderer());
		}
	}
}
