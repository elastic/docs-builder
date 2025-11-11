# Visual Walkthrough

This visual guide shows how documentation navigation works in practice. We'll use diagrams to show how the same content appears differently in isolated vs assembler builds.

> **Best for:** First-time readers who want to understand navigation visually before diving into details.

## Navigation Node Icons

These icons represent different parts of the navigation tree:

- ![DocumentationSetNavigation](images/bullet-documentation-set-navigation.svg) **DocumentationSetNavigation** - Root of a repository
- ![TableOfContentsNavigation](images/bullet-table-of-contents-navigation.svg) **TableOfContentsNavigation** - A `toc.yml` section
- ![FolderNavigation](images/bullet-folder-navigation.svg) **FolderNavigation** - A directory
- ![FileNavigationLeaf](images/bullet-file-navigation-leaf.svg) **FileNavigationLeaf** - A markdown file
- ![SiteNavigation](images/bullet-site-navigation.svg) **SiteNavigation** - Root of assembled site

## Isolated Builds

Building a single repository (e.g., `docs-builder isolated build`):

**Example docset.yml:**
```yaml
project: elastic-project
toc:
  - file: index.md
  - toc: api           # api/toc.yml
  - toc: guides        # guides/toc.yml
```

### Visual: Isolated Build Tree

![Isolated Build](images/isolated-build-tree.svg)

**What the diagram shows:**

- ![DocumentationSetNavigation](images/bullet-documentation-set-navigation.svg) `elastic-project://` - Repository root
- ![TableOfContentsNavigation](images/bullet-table-of-contents-navigation.svg) `api/` and `guides/` - Sections from `toc.yml` files
- ![FileNavigationLeaf](images/bullet-file-navigation-leaf.svg) Individual files under each section
- URLs: `/api/overview/`, `/guides/getting-started/`

**Key points:**
- One repository = one navigation tree
- URLs default to `/` (configurable with `--url-path-prefix`)
- Fast for testing and iteration

---

## Assembler Builds

Combining multiple repositories into one site (e.g., `docs-builder assemble`):

**Example: Split One Repository Across Site**

Take the same `elastic-project` from above and split it:

```yaml
# navigation.yml
toc:
  - toc: elastic-project://api
    path_prefix: elasticsearch/api

  - toc: elastic-project://guides
    path_prefix: elasticsearch/guides
```

**Result:**
- `/api/` → `/elasticsearch/api/`
- `/guides/` → `/elasticsearch/guides/`

Same content, different URLs!

### Visual: Assembler Build Tree

![Assembler Build](images/assembler-build-tree.svg)

**What the diagram shows:**

- ![SiteNavigation](images/bullet-site-navigation.svg) `site://` - Root of entire assembled site
- ![TableOfContentsNavigation](images/bullet-table-of-contents-navigation.svg) `elastic-project://api` - Re-homed from `/api/` to `/elasticsearch/api/`
- ![TableOfContentsNavigation](images/bullet-table-of-contents-navigation.svg) `elastic-project://guides` - Re-homed from `/guides/` to `/elasticsearch/guides/`
- ![FileNavigationLeaf](images/bullet-file-navigation-leaf.svg) Files automatically use new prefixes

**Key points:**
- Multiple repositories → one site
- Custom URL prefixes via `path_prefix`
- Re-homing changes URLs without rebuilding

> **How re-homing works:** See [Assembler Process](assembler-process.md) for the four-phase assembly process and [Home Provider Architecture](home-provider-architecture.md) for how URLs update instantly (O(1)).

---

## Same File, Different URLs

**File:** `elastic-project/api/overview.md`

**Isolated Build:**
```
URL: /api/overview/
```

**Assembler Build:**
```
URL: /elasticsearch/api/overview/
```

Same file, same tree structure, different URL prefix. The assembler re-homes the navigation subtree without rebuilding it.

---

## Common Assembly Patterns

**Keep repository together:**
```yaml
- toc: elastic-project://
  path_prefix: elasticsearch
```
→ Everything under `/elasticsearch/`

**Split repository apart:**
```yaml
- toc: elastic-project://api
  path_prefix: reference/api
- toc: elastic-project://guides
  path_prefix: learn/guides
```
→ API under `/reference/api/`, guides under `/learn/guides/`

**Combine multiple repositories:**
```yaml
- toc: clients
  children:
    - toc: java-client://
      path_prefix: clients/java
    - toc: dotnet-client://
      path_prefix: clients/dotnet
```
→ All clients under `/clients/`

---

## What You Can Reference

In `navigation.yml`, you can reference:

**Entire repositories:**
```yaml
- toc: elastic-project://
  path_prefix: elasticsearch
```

**Individual TOC sections:**
```yaml
- toc: elastic-project://api
  path_prefix: elasticsearch/api
```

**You cannot reference:**
- Individual files
- Folders

Files and folders are automatically included as children of their parent TOC.

> **Node type details:** See [Node Types](node-types.md) for complete reference on each navigation node type.

---

## Phantom Nodes

Acknowledge content exists without including it in the site:

```yaml
# navigation.yml
phantoms:
  - source: plugins://
  - source: cloud://monitoring
```

**Use for:**
- Work-in-progress content
- Legacy content being phased out
- External content that's cross-linked but not hosted

This prevents "undeclared navigation" warnings.

---

## Summary

**Isolated builds:** One repository → one navigation tree (default prefix `/`)

**Assembler builds:** Multiple repositories → one site with custom URL prefixes

**The key insight:** Same navigation structure, different URLs. Re-homing changes URL prefixes without rebuilding the tree.

---

## Learn More

- **[First Principles](first-principles.md)** - Core design decisions
- **[Assembler Process](assembler-process.md)** - Four-phase assembly explained
- **[Home Provider Architecture](home-provider-architecture.md)** - How O(1) re-homing works
- **[Node Types](node-types.md)** - Complete reference for each node type
- **[Two-Phase Loading](two-phase-loading.md)** - Configuration vs navigation construction
