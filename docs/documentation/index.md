---
navigation_title: Documentation Builds
---

# Documentation builds

{{dbuild}} is a distributed documentation build system. Rather than pulling all content into a single repository, each team maintains documentation in their own repo and {{dbuild}} combines the results into cohesive documentation sites.

## Build modes

### Isolated builds

:::{page-card} [Isolated builds](./isolated/index.md)
Build a single repository's documentation for local development, PR previews, or publishing as a standalone static site (e.g. to GitHub Pages). The baseline that assembler and codex extend — content-set and page configuration apply here.
:::

### Distributed builds

Assembler and codex builds compose over many isolated builds — each repository is built independently, then the results are combined into a unified experience.

:::{page-card} [Assembler builds](./assembler/index.md)
Compose a unified documentation website with global navigation across multiple repositories. Used for public documentation sites like elastic.co/docs.
:::

:::{page-card} [Codex builds](./codex/index.md)
Create knowledge base environments where multiple repositories publish independently under a shared domain. Simpler setup with no centralized navigation composition.
:::

## How distributed builds work

All three build modes share the same [syntax](../syntax/index.md) foundations. A repository's `docset.yml` defines its documentation set, and the same Markdown content renders identically regardless of build mode.

Cross-repository linking is what makes distributed builds possible. When a repository builds, it publishes a **link index** — a manifest of every page and anchor it contains. Other repositories validate their cross-links against these published indexes, ensuring references stay correct even though repositories build independently.

Learn more about the [link infrastructure](./distributed-builds.md).
