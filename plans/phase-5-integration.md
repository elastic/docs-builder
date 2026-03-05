# Phase 5: Integration & Testing

## Objective
Wire CLI commands, add comprehensive tests, and verify end-to-end functionality with rich interactive display.

## Tasks

### 5.0 Spectre.Console Interactive Display

**File**: `/src/tooling/crawl-indexer/Display/CrawlProgressDisplay.cs`

When running interactively (not CI), use Spectre.Console for rich progress display:

```csharp
public class CrawlProgressDisplay
{
    private readonly bool _isInteractive;

    public CrawlProgressDisplay()
    {
        // Detect if running interactively (not CI)
        _isInteractive = !Console.IsOutputRedirected &&
            Environment.GetEnvironmentVariable("CI") == null &&
            Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == null;
    }

    public async Task RunWithProgressAsync(
        string title,
        Func<ProgressContext, Task> action)
    {
        if (!_isInteractive)
        {
            // Fall back to simple logging
            await action(null!);
            return;
        }

        await AnsiConsole.Progress()
            .AutoRefresh(true)
            .AutoClear(false)
            .HideCompleted(false)
            .Columns(
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new RemainingTimeColumn(),
                new SpinnerColumn())
            .StartAsync(action);
    }

    public void ShowStatus(string status, Action action)
    {
        if (!_isInteractive)
        {
            action();
            return;
        }

        AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("green"))
            .Start(status, ctx => action());
    }

    public void ShowBreakdownChart(string title, Dictionary<string, (double Value, Color Color)> data)
    {
        if (!_isInteractive) return;

        var chart = new BreakdownChart()
            .Width(60);

        foreach (var (label, (value, color)) in data)
            chart.AddItem(label, value, color);

        AnsiConsole.Write(new Panel(chart)
            .Header(title)
            .Border(BoxBorder.Rounded));
    }

    public void ShowBarChart(string title, Dictionary<string, double> data)
    {
        if (!_isInteractive) return;

        var chart = new BarChart()
            .Width(60)
            .Label(title);

        foreach (var (label, value) in data)
            chart.AddItem(label, value, Color.Green);

        AnsiConsole.Write(chart);
    }

    public void ShowCrawlSummary(CrawlSummary summary)
    {
        if (!_isInteractive)
        {
            Console.WriteLine($"Crawled: {summary.Total}, Success: {summary.Success}, Failed: {summary.Failed}");
            return;
        }

        // Status breakdown chart
        ShowBreakdownChart("Crawl Results", new Dictionary<string, (double, Color)>
        {
            ["Success"] = (summary.Success, Color.Green),
            ["Failed"] = (summary.Failed, Color.Red),
            ["Skipped"] = (summary.Skipped, Color.Yellow)
        });

        // Page type distribution
        if (summary.PageTypeDistribution.Count > 0)
        {
            ShowBarChart("Pages by Type", summary.PageTypeDistribution);
        }

        // Language distribution
        if (summary.LanguageDistribution.Count > 0)
        {
            ShowBarChart("Pages by Language", summary.LanguageDistribution);
        }

        // Version distribution (for guide)
        if (summary.VersionDistribution.Count > 0)
        {
            ShowBarChart("Pages by Version", summary.VersionDistribution);
        }

        // Summary table
        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("Metric")
            .AddColumn("Value");

        table.AddRow("Total URLs", summary.Total.ToString());
        table.AddRow("Indexed", summary.Success.ToString());
        table.AddRow("Failed", summary.Failed.ToString());
        table.AddRow("Duration", summary.Duration.ToString(@"hh\:mm\:ss"));
        table.AddRow("Avg Response", $"{summary.AvgResponseMs:F0}ms");

        AnsiConsole.Write(table);
    }
}

public record CrawlSummary
{
    public int Total { get; init; }
    public int Success { get; init; }
    public int Failed { get; init; }
    public int Skipped { get; init; }
    public TimeSpan Duration { get; init; }
    public double AvgResponseMs { get; init; }
    public Dictionary<string, double> PageTypeDistribution { get; init; } = [];
    public Dictionary<string, double> LanguageDistribution { get; init; } = [];
    public Dictionary<string, double> VersionDistribution { get; init; } = [];
}
```

