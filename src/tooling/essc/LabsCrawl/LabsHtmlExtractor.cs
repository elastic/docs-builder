// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Security.Cryptography;
using System.Text;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Elastic.Documentation.Search.Contract;
using Elastic.SiteSearch.Cli.Elasticsearch;
using Microsoft.Extensions.Logging;

namespace Elastic.SiteSearch.Cli.LabsCrawl;

/// <summary>
/// Extracts content from site HTML pages into LabsDocument.
/// </summary>
public class LabsHtmlExtractor(ILogger<LabsHtmlExtractor> logger) : IDocumentExtractor<LabsDocument>
{
	private readonly HtmlParser _parser = new();

	public Task<LabsDocument?> ExtractAsync(CrawlResult result, CancellationToken ct) =>
		ExtractCoreAsync(
			result.Url,
			result.Content!,
			result.LastModified,
			GetLanguageFromUrl(result.Url),
			GetNavigationSection(result.Url),
			result.HttpEtag,
			result.HttpLastModified,
			ct
		);

	public Task<LabsDocument?> ExtractAsync(
		string url,
		string html,
		DateTimeOffset? sitemapLastModified,
		string language,
		string navigationSection,
		CancellationToken ct = default
	) => ExtractCoreAsync(url, html, sitemapLastModified, language, navigationSection, null, null, ct);

