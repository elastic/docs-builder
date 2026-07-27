---
navigation_title: Navigation
---

# Navigation

Navigation in {{dbuild}} is defined through the `toc:` section in your `docset.yml` or in separate `toc.yml` files. The table of contents controls which pages appear in the sidebar and in what order.

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

## Key concepts

- **`file:`** — adds a single page to the nav
- **`folder:`** — groups pages under a collapsible section
- **`toc:`** — references a separate `toc.yml` file for modularity
- **`hidden:`** — includes a page in the build but hides it from the nav

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

For the complete reference of all navigation options, patterns, and validation rules, see the [docset.yml reference](./configure/content-set/navigation.md).