**File**: `/src/tooling/crawl-indexer/Display/LiveCrawlDisplay.cs`

Live display during crawl:

```csharp
public class LiveCrawlDisplay
{
    public async Task RunLiveAsync(
        IAsyncEnumerable<CrawlResult> results,
        Func<CrawlResult, Task> processor,
        CancellationToken ct)
    {
        if (!IsInteractive())
        {
            await foreach (var result in results.WithCancellation(ct))
                await processor(result);
            return;
        }

        var stats = new CrawlStats();

        await AnsiConsole.Live(CreateLayout(stats))
            .AutoClear(false)
            .Overflow(VerticalOverflow.Ellipsis)
            .StartAsync(async ctx =>
            {
                await foreach (var result in results.WithCancellation(ct))
                {
                    stats.Update(result);
                    ctx.UpdateTarget(CreateLayout(stats));
                    await processor(result);
                }
            });
    }

    private static IRenderable CreateLayout(CrawlStats stats) =>
        new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("Crawl Progress")
            .AddRow(new Rows(
                new Markup($"[green]✓ Indexed:[/] {stats.Indexed}"),
                new Markup($"[red]✗ Failed:[/] {stats.Failed}"),
                new Markup($"[blue]⟳ Pending:[/] {stats.Pending}"),
                new Markup($"[yellow]⚡ Rate:[/] {stats.CurrentDelayMs}ms"),
                new Markup($"[cyan]📄 Current:[/] {stats.CurrentUrl?.Truncate(50)}")
            ));
}
```

Usage in GuideCommand:

```csharp
[Command("")]
public async Task<int> Execute(/* params */)
{
    var display = new CrawlProgressDisplay();

    // Show status while discovering versions
    HashSet<string> allowedVersions = null!;
    display.ShowStatus("Discovering versions from sitemap...", () =>
    {
        var discovered = await versionDiscovery.DiscoverAsync(sitemap, ctx);
        allowedVersions = /* ... */;
    });

    // Show progress during crawl
    await display.RunWithProgressAsync("Crawling guide pages", async ctx =>
    {
        var task = ctx.AddTask("Crawling", maxValue: urlCount);

        await foreach (var result in crawler.CrawlAsync(urls, config, ct))
        {
            // Process...
            task.Increment(1);
        }
    });

    // Show summary at end
    display.ShowCrawlSummary(new CrawlSummary
    {
        Total = indexed + failed,
        Success = indexed,
        Failed = failed,
        Duration = stopwatch.Elapsed,
        VersionDistribution = versionCounts
    });

    return 0;
}
```

### 5.1 Guide Command

**File**: `/src/tooling/crawl-indexer/Commands/GuideCommand.cs`