	private async Task<LabsDocument?> ExtractCoreAsync(
		string url,
		string html,
		DateTimeOffset? sitemapLastModified,
		string language,
		string navigationSection,
		string? httpEtag,
		DateTimeOffset? httpLastModified,
		CancellationToken ct = default
	)
	{
		IHtmlDocument document;
		try
		{
			document = await _parser.ParseDocumentAsync(html, ct);
		}
		catch (Exception ex)
		{
			logger.LogWarning(ex, "Failed to parse HTML for {Url}", url);
			return null;
		}

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

		// Get title (prefers the article's own <h1> within contentDiv - see GetTitle)
		var title = HtmlMetaExtractor.GetTitle(document, contentDiv);
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

		string textContent;
		string[] headings;
		string abstractText;

		var listingKind = GetListingKind(url);
		if (listingKind != ListingKind.None)
		{
			// Tag/author listing pages: don't index the article-card grid as body content - it's
			// noisy and duplicates the linked articles' own indexed content. Use a static,
			// content-independent title/description instead, so relevance doesn't churn every
			// time the listing is updated with a new post.
			publishedDate ??= HtmlMetaExtractor.ExtractMaxListingDate(contentDiv);

			var sectionLabel = GetLabsSectionLabel(url) ?? "Elastic Labs";
			var slug = GetLastPathSegment(url);
			var slugLabel = HtmlMetaExtractor.TitleCaseSlug(slug);

			(title, description) = listingKind == ListingKind.Tag
				? ($"Articles tagged with '{slugLabel}'",
				   $"Recent {sectionLabel} articles tagged {slug}. A curated listing of {sectionLabel} blog posts, tutorials, and articles about {slug}.")
				: ($"Articles written by {slugLabel}",
				   $"Articles written by {slugLabel} for {sectionLabel}. A listing of {sectionLabel} blog posts, tutorials, and articles authored by {slugLabel}.");

			textContent = string.Empty;
			headings = [];
			abstractText = description;
		}
		else
		{
			StripBoilerplate(contentDiv);

			textContent = HtmlMetaExtractor.ExtractTextContent(contentDiv);
			headings = HtmlMetaExtractor.ExtractHeadings(contentDiv);
			abstractText = HtmlMetaExtractor.CreateAbstract(textContent, description);
		}

		// Determine last updated date
		// Priority: article:modified_time > sitemap lastmod > article:published_time > current time
		var lastUpdated = modifiedDate
						  ?? sitemapLastModified
						  ?? publishedDate
						  ?? DateTimeOffset.UtcNow;

		// Calculate content hash
		var hash = ComputeHash(title + textContent);

		// Build search title
		var searchTitle = BuildSearchTitle(title, navigationSection, url);

		var indexUrl = Uri.TryCreate(url, UriKind.Absolute, out var parsed) ? parsed.AbsolutePath : url;

		return new LabsDocument
		{
			Title = title,
			SearchTitle = searchTitle,
			Url = indexUrl,
			Hash = hash,
			BatchIndexDate = DateTimeOffset.UtcNow,
			LastUpdated = lastUpdated,
			Description = description,
			Headings = headings,
			Body = textContent,
			StrippedBody = textContent,
			Abstract = abstractText,
			NavigationSection = navigationSection,
			ContentTier = ContentTierClassifier.FromNavigationSection(navigationSection),
			// navigation_depth/navigation_table_of_contents are rank features with positive_score_impact:false,
			// designed for hierarchical docs: lower values score higher.
			NavigationDepth = ComputeNavigationDepth(url) + 1,
			NavigationTableOfContents = ComputeNavigationDepth(url) <= 1 ? 10 : 100,
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

	private static readonly string[] BoilerplateSelectors =
	[
		"[class*='blog_ctaDivider']",   // "New to Elasticsearch? Join our ..." CTA banner
		"[class*='PageActions']",       // Copy / Share buttons
		"[class*='Rating_']",           // "How helpful was this content?" feedback widget
		"[class*='containerDivider']",  // "Related Content" cards
		"[class*='PostPreview_']",      // individual related-post previews
		"[class*='ready-to-build']",    // bottom marketing CTA
	];

	/// <summary>
	/// Removes repeating chrome that appears on every Labs page but adds no
	/// search value (CTAs, feedback widgets, related-post cards, share buttons).
	/// Operates on the DOM in-place before text extraction.
	/// </summary>
	private static void StripBoilerplate(AngleSharp.Dom.IElement root)
	{
		foreach (var selector in BoilerplateSelectors)
		{
			foreach (var el in root.QuerySelectorAll(selector).ToList())
				_ = el.ParentElement?.RemoveChild(el);
		}

		// The CTA banner is the first child div of <main> and contains the ctaDivider.
		// When the ctaDivider was removed above the wrapping div may still have the
		// "New to Elasticsearch?..." paragraph. Walk up from any remaining "getting
		// started" link text and remove its nearest block-level ancestor within the root.
		foreach (var a in root.QuerySelectorAll("a[href*='getting-started-elasticsearch']").ToList())
		{
			var block = a.Closest("div");
			if (block is not null && block != root)
				_ = block.ParentElement?.RemoveChild(block);
		}
	}

	private static int ComputeNavigationDepth(string url)
	{
		var path = url.StartsWith("http", StringComparison.OrdinalIgnoreCase)
			? new Uri(url).AbsolutePath
			: url;
		return path.Split('/', StringSplitOptions.RemoveEmptyEntries).Length;
	}

	private static readonly Dictionary<string, string> LabsSectionLabels = new(StringComparer.OrdinalIgnoreCase)
	{
		["search-labs"] = "Search Labs",
		["security-labs"] = "Security Labs",
		["observability-labs"] = "Observability Labs",
	};

	private enum ListingKind
	{
		None,
		Tag,
		Author
	}

	private static ListingKind GetListingKind(string url)
	{
		var path = new Uri(url).AbsolutePath;
		if (path.Contains("/tag/", StringComparison.OrdinalIgnoreCase))
			return ListingKind.Tag;
		if (path.Contains("/author/", StringComparison.OrdinalIgnoreCase))
			return ListingKind.Author;
		return ListingKind.None;
	}

	private static string? GetLabsSectionLabel(string url)
	{
		var segments = new Uri(url).AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
		return segments.Length > 0 && LabsSectionLabels.TryGetValue(segments[0], out var label) ? label : null;
	}

	private static string GetLastPathSegment(string url)
	{
		var segments = new Uri(url).AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
		return segments.Length > 0 ? segments[^1] : string.Empty;
	}

	/// <summary>
	/// Appends navigation context as a "{Title} - {Section}" suffix rather than the previous
	/// "{Section}: {Title}" prefix, so the clean article title stays intact for display while
	/// search_title still carries the routing context for matching (e.g. "labs", "blog").
	/// </summary>
	private static string BuildSearchTitle(string title, string navigationSection, string url)
	{
		var suffix = GetSearchTitleSuffix(navigationSection, url);
		if (suffix is null || title.Contains(suffix, StringComparison.OrdinalIgnoreCase))
			return title;

		return $"{title} - {suffix}";
	}

	private static string? GetSearchTitleSuffix(string navigationSection, string url)
	{
		var segments = new Uri(url).AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

		if (segments.Length > 0 && LabsSectionLabels.TryGetValue(segments[0], out var labsLabel))
		{
			return segments.Length > 1 && string.Equals(segments[1], "blog", StringComparison.OrdinalIgnoreCase)
				? $"{labsLabel} Blog"
				: labsLabel;
		}

		// Non-Labs content types (shared with other site sections using this extractor)
		var prefix = navigationSection switch
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
			return prefix;

		// Extract category from URL for marketing pages
		if (segments.Length > 0)
		{
			var category = segments[0].Replace('-', ' ');
			return char.ToUpperInvariant(category[0]) + category[1..];
		}

		return null;
	}

	internal static string GetLanguageFromUrl(string url)
	{
		var uri = new Uri(url);
		var path = uri.AbsolutePath;

		if (path.StartsWith("/de/", StringComparison.OrdinalIgnoreCase))
			return "de";
		if (path.StartsWith("/fr/", StringComparison.OrdinalIgnoreCase))
			return "fr";
		if (path.StartsWith("/jp/", StringComparison.OrdinalIgnoreCase))
			return "ja";
		if (path.StartsWith("/kr/", StringComparison.OrdinalIgnoreCase))
			return "ko";
		if (path.StartsWith("/cn/", StringComparison.OrdinalIgnoreCase))
			return "zh";
		if (path.StartsWith("/es/", StringComparison.OrdinalIgnoreCase))
			return "es";
		if (path.StartsWith("/pt/", StringComparison.OrdinalIgnoreCase))
			return "pt";

		return "en";
	}

	internal static string GetNavigationSection(string url)
	{
		var uri = new Uri(url);
		var path = uri.AbsolutePath;

		// Labs content - technical articles and tutorials
		if (path.Contains("/search-labs/", StringComparison.OrdinalIgnoreCase))
			return "search-labs";
		if (path.Contains("/security-labs/", StringComparison.OrdinalIgnoreCase))
			return "security-labs";
		if (path.Contains("/observability-labs/", StringComparison.OrdinalIgnoreCase))
			return "observability-labs";

		// General content types
		if (path.Contains("/blog/", StringComparison.OrdinalIgnoreCase))
			return "blog";
		if (path.Contains("/what-is/", StringComparison.OrdinalIgnoreCase))
			return "concept";
		if (path.Contains("/webinars/", StringComparison.OrdinalIgnoreCase))
			return "webinar";
		if (path.Contains("/virtual-events/", StringComparison.OrdinalIgnoreCase))
			return "event";
		if (path.Contains("/elasticon/", StringComparison.OrdinalIgnoreCase))
			return "event";
		if (path.Contains("/events/", StringComparison.OrdinalIgnoreCase))
			return "event";
		if (path.Contains("/training/", StringComparison.OrdinalIgnoreCase))
			return "training";
		if (path.Contains("/resources/", StringComparison.OrdinalIgnoreCase))
			return "resource";
		if (path.Contains("/customers/", StringComparison.OrdinalIgnoreCase))
			return "customer-story";
		if (path.Contains("/downloads/", StringComparison.OrdinalIgnoreCase))
			return "download";
		if (path.Contains("/demo-gallery/", StringComparison.OrdinalIgnoreCase))
			return "demo";
		if (path.Contains("/industries/", StringComparison.OrdinalIgnoreCase))
			return "industry";
		if (path.Contains("/partners/", StringComparison.OrdinalIgnoreCase))
			return "partner";
		if (path.Contains("/about/", StringComparison.OrdinalIgnoreCase))
			return "about";

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
