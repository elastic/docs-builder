---
navigation_title: Navigation
---

# Navigation

Navigation in {{dbuild}} is defined through the `toc:` section in your `docset.yml` or in separate `toc.yml` files. The table of contents controls which pages appear in the sidebar and in what order.

For the complete reference of all `toc:` keys, see the [docset.yml reference](./configure/index.md).

## Basic structure

```yaml
toc:
  - file: index.md
  - file: getting-started.md
  - folder: guides
    children:
      - file: index.md
      - file: quickstart.md
```

## Navigation title

By default, the sidebar uses the page's `# Heading`. To show a shorter label, add `navigation_title` frontmatter:

```markdown
---
navigation_title: Quick start
---

# Getting started with docs-builder in 5 minutes
```

## Splitting large navigation

For larger documentation sets, split navigation into separate `toc.yml` files:

```yaml
# docset.yml
toc:
  - file: index.md
  - toc: guides
  - toc: reference
```

```yaml
# guides/toc.yml
toc:
  - file: index.md
  - file: quickstart.md
```

## Common patterns

### Single file reference

```yaml
toc:
  - file: index.md
  - file: getting-started.md
  - file: api-reference.md
```

### File with children (virtual grouping)

Group related sibling files under a parent without creating a folder:

```yaml
toc:
  - file: getting-started.md
    children:
      - file: installation.md
      - file: configuration.md
```

All children must be siblings of the parent file (same directory).

### Folder without explicit children

Auto-include all markdown files in a folder. Useful during development:

```yaml
toc:
  - folder: api
```

### Folder with explicit children

Define exact files and ordering:

```yaml
toc:
  - folder: api
    children:
      - file: index.md
      - file: authentication.md
      - file: endpoints.md
```

When `children` is defined, all markdown files in the folder must be listed.

### Nested toc reference

Include a dedicated `toc.yml` for large sections:

```yaml
toc:
  - file: index.md
  - toc: api-reference
  - toc: tutorials
```

### Mixed patterns

Combine patterns as needed:

```yaml
toc:
  - file: index.md
  - file: quick-start.md
  - folder: guides
    children:
      - file: index.md
      - file: installation.md
  - toc: api-reference
  - folder: troubleshooting
```
