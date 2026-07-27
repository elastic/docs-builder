---
navigation_title: Isolated
---

# Isolated builds

An isolated build processes a single `docset.yml` — one documentation set from one repository. This is the default when you run {{dbuild}} locally.

An isolated build can be:

- **Published on its own** — as a standalone static site to GitHub Pages or any other host (see [Publish](../../getting-started/publish.md))
- **A building block** for a larger [assembler](../assembler/index.md) or [codex](../codex/index.md) build, where many isolated builds are composed into a unified experience

## Getting started

1. [Install docs-builder](../../getting-started/installation.md)
2. Create a `docs/` folder with a `docset.yml` and `index.md`
3. Run `docs-builder serve` — see [Serve and preview](../../getting-started/serve.md)

## Cross-link validation

Even in isolated mode, cross-repository links are validated against published link indexes from the [link service](../../development/link-infrastructure.md), ensuring your cross-references are valid without needing to clone other repositories locally.
