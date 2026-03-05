# Phase 3: HTML Processing

## Objective
Extract content and metadata from HTML pages, convert to markdown for AI enrichment.

## Tasks

### 3.1 HTML Content Extractor Interface

**File**: `/src/tooling/crawl-indexer/Html/IHtmlContentExtractor.cs`

```csharp
public interface IHtmlContentExtractor<TDocument>
    where TDocument : BaseSearchDocument
{
    bool CanExtract(Uri url);
    TDocument? Extract(Uri url, string htmlContent, DateTimeOffset? sitemapLastMod);
}
```

### 3.2 Guide HTML Extractor

**File**: `/src/tooling/crawl-indexer/Html/GuideHtmlExtractor.cs`

Extracts `DocumentationDocument` from legacy /guide pages.

```csharp
public class GuideHtmlExtractor(ILogger<GuideHtmlExtractor> logger)
    : IHtmlContentExtractor<DocumentationDocument>
{
    public bool CanExtract(Uri url) =>
        url.AbsolutePath.StartsWith("/guide/");

    public DocumentationDocument? Extract(Uri url, string htmlContent, DateTimeOffset? sitemapLastMod)
    {
        var document = new HtmlParser().ParseDocument(htmlContent);

        // 1. Extract meta tags
        var productName = GetMetaContent(document, "product_name");
        var productVersion = GetMetaContent(document, "product_version");
        var dcSubject = GetMetaContent(document, "DC.subject");

        if (productName == null || productVersion == null)
        {
            logger.LogWarning("Missing product metadata for {Url}", url);
            return null;
        }

        // 2. Extract title
        var title = document.QuerySelector("title")?.TextContent
            ?? document.QuerySelector("h1")?.TextContent
            ?? url.AbsolutePath;

        // 3. Extract main content from <div id="content">
        var contentDiv = document.QuerySelector("#content");
        if (contentDiv == null)
        {
            logger.LogWarning("No #content div found for {Url}", url);
            return null;
        }

        // Remove nav elements, edit links, warning banners
        foreach (var element in contentDiv.QuerySelectorAll(".navheader, .navfooter, .edit_me, .page_header, .version-warning"))
            element.Remove();

        var bodyHtml = contentDiv.InnerHtml;
        var strippedBody = contentDiv.TextContent.Trim();

        // 4. Extract headings
        var headings = contentDiv
            .QuerySelectorAll("h2, h3, h4, h5, h6")
            .Select(h => h.TextContent.Trim())
            .Where(h => !string.IsNullOrEmpty(h))
            .ToArray();

        // 5. Extract breadcrumbs for Parents
        var breadcrumbs = document.QuerySelectorAll(".breadcrumb-link")
            .Select(a => new ParentDocument
            {
                Title = a.TextContent.Trim(),
                Url = a.GetAttribute("href") ?? ""
            })
            .ToArray();

        // 6. Compute navigation depth from URL
        var navigationDepth = url.AbsolutePath.Count(c => c == '/') - 2;

        // 7. Extract social metadata
        var ogImage = GetMetaProperty(document, "og:image");
        var ogTitle = GetMetaProperty(document, "og:title");
        var ogDescription = GetMetaProperty(document, "og:description");

        // 8. Determine last updated (prefer article:modified_time)
        var articleModified = GetMetaProperty(document, "article:modified_time");
        var lastUpdated = ParseDate(articleModified) ?? sitemapLastMod ?? DateTimeOffset.UtcNow;

        return new DocumentationDocument
        {
            Title = title.Trim(),
            SearchTitle = $"{title} {string.Join(" ", url.AbsolutePath.Split('/'))}",
            Type = "doc",
            Url = url.ToString(),
            Product = new IndexedProduct
            {
                Id = productName.ToLowerInvariant(),
                Version = productVersion
            },
            Body = bodyHtml,
            StrippedBody = strippedBody,
            Headings = headings,
            Parents = breadcrumbs,
            NavigationDepth = navigationDepth,
            LastUpdated = lastUpdated,
            OgImage = ogImage,
            OgTitle = ogTitle,
            OgDescription = ogDescription
        };
    }

    private static string? GetMetaContent(IHtmlDocument doc, string name) =>
        doc.QuerySelector($"meta[name='{name}'], meta.elastic[name='{name}']")
            ?.GetAttribute("content");

    private static string? GetMetaProperty(IHtmlDocument doc, string property) =>
        doc.QuerySelector($"meta[property='{property}']")?.GetAttribute("content");

    private static DateTimeOffset? ParseDate(string? dateStr) =>
        DateTimeOffset.TryParse(dateStr, out var date) ? date : null;
}
```

