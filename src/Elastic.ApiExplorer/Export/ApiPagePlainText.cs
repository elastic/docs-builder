// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

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
		// The page renderer takes its title from a top-level H1, never from one nested in a quote or a list.
		var title = document
			.OfType<HeadingBlock>()
			.Where(static h => h.Level == 1)
			.Select(HeadingText)
			.FirstOrDefault(static t => t.Length > 0);
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
				// The export writes a generated title such as `Access <tenant>` as it is, so a tag in a heading is its visible text.
				HtmlInline html => text.Append(html.Tag),
				AutolinkInline autolink => text.Append(autolink.Url),
				LineBreakInline => text.Append(' '),
				_ => text
			};
		}
		return text.ToString().Trim();
	}
}
