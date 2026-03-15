// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Security.Cryptography;
using System.Text;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using CrawlIndexer.Crawling;
using Elastic.Documentation.Search;
using Microsoft.Extensions.Logging;

namespace CrawlIndexer.Html;

/// <summary>
/// Extracts content from site HTML pages into SiteDocument.
/// </summary>
public class SiteHtmlExtractor(ILogger<SiteHtmlExtractor> logger) : ISiteHtmlExtractor, IDocumentExtractor<SiteDocument>
{
	private readonly HtmlParser _parser = new();

	public Task<SiteDocument?> ExtractAsync(CrawlResult result, CancellationToken ct) =>
		ExtractAsync(
			result.Url,
			result.Content!,
			result.LastModified,
			GetLanguageFromUrl(result.Url),
			GetPageType(result.Url),
			ct,
			result.HttpEtag,
			result.HttpLastModified
		);

	public async Task<SiteDocument?> ExtractAsync(
		string url,
		string html,
		DateTimeOffset? sitemapLastModified,
		string language,
		string pageType,
		Cancel ctx = default,
		string? httpEtag = null,
		DateTimeOffset? httpLastModified = null
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
			OgTitle = ogTitle,
			OgDescription = ogDescription,
			OgImage = ogImage,
			TwitterImage = twitterImage,
			TwitterCard = twitterCard,
			HttpEtag = httpEtag,
			HttpLastModified = httpLastModified
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

	internal static string GetLanguageFromUrl(string url)
	{
		var uri = new Uri(url);
		var path = uri.AbsolutePath;

		if (path.StartsWith("/de/", StringComparison.OrdinalIgnoreCase)) return "de";
		if (path.StartsWith("/fr/", StringComparison.OrdinalIgnoreCase)) return "fr";
		if (path.StartsWith("/jp/", StringComparison.OrdinalIgnoreCase)) return "ja";
		if (path.StartsWith("/kr/", StringComparison.OrdinalIgnoreCase)) return "ko";
		if (path.StartsWith("/cn/", StringComparison.OrdinalIgnoreCase)) return "zh";
		if (path.StartsWith("/es/", StringComparison.OrdinalIgnoreCase)) return "es";
		if (path.StartsWith("/pt/", StringComparison.OrdinalIgnoreCase)) return "pt";

		return "en";
	}

	internal static string GetPageType(string url)
	{
		var uri = new Uri(url);
		var path = uri.AbsolutePath;

		// Labs content - technical articles and tutorials
		if (path.Contains("/search-labs/", StringComparison.OrdinalIgnoreCase)) return "search-labs";
		if (path.Contains("/security-labs/", StringComparison.OrdinalIgnoreCase)) return "security-labs";
		if (path.Contains("/observability-labs/", StringComparison.OrdinalIgnoreCase)) return "observability-labs";

		// General content types
		if (path.Contains("/blog/", StringComparison.OrdinalIgnoreCase)) return "blog";
		if (path.Contains("/what-is/", StringComparison.OrdinalIgnoreCase)) return "concept";
		if (path.Contains("/webinars/", StringComparison.OrdinalIgnoreCase)) return "webinar";
		if (path.Contains("/virtual-events/", StringComparison.OrdinalIgnoreCase)) return "event";
		if (path.Contains("/elasticon/", StringComparison.OrdinalIgnoreCase)) return "event";
		if (path.Contains("/events/", StringComparison.OrdinalIgnoreCase)) return "event";
		if (path.Contains("/training/", StringComparison.OrdinalIgnoreCase)) return "training";
		if (path.Contains("/resources/", StringComparison.OrdinalIgnoreCase)) return "resource";
		if (path.Contains("/customers/", StringComparison.OrdinalIgnoreCase)) return "customer-story";
		if (path.Contains("/downloads/", StringComparison.OrdinalIgnoreCase)) return "download";
		if (path.Contains("/demo-gallery/", StringComparison.OrdinalIgnoreCase)) return "demo";
		if (path.Contains("/industries/", StringComparison.OrdinalIgnoreCase)) return "industry";
		if (path.Contains("/partners/", StringComparison.OrdinalIgnoreCase)) return "partner";
		if (path.Contains("/about/", StringComparison.OrdinalIgnoreCase)) return "about";

		// Product pages
		if (path.Contains("/elasticsearch", StringComparison.OrdinalIgnoreCase) ||
			path.Contains("/kibana", StringComparison.OrdinalIgnoreCase) ||
			path.Contains("/observability", StringComparison.OrdinalIgnoreCase) ||
			path.Contains("/security", StringComparison.OrdinalIgnoreCase) ||
			path.Contains("/enterprise-search", StringComparison.OrdinalIgnoreCase))
			return "product";

		return "marketing";
	}
}
