// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;

namespace Elastic.SiteSearch.Cli.LabsCrawl;

/// <summary>
/// Utility class for extracting metadata from HTML documents.
/// </summary>
internal static class HtmlMetaExtractor
{
	private static readonly HashSet<string> BlockElements =
	[
with(StringComparer.OrdinalIgnoreCase),
		"p",
		"div",
		"h1",
		"h2",
		"h3",
		"h4",
		"h5",
		"h6",
		"li",
		"ul",
		"ol",
		"blockquote",
		"pre",
		"section",
		"article",
		"main",
		"header",
		"footer",
		"table",
		"tr",
		"dd",
		"dt",
		"figcaption",
		"figure",
		"hr",
		"br",
		"address",
		"details",
		"summary"
	];

	public static string? GetMetaContent(IHtmlDocument document, string name, string? attribute = null)
	{
		// Try standard name attribute
		var meta = document.QuerySelector($"meta[name='{name}']");
		if (meta is not null)
			return meta.GetAttribute("content");

		// Try property attribute (for Open Graph)
		meta = document.QuerySelector($"meta[property='{name}']");
		if (meta is not null)
			return meta.GetAttribute("content");

		// Try custom class attribute (for Elastic-specific meta tags)
		if (attribute is not null)
		{
			meta = document.QuerySelector($"meta[class='{attribute}'][name='{name}']");
			if (meta is not null)
				return meta.GetAttribute("content");
		}

		return null;
	}

	/// <summary>
	/// Resolves the clean, displayable title for a page. Prefers the article's own
	/// headline (<c>&lt;h1&gt;</c>, scoped to <paramref name="contentContainer"/> when given)
	/// over SEO metadata: <c>og:title</c>/<c>&lt;title&gt;</c> are frequently phrased differently
	/// for search-engine purposes and sometimes carry a category/product prefix
	/// (e.g. "ES|QL Kibana: ...", "Author: ..."). The raw <c>og:title</c> is still captured
	/// separately by callers (see <c>og_title</c>) for anyone who needs the original SEO string.
	/// </summary>
	public static string? GetTitle(IHtmlDocument document, IElement? contentContainer = null)
	{
		var h1 = (contentContainer?.QuerySelector("h1") ?? document.QuerySelector("h1"))?.TextContent;
		if (!string.IsNullOrWhiteSpace(h1))
			return h1.Trim();

		// Try <title> tag
		var title = document.QuerySelector("title")?.TextContent;
		if (!string.IsNullOrWhiteSpace(title))
		{
			// Remove common suffixes like " | Elastic"
			var pipeIndex = title.LastIndexOf('|');
			if (pipeIndex > 0)
				title = title[..pipeIndex].Trim();
			return title;
		}

		// Fall back to og:title
		var ogTitle = GetMetaContent(document, "og:title");
		if (!string.IsNullOrWhiteSpace(ogTitle))
			return ogTitle;

		return null;
	}

	public static string? GetDescription(IHtmlDocument document)
	{
		// Try og:description first
		var ogDesc = GetMetaContent(document, "og:description");
		if (!string.IsNullOrWhiteSpace(ogDesc))
			return ogDesc;

		// Try standard description
		var desc = GetMetaContent(document, "description");
		if (!string.IsNullOrWhiteSpace(desc))
			return desc;

		return null;
	}

	public static DateTimeOffset? GetArticleModifiedTime(IHtmlDocument document)
	{
		var modified = GetMetaContent(document, "article:modified_time");
		if (!string.IsNullOrWhiteSpace(modified) && DateTimeOffset.TryParse(modified, out var parsed))
			return parsed;
		return null;
	}

	public static DateTimeOffset? GetArticlePublishedTime(IHtmlDocument document)
	{
		var published = GetMetaContent(document, "article:published_time");
		if (!string.IsNullOrWhiteSpace(published) && DateTimeOffset.TryParse(published, out var parsed))
			return parsed;
		return null;
	}

	public static string? GetAuthor(IHtmlDocument document) =>
		GetMetaContent(document, "article:author") ?? GetMetaContent(document, "author");

	public static string? GetOgImage(IHtmlDocument document) =>
		GetMetaContent(document, "og:image");

