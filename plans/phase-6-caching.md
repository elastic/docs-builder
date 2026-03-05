# Phase 6: Caching Infrastructure

## Overview

Implement an Elasticsearch-based caching layer to avoid redundant:
1. **HTTP requests** - Skip crawling unchanged pages
2. **AI enrichment** - Skip re-enriching unchanged content
3. **Indexing** - Only update `batch_index_date` for unchanged docs

## Design Goals

- **First run**: Full crawl always (no cache exists)
- **Subsequent runs**: ~90% reduction in HTTP requests and AI calls
- **Scale**: Support 500K+ documents using PIT API
- **Correctness**: Never serve stale content

---

## New Files to Create

```
src/tooling/crawl-indexer/Caching/
├── ICrawlCache.cs               # Interface for cache operations
├── ElasticsearchCrawlCache.cs   # PIT-based cache loading from ES
├── CachedDocInfo.cs             # Record for cached document metadata
├── CrawlDecision.cs             # Decision type: New/Unchanged/PossiblyChanged
└── CrawlDecisionMaker.cs        # Compare sitemap vs cache, yield decisions
```

---

## New Fields to Add

### SiteDocument.cs and DocumentationDocument.cs

```csharp
/// <summary>
/// ETag header from last crawl - used for conditional HTTP requests.
/// </summary>
[JsonPropertyName("http_etag")]
[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
public string? HttpEtag { get; set; }

/// <summary>
/// Last-Modified header from last crawl - used for conditional HTTP requests.
/// </summary>
[JsonPropertyName("http_last_modified")]
[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
public DateTimeOffset? HttpLastModified { get; set; }
```

### SiteIndexMapping.cs and GuideIndexMapping.cs

```json
"http_etag": { "type": "keyword", "index": false },
"http_last_modified": { "type": "date" }
```

---

## Implementation Details

### 1. CachedDocInfo Record

```csharp
// Caching/CachedDocInfo.cs

namespace CrawlIndexer.Caching;

/// <summary>
/// Lightweight record for cached document metadata loaded from Elasticsearch.
/// </summary>
public record CachedDocInfo(
    string Url,
    string Hash,
    DateTimeOffset LastUpdated,
    string? HttpEtag,
    DateTimeOffset? HttpLastModified,
    string? EnrichmentKey,
    string? EnrichmentPromptHash
);
```

### 2. CrawlDecision Types

```csharp
// Caching/CrawlDecision.cs

namespace CrawlIndexer.Caching;

public enum CrawlReason
{
    /// <summary>URL not in cache - must crawl.</summary>
    New,

    /// <summary>Sitemap lastmod indicates unchanged - skip crawl.</summary>
    Unchanged,

    /// <summary>Sitemap changed or no lastmod - verify via HTTP conditional request.</summary>
    PossiblyChanged
}

public record CrawlDecision(
    SitemapEntry Entry,
    CrawlReason Reason,
    CachedDocInfo? Cached = null
);
```

### 3. ICrawlCache Interface

```csharp
// Caching/ICrawlCache.cs

namespace CrawlIndexer.Caching;

/// <summary>
/// Interface for loading cached document metadata from Elasticsearch.
/// </summary>
public interface ICrawlCache
{
    /// <summary>
    /// Loads all cached documents from the index using PIT API for consistency.
    /// </summary>
    Task<Dictionary<string, CachedDocInfo>> LoadCacheAsync(
        string indexAlias,
        IProgress<(int loaded, string? currentUrl)>? progress = null,
        CancellationToken ct = default
    );

    /// <summary>
    /// Checks if the index exists (first run detection).
    /// </summary>
    Task<bool> IndexExistsAsync(string indexAlias, CancellationToken ct = default);
}
```

### 4. ElasticsearchCrawlCache Implementation

