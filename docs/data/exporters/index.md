---
navigation_title: Exporters
---

# Exporters

Beyond HTML, {{dbuild}} exports documentation in several data-oriented formats. These exporters are what make "docs as data" concrete — the same content that renders as web pages can also be indexed for search, served to AI agents, or packaged for knowledge systems.

:::{note}
This is not an exhaustive list of all internal exporters. These are the formats most relevant to consuming documentation as structured data.
:::

### [Elasticsearch](./elasticsearch.md)

Indexes documentation pages into Elasticsearch as structured documents. Powers the search infrastructure behind the docs site, the [MCP server](../mcp/index.md), and the [REST API](../api.md).

### [LLM Markdown](./llm.md)

Per-page LLM-optimized CommonMark served alongside HTML via content negotiation. Enabled by default — agents requesting `text/markdown` get token-efficient Markdown instead of HTML.

### [OKF](./okf.md)

An [Open Knowledge Format](https://github.com/GoogleCloudPlatform/knowledge-catalog/blob/main/okf/SPEC.md) zip bundle with bundle-relative links and synthesized directory indexes. Designed for knowledge base interchange between platforms.

### [Plain Text](./plain-text.md)

Stripped-down plain text with all formatting removed. Used internally by other exporters (notably Elasticsearch) for search indexing.

### [Git diff](./git-diff.md)

Maps the git diff to published page URLs and titles. Writes `changed-pages.json` for CI preview comment jobs.
