---
navigation_title: "Navigation system"
---

# Navigation system

:::{warning}
Old development note — likely to be deleted or substantially rewritten.
:::

{{dbuild}} builds hierarchical navigation trees for documentation sites. The system supports two build modes — **isolated** (single repo) and **assembler** (multi-repo unified site) — using a two-phase loading process that separates configuration from URL calculation.

## Overview

Navigation transforms a YAML table-of-contents file (`toc.yml`) into a tree of nodes representing the site structure. Each node has a type (documentation set, folder, file, or virtual group) and a computed URL. The key design insight: URLs are never stored statically — they're calculated dynamically from a "home provider," which means the same tree can be re-rooted at any URL prefix without modification.

## Visual overview

### Isolated build

In an isolated build, a single repo's `toc.yml` produces a self-contained navigation tree:

:::{image} images/isolated-build-tree.svg
:alt: Isolated build navigation tree
:::

### Assembler build

In an assembler build, multiple repo trees are composed under a global site navigation:

:::{image} images/assembler-build-tree.svg
:alt: Assembler build navigation tree
:::

### Node types

The navigation tree uses distinct node types:

- ![Documentation set](images/bullet-documentation-set-navigation.svg) **Documentation set** — root of a docset's nav tree
- ![Table of contents](images/bullet-table-of-contents-navigation.svg) **Table of contents** — a `toc.yml` entry point
- ![Folder](images/bullet-folder-navigation.svg) **Folder** — groups pages without a landing page
- ![File (leaf)](images/bullet-file-navigation-leaf.svg) **File** — a markdown page (leaf node)
- ![Site navigation](images/bullet-site-navigation.svg) **Site navigation** — top-level assembler grouping

## Two-phase loading

Navigation loading is split into two distinct phases:

**Phase 1 — Configuration:** Parse `toc.yml`, resolve file paths relative to the docset root, and validate that referenced files exist. This phase produces a validated configuration tree but no URLs. Errors here are structural — missing files, invalid YAML, circular references.

**Phase 2 — Navigation:** Walk the configuration tree, assign a home provider, and compute URLs for every node. This phase transforms the static config into a live navigation tree with fully resolved paths. The separation means Phase 1 can run once while Phase 2 can re-run with different home providers (isolated vs. assembler context).

This two-phase design allows the same parsed configuration to produce different URL structures depending on the build context, without re-parsing or re-validating the source files.

## Re-homing

URLs in the navigation tree are not stored — they're **calculated** from a home provider attached to each subtree's root. The home provider defines the base path, and every descendant node derives its URL relative to that base.

Changing the home provider instantly updates all descendant URLs. This is an O(1) operation — no tree traversal needed — because URLs are computed lazily on access, not eagerly stored.

This is what makes assembler builds possible: each repo builds its navigation tree in isolation (with a `/` home), and the assembler re-homes each subtree to its target prefix (e.g., `/docs/elasticsearch/`) without touching the tree structure.

## Assembler process

The assembler composes navigation in four phases:

```mermaid
flowchart LR
    SiteConfig["Load site config"] --> BuildNav["Build each<br/>docset nav"]
    BuildNav --> ReHome["Re-home subtrees<br/>with path prefixes"]
    ReHome --> Validate["Validate<br/>(no dangling TOCs)"]
```

1. **Load site config** — read the assembler configuration defining which repos appear and their URL prefixes
2. **Build each docset nav** — run Phase 1 + Phase 2 for every repo's `toc.yml` independently
3. **Re-home subtrees** — attach each docset's nav tree under the global site tree with the configured path prefix
4. **Validate** — ensure no table-of-contents nodes are left dangling (every TOC reference must resolve to a real subtree)

The result is a single unified navigation tree that can be serialized for the frontend, with each subtree maintaining its original structure but now addressable under the global URL scheme.