```csharp
internal sealed class GuideCommand(
    ILoggerFactory logFactory,
    IDiagnosticsCollector collector,
    ISitemapParser sitemapParser,
    VersionDiscovery versionDiscovery,
    AdaptiveCrawler crawler,
    GuideHtmlExtractor extractor,
    IConfigurationContext configContext)
{
    /// <summary>
    /// Crawl and index legacy documentation from /guide
    /// </summary>
    /// <param name="versions">Comma-separated versions (default: auto-discover 8.x + 7.latest)</param>
    /// <param name="products">Comma-separated products to include (default: all)</param>
    /// <param name="sitemapUrl">Override sitemap URL</param>
    /// <param name="maxPages">Maximum pages to crawl (0 = unlimited)</param>
    /// <param name="dryRun">Discover URLs without crawling</param>
    /// <param name="endpoint">Elasticsearch endpoint</param>
    /// <param name="apiKey">Elasticsearch API key</param>
    /// <param name="enableAiEnrichment">Enable AI enrichment</param>
    /// <param name="noSemantic">Skip semantic index</param>
    /// <param name="ctx">Cancellation token</param>
    [Command("")]
    public async Task<int> Execute(
        string? versions = null,
        string? products = null,
        string? sitemapUrl = null,
        int maxPages = 0,
        bool dryRun = false,
        string? endpoint = null,
        string? apiKey = null,
        bool enableAiEnrichment = true,
        bool noSemantic = false,
        CancellationToken ctx = default)
    {
        var sitemap = new Uri(sitemapUrl ?? "https://www.elastic.co/sitemap.xml");

        // 1. Discover or parse versions
        HashSet<string> allowedVersions;
        if (versions != null)
        {
            allowedVersions = versions.Split(',').Select(v => v.Trim()).ToHashSet();
        }
        else
        {
            collector.Write("Discovering versions from sitemap...");
            var discovered = await versionDiscovery.DiscoverAsync(sitemap, ctx);
            allowedVersions = discovered.V8Versions.Concat(
                discovered.LatestV7 != null ? [discovered.LatestV7] : []
            ).ToHashSet();
            collector.Write($"Found versions: {string.Join(", ", allowedVersions)}");
        }

        // 2. Parse products filter
        HashSet<string>? allowedProducts = products?.Split(',').Select(p => p.Trim()).ToHashSet();

        // 3. Collect URLs from sitemap
        var urlFilter = new UrlFilter();
        var urls = sitemapParser.ParseAsync(sitemap, ctx)
            .Where(e => urlFilter.ShouldCrawlGuide(e.Url, allowedVersions))
            .Where(e => allowedProducts == null || /* match product in URL */);

        if (maxPages > 0)
            urls = urls.Take(maxPages);

        // 4. Dry run: just report URLs
        if (dryRun)
        {
            var count = 0;
            await foreach (var entry in urls)
            {
                collector.Write($"  {entry.Url}");
                count++;
            }
            collector.Write($"Total URLs: {count}");
            return 0;
        }

        // 5. Initialize exporter
        await using var exporter = new GuideIndexerExporter(
            logFactory, collector, configContext.Endpoints,
            enableAiEnrichment, !noSemantic);
        await exporter.StartAsync(ctx);

        // 6. Crawl and index
        var crawlConfig = new CrawlConfiguration();
        var crawlResults = crawler.CrawlAsync(
            urls.Select(e => e.Url),
            crawlConfig, ctx);

        var indexed = 0;
        var failed = 0;

        await foreach (var result in crawlResults)
        {
            if (result.HtmlContent == null)
            {
                failed++;
                continue;
            }

            var doc = extractor.Extract(result.Url, result.HtmlContent, result.SitemapLastMod);
            if (doc == null)
            {
                failed++;
                continue;
            }

            await exporter.IndexDocumentAsync(doc, ctx);
            indexed++;

            if (indexed % 100 == 0)
                collector.Write($"Indexed {indexed} documents...");
        }

        // 7. Finalize
        await exporter.StopAsync(ctx);
        collector.Write($"Complete: {indexed} indexed, {failed} failed");

        return failed > 0 ? 1 : 0;
    }
}
```

### 5.2 Site Command

**File**: `/src/tooling/crawl-indexer/Commands/SiteCommand.cs`

