# Phase 6 (Continued): Caching Integration

## Status

✅ **Complete** - Caching is now integrated into both commands.

### What was implemented:
1. **ElasticsearchCrawlCache** - Updated to use `DistributedTransport` instead of `HttpClient`
2. **SiteCommand.cs** - Integrated cache loading, decision making, and cache-aware crawling
3. **GuideCommand.cs** - Same integration as SiteCommand
4. **Exporters** - Updated to accept transport from outside (shared with cache)
5. **IndexingDisplay** - Added `DisplayDryRunWithCacheStats` for dry run with cache statistics

### Remaining (optional follow-up):
- Batch date update for unchanged URLs (currently skipped in crawl)
- Stale URL deletion from index

---

## Technical Debt: ElasticsearchCrawlCache Transport

### Current Issue
`ElasticsearchCrawlCache` currently uses `HttpClient` directly with manual authentication header setup. This is inconsistent with the rest of the codebase which uses `Elastic.Transport.DistributedTransport`.

### Solution
1. **Create transport once in command handler** using `ElasticsearchTransportFactory.Create(endpoint)`
2. **Pass transport down** to `ElasticsearchCrawlCache` constructor instead of endpoint
3. **Remove HttpClient dependency** from `ElasticsearchCrawlCache`

### Changes Required

#### ElasticsearchCrawlCache.cs
```csharp
// BEFORE
public class ElasticsearchCrawlCache : ICrawlCache
{
    public ElasticsearchCrawlCache(
        ILogger<ElasticsearchCrawlCache> logger,
        HttpClient httpClient,
        ElasticsearchEndpoint endpoint
    )

// AFTER
public class ElasticsearchCrawlCache : ICrawlCache
{
    public ElasticsearchCrawlCache(
        ILogger<ElasticsearchCrawlCache> logger,
        DistributedTransport transport
    )
```

#### SiteCommand.cs / GuideCommand.cs
```csharp
// Create transport ONCE at command start
var endpoint = configurationContext.Endpoints.Elasticsearch;
var transport = ElasticsearchTransportFactory.Create(endpoint);

// Pass to cache
var crawlCache = new ElasticsearchCrawlCache(
    loggerFactory.CreateLogger<ElasticsearchCrawlCache>(),
    transport
);

// Pass same transport to exporter (already uses transport internally)
await using var exporter = new SiteIndexerExporter(
    loggerFactory,
    diagnostics,
    endpoint,
    noSemantic
);
```

#### RequestAsync Usage
Figure out the correct `RequestAsync` signature for `DistributedTransport`. The method signature is:
```csharp
RequestAsync<TResponse>(
    HttpMethod method,
    string path,
    PostData? postData = null,
    Action<Activity>? configureActivity = null,
    IRequestConfiguration? localConfiguration = null,
    CancellationToken cancellationToken = default
)
```

---

## Integration Steps

### Step 1: Update SiteCommand.cs

Add cache loading and decision making to the crawl flow:

```csharp
public async Task RunAsync(...)
{
    // ... existing sitemap parsing ...

    // Load cache from Elasticsearch (if index exists)
    var cache = new Dictionary<string, CachedDocInfo>();
    var crawlCache = new ElasticsearchCrawlCache(
        loggerFactory.CreateLogger<ElasticsearchCrawlCache>(),
        transport
    );

    if (await crawlCache.IndexExistsAsync("site-lexical", ctx))
    {
        SpectreConsoleTheme.WriteSection("Loading Cache");
        await AnsiConsole.Progress()
            .StartAsync(async progressCtx =>
            {
                var task = progressCtx.AddTask("[aqua]📦 Loading cached documents[/]");
                cache = await crawlCache.LoadCacheAsync(
                    "site-lexical",
                    new Progress<(int loaded, string? url)>(p =>
                    {
                        task.Value = p.loaded;
                        task.Description = $"[aqua]📦 Loaded {p.loaded:N0} docs[/]";
                    }),
                    ctx
                );
                task.Value = task.MaxValue = cache.Count;
            });
        SpectreConsoleTheme.WriteSuccess($"Loaded [yellow]{cache.Count:N0}[/] documents from cache");
    }
    else
    {
        SpectreConsoleTheme.WriteInfo("No existing index - performing full crawl");
    }

    // Make crawl decisions
    var decisionMaker = new CrawlDecisionMaker(loggerFactory.CreateLogger<CrawlDecisionMaker>());
    var decisions = decisionMaker.MakeDecisions(filteredUrls, cache).ToList();
    var stats = CrawlDecisionMaker.GetStats(decisions);

    SpectreConsoleTheme.WriteInfo(
        $"Crawl analysis: [green]{stats.NewUrls:N0}[/] new, " +
        $"[grey]{stats.UnchangedUrls:N0}[/] unchanged, " +
        $"[yellow]{stats.PossiblyChangedUrls:N0}[/] to verify"
    );

    if (dryRun)
    {
        DisplayDryRunWithCacheStats(decisions, stats);
        return;
    }

    // Crawl only New and PossiblyChanged URLs
    var urlsToCrawl = decisions
        .Where(d => d.Reason != CrawlReason.Unchanged)
        .ToList();

    // ... crawl and index urlsToCrawl ...

    // Update batch_index_date for unchanged URLs (bulk update)
    // ... handle unchanged URLs ...

    // Delete stale URLs not in sitemap
    var sitemapUrlSet = filteredUrls.Select(u => u.Location).ToHashSet(StringComparer.OrdinalIgnoreCase);
    var staleUrls = decisionMaker.FindStaleUrls(cache, sitemapUrlSet).ToList();
    if (staleUrls.Count > 0)
    {
        SpectreConsoleTheme.WriteWarning($"Found [red]{staleUrls.Count:N0}[/] stale URLs to delete");
        // await exporter.DeleteUrlsAsync(staleUrls, ctx);
    }
}
```

