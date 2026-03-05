# Phase 2: Crawling Infrastructure

## Objective
Implement sitemap parsing, version discovery, and adaptive HTTP crawling.

## Tasks

### 2.1 Sitemap Parser

**File**: `/src/tooling/crawl-indexer/Crawling/SitemapParser.cs`

```csharp
public interface ISitemapParser
{
    IAsyncEnumerable<SitemapEntry> ParseAsync(Uri sitemapUrl, CancellationToken ct);
}

public record SitemapEntry(
    Uri Url,
    DateTimeOffset? LastModified,
    string? ChangeFrequency,
    double? Priority
);

public class SitemapParser(HttpClient httpClient, ILogger<SitemapParser> logger) : ISitemapParser
{
    public async IAsyncEnumerable<SitemapEntry> ParseAsync(
        Uri sitemapUrl,
        [EnumeratorCancellation] CancellationToken ct)
    {
        // 1. Fetch sitemap with Accept-Encoding: gzip
        // 2. Parse XML
        // 3. If <sitemapindex>, recursively parse child sitemaps
        // 4. For each <url>, yield SitemapEntry
        // 5. Handle compressed sitemaps (.xml.gz)
    }
}
```

**Key Implementation Details**:
- Support gzip-compressed sitemaps
- Handle sitemap index files (recursive parsing)
- Parse `<loc>`, `<lastmod>`, `<changefreq>`, `<priority>`
- Use `XmlReader` for streaming large sitemaps

### 2.2 Version Discovery

**File**: `/src/tooling/crawl-indexer/Crawling/VersionDiscovery.cs`

```csharp
public record DiscoveredVersions(
    IReadOnlyList<string> V8Versions,  // All 8.x versions found
    string? LatestV7                    // Latest 7.x version (e.g., "7.17")
);

public class VersionDiscovery(ISitemapParser sitemapParser, ILogger<VersionDiscovery> logger)
{
    private static readonly Regex VersionRegex = new(@"/guide/en/[^/]+/[^/]+/(\d+\.\d+)/", RegexOptions.Compiled);

    public async Task<DiscoveredVersions> DiscoverAsync(Uri sitemapUrl, CancellationToken ct)
    {
        var versions = new HashSet<Version>();

        await foreach (var entry in sitemapParser.ParseAsync(sitemapUrl, ct))
        {
            if (!entry.Url.AbsolutePath.StartsWith("/guide/"))
                continue;

            var match = VersionRegex.Match(entry.Url.AbsolutePath);
            if (match.Success && Version.TryParse(match.Groups[1].Value, out var version))
                versions.Add(version);
        }

        var v8Versions = versions
            .Where(v => v.Major == 8)
            .OrderByDescending(v => v)
            .Select(v => v.ToString())
            .ToList();

        var latestV7 = versions
            .Where(v => v.Major == 7)
            .OrderByDescending(v => v)
            .FirstOrDefault()
            ?.ToString();

        return new DiscoveredVersions(v8Versions, latestV7);
    }
}
```

### 2.3 URL Filtering

**File**: `/src/tooling/crawl-indexer/Crawling/UrlFilter.cs`

```csharp
public class UrlFilter
{
    public bool ShouldCrawlGuide(Uri url, IReadOnlySet<string> allowedVersions)
    {
        if (!url.AbsolutePath.StartsWith("/guide/"))
            return false;

        // Extract version from URL
        var match = Regex.Match(url.AbsolutePath, @"/guide/en/[^/]+/[^/]+/(\d+\.\d+)/");
        if (!match.Success)
            return false;

        return allowedVersions.Contains(match.Groups[1].Value);
    }

    public bool ShouldCrawlSite(Uri url)
    {
        var path = url.AbsolutePath;

        // Exclusions
        if (path.StartsWith("/guide/"))
            return false;
        if (path.StartsWith("/downloads/past-releases/"))
            return false;

        return true;
    }

    public string? ExtractLanguage(Uri url)
    {
        // /de/*, /fr/*, /jp/*, /kr/*, /cn/*, /es/*, /pt/*
        var match = Regex.Match(url.AbsolutePath, @"^/(de|fr|jp|kr|cn|es|pt)/");
        return match.Success ? match.Groups[1].Value : "en";
    }
}
```

### 2.4 Adaptive Crawler

**File**: `/src/tooling/crawl-indexer/Crawling/AdaptiveCrawler.cs`

