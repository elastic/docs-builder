---
navigation_title: Configuration
---

# Configuration

Codex builds require minimal configuration beyond the standard [docset.yml](../../isolated/configure/index.md) used by all build modes.

## environment.yml

Each codex environment is defined by an `environment.yml` (or `config.yml`) file that controls the environment's identity, groups, and settings:

```yaml
environment: my-environment
site_prefix: /

title: My Documentation Hub

groups:
  - id: platform
    name: "Platform Team"
    description: "Platform services documentation."
    icon: documentation
  - id: tools
    name: "Internal Tools"
    description: "Tooling and automation documentation."
    icon: asterisk
```

| Field | Description |
|-------|-------------|
| `environment` | Environment identifier. Repositories opt in by setting `registry: <environment>` in their `docset.yml`. |
| `site_prefix` | URL prefix for the entire codex site. Typically `/`. |
| `title` | Display title shown on the landing page. |
| `groups` | Optional list of groups for organizing repositories on the landing page. |

### Group configuration

| Field | Required | Description |
|-------|----------|-------------|
| `id` | Yes | Unique identifier. Used in URLs (`/g/{id}`) and referenced by docsets via `codex.group`. |
| `name` | Yes | Display name shown on the landing page card. |
| `description` | No | Short description shown on the card. |
| `icon` | No | Icon identifier for the group card. |

## Repository configuration

Each repository opts into a codex environment through its `docset.yml`:

```yaml
icon: asterisk
registry: my-environment

toc:
  - file: index.md
```

To join a group:

```yaml
icon: asterisk
registry: my-environment

codex:
  group: platform

toc:
  - file: index.md
```

### Repositories with multiple docsets

A repository can publish both a public docset (for elastic.co/docs) and a separate codex docset,
colocated in the same repository. For example, `docs/docset.yml` with no `registry` (public) and
`docs-dev/docset.yml` with `registry: my-environment`.

When discovering a repository's docset, codex prefers the docset whose `registry` matches the
codex `environment`, and only falls back to the default search order (`docs/docset.yml`,
`docs/_docset.yml`, `docset.yml`, `_docset.yml`, then a recursive search) when no docset declares
that registry. This lets a repository keep its internal documentation next to its code instead of
moving it to a separate repository.