### Step 2: Update Dry Run Display

Show cache statistics in the dry run output:

```csharp
private static void DisplayDryRunWithCacheStats(
    IReadOnlyList<CrawlDecision> decisions,
    CrawlDecisionStats stats
)
{
    var panel = new Panel(
        new Rows(
            new Markup($"[green]🆕 New URLs:[/] [white]{stats.NewUrls:N0}[/]"),
            new Markup($"[grey]✓ Unchanged (cached):[/] [white]{stats.UnchangedUrls:N0}[/]"),
            new Markup($"[yellow]🔄 To verify (HTTP):[/] [white]{stats.PossiblyChangedUrls:N0}[/]"),
            new Rule { Style = Style.Parse("grey") },
            new Markup($"[aqua]Total URLs:[/] [white]{stats.TotalUrls:N0}[/]"),
            new Markup($"[cyan]URLs to crawl:[/] [white]{stats.UrlsToCrawl:N0}[/]"),
            new Rule { Style = Style.Parse("grey") },
            new Markup($"[dim]Estimated savings:[/]"),
            new Markup($"[dim]  • HTTP requests: {stats.UnchangedUrls:N0} skipped ({100.0 * stats.UnchangedUrls / stats.TotalUrls:F0}%)[/]")
        )
    )
    {
        Header = new PanelHeader("[aqua bold]📊 Crawl Analysis[/]"),
        Border = BoxBorder.Rounded,
        BorderStyle = Style.Parse("aqua"),
        Padding = new Padding(2, 1)
    };

    AnsiConsole.Write(panel);
}
```

### Step 3: Update GuideCommand.cs

Apply the same changes to `GuideCommand.cs` using `guide-lexical` as the index alias.

### Step 4: Add Batch Update Support

For unchanged URLs, we only need to update `batch_index_date`. Add a method to the exporter:

```csharp
// In SiteIndexerExporter.cs
public async Task UpdateBatchDatesAsync(
    IEnumerable<string> urls,
    DateTimeOffset batchDate,
    CancellationToken ct = default
)
{
    // Bulk update script to only update batch_index_date
    // This avoids re-indexing the full document
}
```

### Step 5: Add Stale URL Deletion

For URLs in cache but not in sitemap:

```csharp
// In SiteIndexerExporter.cs
public async Task DeleteUrlsAsync(
    IEnumerable<string> urls,
    CancellationToken ct = default
)
{
    // Bulk delete by URL
}
```

---

## Files to Modify

| File | Changes |
|------|---------|
| `Caching/ElasticsearchCrawlCache.cs` | Replace HttpClient with DistributedTransport |
| `Caching/ICrawlCache.cs` | Update constructor signature in docs |
| `Commands/SiteCommand.cs` | Add cache loading, decision making, integrate with crawl |
| `Commands/GuideCommand.cs` | Same as SiteCommand |
| `Indexing/SiteIndexerExporter.cs` | Add `UpdateBatchDatesAsync`, `DeleteUrlsAsync` |
| `Indexing/GuideIndexerExporter.cs` | Same as SiteIndexerExporter |
| `Display/IndexingDisplay.cs` | Add `DisplayDryRunWithCacheStats` |

---

## Testing Checklist

1. **First run** (no index exists):
   - [x] Cache loading skipped with info message
   - [x] All URLs marked as `New`
   - [x] Full crawl performed

2. **Second run** (unchanged):
   - [x] Cache loaded from Elasticsearch
   - [x] Most URLs marked as `Unchanged`
   - [x] Only changed URLs crawled
   - [ ] Batch date updated for unchanged (not yet implemented)

3. **HTTP 304 handling**:
   - [x] Conditional headers sent (If-None-Match, If-Modified-Since)
   - [x] 304 response handled correctly
   - [x] Document not re-indexed

4. **Stale URL deletion**:
   - [x] URLs in cache but not sitemap identified
   - [ ] Stale URLs deleted from index (not yet implemented)

5. **Dry run with cache**:
   - [x] Shows new/unchanged/to-verify breakdown
   - [x] Shows estimated savings

---

## Performance Expectations

| Scenario | First Run | Subsequent (unchanged) | Subsequent (5% changed) |
|----------|-----------|------------------------|-------------------------|
| Cache load | N/A | ~5 sec (7K docs) | ~5 sec |
| Crawl decisions | N/A | <1 sec | <1 sec |
| HTTP requests | ~7,500 | 0 | ~375 |
| Index operations | ~7,500 | ~7,500 (batch_date only) | ~375 full + ~7,125 partial |
| Total duration | ~30 min | ~5 min | ~8 min |
