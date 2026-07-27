---
navigation_title: Documentation
---

# Documentation builds

{{dbuild}} is a distributed documentation build system. Rather than pulling all content into a single repository, each team maintains documentation in their own repo and {{dbuild}} combines the results into cohesive documentation sites.

## Build modes

{{dbuild}} supports three build modes, each designed for different publishing scenarios:

:::{page-card} [Assembler builds](./assembler.md)
Compose a unified documentation website with global navigation across multiple repositories. Used for public documentation sites like elastic.co/docs.
:::

:::{page-card} [Codex builds](./codex.md)
Create knowledge base environments where multiple repositories publish independently under a shared domain. Simpler setup with no centralized navigation composition.
:::

:::{page-card} [Isolated builds](./isolated.md)
Build a single repository's documentation for local development and PR previews. No assembly or global navigation — just one docset.
:::

## How distributed builds work

All three build modes share the same [syntax](../syntax/index.md) and [configuration](./configure/index.md) foundations. A repository's `docset.yml` defines its documentation set, and the same Markdown content renders identically regardless of build mode.

Cross-repository linking is what makes distributed builds possible. When a repository builds, it publishes a **link index** — a manifest of every page and anchor it contains. Other repositories validate their cross-links against these published indexes, ensuring references stay correct even though repositories build independently.

Learn more about the [link infrastructure](../development/link-infrastructure.md).