```csharp
internal sealed class SiteCommand(
    ILoggerFactory logFactory,
    IDiagnosticsCollector collector,
    ISitemapParser sitemapParser,
    AdaptiveCrawler crawler,
    SiteHtmlExtractor extractor,
    IConfigurationContext configContext)
{
    /// <summary>
    /// Crawl and index site pages (excluding /guide)
    /// </summary>
    /// <param name="languages">Comma-separated languages (default: all)</param>
    /// <param name="excludePaths">Additional paths to exclude</param>
    /// <param name="sitemapUrl">Override sitemap URL</param>
    /// <param name="maxPages">Maximum pages to crawl (0 = unlimited)</param>
    /// <param name="dryRun">Discover URLs without crawling</param>
    /// <param name="endpoint">Elasticsearch endpoint</param>
    /// <param name="apiKey">Elasticsearch API key</param>
    /// <param name="enableAiEnrichment">Enable AI enrichment</param>
    /// <param name="noSemantic">Skip semantic index</param>
    /// <param name="ctx">Cancellation token</param>
    [Command("")]
    public async Task<int> Execute(
        string? languages = null,
        string? excludePaths = null,
        string? sitemapUrl = null,
        int maxPages = 0,
        bool dryRun = false,
        string? endpoint = null,
        string? apiKey = null,
        bool enableAiEnrichment = true,
        bool noSemantic = false,
        CancellationToken ctx = default)
    {
        // Similar structure to GuideCommand
        // Uses SiteHtmlExtractor and SiteIndexerExporter
    }
}
```

### 5.3 DI Setup

**File**: `/src/tooling/crawl-indexer/CrawlIndexerTooling.cs`

```csharp
public static class CrawlIndexerTooling
{
    public static HostApplicationBuilder AddCrawlIndexerDefaults(this HostApplicationBuilder builder)
    {
        var services = builder.Services;

        // HTTP client
        services.AddHttpClient<AdaptiveCrawler>(client =>
        {
            client.DefaultRequestHeaders.Add("User-Agent", "elastic-crawl-indexer/1.0");
            client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml");
            client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // Crawling services
        services.AddSingleton<ISitemapParser, SitemapParser>();
        services.AddSingleton<VersionDiscovery>();
        services.AddSingleton<UrlFilter>();

        // HTML extractors
        services.AddSingleton<GuideHtmlExtractor>();
        services.AddSingleton<SiteHtmlExtractor>();
        services.AddSingleton<HtmlToMarkdownConverter>();

        return builder;
    }
}
```

### 5.4 Unit Tests

**Directory**: `/tests/Elastic.CrawlIndexer.Tests/`

#### SitemapParserTests.cs
```csharp
[Test]
public async Task ParseAsync_WithValidSitemap_ReturnsEntries()
{
    var xml = """
        <?xml version="1.0"?>
        <urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">
            <url>
                <loc>https://www.elastic.co/guide/en/elasticsearch/reference/8.15/index.html</loc>
                <lastmod>2024-01-15</lastmod>
            </url>
        </urlset>
        """;

    var parser = CreateParser(xml);
    var entries = await parser.ParseAsync(new Uri("https://example.com/sitemap.xml"), default).ToListAsync();

    entries.Should().HaveCount(1);
    entries[0].Url.Should().Be(new Uri("https://www.elastic.co/guide/en/elasticsearch/reference/8.15/index.html"));
}
```

#### VersionDiscoveryTests.cs
```csharp
[Test]
public async Task DiscoverAsync_FindsAllV8AndLatestV7()
{
    var entries = new[]
    {
        new SitemapEntry(new Uri("https://www.elastic.co/guide/en/es/reference/8.15/index.html"), null, null, null),
        new SitemapEntry(new Uri("https://www.elastic.co/guide/en/es/reference/8.14/index.html"), null, null, null),
        new SitemapEntry(new Uri("https://www.elastic.co/guide/en/es/reference/7.17/index.html"), null, null, null),
        new SitemapEntry(new Uri("https://www.elastic.co/guide/en/es/reference/7.16/index.html"), null, null, null),
    };

    var discovery = new VersionDiscovery(MockSitemapParser(entries), NullLogger<VersionDiscovery>.Instance);
    var result = await discovery.DiscoverAsync(new Uri("https://example.com"), default);

    result.V8Versions.Should().BeEquivalentTo(["8.15", "8.14"]);
    result.LatestV7.Should().Be("7.17");
}
```