```csharp
// Caching/ElasticsearchCrawlCache.cs

namespace CrawlIndexer.Caching;

public class ElasticsearchCrawlCache(
    ILogger<ElasticsearchCrawlCache> logger,
    HttpClient httpClient,
    ElasticsearchEndpoint endpoint
) : ICrawlCache
{
    private const int BatchSize = 10000;
    private const string PitKeepAlive = "5m";

    public async Task<bool> IndexExistsAsync(string indexAlias, CancellationToken ct = default)
    {
        // HEAD request to check index existence
        var response = await httpClient.SendAsync(
            new HttpRequestMessage(HttpMethod.Head, $"{endpoint.Url}/{indexAlias}"),
            ct
        );
        return response.IsSuccessStatusCode;
    }

    public async Task<Dictionary<string, CachedDocInfo>> LoadCacheAsync(
        string indexAlias,
        IProgress<(int loaded, string? currentUrl)>? progress = null,
        CancellationToken ct = default
    )
    {
        var cache = new Dictionary<string, CachedDocInfo>(StringComparer.OrdinalIgnoreCase);

        // Open Point-in-Time for consistent reads
        var pitId = await OpenPitAsync(indexAlias, ct);
        if (pitId is null)
        {
            logger.LogWarning("Failed to open PIT - index may not exist");
            return cache;
        }

        try
        {
            object[]? searchAfter = null;
            var loaded = 0;

            while (true)
            {
                var (docs, nextSearchAfter) = await SearchAfterAsync(pitId, searchAfter, ct);

                if (docs.Count == 0)
                    break;

                foreach (var doc in docs)
                {
                    cache[doc.Url] = doc;
                    loaded++;
                }

                progress?.Report((loaded, docs.LastOrDefault()?.Url));
                searchAfter = nextSearchAfter;

                if (docs.Count < BatchSize)
                    break;
            }

            logger.LogInformation("Loaded {Count} documents from cache", cache.Count);
        }
        finally
        {
            await ClosePitAsync(pitId, ct);
        }

        return cache;
    }

    private async Task<string?> OpenPitAsync(string indexAlias, CancellationToken ct)
    {
        // POST /{index}/_pit?keep_alive=5m
        var response = await httpClient.PostAsync(
            $"{endpoint.Url}/{indexAlias}/_pit?keep_alive={PitKeepAlive}",
            null,
            ct
        );

        if (!response.IsSuccessStatusCode)
            return null;

        var json = await response.Content.ReadAsStringAsync(ct);
        // Parse PIT ID from response
        // {"id":"..."}
        return ExtractPitId(json);
    }

    private async Task ClosePitAsync(string pitId, CancellationToken ct)
    {
        // DELETE /_pit
        var request = new HttpRequestMessage(HttpMethod.Delete, $"{endpoint.Url}/_pit")
        {
            Content = new StringContent($"{{\"id\":\"{pitId}\"}}", Encoding.UTF8, "application/json")
        };
        await httpClient.SendAsync(request, ct);
    }

    private async Task<(List<CachedDocInfo> docs, object[]? searchAfter)> SearchAfterAsync(
        string pitId,
        object[]? searchAfter,
        CancellationToken ct
    )
    {
        // POST /_search with PIT and search_after
        var query = BuildSearchAfterQuery(pitId, searchAfter);
        var request = new HttpRequestMessage(HttpMethod.Post, $"{endpoint.Url}/_search")
        {
            Content = new StringContent(query, Encoding.UTF8, "application/json")
        };

        var response = await httpClient.SendAsync(request, ct);
        var json = await response.Content.ReadAsStringAsync(ct);

        return ParseSearchResponse(json);
    }

    private static string BuildSearchAfterQuery(string pitId, object[]? searchAfter)
    {
        var searchAfterJson = searchAfter is not null
            ? $",\"search_after\": {JsonSerializer.Serialize(searchAfter)}"
            : "";

        return $$"""
        {
            "size": {{BatchSize}},
            "pit": { "id": "{{pitId}}", "keep_alive": "{{PitKeepAlive}}" },
            "sort": [{ "url": "asc" }],
            "_source": ["url", "hash", "last_updated", "http_etag", "http_last_modified",
                        "enrichment_key", "enrichment_prompt_hash"]
            {{searchAfterJson}}
        }
        """;
    }

    // ... parsing helpers
}
```

### 5. CrawlDecisionMaker

