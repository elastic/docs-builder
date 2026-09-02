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

### Deep-linked `index.md` files

A `file` entry without `children` that points at an `index.md` inside a subdirectory is treated as a single-page subsection for that directory:

```yaml
toc:
  - file: reference/1password/index.md
  - file: reference/activemq/index.md
```

Each entry becomes its own subsection linking to the directory's `index.md`. This is shorthand for the explicit `folder:` + `file:` form, which remains the preferred approach for folders that also have additional children:

```yaml
toc:
  - folder: reference/1password
    file: index.md
  - folder: reference/activemq
    file: index.md
```

The shorthand also works inside a parent folder's `children` list, which is the common case for a directory of single-page subsections (for example, dozens of integrations under `reference`):

```yaml
toc:
  - folder: reference
    file: index.md
    children:
      - file: 1password/index.md
      - file: activemq/index.md
```

Each child becomes its own subsection; none is consumed as the parent's index page.

If the entry declares its own `children`, it keeps the virtual grouping behavior instead.

### Content stored outside the documentation set

Add `source:` to read a page from elsewhere in the repository while keeping its place in the documentation set. This lets a page live next to the code it documents without moving the docset root:

```yaml
toc:
  - file: index.md
  - file: feedback.md
    source: ../packages/kbn-ui/feedback/feedback.md
```

`file:` remains the page's docset-relative position and drives the URL (`/feedback`), the navigation entry and the link reference other repositories resolve against. `source:` is resolved relative to the directory holding the `docset.yml` or `toc.yml` that declares the entry, and only determines which file is read.

Single markdown files only, and the source must resolve outside the documentation set root but inside the repository checkout. See the [`source` reference](./configure/index.md#source) for the full set of rules.

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