```csharp
public record CrawlResult(
    Uri Url,
    string? HtmlContent,
    HttpStatusCode StatusCode,
    TimeSpan ResponseTime,
    DateTimeOffset CrawledAt,
    Dictionary<string, string> ResponseHeaders
);

public record CrawlConfiguration
{
    public int InitialDelayMs { get; init; } = 100;
    public int MinDelayMs { get; init; } = 50;
    public int MaxDelayMs { get; init; } = 2000;
    public int SlowResponseThresholdMs { get; init; } = 1000;
    public int MaxConcurrency { get; init; } = 4;
    public TimeSpan RequestTimeout { get; init; } = TimeSpan.FromSeconds(30);
}

public class AdaptiveCrawler(HttpClient httpClient, ILogger<AdaptiveCrawler> logger)
{
    private readonly SlidingWindow _responseWindow = new(windowSize: 10);
    private int _currentDelayMs;

    public async IAsyncEnumerable<CrawlResult> CrawlAsync(
        IAsyncEnumerable<Uri> urls,
        CrawlConfiguration config,
        [EnumeratorCancellation] CancellationToken ct)
    {
        _currentDelayMs = config.InitialDelayMs;

        await foreach (var url in urls.WithCancellation(ct))
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                using var response = await httpClient.GetAsync(url, ct);
                stopwatch.Stop();

                var content = response.IsSuccessStatusCode
                    ? await response.Content.ReadAsStringAsync(ct)
                    : null;

                var headers = response.Headers
                    .Concat(response.Content.Headers)
                    .ToDictionary(h => h.Key, h => string.Join(", ", h.Value));

                yield return new CrawlResult(
                    url,
                    content,
                    response.StatusCode,
                    stopwatch.Elapsed,
                    DateTimeOffset.UtcNow,
                    headers
                );

                AdjustDelay(stopwatch.Elapsed, config);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to crawl {Url}", url);
                yield return new CrawlResult(
                    url,
                    null,
                    HttpStatusCode.ServiceUnavailable,
                    stopwatch.Elapsed,
                    DateTimeOffset.UtcNow,
                    []
                );
            }

            await Task.Delay(_currentDelayMs, ct);
        }
    }

    private void AdjustDelay(TimeSpan responseTime, CrawlConfiguration config)
    {
        _responseWindow.Add(responseTime.TotalMilliseconds);
        var avgMs = _responseWindow.Average();

        if (avgMs > config.SlowResponseThresholdMs)
        {
            // Slow down
            _currentDelayMs = Math.Min(_currentDelayMs * 2, config.MaxDelayMs);
            logger.LogInformation("Slowing down: delay now {Delay}ms (avg response: {Avg}ms)",
                _currentDelayMs, avgMs);
        }
        else if (avgMs < config.SlowResponseThresholdMs / 2 && _currentDelayMs > config.MinDelayMs)
        {
            // Speed up
            _currentDelayMs = Math.Max(_currentDelayMs / 2, config.MinDelayMs);
            logger.LogDebug("Speeding up: delay now {Delay}ms", _currentDelayMs);
        }
    }
}
```

### 2.5 Crawl Configuration

**File**: `/src/tooling/crawl-indexer/Crawling/CrawlOptions.cs`

```csharp
public record GuideCrawlOptions
{
    public string? Versions { get; init; }  // null = auto-discover
    public string? Products { get; init; }  // null = all
    public string? SitemapUrl { get; init; }
    public int MaxPages { get; init; }
    public bool DryRun { get; init; }
}

public record SiteCrawlOptions
{
    public string? Languages { get; init; }  // null = all
    public string? ExcludePaths { get; init; }
    public string? SitemapUrl { get; init; }
    public int MaxPages { get; init; }
    public bool DryRun { get; init; }
}
```

## Verification

1. Unit test: Parse sample sitemap XML
2. Unit test: Version discovery with mock sitemap
3. Unit test: URL filtering logic
4. Integration test: Crawl a few pages with rate limiting

## Files Created

| File | Description |
|------|-------------|
| `Crawling/ISitemapParser.cs` | Interface |
| `Crawling/SitemapParser.cs` | Implementation |
| `Crawling/SitemapEntry.cs` | Data record |
| `Crawling/VersionDiscovery.cs` | Auto-discover versions |
| `Crawling/UrlFilter.cs` | URL filtering logic |
| `Crawling/AdaptiveCrawler.cs` | HTTP crawler with rate limiting |
| `Crawling/CrawlOptions.cs` | CLI options records |
