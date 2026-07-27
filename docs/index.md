---
navigation_title: docs-builder
---

# docs-builder

docs-builder is Elastic's distributed documentation platform. It processes Markdown from multiple repositories into unified documentation sites, validates cross-repo references, and ships as native AOT binaries for CI.

## What does docs-builder produce?

### Documentation builds

docs-builder builds documentation across many repositories independently, then assembles them into unified experiences. There are three build modes:

- **[Assembler builds](./documentation/assembler.md)** — Compose a global navigation over many repositories to produce a unified documentation site. Powers sites like [elastic.co/docs](https://www.elastic.co/docs/).
- **[Codex builds](./documentation/codex.md)** — Create knowledge base environments where repositories publish independently at `/r/<repo>`, optionally grouped under `/g/<group>`.
- **[Isolated builds](./documentation/isolated.md)** — Build a single repository's docs locally or as a PR preview.

Learn more about the [distributed documentation model](./documentation/index.md).

### Reference generation

- **[OpenAPI](./data/openapi/index.md)** — Generate interactive API reference documentation from OpenAPI specifications.
- **[CLI Schema](./data/cli-schema/index.md)** — Generate CLI reference documentation from [CLI Schema](https://cli-schema.org) files.

### Release notes

- **[Release notes](./data/release-notes/index.md)** — Create, bundle, and publish release documentation from structured changelog files.

### Docs as data

- **[Exporters](./data/exporters/index.md)** — Export documentation as Elasticsearch documents, LLM Markdown, OKF, and plain text.
- **[MCP server](./data/mcp/index.md)** — Let AI assistants and coding agents interact with Elastic documentation via the Model Context Protocol.
- **[REST API](./data/api.md)** — Search, ask questions, and query documentation changes programmatically.

### Integrations

- **[docs-actions](./integrations/docs-actions.md)** — Reusable GitHub Actions for documentation CI/CD.
- **[Elastic CLI](./integrations/elastic-cli.md)** — Search and read documentation from the `elastic` CLI.

## Get started

New to docs-builder? Start here:

- **[Getting started](./getting-started/index.md)** — Choose your path based on what you want to build.
- **[Installation](./getting-started/installation.md)** — Install the docs-builder CLI.

## Authoring & reference

- **[Syntax reference](./syntax/index.md)** — Markdown syntax with MyST directives and roles.
- **[Configuration](./documentation/configure/index.md)** — Site-level, content-set, and page-level configuration.
- **[How-to guides](./documentation/how-to/index.md)** — Guides for managing files, repositories, and releases.
- **[CLI reference](./cli/index.md)** — All docs-builder commands and options.

## Development

- **[Development guide](./development/index.md)** — Building blocks, link infrastructure, navigation system, and contributing to docs-builder.
