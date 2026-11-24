// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.Text;
using Elastic.ApiExplorer.Infrastructure;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Elastic.ApiExplorer.Export;

/// <summary>The search text of an API page's Markdown export: the page as plain text, its title and the titles of its sections.</summary>
internal static class ApiPagePlainText
{
	private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder().Build();

	/// <summary>
	/// Reads the search text of <paramref name="markdown"/>. Titles come from each heading's own inline content, so a heading such as
	/// <c>1. Authentication</c> is never parsed again as a list, and a linked heading keeps its text without its target. Without an
	/// H1, the title is <paramref name="fallbackTitle"/>.
	/// </summary>
	public static ApiPageSearchText Read(string markdown, string fallbackTitle, string? description)
	{
		var document = Markdown.Parse(markdown, Pipeline);
		var headings = document.Descendants<HeadingBlock>().ToArray();
		// The page renderer prefers a top-level H1 to one nested in a quote or a list, and falls back to the nested one.
		var title = FirstTitle(document.OfType<HeadingBlock>()) ?? FirstTitle(headings);
		using var writer = new StringWriter();
		_ = Markdown.ToPlainText(markdown, writer, Pipeline);
		return new ApiPageSearchText(
			title ?? fallbackTitle,
			Description(description),
			writer.ToString().Trim(),
			[
				.. headings
					.Where(static h => h.Level > 1)
					.Select(HeadingText)
					.Where(static t => t.Length > 0)
					.Distinct(StringComparer.Ordinal)
			]
		);
	}

	/// <summary>
	/// A page description as plain text, whether it arrives as a plain-text excerpt or as a line of authored Markdown: comments,
	/// emphasis and link targets go. Angle brackets stay, because a literal placeholder such as <c>&lt;ping&gt;</c> reads like a tag.
	/// </summary>
	public static string Description(string? description) =>
		string.IsNullOrWhiteSpace(description) ? "" : ApiSeoDescription.ToPlainText(ApiSeoDescription.WithoutComments(description));

	private static string? FirstTitle(IEnumerable<HeadingBlock> headings) =>
		headings.Where(static h => h.Level == 1).Select(HeadingText).FirstOrDefault(static t => t.Length > 0);

	// Inline elements an author formats a heading with. Any other tag, such as `<tenant>`, is a literal placeholder readers see.
	private static readonly FrozenSet<string> MarkupTags = FrozenSet.Create(
		StringComparer.OrdinalIgnoreCase,
		"a",
		"abbr",
		"b",
		"br",
		"code",
		"del",
		"em",
		"i",
		"ins",
		"kbd",
		"mark",
		"q",
		"s",
		"small",
		"span",
		"strong",
		"sub",
		"sup",
		"u",
		"wbr"
	);

	private static bool IsPlaceholder(string tag)
	{
		if (tag.StartsWith("<!--", StringComparison.Ordinal) || tag.StartsWith("</", StringComparison.Ordinal))
			return false;
		var name = tag.AsSpan(1).TrimEnd('>').TrimEnd('/');
		var end = name.IndexOfAny(' ', '\t', '/');
		return !MarkupTags.Contains((end < 0 ? name : name[..end]).ToString());
	}

	private static string HeadingText(HeadingBlock heading)
	{
		if (heading.Inline is null)
			return "";
		var text = new StringBuilder();
		foreach (var inline in heading.Inline.Descendants())
		{
			_ = inline switch
			{
				LiteralInline literal => text.Append(literal.Content),
				CodeInline code => text.Append(code.Content),
				HtmlEntityInline entity => text.Append(entity.Transcoded),
				// The export writes a generated title such as `Access <tenant>` as it is, so a placeholder is visible text.
				HtmlInline html when IsPlaceholder(html.Tag) => text.Append(html.Tag),
				AutolinkInline autolink => text.Append(autolink.Url),
				LineBreakInline => text.Append(' '),
				_ => text
			};
		}
		return text.ToString().Trim();
	}
}
