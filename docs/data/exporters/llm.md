---
navigation_title: LLM Markdown
---

# LLM Markdown exporter

The LLM Markdown exporter produces an optimized CommonMark version of every documentation page alongside the standard HTML output. This is **enabled by default** — every HTML page gets a corresponding `.md` file that web servers can serve through content negotiation.

API Explorer pages use the same co-located file rule. The generator writes CommonMark next to each API HTML page. Preview and static hosts serve that file when the request path ends with `.md` or when `Accept` prefers `text/markdown`.

## How it works

When a request arrives with an `Accept: text/markdown` header (or appends `.md` to a URL path), the web server returns the LLM-optimized Markdown instead of HTML. This is how [elastic.co/docs](https://www.elastic.co/docs/) serves documentation to AI agents — they receive clean, token-efficient Markdown rather than raw HTML, significantly reducing context window usage.

## Output

The exporter generates:

- **Per-page `.md` files** — one for every HTML page, placed alongside it in the output directory. For `section/page.html` → `section/page.md`. For `section/index.html` → `section.md` (so appending `.md` to the URL path works naturally).
- **`llms.txt`** — an index file following the [llms.txt specification](https://llmstxt.org/) that lists all available pages with titles, descriptions, and URLs. When `ASSEMBLER_API_EXPLORER` is on, the assembler appends an **APIs** section that links to each published API landing `.md` file.
- **`docs/api/llms.txt`** — a hub index of those same API landing pages. The assembler writes this file next to the API catalog when the flag is on.
- **`llm.zip`** — a compressed archive containing `llms.txt` and all per-page Markdown files for bulk download.

## What changes from source

The LLM Markdown is not the raw source — it is a rendered, simplified representation:

- Substitution variables are resolved to their values
- MyST directives are simplified to standard Markdown equivalents
- Code blocks, tables, and list structure are preserved
- Navigation-only elements (TOC references, page cards) are stripped
- Applies-to metadata is included as readable frontmatter context

Each exported page includes structured YAML frontmatter with:

```yaml
---
title: Page title
description: Auto-generated or authored description
url: https://www.elastic.co/docs/path/to/page
products:
  - Elasticsearch
  - Kibana
applies_to:
  - "Elastic Cloud Hosted: Available since 8.0"
---
```

API Explorer pages use the same `title`, `description`, `url`, and `products` keys. They also set `type: api` and `resource` (the same URI as `url`) so an OKF consumer can read the file as a concept.

## Content negotiation

Web servers (like the one serving elastic.co/docs) can use the co-located `.md` files to implement content negotiation:

- Browser requests → serve HTML as usual
- Agent requests with `Accept: text/markdown` → serve the `.md` file
- Programmatic requests appending `.md` to the path → serve the `.md` file

This gives AI agents and LLMs the documentation content with as few tokens as possible, in a format optimized for their consumption — not simply the raw Markdown source.