```csharp
// Caching/CrawlDecisionMaker.cs

namespace CrawlIndexer.Caching;

/// <summary>
/// Compares sitemap entries against cached documents to determine crawl actions.
/// </summary>
public class CrawlDecisionMaker(ILogger<CrawlDecisionMaker> logger)
{
    public IEnumerable<CrawlDecision> MakeDecisions(
        IEnumerable<SitemapEntry> sitemapUrls,
        IReadOnlyDictionary<string, CachedDocInfo> cache
    )
    {
        var urlsInSitemap = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in sitemapUrls)
        {
            urlsInSitemap.Add(entry.Location);

            if (!cache.TryGetValue(entry.Location, out var cached))
            {
                // New URL - must crawl
                yield return new CrawlDecision(entry, CrawlReason.New);
                continue;
            }

            // Compare sitemap lastmod with cached last_updated
            if (entry.LastModified.HasValue && cached.LastUpdated >= entry.LastModified.Value)
            {
                // Sitemap says unchanged - skip crawl
                logger.LogDebug("Unchanged (sitemap): {Url}", entry.Location);
                yield return new CrawlDecision(entry, CrawlReason.Unchanged, cached);
                continue;
            }

            // Sitemap says changed or no lastmod - verify via HTTP
            logger.LogDebug("Possibly changed: {Url}", entry.Location);
            yield return new CrawlDecision(entry, CrawlReason.PossiblyChanged, cached);
        }
    }

    public IEnumerable<string> FindStaleUrls(
        IReadOnlyDictionary<string, CachedDocInfo> cache,
        IReadOnlySet<string> sitemapUrls
    )
    {
        foreach (var url in cache.Keys)
        {
            if (!sitemapUrls.Contains(url))
            {
                logger.LogDebug("Stale URL (not in sitemap): {Url}", url);
                yield return url;
            }
        }
    }
}
```

### 6. Modified CrawlResult

```csharp
// Crawling/CrawlResult.cs (updated)

public record CrawlResult
{
    public required string Url { get; init; }
    public required bool Success { get; init; }
    public string? Content { get; init; }
    public DateTimeOffset? LastModified { get; init; }
    public string? Error { get; init; }
    public int? StatusCode { get; init; }

    // New caching fields
    public string? HttpEtag { get; init; }
    public DateTimeOffset? HttpLastModified { get; init; }
    public bool NotModified { get; init; }
    public string? CachedHash { get; init; }

    public static CrawlResult Succeeded(
        string url,
        string content,
        DateTimeOffset? lastModified,
        string? etag = null,
        DateTimeOffset? httpLastModified = null
    ) => new()
    {
        Url = url,
        Success = true,
        Content = content,
        LastModified = lastModified,
        HttpEtag = etag,
        HttpLastModified = httpLastModified
    };

    public static CrawlResult NotModifiedResult(string url, string cachedHash) =>
        new()
        {
            Url = url,
            Success = true,
            NotModified = true,
            CachedHash = cachedHash
        };

    public static CrawlResult Failed(string url, string error, int? statusCode = null) =>
        new() { Url = url, Success = false, Error = error, StatusCode = statusCode };
}
```

### 7. Modified AdaptiveCrawler

