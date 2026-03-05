# Crawl Indexer - Implementation Overview

## Summary

Create `crawl-indexer` binary to crawl and index elastic.co content:
1. **`crawl-indexer guide`** - Legacy docs from `/guide/*` (8.x + 7.latest)
2. **`crawl-indexer site`** - Marketing/blog pages (excluding `/guide`)

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

## Implementation Phases

| Phase | Description | Plan File |
|-------|-------------|-----------|
| 1 | Foundation - Project setup, base classes, document types | [plans/phase-1-foundation.md](plans/phase-1-foundation.md) |
| 2 | Crawling - Sitemap parser, version discovery, adaptive crawler | [plans/phase-2-crawling.md](plans/phase-2-crawling.md) |
| 3 | HTML Processing - Extractors, HTML-to-markdown | [plans/phase-3-html-processing.md](plans/phase-3-html-processing.md) |
| 4 | Indexing - ES exporters, multilingual mapping, AI enrichment | [plans/phase-4-indexing.md](plans/phase-4-indexing.md) |
| 5 | Integration - CLI wiring, testing, verification | [plans/phase-5-integration.md](plans/phase-5-integration.md) |

## Project Structure

```
src/tooling/crawl-indexer/
├── crawl-indexer.csproj
├── Program.cs
├── Commands/
│   ├── GuideCommand.cs
│   └── SiteCommand.cs
├── Crawling/
│   ├── SitemapParser.cs
│   ├── VersionDiscovery.cs
│   └── AdaptiveCrawler.cs
├── Html/
│   ├── GuideHtmlExtractor.cs
│   ├── SiteHtmlExtractor.cs
│   └── HtmlToMarkdownConverter.cs
└── Indexing/
    ├── GuideIndexerExporter.cs
    └── SiteIndexerExporter.cs
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
- Languages: All (en, de, fr, jp, kr, cn, es, pt)

## Document Types

### BaseSearchDocument (shared)
Core fields + AI enrichment + social metadata (og:*, twitter:*)

### DocumentationDocument (guide)
BaseSearchDocument + Product (with Version), Parents, NavigationDepth

### SiteDocument (site)
BaseSearchDocument + PageType, Language, Author, PublishedDate, Relevance
