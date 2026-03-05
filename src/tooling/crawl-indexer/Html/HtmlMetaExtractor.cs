// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AngleSharp.Dom;
using AngleSharp.Html.Dom;

namespace CrawlIndexer.Html;

/// <summary>
/// Utility class for extracting metadata from HTML documents.
/// </summary>
internal static class HtmlMetaExtractor
{
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

	public static string? GetTitle(IHtmlDocument document)
	{
		// Try og:title first
		var ogTitle = GetMetaContent(document, "og:title");
		if (!string.IsNullOrWhiteSpace(ogTitle))
			return ogTitle;

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

		// Try h1
		var h1 = document.QuerySelector("h1")?.TextContent;
		if (!string.IsNullOrWhiteSpace(h1))
			return h1.Trim();

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
		// Clone to avoid modifying original
		if (container.Clone(true) is not IElement clone)
			return string.Empty;

		// Remove script, style, nav elements
		foreach (var el in clone.QuerySelectorAll("script, style, nav, aside, footer, header, .sidebar, .navigation, .toc").ToList())
			el.Remove();

		return NormalizeWhitespace(clone.TextContent);
	}

	public static string CreateAbstract(string textContent, string[] headings, int maxLength = 400)
	{
		// Take first N characters of text
		var abstractText = textContent.Length > maxLength
			? textContent[..maxLength] + "..."
			: textContent;

		// Append headings for context
		if (headings.Length > 0)
		{
			var headingText = string.Join(" | ", headings.Take(5));
			abstractText = $"{abstractText} [{headingText}]";
		}

		return abstractText;
	}

	private static string NormalizeWhitespace(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
			return string.Empty;

		// Replace multiple whitespace with single space
		var chars = new char[text.Length];
		var j = 0;
		var lastWasWhitespace = true;

		for (var i = 0; i < text.Length; i++)
		{
			var c = text[i];
			var isWhitespace = char.IsWhiteSpace(c);

			if (isWhitespace)
			{
				if (!lastWasWhitespace)
				{
					chars[j++] = ' ';
					lastWasWhitespace = true;
				}
			}
			else
			{
				chars[j++] = c;
				lastWasWhitespace = false;
			}
		}

		return new string(chars, 0, j).Trim();
	}
}