### 3.3 Site HTML Extractor

**File**: `/src/tooling/crawl-indexer/Html/SiteHtmlExtractor.cs`

Extracts `SiteDocument` from marketing/blog pages.

```csharp
public class SiteHtmlExtractor(UrlFilter urlFilter, ILogger<SiteHtmlExtractor> logger)
    : IHtmlContentExtractor<SiteDocument>
{
    public bool CanExtract(Uri url) =>
        !url.AbsolutePath.StartsWith("/guide/");

    public SiteDocument? Extract(Uri url, string htmlContent, DateTimeOffset? sitemapLastMod)
    {
        var document = new HtmlParser().ParseDocument(htmlContent);

        // 1. Extract meta tags
        var title = GetMetaProperty(document, "og:title")
            ?? document.QuerySelector("title")?.TextContent
            ?? url.AbsolutePath;

        var description = GetMetaContent(document, "description")
            ?? GetMetaProperty(document, "og:description");

        // 2. Determine page type from URL
        var pageType = DeterminePageType(url);

        // 3. Extract language
        var language = urlFilter.ExtractLanguage(url);
        var htmlLang = document.QuerySelector("html")?.GetAttribute("lang");
        if (!string.IsNullOrEmpty(htmlLang))
            language = htmlLang.Split('-')[0];

        // 4. Extract main content
        var mainContent = FindMainContent(document);
        var strippedBody = mainContent?.TextContent.Trim() ?? "";

        // 5. Extract headings
        var headings = (mainContent ?? document.Body)?
            .QuerySelectorAll("h1, h2, h3, h4, h5, h6")
            .Select(h => h.TextContent.Trim())
            .Where(h => !string.IsNullOrEmpty(h))
            .ToArray() ?? [];

        // 6. Social metadata
        var ogImage = GetMetaProperty(document, "og:image");
        var twitterImage = GetMetaContent(document, "twitter:image");

        // 7. Article metadata
        var author = GetMetaProperty(document, "article:author");
        var publishedTime = ParseDate(GetMetaProperty(document, "article:published_time"));
        var modifiedTime = ParseDate(GetMetaProperty(document, "article:modified_time"));

        // 8. Determine last updated (article:modified_time > sitemap > now)
        var lastUpdated = modifiedTime ?? sitemapLastMod ?? DateTimeOffset.UtcNow;

        // 9. Determine relevance based on URL category
        var relevance = DetermineRelevance(url);

        return new SiteDocument
        {
            Title = title.Trim(),
            SearchTitle = $"{title} {string.Join(" ", url.AbsolutePath.Split('/'))}",
            Type = "site",
            Url = url.ToString(),
            Description = description,
            Body = mainContent?.InnerHtml,
            StrippedBody = strippedBody,
            Headings = headings,
            PageType = pageType,
            Language = language,
            Author = author,
            PublishedDate = publishedTime,
            ModifiedDate = modifiedTime,
            LastUpdated = lastUpdated,
            OgImage = ogImage ?? twitterImage,
            Relevance = relevance
        };
    }

    private static IElement? FindMainContent(IHtmlDocument doc) =>
        doc.QuerySelector("main") ??
        doc.QuerySelector("article") ??
        doc.QuerySelector("[role='main']") ??
        doc.QuerySelector(".content-wrapper");

    private static string DeterminePageType(Uri url)
    {
        var path = url.AbsolutePath;
        return path switch
        {
            _ when path.StartsWith("/blog/") => "blog",
            _ when path.StartsWith("/what-is/") => "what-is",
            _ when path.StartsWith("/training/") => "training",
            _ when path.StartsWith("/webinars/") => "webinar",
            _ when path.StartsWith("/elasticon/") => "event",
            _ when path.StartsWith("/customers/") => "customer-story",
            _ when path.StartsWith("/downloads/") => "download",
            _ when path.StartsWith("/about/") => "about",
            _ => "marketing"
        };
    }

    private static string DetermineRelevance(Uri url)
    {
        var path = url.AbsolutePath;
        return path switch
        {
            _ when path.StartsWith("/blog/") => "high",
            _ when path.StartsWith("/what-is/") => "high",
            _ when path.StartsWith("/elasticsearch") => "high",
            _ when path.StartsWith("/kibana") => "high",
            _ when path.StartsWith("/observability") => "high",
            _ when path.StartsWith("/security") => "high",
            _ when path.StartsWith("/explore") => "high",
            _ when path.StartsWith("/downloads/") => "medium",
            _ when path.StartsWith("/training/") => "medium",
            _ when path.StartsWith("/webinars/") => "medium",
            _ when path.StartsWith("/campaigns/") => "low",
            _ when path.StartsWith("/customers/") => "low",
            _ when path.StartsWith("/partners/") => "low",
            _ when path.StartsWith("/about/") => "low",
            _ when path.StartsWith("/agreements/") => "low",
            _ => "medium"
        };
    }
}
```

