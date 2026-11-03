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

/// <summary>A link target with the file and 1-based position it was written at.</summary>
public readonly record struct OutboundLink(string Url, string File, int Line, int Column);

/// <summary>
/// Collects the link targets of a parsed markdown document for the search index and for link checks.
/// </summary>
public static class OutboundLinkExtractor
{
	/// <summary>
	/// Returns the distinct link targets in document order, or <c>null</c> when the page has none.
	/// Covers inline links, autolinks, <c>{page-card}</c> and <c>{link-card}</c> targets, and the content of
	/// <c>{include}</c> snippets. Images, same-page anchors and links inside comments are skipped.
	/// </summary>
	public static string[]? Extract(MarkdownDocument document)
	{
		var distinct = ExtractWithLocations(document, string.Empty).Select(link => link.Url).Distinct(StringComparer.Ordinal).ToArray();
		return distinct.Length > 0 ? distinct : null;
	}

	/// <summary>
	/// Returns every link occurrence in document order with the file it was written in. Links inside an
	/// <c>{include}</c> snippet report the snippet file and the line within it.
	/// </summary>
	public static IReadOnlyList<OutboundLink> ExtractWithLocations(MarkdownDocument document, string file)
	{
		var links = new List<OutboundLink>();
		Collect(document, file, links, []);
		return [.. links.Where(link => !string.IsNullOrWhiteSpace(link.Url) && !link.Url.StartsWith('#'))];
	}

	private static void Collect(MarkdownDocument document, string file, List<OutboundLink> links, HashSet<string> visitedIncludes)
	{
		foreach (var obj in document.Descendants<MarkdownObject>())
		{
			switch (obj)
			{
				case LinkInline { IsImage: false } link when !IsInComment(link):
					Add(links, link.Url, file, link);
					break;
				case AutolinkInline autolink when !IsInComment(autolink):
					Add(links, autolink.Url, file, autolink);
					break;
				case PageCardBlock card:
					Add(links, card.ResolvedUrl, file, card);
					break;
				case LinkCardBlock card:
					AddCardTarget(links, card.Data.Link, file, card);
					foreach (var linkItem in card.Data.Links)
						AddCardTarget(links, linkItem.Url, file, card);
					break;
				case IncludeBlock { Found: true, Literal: false, IncludePath: { } path } include when visitedIncludes.Add(path):
					Collect(include.ParseSnippet(), path, links, visitedIncludes);
					break;
			}
		}
	}

	// The autolink scan already reports https:// targets in a card's YAML body at their exact position.
	private static void AddCardTarget(List<OutboundLink> links, string? url, string file, MarkdownObject card)
	{
		if (url is not null && !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
			Add(links, url, file, card);
	}

	private static void Add(List<OutboundLink> links, string? url, string file, MarkdownObject source)
	{
		if (url is not null)
			links.Add(new OutboundLink(url, file, source.Line + 1, Math.Max(source.Column, 1)));
	}

	private static bool IsInComment(Inline inline)
	{
		var container = inline as ContainerInline ?? inline.Parent;
		while (container?.Parent is { } parent)
			container = parent;
		return container?.ParentBlock is CommentBlock;
	}
}
