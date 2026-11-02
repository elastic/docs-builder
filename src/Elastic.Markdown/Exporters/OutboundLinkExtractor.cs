// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

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
	/// Images and same-page anchors are skipped.
	/// </summary>
	public static string[]? Extract(MarkdownDocument document)
	{
		var links = document
			.Descendants<LinkInline>()
			.Where(l => !l.IsImage)
			.Select(l => l.Url)
			.Concat(document.Descendants<AutolinkInline>().Select(l => l.Url))
			.Where(url => !string.IsNullOrWhiteSpace(url) && !url.StartsWith('#'))
			.Select(url => url!)
			.Distinct(StringComparer.Ordinal)
			.ToArray();
		return links.Length > 0 ? links : null;
	}
}
