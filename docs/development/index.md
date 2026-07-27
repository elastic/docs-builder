---
navigation_title: Development Notes
---

# Development notes

:::{warning}
These are old development notes created during the initial development of {{dbuild}}. They are likely to be deleted or substantially rewritten. Do not rely on them as authoritative documentation.
:::

## Architecture

- **[Building blocks](./building-blocks.md)** — Conceptual overview of the documentation model: documentation sets, distributed builds, assembly, and link resolution.
- **[Link infrastructure](./link-infrastructure.md)** — The S3-backed link service, link indexes, link catalog, and cross-link validation.
- **[Navigation system](./navigation.md)** — How navigation trees are built, re-homed, and assembled across repositories.

## Subsystems

- **[Elasticsearch ingest](./ingest.md)** — The lexical and semantic indexing pipeline with hash-based change detection.
- **[essc](./essc.md)** — The AOT-compiled CLI for indexing elastic.co content (Contentstack, Labs) into Elasticsearch.
- **[Changelog bundle registry](./changelog-bundle-registry.md)** — CDN-based changelog bundle publishing, scrubbing, and the `{changelog}` directive's `cdn:` mode.
- **[Link validation](./link-validation.md)** — Cross-repository link validation infrastructure.