### 3.4 HTML to Markdown Converter

**File**: `/src/tooling/crawl-indexer/Html/HtmlToMarkdownConverter.cs`

Converts HTML to CommonMark for AI enrichment.

```csharp
public class HtmlToMarkdownConverter
{
    public string Convert(string htmlContent)
    {
        var document = new HtmlParser().ParseDocument(htmlContent);
        var sb = new StringBuilder();

        // Remove script, style, nav, footer elements
        foreach (var element in document.QuerySelectorAll("script, style, nav, footer, header, .navigation"))
            element.Remove();

        ConvertNode(document.Body, sb);
        return sb.ToString().Trim();
    }

    private void ConvertNode(INode? node, StringBuilder sb)
    {
        if (node == null) return;

        switch (node)
        {
            case IText text:
                sb.Append(text.TextContent);
                break;

            case IElement element:
                ConvertElement(element, sb);
                break;

            default:
                foreach (var child in node.ChildNodes)
                    ConvertNode(child, sb);
                break;
        }
    }

    private void ConvertElement(IElement element, StringBuilder sb)
    {
        switch (element.TagName.ToLowerInvariant())
        {
            case "h1":
                sb.AppendLine().Append("# ");
                ConvertChildren(element, sb);
                sb.AppendLine().AppendLine();
                break;

            case "h2":
                sb.AppendLine().Append("## ");
                ConvertChildren(element, sb);
                sb.AppendLine().AppendLine();
                break;

            case "h3":
                sb.AppendLine().Append("### ");
                ConvertChildren(element, sb);
                sb.AppendLine().AppendLine();
                break;

            case "h4":
            case "h5":
            case "h6":
                sb.AppendLine().Append("#### ");
                ConvertChildren(element, sb);
                sb.AppendLine().AppendLine();
                break;

            case "p":
                ConvertChildren(element, sb);
                sb.AppendLine().AppendLine();
                break;

            case "ul":
            case "ol":
                sb.AppendLine();
                ConvertList(element, sb, element.TagName == "OL");
                sb.AppendLine();
                break;

            case "li":
                sb.Append("- ");
                ConvertChildren(element, sb);
                sb.AppendLine();
                break;

            case "a":
                sb.Append('[');
                ConvertChildren(element, sb);
                sb.Append("](").Append(element.GetAttribute("href") ?? "").Append(')');
                break;

            case "strong":
            case "b":
                sb.Append("**");
                ConvertChildren(element, sb);
                sb.Append("**");
                break;

            case "em":
            case "i":
                sb.Append('*');
                ConvertChildren(element, sb);
                sb.Append('*');
                break;

            case "code":
                sb.Append('`');
                ConvertChildren(element, sb);
                sb.Append('`');
                break;

            case "pre":
                sb.AppendLine("```");
                ConvertChildren(element, sb);
                sb.AppendLine().AppendLine("```").AppendLine();
                break;

            case "br":
                sb.AppendLine();
                break;

            default:
                ConvertChildren(element, sb);
                break;
        }
    }

    private void ConvertChildren(IElement element, StringBuilder sb)
    {
        foreach (var child in element.ChildNodes)
            ConvertNode(child, sb);
    }

    private void ConvertList(IElement element, StringBuilder sb, bool ordered)
    {
        var index = 1;
        foreach (var item in element.Children.Where(c => c.TagName == "LI"))
        {
            sb.Append(ordered ? $"{index++}. " : "- ");
            ConvertChildren(item, sb);
            sb.AppendLine();
        }
    }
}
```

## Verification

1. Unit test: Extract metadata from sample /guide HTML
2. Unit test: Extract metadata from sample blog HTML
3. Unit test: HTML to markdown conversion
4. Visual inspection of extracted content

## Files Created

| File | Description |
|------|-------------|
| `Html/IHtmlContentExtractor.cs` | Interface |
| `Html/GuideHtmlExtractor.cs` | /guide page extractor |
| `Html/SiteHtmlExtractor.cs` | Site page extractor |
| `Html/HtmlToMarkdownConverter.cs` | HTML to markdown |
