---
navigation_title: Environments
---

# Environments

Assembler builds support multiple **publish environments** (e.g. dev, edge, staging, production), each tied to a **content source** phase that maps to a specific git branch, tag, or ref. This enables publication workflows where documentation can be previewed internally before going live.

## Publish environments

Environments are defined in `assembler.yml`. Each environment specifies a URI, path prefix, and which content source phase it consumes:

```yaml
environments:
  prod:
    uri: https://www.elastic.co
    path_prefix: docs
    content_source: current
    allow_indexing: true
  staging:
    uri: https://staging-website.elastic.co
    path_prefix: docs
    content_source: current
  edge:
    uri: https://edge-website.elastic.co
    path_prefix: docs
    content_source: next
```

| Field | Description |
|-------|-------------|
| `uri` | The base URL for the environment |
| `path_prefix` | URL path prefix for all documentation |
| `content_source` | Which content source phase to use (`current` or `next`) |
| `allow_indexing` | Whether search engines should index this environment |

## Content sources

A **content source** defines which git ref (branch or tag) a repository publishes from. Each repository in `assembler.yml` declares its own phases:

| Phase | Description | Default |
|-------|-------------|---------|
| `current` | The actively published documentation. Used by production. | `main` |
| `next` | Upcoming documentation. Used by edge/preview environments. | `main` |

By default, both phases point to `main` — this is the [continuous deployment](#continuous-deployment-default) workflow. Repositories that need to publish from a version branch can override these individually:

```yaml
references:
  elasticsearch:
    current: "9.0"
    next: main
```

## Branching strategies

Each repository chooses a branching strategy that determines how its content maps to the `current` and `next` phases.

### Continuous deployment (default)

The repository's `main` branch is continuously deployed to production. This is the simplest strategy:

- `current` → `main`
- `next` → `main`

No special branch management is needed. Every merge to `main` is published within minutes.

**Best for:** repositories where `main` always reflects the latest released state, or where unreleased changes are gated behind `applies_to` version badges.

### Tagged

A specific branch is tagged as `current`, giving you control over when new documentation goes live:

- `current` → a version branch (e.g. `9.0`)
- `next` → `main` (or the next version branch)

```yaml
references:
  elasticsearch:
    current: "9.0"
    next: main
```

**Best for:** repositories where `main` contains unreleased documentation that should not be published yet.

### Choosing a strategy

You don't need tagged branching to gate unreleased content. With continuous deployment, you can use [cumulative documentation](https://www.elastic.co/docs/contribute-docs/how-to/cumulative-docs) and `applies_to` version badges to mark content as `Planned` until the associated version is released.

Choose tagged branching only when:
- `main` regularly contains documentation for unreleased features that cannot be adequately gated with version badges
- Your release cadence requires strict control over when documentation changes go live

## CI configuration for tagged branching

Repositories using the tagged strategy need branch triggers in their CI workflows so version branches build and publish link indexes:

```yaml
name: docs-build

on:
  pull_request:
    types: [opened, synchronize, reopened]
  push:
    branches:
      - main
      - '\d+.\d+'
  merge_group: ~

jobs:
  build:
    uses: elastic/docs-actions/.github/workflows/docs-build.yml@v1
    with:
      path-pattern: docs/**
      use-release-branches: true
```

The `use-release-branches: true` flag ensures version branch pushes still publish `links.json` when needed.

## Switching strategies

Changing a repository's branching strategy is a long-term decision that impacts all documentation in that repository. The process involves:

1. Update CI workflows to include version branch triggers
2. Open a PR to `assembler.yml` specifying `current` and `next` branches
3. Verify link indexes are publishing for the new branches
4. Merge and release

Coordinate with the documentation team before switching strategies.
