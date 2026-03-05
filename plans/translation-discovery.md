# Translation Discovery Plan

## Overview

Add a "Translation Discovery" phase to the crawl-indexer that discovers translated versions of English pages by probing URLs with HEAD requests. Uses cache to skip unchanged pages for efficiency.

---

## Current Flow (SiteCommand)

```
1. Sitemap Discovery → fetch main + labs sitemaps
2. URL Filtering → exclude paths, filter languages
3. Cache Loading → load existing documents from Elasticsearch
4. Crawl Decisions → New / Unchanged / PossiblyChanged
5. Crawl & Index → fetch and index pages
```

## Proposed Flow

```
1. Sitemap Discovery → fetch main + labs sitemaps
2. URL Filtering → exclude paths (NO language filter yet)
3. Cache Loading → load existing documents from Elasticsearch
4. English URL Extraction → filter sitemap to English-only pages
5. Crawl Decisions → New / Unchanged / PossiblyChanged (English only)
6. ★ TRANSLATION DISCOVERY ★ → probe for translations of NEW/MODIFIED English pages
7. Merge URLs → combine English + discovered translations
8. Crawl & Index → fetch and index all pages
```

---

## Translation Discovery Logic

### When to Probe

Only probe for translations of English pages that are:
- **New** (not in cache) - all translations are potentially new
- **PossiblyChanged** (modified since last crawl) - translations may have been added

Skip unchanged English pages - their translations would have been discovered on previous runs.

### URL Transformation

English URL → Translation URL:
```
https://www.elastic.co/blog/article
→ https://www.elastic.co/de/blog/article
→ https://www.elastic.co/fr/blog/article
→ https://www.elastic.co/es/blog/article
... etc
```

### Categories with Translations

Based on testing all categories:
| Category | Has Translations | Response |
|----------|-----------------|----------|
| /blog/ | ✅ Yes | 200 |
| /elasticon/ | ✅ Yes | 302 redirect |
| /virtual-events/ | ✅ Yes | 200 |
| /customers/ | ✅ Yes | 200 |
| /about/ | ✅ Yes | 200 |
| /downloads/ | ✅ Yes | 200 |
| /demo-gallery/ | ✅ Yes | 200 |
| /security-labs/ | ✅ Yes | 200 |
| /what-is/ | ✅ Yes | 200 |
| /training/ | ✅ Yes | 200 |
| /resources/ | ✅ Yes | 200 |
| /webinars/ | ❌ No | 404 |
| /search-labs/ | ❌ No | 404 |
| /observability-labs/ | ❌ No | 404 |

**Only 3 categories lack translations**: webinars, search-labs, observability-labs

### Language Prefixes

```csharp
private static readonly string[] TranslationPrefixes =
    ["de", "fr", "es", "jp", "kr", "cn", "pt"];
```

---

## Implementation

### New Interface: `ITranslationDiscovery`

```csharp
public interface ITranslationDiscovery
{
    IAsyncEnumerable<DiscoveredTranslation> DiscoverAsync(
        IReadOnlyList<CrawlDecision> englishDecisions,
        IReadOnlyDictionary<string, CachedDocInfo> cache,
        HashSet<string> languageFilter,
        CancellationToken ct
    );
}

public record DiscoveredTranslation(
    string EnglishUrl,
    string TranslatedUrl,
    string Language
);
```

### Implementation: `TranslationDiscovery`

```csharp
public class TranslationDiscovery : ITranslationDiscovery
{
    private static readonly string[] TranslationPrefixes =
        ["de", "fr", "es", "jp", "kr", "cn", "pt"];

    // Categories that DON'T have translations (skip probing)
    private static readonly string[] NoTranslationPaths =
        ["/webinars/", "/search-labs/", "/observability-labs/"];

    public async IAsyncEnumerable<DiscoveredTranslation> DiscoverAsync(...)
    {
        // Filter to New + PossiblyChanged only
        var toProbe = englishDecisions
            .Where(d => d.Reason != CrawlReason.Unchanged)
            .Where(d => !NoTranslationPaths.Any(p =>
                d.Entry.Location.Contains(p, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        // Determine which languages to probe
        var languages = languageFilter.Count > 0
            ? TranslationPrefixes.Where(l => languageFilter.Contains(l))
            : TranslationPrefixes;

        foreach (var decision in toProbe)
        {
            foreach (var lang in languages)
            {
                var translatedUrl = BuildTranslatedUrl(decision.Entry.Location, lang);

                // Skip if already in cache
                if (cache.ContainsKey(translatedUrl))
                {
                    yield return new DiscoveredTranslation(
                        decision.Entry.Location, translatedUrl, lang);
                    continue;
                }

                // HEAD request to check existence
                if (await ExistsAsync(translatedUrl, ct))
                {
                    yield return new DiscoveredTranslation(
                        decision.Entry.Location, translatedUrl, lang);
                }
            }
        }
    }

    private static string BuildTranslatedUrl(string englishUrl, string lang)
    {
        // https://www.elastic.co/blog/x → https://www.elastic.co/de/blog/x
        var uri = new Uri(englishUrl);
        return $"{uri.Scheme}://{uri.Host}/{lang}{uri.AbsolutePath}";
    }

    private async Task<bool> ExistsAsync(string url, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Head, url);
        var response = await _httpClient.SendAsync(request, ct);
        // 200 or 301/302 redirect = exists
        return response.IsSuccessStatusCode ||
               response.StatusCode == HttpStatusCode.MovedPermanently ||
               response.StatusCode == HttpStatusCode.Found;
    }
}
```

