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
/// Extracts content from legacy /guide HTML pages into DocumentationDocument.
/// </summary>
public class GuideHtmlExtractor(ILogger<GuideHtmlExtractor> logger) : IGuideHtmlExtractor
{
	private readonly HtmlParser _parser = new();

	public async Task<DocumentationDocument?> ExtractAsync(string url, string html, DateTimeOffset? sitemapLastModified, Cancel ctx = default)
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

		// Extract Elastic-specific metadata
		var productName = HtmlMetaExtractor.GetMetaContent(document, "product_name", "elastic");
		var productVersion = HtmlMetaExtractor.GetMetaContent(document, "product_version", "elastic");
		var dcType = HtmlMetaExtractor.GetMetaContent(document, "DC.type");
		var dcSubject = HtmlMetaExtractor.GetMetaContent(document, "DC.subject");
		var dcIdentifier = HtmlMetaExtractor.GetMetaContent(document, "DC.identifier");

		// Get title
		var title = HtmlMetaExtractor.GetTitle(document);
		if (string.IsNullOrWhiteSpace(title))
		{
			logger.LogWarning("No title found for {Url}", url);
			return null;
		}

		// Get description
		var description = HtmlMetaExtractor.GetDescription(document);

		// Get main content
		var contentDiv = document.QuerySelector("#content") ??
						 document.QuerySelector("article") ??
						 document.QuerySelector("main") ??
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
		// Priority: article:modified_time > sitemap lastmod > current time
		var lastUpdated = HtmlMetaExtractor.GetArticleModifiedTime(document)
						  ?? sitemapLastModified
						  ?? DateTimeOffset.UtcNow;

		// Calculate navigation depth from URL
		var uri = new Uri(url);
		var navigationDepth = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries).Length;

		// Extract parent chain from breadcrumbs
		var parents = ExtractBreadcrumbs(document, url);

		// Create product
		IndexedProduct? product = null;
		if (!string.IsNullOrWhiteSpace(productName))
		{
			product = new IndexedProduct
			{
				Id = NormalizeProductId(productName),
				Version = productVersion
			};
		}

		// Calculate content hash
		var hash = ComputeHash(title + textContent);

		// Build search title (title + URL components for better scoring)
		var searchTitle = BuildSearchTitle(title, url);

		// Determine type from DC.type or URL pattern
		var docType = DetermineDocType(dcType, url);

		// Extract navigation section from URL
		var navigationSection = ExtractNavigationSection(url);

		return new DocumentationDocument
		{
			Title = title,
			SearchTitle = searchTitle,
			Type = docType,
			Url = url,
			Hash = hash,
			NavigationDepth = navigationDepth,
			NavigationTableOfContents = 1, // Guide pages are in TOC
			NavigationSection = navigationSection,
			BatchIndexDate = DateTimeOffset.UtcNow,
			LastUpdated = lastUpdated,
			Description = description,
			Headings = headings,
			Links = [],
			Product = product,
			Body = textContent,
			StrippedBody = textContent, // Already stripped
			Abstract = abstractText,
			Parents = parents
		};
	}

	private static ParentDocument[] ExtractBreadcrumbs(IHtmlDocument document, string url)
	{
		var breadcrumbs = new List<ParentDocument>();

		// Try common breadcrumb selectors
		var breadcrumbNav = document.QuerySelector(".breadcrumbs, nav[aria-label='breadcrumb'], .breadcrumb, #breadcrumbs");
		if (breadcrumbNav is not null)
		{
			var links = breadcrumbNav.QuerySelectorAll("a");
			foreach (var link in links)
			{
				var href = link.GetAttribute("href");
				var text = link.TextContent.Trim();

				if (!string.IsNullOrWhiteSpace(href) && !string.IsNullOrWhiteSpace(text))
				{
					// Make URL absolute
					if (!href.StartsWith("http", StringComparison.OrdinalIgnoreCase))
					{
						var baseUri = new Uri(url);
						href = new Uri(baseUri, href).ToString();
					}

					breadcrumbs.Add(new ParentDocument { Title = text, Url = href });
				}
			}
		}

		return breadcrumbs.ToArray();
	}

	private static string NormalizeProductId(string productName) =>
		productName.ToLowerInvariant()
			.Replace(' ', '-')
			.Replace("_", "-");

	private static string ComputeHash(string content)
	{
		var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
		return Convert.ToHexString(bytes)[..16].ToLowerInvariant();
	}

	private static string BuildSearchTitle(string title, string url)
	{
		// Extract meaningful URL components
		var uri = new Uri(url);
		var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

		// Skip 'guide' and language, take product and section
		var relevantSegments = segments
			.Skip(2) // guide, en
			.Take(2) // product, section
			.Select(s => s.Replace('-', ' ').Replace('_', ' '))
			.Where(s => !string.IsNullOrWhiteSpace(s));

		var prefix = string.Join(" > ", relevantSegments);
		return string.IsNullOrWhiteSpace(prefix) ? title : $"{prefix}: {title}";
	}

	private static string DetermineDocType(string? dcType, string url)
	{
		if (!string.IsNullOrWhiteSpace(dcType))
		{
			if (dcType.Contains("Reference", StringComparison.OrdinalIgnoreCase))
				return "reference";
			if (dcType.Contains("Guide", StringComparison.OrdinalIgnoreCase))
				return "guide";
			if (dcType.Contains("Tutorial", StringComparison.OrdinalIgnoreCase))
				return "tutorial";
		}

		// Infer from URL
		if (url.Contains("/reference/", StringComparison.OrdinalIgnoreCase))
			return "reference";
		if (url.Contains("/guide/", StringComparison.OrdinalIgnoreCase))
			return "guide";

		return "doc";
	}

	private static string? ExtractNavigationSection(string url)
	{
		var uri = new Uri(url);
		var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

		// Pattern: guide/en/product/section/version/...
		// Return the section (index 3)
		return segments.Length > 3 ? segments[3] : null;
	}
}