```csharp
// Crawling/AdaptiveCrawler.cs (updated CrawlUrlAsync method)

private async Task<CrawlResult> CrawlUrlAsync(
    SitemapEntry entry,
    CachedDocInfo? cached,
    Cancel ctx
)
{
    var stopwatch = Stopwatch.StartNew();

    try
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, entry.Location);
        request.Headers.Add("User-Agent", "Elastic-Crawl-Indexer/1.0 (+https://elastic.co)");
        request.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");

        // Add conditional headers if we have cached values
        if (cached?.HttpEtag is not null)
        {
            try
            {
                request.Headers.IfNoneMatch.Add(new EntityTagHeaderValue($"\"{cached.HttpEtag}\""));
            }
            catch (FormatException)
            {
                // Invalid ETag format - skip conditional request
            }
        }

        if (cached?.HttpLastModified is not null)
            request.Headers.IfModifiedSince = cached.HttpLastModified;

        using var response = await httpClient.SendAsync(request, ctx);
        stopwatch.Stop();

        // Handle 304 Not Modified
        if (response.StatusCode == HttpStatusCode.NotModified)
        {
            logger.LogDebug("Not modified (304): {Url}", entry.Location);
            return CrawlResult.NotModifiedResult(entry.Location, cached!.Hash);
        }

        logger.LogDebug("Crawled {Url} in {ElapsedMs}ms (status: {StatusCode})",
            entry.Location, stopwatch.ElapsedMilliseconds, (int)response.StatusCode);

        if (!response.IsSuccessStatusCode)
            return CrawlResult.Failed(entry.Location, $"HTTP {(int)response.StatusCode}", (int)response.StatusCode);

        var content = await response.Content.ReadAsStringAsync(ctx);

        // Extract caching headers
        var etag = response.Headers.ETag?.Tag?.Trim('"');
        var httpLastModified = response.Content.Headers.LastModified;

        // Prefer article:modified_time from HTML, fall back to headers, then sitemap
        var lastModified = entry.LastModified;
        if (httpLastModified.HasValue)
            lastModified = httpLastModified.Value;

        return CrawlResult.Succeeded(entry.Location, content, lastModified, etag, httpLastModified);
    }
    catch (HttpRequestException ex)
    {
        stopwatch.Stop();
        logger.LogWarning("Failed to crawl {Url}: {Message}", entry.Location, ex.Message);
        return CrawlResult.Failed(entry.Location, ex.Message);
    }
    catch (TaskCanceledException) when (ctx.IsCancellationRequested)
    {
        throw;
    }
    catch (Exception ex)
    {
        stopwatch.Stop();
        logger.LogWarning(ex, "Unexpected error crawling {Url}", entry.Location);
        return CrawlResult.Failed(entry.Location, ex.Message);
    }
}
```

---

## Integration with Commands

### SiteCommand.cs (updated flow)

```csharp
// Pseudo-code for the updated crawl flow

public async Task RunAsync(...)
{
    // 1. Parse sitemaps
    var sitemapUrls = await ParseSitemapsAsync();

    // 2. Load cache from Elasticsearch
    var cache = new Dictionary<string, CachedDocInfo>();
    if (await crawlCache.IndexExistsAsync("site-lexical"))
    {
        cache = await crawlCache.LoadCacheAsync("site-lexical", progress, ctx);
        SpectreConsoleTheme.WriteInfo($"Loaded {cache.Count:N0} documents from cache");
    }
    else
    {
        SpectreConsoleTheme.WriteInfo("No existing index - performing full crawl");
    }

    // 3. Make crawl decisions
    var decisions = decisionMaker.MakeDecisions(filteredUrls, cache).ToList();
    var newUrls = decisions.Count(d => d.Reason == CrawlReason.New);
    var unchangedUrls = decisions.Count(d => d.Reason == CrawlReason.Unchanged);
    var possiblyChanged = decisions.Count(d => d.Reason == CrawlReason.PossiblyChanged);

    SpectreConsoleTheme.WriteInfo(
        $"Crawl decisions: {newUrls:N0} new, {unchangedUrls:N0} unchanged, {possiblyChanged:N0} to verify"
    );

    if (dryRun)
    {
        DisplayDryRunWithCacheStats(decisions);
        return;
    }

    // 4. Crawl only New and PossiblyChanged URLs
    var urlsToCrawl = decisions
        .Where(d => d.Reason != CrawlReason.Unchanged)
        .ToList();

    await foreach (var result in crawler.CrawlAsync(urlsToCrawl, ctx))
    {
        if (result.NotModified)
        {
            // Just update batch_index_date
            await exporter.UpdateBatchDateAsync(result.Url, ctx);
            continue;
        }

        // ... normal processing
    }

    // 5. Handle unchanged URLs - just update batch_index_date
    foreach (var decision in decisions.Where(d => d.Reason == CrawlReason.Unchanged))
    {
        await exporter.UpdateBatchDateAsync(decision.Entry.Location, ctx);
    }

    // 6. Delete stale URLs
    var sitemapUrlSet = filteredUrls.Select(u => u.Location).ToHashSet();
    var staleUrls = decisionMaker.FindStaleUrls(cache, sitemapUrlSet);
    await exporter.DeleteUrlsAsync(staleUrls, ctx);
}
```

---

## AI Enrichment Caching

The existing `HashedBulkUpdate` pattern already handles content-based deduplication. For AI enrichment:

