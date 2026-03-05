# Crawl Indexer - Implementation Overview (Updated)

## Summary

Create `crawl-indexer` binary to crawl and index elastic.co content:
1. **`crawl-indexer guide`** - Legacy docs from `/guide/*` (8.x + 7.latest)
2. **`crawl-indexer site`** - Marketing/blog pages (excluding `/guide`)

## Current Status

| Phase | Description | Status |
|-------|-------------|--------|
| 1 | Foundation - Project setup, base classes, document types | ✅ Complete |
| 2 | Crawling - Sitemap parser, version discovery, adaptive crawler | ✅ Complete |
| 3 | HTML Processing - Extractors, HTML-to-markdown | ✅ Complete |
| 4 | Indexing - ES exporters, multilingual mapping | ✅ Complete |
| 5 | Integration - CLI wiring, display components | ✅ Complete |
| 6 | **Caching** - Incremental sync, conditional HTTP, AI cache | ✅ Complete |

## Key Decisions

| Decision | Answer |
|----------|--------|
| Interactive CLI | Spectre.Console for progress, charts, live display |
| Document model for /guide | Reuse `DocumentationDocument` |
| Site document model | New `SiteDocument` extending `BaseSearchDocument` |
| Versions | Auto-discover all 8.x + latest 7.x from sitemap |
| Languages | All languages (de, fr, jp, kr, cn, es, pt + en) |
| AI enrichment | Yes for both |
| HTML parsing | Static only (verified content in source) |
| Multilingual mapping | Language sub-fields (body.en, body.de, etc.) |
| Date source | article:modified_time > sitemap lastmod > crawl time |
| **Caching** | Elasticsearch-based with PIT API for 500K+ scale |
| **HTTP caching** | Store ETags and Last-Modified, use conditional requests |
| **First run** | Full crawl always (no cache to load) |

## Implementation Phases

| Phase | Description | Plan File | Status |
|-------|-------------|-----------|--------|
| 1 | Foundation - Project setup, base classes, document types | [plans/phase-1-foundation.md](plans/phase-1-foundation.md) | ✅ |
| 2 | Crawling - Sitemap parser, version discovery, adaptive crawler | [plans/phase-2-crawling.md](plans/phase-2-crawling.md) | ✅ |
| 3 | HTML Processing - Extractors, HTML-to-markdown | [plans/phase-3-html-processing.md](plans/phase-3-html-processing.md) | ✅ |
| 4 | Indexing - ES exporters, multilingual mapping, AI enrichment | [plans/phase-4-indexing.md](plans/phase-4-indexing.md) | ✅ |
| 5 | Integration - CLI wiring, testing, verification | [plans/phase-5-integration.md](plans/phase-5-integration.md) | ✅ |
| 6a | **Caching Infrastructure** - Core types, conditional HTTP | [plans/phase-6-caching.md](plans/phase-6-caching.md) | ✅ |
| 6b | **Caching Integration** - Wire into commands, batch updates | [plans/phase-6-caching-integration.md](plans/phase-6-caching-integration.md) | ✅ |

## Project Structure

```
src/tooling/crawl-indexer/
├── crawl-indexer.csproj
├── Program.cs
├── CrawlIndexerTooling.cs           # DI setup
├── Commands/
│   ├── GuideCommand.cs
│   └── SiteCommand.cs
├── Crawling/
│   ├── ISitemapParser.cs
│   ├── SitemapParser.cs
│   ├── IVersionDiscovery.cs
│   ├── VersionDiscovery.cs
│   ├── IAdaptiveCrawler.cs
│   ├── AdaptiveCrawler.cs
│   ├── CrawlResult.cs
│   └── SitemapEntry.cs
├── Caching/                         # 🆕 Phase 6
│   ├── ICrawlCache.cs
│   ├── ElasticsearchCrawlCache.cs
│   ├── CachedDocInfo.cs
│   ├── CrawlDecision.cs
│   └── CrawlDecisionMaker.cs
├── Html/
│   ├── IHtmlContentExtractor.cs
│   ├── GuideHtmlExtractor.cs
│   ├── SiteHtmlExtractor.cs
│   └── HtmlToMarkdownConverter.cs
├── Display/
│   ├── SpectreConsoleTheme.cs
│   ├── SpectreLogger.cs
│   ├── CrawlProgressContext.cs
│   └── IndexingDisplay.cs
└── Indexing/
    ├── GuideIndexerExporter.cs
    ├── GuideIndexMapping.cs
    ├── SiteIndexerExporter.cs
    └── SiteIndexMapping.cs
```

## CLI Usage

```bash
# Guide: Legacy docs (auto-discovers 8.x + 7.latest)
crawl-indexer guide --endpoint "https://es:9200"

# Site: Marketing/blog pages
crawl-indexer site --endpoint "https://es:9200"

# Dry run
crawl-indexer guide --dry-run
crawl-indexer site --dry-run
```

## URL Filtering

### Guide
- Include: `/guide/en/*/reference/{8.x|7.17}/*`
- Exclude: All other versions

### Site
- Include: All except `/guide/*` and `/downloads/past-releases/*`
- Categories: blog, what-is, search-labs, security-labs, observability-labs, product pages
- Languages: All (en, de, fr, jp, kr, cn, es, pt)

## Document Types

### BaseSearchDocument (shared)
Core fields + AI enrichment + social metadata (og:*, twitter:*)

### DocumentationDocument (guide)
BaseSearchDocument + Product (with Version), Parents, NavigationDepth

### SiteDocument (site)
BaseSearchDocument + PageType, Language, Author, PublishedDate, Relevance

## Expected Performance with Caching

| Metric | First Run | Subsequent Runs |
|--------|-----------|-----------------|
| HTTP requests | ~7,400 | ~500 (93% reduction) |
| AI API calls | ~7,400 | ~50 (99% reduction) |
| Run time | ~30 min | ~3 min |
