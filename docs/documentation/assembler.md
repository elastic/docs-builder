---
navigation_title: Assembler builds
---

# Assembler builds

Assembler builds compose a unified documentation website by combining individual repositories' content under a single global navigation. The output is a complete documentation site — like [elastic.co/docs](https://www.elastic.co/docs) — where content from many repos appears as one cohesive experience.

## How it works

An assembler build follows these steps:

1. **Clone** — fetch all configured documentation repositories
2. **Build** — process each repository's docset independently
3. **Assemble** — compose the global navigation over all docsets
4. **Deploy** — publish the unified site

Each repository defines its own content and local table of contents via `docset.yml` and `toc.yml`. The assembler overlays a global navigation structure that ties everything together.

## Key features

- **Global navigation** — a unified nav tree spanning all repositories, defined centrally
- **Redirect controls** — tighter redirect management for public-facing sites
- **Versioning** — support for versioned documentation across products
- **Cross-repo linking** — validated links between repositories using the [link index](../architecture/link-index.md)

## Configuration

Assembler builds use several configuration files to define the site structure:

| File | Purpose |
|------|---------|
| `assembler.yml` | Declares which repositories to include and how to clone them |
| `navigation.yml` | Defines the global navigation tree |
| `versions.yml` | Configures version sets for versioned documentation |
| `products.yml` | Maps product metadata used in navigation and filtering |

See [site configuration](./configure/site/index.md) for details on each file.

## CLI commands

```bash
# Clone all configured repositories
docs-builder assembler clone

# Build the assembled site
docs-builder assembler build

# Serve the assembled site locally
docs-builder assembler serve
```

## Architecture

For a deeper dive into how assembler builds work internally, see [assembled documentation architecture](../architecture/assembled-documentation.md).