```csharp
// When processing a document, check if AI enrichment is needed
var enrichmentKey = EnrichmentKeyGenerator.Generate(document.Title, document.Body);
var currentPromptHash = LlmPrompts.CurrentHash;

if (cached?.EnrichmentKey == enrichmentKey && cached?.EnrichmentPromptHash == currentPromptHash)
{
    // Content AND prompts unchanged - preserve existing AI fields
    document.EnrichmentKey = cached.EnrichmentKey;
    document.EnrichmentPromptHash = cached.EnrichmentPromptHash;
    // AI fields will be preserved via HashedBulkUpdate script
}
else
{
    // Need new AI enrichment
    var enrichment = await llmClient.EnrichAsync(document, ctx);
    document.ApplyEnrichment(enrichment);
}
```

---

## Display Updates

### Dry Run with Cache Stats

```
╭──────────────────────────────────────────────────────────────╮
│                    📊 Crawl Analysis                         │
├──────────────────────────────────────────────────────────────┤
│  🆕 New URLs:           1,234                                │
│  ✓ Unchanged (cached):  5,823                                │
│  🔄 To verify (HTTP):     450                                │
│  ───────────────────────────────────────────────────────────│
│  Total URLs:            7,507                                │
│                                                              │
│  Estimated savings:                                          │
│  • HTTP requests:       5,823 skipped (78%)                  │
│  • AI enrichments:     ~6,200 skipped (83%)                  │
╰──────────────────────────────────────────────────────────────╯
```

### Progress Display with Cache

```
🔍 Loading cache...              [████████████████████] 100% 7,507 docs
📊 Analyzing sitemap...          Done (1,234 new, 5,823 cached, 450 to verify)
🌐 Crawling new/changed pages    [████████░░░░░░░░░░░░]  42% 710/1,684
📦 Indexing documents            [██████░░░░░░░░░░░░░░]  31% 520/1,684
```

---

## Testing Verification

### Unit Tests

1. **CrawlDecisionMaker tests**
   - New URL → CrawlReason.New
   - Unchanged (sitemap lastmod <= cache) → CrawlReason.Unchanged
   - Changed (sitemap lastmod > cache) → CrawlReason.PossiblyChanged
   - No lastmod in sitemap → CrawlReason.PossiblyChanged
   - Stale URL detection

2. **Conditional HTTP tests**
   - ETag sent in If-None-Match header
   - Last-Modified sent in If-Modified-Since header
   - 304 response handled correctly

### Integration Tests

```bash
# First run - expect full crawl
crawl-indexer site --max-pages 100 --dry-run
# Output: "100 URLs to crawl (0 cached, 100 new)"

# Second run - expect cache hits
crawl-indexer site --max-pages 100 --dry-run
# Output: "100 URLs checked (95 unchanged, 5 to verify)"
```

---

## Files to Modify

| File | Changes |
|------|---------|
| `SiteDocument.cs` | Add HttpEtag, HttpLastModified |
| `DocumentationDocument.cs` | Add HttpEtag, HttpLastModified |
| `SiteIndexMapping.cs` | Add http_etag, http_last_modified fields |
| `GuideIndexMapping.cs` | Add http_etag, http_last_modified fields |
| `CrawlResult.cs` | Add NotModified, CachedHash, HttpEtag, HttpLastModified |
| `AdaptiveCrawler.cs` | Add conditional HTTP request support |
| `IAdaptiveCrawler.cs` | Update signature to accept CachedDocInfo |
| `SiteCommand.cs` | Integrate cache loading and decision making |
| `GuideCommand.cs` | Integrate cache loading and decision making |
| `CrawlIndexerTooling.cs` | Register ICrawlCache, CrawlDecisionMaker |

---

## Performance Expectations

| Scenario | First Run | Subsequent (no changes) | Subsequent (5% changed) |
|----------|-----------|-------------------------|-------------------------|
| HTTP requests | 7,500 | 0 | ~375 |
| AI API calls | 7,500 | 0 | ~50 |
| ES operations | 7,500 | 7,500 (batch_date only) | ~375 full + 7,125 partial |
| Duration | ~30 min | ~5 min | ~8 min |
