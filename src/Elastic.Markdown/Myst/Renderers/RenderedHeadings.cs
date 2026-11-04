// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation;
using Elastic.Markdown.Helpers;
using Elastic.Markdown.Myst.InlineParsers;
using Markdig.Syntax;

namespace Elastic.Markdown.Myst.Renderers;

/// <summary>Reads headings from a parsed document with the anchor ids that <see cref="SectionedHeadingRenderer"/> writes for them.</summary>
public static class RenderedHeadings
{
	/// <summary>The level-1 heading that rendering drops so the layout can show the page title on its own.</summary>
	public static HeadingBlock? FirstLevel1(MarkdownDocument document) =>
		document.Descendants<HeadingBlock>().FirstOrDefault(h => h.Level == 1);

	public static IReadOnlyList<TocItem> Read(MarkdownDocument document, bool stripFirstHeadingLevel1)
	{
		var dropped = stripFirstHeadingLevel1 ? FirstLevel1(document) : null;
		return [
			.. document
				.Descendants<HeadingBlock>()
				.Where(heading => heading != dropped)
				.Select(heading => new TocItem(Title(heading), SectionedHeadingRenderer.Slug(heading), heading.Level))
				.Where(item => item.Slug.Length > 0)
		];
	}

	/// <summary>The plain-text title of <paramref name="heading"/>, without markup or <c>$$$anchor$$$</c> markers.</summary>
	public static string Title(HeadingBlock heading)
	{
		var header = heading.GetData("header") as string ?? string.Empty;
		if (header.Contains('$'))
			header = HeadingAnchorParser.InlineAnchors().Replace(header, "");
		return header.StripMarkdown().Trim();
	}
}
