---
navigation_title: Plain Text
---

# Plain Text exporter

The Plain Text exporter converts rendered Markdown documents to plain text by stripping all formatting while preserving readable content structure.

## Purpose

This exporter is primarily used internally by other components:

- **Search indexing** — the [Elasticsearch exporter](./elasticsearch.md) uses plain text conversion to generate the searchable body of each document
- **Content extraction** — other exporters use plain text as an intermediate step

## Behavior

The exporter:

- Strips all Markdown formatting (bold, italic, links, etc.)
- Removes directive syntax and renders only the content within
- Preserves paragraph and heading structure as whitespace
- Resolves substitution variables to their values
