---
navigation_title: Assembler
---

# Assembler builds

Assembler builds compose a unified documentation website by combining many [isolated builds](../isolated/index.md) under a single global navigation. The output is a complete documentation site — like [elastic.co/docs](https://www.elastic.co/docs) — where content from many repos appears as one cohesive experience.

## How it works

1. **Clone** — fetch all configured documentation repositories
2. **Build** — process each repository's docset independently (each is an isolated build)
3. **Assemble** — compose the global navigation over all docsets
4. **Deploy** — publish the unified site

Each repository defines its own content and local table of contents via `docset.yml` and `toc.yml`. The assembler overlays a global navigation structure that ties everything together.

## Key features

- **Global navigation** — a unified nav tree spanning all repositories, defined centrally
- **Redirect controls** — tighter redirect management for public-facing sites
- **Versioning** — support for versioned documentation across products
- **Cross-repo linking** — validated links between repositories using the [link index](../../development/link-infrastructure.md)

## CLI commands

```bash
docs-builder assembler clone        # Clone all configured repositories
docs-builder assembler build        # Build the assembled site
docs-builder assembler serve        # Serve the assembled site locally
```

To build with a local configuration checkout:

```bash
docs-builder assembler config init --local
docs-builder assembler clone --local
docs-builder assembler build --local
```

## Architecture

For a deeper dive into how assembler builds work internally, see the [building blocks](../../development/building-blocks.md) overview.