### Updated SiteCommand Flow

```csharp
// 1-3: Sitemap discovery, filtering, cache loading (unchanged)

// 4: Extract English URLs from sitemap
var englishUrls = filteredUrls
    .Where(u => GetLanguageFromUrl(u.Location) == "en")
    .ToList();

// 5: Make crawl decisions for English pages only
var decisions = decisionMaker.MakeDecisions(englishUrls, cache).ToList();

// 6: ★ TRANSLATION DISCOVERY ★
SpectreConsoleTheme.WriteSection("Translation Discovery");

var discoveredTranslations = new List<SitemapEntry>();
await foreach (var translation in translationDiscovery.DiscoverAsync(
    decisions, cache, languageFilter, ctx))
{
    discoveredTranslations.Add(new SitemapEntry(
        translation.TranslatedUrl,
        // Inherit lastmod from English page
        decisions.First(d => d.Entry.Location == translation.EnglishUrl).Entry.LastModified
    ));
}

SpectreConsoleTheme.WriteSuccess(
    $"Discovered [yellow]{discoveredTranslations.Count:N0}[/] translations");

// 7: Merge and make decisions for translations
var translationDecisions = decisionMaker.MakeDecisions(discoveredTranslations, cache);
var allDecisions = decisions.Concat(translationDecisions).ToList();

// 8: Crawl & Index (all URLs)
var urlsToCrawl = allDecisions
    .Where(d => d.Reason != CrawlReason.Unchanged)
    .ToList();
```

---

## Display Updates

### Translation Discovery Progress

```
── Translation Discovery ──────────────────────────────────────
  Probing translations ━━━━━━━━━━━━━━━━━━━━ 100%  234/234 pages

✓ Discovered 1,456 translations across 7 languages
  • German (de): 234
  • French (fr): 220
  • Spanish (es): 234
  • Japanese (ja): 189
  • Korean (ko): 178
  • Chinese (cn): 201
  • Portuguese (pt): 200
```

### Updated Site Analysis

Show language breakdown after discovery:
```
Languages:
  English 🇬🇧: 2,000    German 🇩🇪: 234    French 🇫🇷: 220
  Spanish 🇪🇸: 234      Japanese 🇯🇵: 189   Korean 🇰🇷: 178
```

---

## Files to Modify/Create

| File | Changes |
|------|---------|
| `Crawling/ITranslationDiscovery.cs` | **NEW** - Interface |
| `Crawling/TranslationDiscovery.cs` | **NEW** - Implementation |
| `Commands/SiteCommand.cs` | Insert translation discovery phase |
| `Display/IndexingDisplay.cs` | Add translation discovery display |
| `CrawlIndexerTooling.cs` | Register ITranslationDiscovery |

---

## CLI Options

```csharp
/// <param name="discoverTranslations">Discover translated versions of pages (default: true)</param>
/// <param name="languages">Comma-separated languages to include (default: all)</param>
bool discoverTranslations = true,
string? languages = null,
```

When `--languages` is specified:
- Only probe for those specific languages
- Only crawl pages in those languages

When `--discover-translations=false`:
- Skip translation discovery phase
- Only crawl URLs found in sitemap

---

## Performance Considerations

1. **HEAD requests are lightweight** - no body transfer
2. **Concurrent probing** - use semaphore for rate limiting (e.g., 20 concurrent)
3. **Cache-first** - skip HEAD if translation already in cache
4. **Category filtering** - skip labs content (known no translations)
5. **Batch progress** - show progress bar during discovery

---

## Verification

```bash
# Test translation discovery (dry run)
crawl-indexer site --dry-run --max-pages 100
# Should show: "Discovered X translations across Y languages"

# Test with specific languages
crawl-indexer site --dry-run --languages de,fr --max-pages 50
# Should only show German and French translations

# Test without discovery
crawl-indexer site --dry-run --discover-translations=false --max-pages 50
# Should only show English URLs from sitemap

# Full crawl with translations
crawl-indexer site --max-pages 100
# Should crawl English + discovered translations
```