	public static string? GetTwitterImage(IHtmlDocument document) =>
		GetMetaContent(document, "twitter:image");

	public static string? GetTwitterCard(IHtmlDocument document) =>
		GetMetaContent(document, "twitter:card");

	public static string[] ExtractHeadings(IElement container)
	{
		var headings = container.QuerySelectorAll("h1, h2, h3, h4, h5, h6");
		return headings
			.Select(h => h.TextContent.Trim())
			.Where(text => !string.IsNullOrWhiteSpace(text))
			.Distinct()
			.ToArray();
	}

	public static string ExtractTextContent(IElement container)
	{
		if (container.Clone(true) is not IElement clone)
			return string.Empty;

		foreach (var el in clone.QuerySelectorAll("script, style, nav, aside, footer, header, .sidebar, .navigation, .toc").ToList())
			el.Remove();

		var sb = new StringBuilder();
		ExtractTextWithBlockBreaks(clone, sb);
		return NormalizeBlockText(sb.ToString());
	}

	private static void ExtractTextWithBlockBreaks(INode node, StringBuilder sb)
	{
		foreach (var child in node.ChildNodes)
		{
			if (child is IElement element && BlockElements.Contains(element.LocalName))
			{
				_ = sb.Append('\n');
				ExtractTextWithBlockBreaks(child, sb);
				_ = sb.Append('\n');
			}
			else if (child.NodeType == NodeType.Text)
			{
				_ = sb.Append(child.TextContent);
			}
			else
			{
				ExtractTextWithBlockBreaks(child, sb);
			}
		}
	}

	/// <summary>Collapses inline whitespace while preserving block-level newlines.</summary>
	private static string NormalizeBlockText(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
			return string.Empty;

		var sb = new StringBuilder(text.Length);
		var lastWasWhitespace = true;
		var lastWasNewline = true;

		for (var i = 0; i < text.Length; i++)
		{
			var c = text[i];

			if (c == '\n')
			{
				if (!lastWasNewline)
				{
					_ = sb.Append('\n');
					lastWasNewline = true;
					lastWasWhitespace = true;
				}
			}
			else if (char.IsWhiteSpace(c))
			{
				if (!lastWasWhitespace)
				{
					_ = sb.Append(' ');
					lastWasWhitespace = true;
				}
			}
			else
			{
				_ = sb.Append(c);
				lastWasWhitespace = false;
				lastWasNewline = false;
			}
		}

		return sb.ToString().Trim();
	}

	/// <summary>
	/// Builds the searchable abstract by folding the rich <c>og:description</c>/<c>description</c>
	/// meta (<paramref name="description"/>) in front of the leading body text. The plain
	/// <c>description</c> field isn't part of the Elasticsearch mapping, so without this it would
	/// contribute nothing to BM25 scoring despite usually being the most information-dense
	/// sentence on the page.
	/// </summary>
	public static string CreateAbstract(string textContent, string? description, int maxLength = 400)
	{
		var lead = textContent.Length > maxLength
			? textContent[..maxLength] + "..."
			: textContent;

		return string.IsNullOrWhiteSpace(description) ? lead : $"{description} {lead}";
	}

	/// <summary>Title-cases a URL slug, e.g. "google-cloud" -> "Google Cloud".</summary>
	public static string TitleCaseSlug(string slug) =>
		string.Join(' ', slug
			.Split('-', StringSplitOptions.RemoveEmptyEntries)
			.Select(w => char.ToUpperInvariant(w[0]) + w[1..]));

	/// <summary>
	/// Scans a listing page's article cards for <c>&lt;time datetime&gt;</c> elements and returns
	/// the most recent one — used as the listing's effective publish date so tag/author pages
	/// reflect their newest linked article instead of the listing shell's own (rarely present)
	/// publish metadata.
	/// </summary>
	public static DateTimeOffset? ExtractMaxListingDate(IElement container)
	{
		DateTimeOffset? max = null;
		foreach (var time in container.QuerySelectorAll("time[datetime]"))
		{
			var raw = time.GetAttribute("datetime");
			if (string.IsNullOrWhiteSpace(raw) || !DateTimeOffset.TryParse(raw, out var parsed))
				continue;
			if (max is null || parsed > max)
				max = parsed;
		}
		return max;
	}
}