#### GuideHtmlExtractorTests.cs
```csharp
[Test]
public void Extract_WithValidGuideHtml_ExtractsMetadata()
{
    var html = File.ReadAllText("TestData/guide-query-dsl.html");

    var extractor = new GuideHtmlExtractor(NullLogger<GuideHtmlExtractor>.Instance);
    var doc = extractor.Extract(new Uri("https://www.elastic.co/guide/en/elasticsearch/reference/8.15/query-dsl.html"), html, null);

    doc.Should().NotBeNull();
    doc!.Title.Should().Contain("Query DSL");
    doc.Product!.Id.Should().Be("elasticsearch");
    doc.Product.Version.Should().Be("8.15");
}
```

#### HtmlToMarkdownConverterTests.cs
```csharp
[Test]
public void Convert_WithHeadings_ConvertsToMarkdown()
{
    var html = "<h1>Title</h1><p>Paragraph</p><ul><li>Item 1</li><li>Item 2</li></ul>";

    var converter = new HtmlToMarkdownConverter();
    var markdown = converter.Convert(html);

    markdown.Should().Contain("# Title");
    markdown.Should().Contain("Paragraph");
    markdown.Should().Contain("- Item 1");
}
```

### 5.5 Integration Tests

**Directory**: `/tests-integration/Elastic.CrawlIndexer.IntegrationTests/`

```csharp
[Test]
public async Task CrawlAndIndex_GuidePages_IndexesSuccessfully()
{
    // 1. Start test ES container
    // 2. Crawl a few pages with --max-pages 5
    // 3. Verify documents in index
    // 4. Verify search works
}

[Test]
public async Task IncrementalSync_UpdatedDocument_SyncsToSemantic()
{
    // 1. Index document
    // 2. Modify document
    // 3. Re-index
    // 4. Verify only changed doc reindexed to semantic
}
```

### 5.6 Test Data

**Directory**: `/tests/Elastic.CrawlIndexer.Tests/TestData/`

Store sample HTML files:
- `guide-query-dsl.html` - Sample /guide page
- `guide-logstash.html` - Another /guide page
- `blog-post.html` - Sample blog post
- `product-elasticsearch.html` - Product page
- `what-is-elasticsearch.html` - What-is page

## Verification Checklist

### Guide Crawl
- [ ] Version auto-discovery works
- [ ] URL filtering by version works
- [ ] Metadata extraction (product, version) works
- [ ] Content extraction works
- [ ] AI enrichment works
- [ ] Incremental sync works
- [ ] Stale document deletion works

### Site Crawl
- [ ] URL filtering works
- [ ] Language detection works
- [ ] Page type detection works
- [ ] Relevance assignment works
- [ ] Multilingual search works
- [ ] AI enrichment works

### End-to-End
```bash
# Test guide crawl
crawl-indexer guide --dry-run
crawl-indexer guide --max-pages 10 --endpoint "http://localhost:9200"

# Test site crawl
crawl-indexer site --dry-run
crawl-indexer site --max-pages 10 --endpoint "http://localhost:9200"

# Verify in Kibana
# - Check guide-lexical-* index
# - Check site-lexical-* index
# - Run sample searches
```

## Files Created

| File | Description |
|------|-------------|
| `Commands/GuideCommand.cs` | Guide crawl CLI |
| `Commands/SiteCommand.cs` | Site crawl CLI |
| `CrawlIndexerTooling.cs` | DI setup |
| `Display/CrawlProgressDisplay.cs` | Spectre.Console progress/charts |
| `Display/LiveCrawlDisplay.cs` | Live crawl display |
| `Display/CrawlSummary.cs` | Summary data record |
| `tests/*/SitemapParserTests.cs` | Unit tests |
| `tests/*/VersionDiscoveryTests.cs` | Unit tests |
| `tests/*/GuideHtmlExtractorTests.cs` | Unit tests |
| `tests/*/SiteHtmlExtractorTests.cs` | Unit tests |
| `tests/*/HtmlToMarkdownConverterTests.cs` | Unit tests |
| `tests-integration/*/CrawlIndexingTests.cs` | Integration tests |
