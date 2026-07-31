---
navigation_title: Elasticsearch
---

# Elasticsearch exporter

The Elasticsearch exporter indexes documentation pages into Elasticsearch, powering full-text search, the AI assistant, and the [MCP server](../mcp/index.md) across all documentation sites.

## How it works

During a build, the exporter processes each rendered page and indexes it as a structured document into Elasticsearch. The document schema is defined in the shared [search contract](https://github.com/elastic/docs-builder/tree/main/src/services/search/Elastic.Documentation.Search.Contract/) to ensure consistency across all content sources.

## Index commands by build mode

Each build mode exposes dedicated index commands:

| Build mode | Command | Description |
|------------|---------|-------------|
| Assembler | `docs-builder assembler build` | Indexes all assembled documentation sets |
| Codex | `docs-builder codex index` | Indexes documentation for a codex environment |
| Isolated | `docs-builder docset index` | Indexes a single documentation set |

## What gets indexed

Each page is indexed with:

- Full rendered content (as plain text for search)
- Page title and headings
- Navigation context (breadcrumbs, section)
- Product and version metadata
- URL and cross-link information

## Related

- [REST API](../api.md) — search endpoints that query the indexed content
- [MCP server](../mcp/index.md) — AI assistant access to indexed documentation
