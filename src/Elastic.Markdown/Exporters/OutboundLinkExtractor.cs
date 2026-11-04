// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Myst.Comments;
using Elastic.Markdown.Myst.Directives.Hub;
using Elastic.Markdown.Myst.Directives.Include;
using Elastic.Markdown.Myst.Directives.PageCard;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Elastic.Markdown.Exporters;

/// <summary>
/// Collects the link targets of a parsed markdown document for the search index.
/// </summary>
public static class OutboundLinkExtractor
{
	/// <summary>
	/// Returns the distinct link targets in document order, or <c>null</c> when the page has none.
	/// Covers inline links, <c>{page-card}</c> and <c>{link-card}</c> targets, and the content of
	/// <c>{include}</c> snippets. Images, same-page anchors and links inside comments are skipped.
	/// </summary>
	public static string[]? Extract(MarkdownDocument document)
	{
		var links = new List<string?>();
		Collect(document, links, []);
		var distinct = links
			.OfType<string>()
			.Where(url => !string.IsNullOrWhiteSpace(url) && !url.StartsWith('#'))
			.Distinct(StringComparer.Ordinal)
			.ToArray();
		return distinct.Length > 0 ? distinct : null;
	}

	private static void Collect(MarkdownDocument document, List<string?> links, HashSet<string> visitedIncludes)
	{
		foreach (var obj in document.Descendants<MarkdownObject>())
		{
			switch (obj)
			{
				case LinkInline { IsImage: false } link when !IsInComment(link):
					links.Add(link.Url);
					break;
				case AutolinkInline autolink when !IsInComment(autolink):
					links.Add(autolink.Url);
					break;
				case PageCardBlock card:
					links.Add(card.ResolvedUrl);
					break;
				case LinkCardBlock card:
					links.Add(card.Data.Link);
					links.AddRange(card.Data.Links.Select(l => l.Url));
					break;
				case IncludeBlock { Found: true, Literal: false, IncludePath: { } path } include when visitedIncludes.Add(path):
					Collect(include.ParseSnippet(), links, visitedIncludes);
					break;
			}
		}
	}

	private static bool IsInComment(Inline inline)
	{
		var container = inline as ContainerInline ?? inline.Parent;
		while (container?.Parent is { } parent)
			container = parent;
		return container?.ParentBlock is CommentBlock;
	}
}
