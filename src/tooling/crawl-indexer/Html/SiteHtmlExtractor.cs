// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Security.Cryptography;
using System.Text;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Elastic.Documentation.Search;
using Microsoft.Extensions.Logging;

namespace CrawlIndexer.Html;

/// <summary>
/// Extracts content from site HTML pages into SiteDocument.
/// </summary>
public class SiteHtmlExtractor(ILogger<SiteHtmlExtractor> logger) : ISiteHtmlExtractor
{
	private readonly HtmlParser _parser = new();

	public async Task<SiteDocument?> ExtractAsync(
		string url,
		string html,
		DateTimeOffset? sitemapLastModified,
		string language,
		string relevance,
		string pageType,
		Cancel ctx = default
	)
	{
		IHtmlDocument document;
		try
		{
			document = await _parser.ParseDocumentAsync(html, ctx);
		}
		catch (Exception ex)
		{
			logger.LogWarning(ex, "Failed to parse HTML for {Url}", url);
			return null;
		}

		// Get title
		var title = HtmlMetaExtractor.GetTitle(document);
		if (string.IsNullOrWhiteSpace(title))
		{
			logger.LogWarning("No title found for {Url}", url);
			return null;
		}

		// Get description
		var description = HtmlMetaExtractor.GetDescription(document);

		// Get Open Graph data
		var ogTitle = HtmlMetaExtractor.GetMetaContent(document, "og:title");
		var ogDescription = HtmlMetaExtractor.GetMetaContent(document, "og:description");
		var ogImage = HtmlMetaExtractor.GetOgImage(document);
		var twitterImage = HtmlMetaExtractor.GetTwitterImage(document);
		var twitterCard = HtmlMetaExtractor.GetTwitterCard(document);

		// Get article metadata
		var author = HtmlMetaExtractor.GetAuthor(document);
		var publishedDate = HtmlMetaExtractor.GetArticlePublishedTime(document);
		var modifiedDate = HtmlMetaExtractor.GetArticleModifiedTime(document);

		// Get main content - try various selectors for site pages
		var contentDiv = document.QuerySelector("main") ??
						 document.QuerySelector("article") ??
						 document.QuerySelector(".content") ??
						 document.QuerySelector(".main-content") ??
						 document.QuerySelector("#content") ??
						 document.Body;

		if (contentDiv is null)
		{
			logger.LogWarning("No content found for {Url}", url);
			return null;
		}

		var textContent = HtmlMetaExtractor.ExtractTextContent(contentDiv);
		var headings = HtmlMetaExtractor.ExtractHeadings(contentDiv);
		var abstractText = HtmlMetaExtractor.CreateAbstract(textContent, headings);

		// Determine last updated date
		// Priority: article:modified_time > sitemap lastmod > article:published_time > current time
		var lastUpdated = modifiedDate
						  ?? sitemapLastModified
						  ?? publishedDate
						  ?? DateTimeOffset.UtcNow;

		// Calculate content hash
		var hash = ComputeHash(title + textContent);

		// Build search title
		var searchTitle = BuildSearchTitle(title, pageType, url);

		return new SiteDocument
		{
			Title = title,
			SearchTitle = searchTitle,
			Type = "site",
			Url = url,
			Hash = hash,
			BatchIndexDate = DateTimeOffset.UtcNow,
			LastUpdated = lastUpdated,
			Description = description,
			Headings = headings,
			Body = textContent,
			StrippedBody = textContent,
			Abstract = abstractText,
			PageType = pageType,
			Language = language,
			Author = author,
			PublishedDate = publishedDate,
			ModifiedDate = modifiedDate,
			Relevance = relevance,
			OgTitle = ogTitle,
			OgDescription = ogDescription,
			OgImage = ogImage,
			TwitterImage = twitterImage,
			TwitterCard = twitterCard
		};
	}

	private static string ComputeHash(string content)
	{
		var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
		return Convert.ToHexString(bytes)[..16].ToLowerInvariant();
	}

	private static string BuildSearchTitle(string title, string pageType, string url)
	{
		// Add context based on page type
		var prefix = pageType switch
		{
			"blog" => "Blog",
			"webinar" => "Webinar",
			"event" => "Event",
			"training" => "Training",
			"resource" => "Resource",
			"customer-story" => "Customer Story",
			"product" => "Product",
			"concept" => "What is",
			_ => null
		};

		if (prefix is not null)
			return $"{prefix}: {title}";

		// Extract category from URL for marketing pages
		var uri = new Uri(url);
		var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
		if (segments.Length > 0)
		{
			var category = segments[0].Replace('-', ' ');
			category = char.ToUpperInvariant(category[0]) + category[1..];
			return $"{category}: {title}";
		}

		return title;
	}
}
