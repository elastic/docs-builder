---
navigation_title: Documentation set navigation
---

# Documentation set navigation

**Documentation set navigation** defines how files within a single documentation set are organized and structured. Each documentation set is responsible for its own internal navigation hierarchy.

## Purpose

Documentation set navigation allows repository maintainers to:

* **Organize content** - Define the logical structure of their documentation
* **Control hierarchy** - Determine which pages are nested under others
* **Create sections** - Group related content together
* **Maintain autonomy** - Structure documentation independently of other repositories

## Basic structure

Navigation is defined in the `toc` (table of contents) section of the `docset.yml` file:

```yaml
toc:
  - file: index.md
  - folder: contribute
    children:
      - file: index.md
      - file: locally.md
        children:
          - file: page.md
```

## TOC node types

The `toc` section supports several node types:

### File nodes

Reference a single markdown file:

```yaml
- file: getting-started.md
```

Files can also have nested children:

```yaml
- file: locally.md
  children:
    - file: page.md
```

### Folder nodes

Group related files together:

```yaml
- folder: configuration
  children:
    - file: index.md
    - file: basic.md
```

`children` is optional for `folder` nodes. This will include all files in the folder.
This is especially useful during development when you are unsure how to structure your documentation.

Once `children` is defined, it must reference all `.md` files in the folder. The build will fail if 
it detects any dangling documentation files.

### Hidden files

Hide pages from navigation while keeping them accessible:

```yaml
- hidden: deprecated-page.md
```

### Named TOC sections

For larger documentation sets, create named TOC sections that can be referenced in [global navigation](global-navigation.md):

```yaml
toc:
  - file: index.md
  - toc: development
```

This will include the `toc` section defined in `development/toc.yml`:`

## Dedicated toc.yml files

When a `toc` section becomes too unwieldy, folders can define a dedicated `toc.yml` file to organize their files and link them in their parent `toc.yml` or `docset.yml` file.

### Example with nested TOC

In your `docset.yml`:

```yaml
toc:
  - file: index.md
  - folder: contribute
    children:
      - file: index.md
      - file: locally.md
        children:
          - file: page.md
  - toc: development
```

Then create `development/toc.yml`:

```yaml
toc:
  - file: index.md
  - toc: link-validation
```

:::{note}
The folder name `development` is not repeated in the `toc.yml` file. This allows for easier renames of the folder itself.
:::

### Benefits of separate toc.yml files

* **Modularity** - Each section can be maintained independently
* **Cleaner docset.yml** - Keep the main file focused and readable
* **Easier refactoring** - Rename folders without updating TOC references
* **Team ownership** - Different teams can manage different TOC sections

## File paths

All file paths in the `toc` section are relative to the documentation set root (where `docset.yml` is located):

```yaml
toc:
  - file: index.md                    # docs/index.md
  - folder: api
    children:
      - file: index.md                # docs/api/index.md
      - file: authentication.md       # docs/api/authentication.md
```

## Navigation metadata

You can customize how items appear in the navigation:

### Custom titles

The navigation title defaults to a markdown's page first `h1` heading. 

To present the file differently in the navigation, add a `navigation_title` metadata field.

```markdown
---
title: Getting Started with the Documentation Builder
navigation_title: Quick Start
---
```

There is no way to set `title` in the `docset.yml` file. This is by design to keep a page's data 
contained in its file.

## Relationship to global navigation

When building [assembled documentation](assembled-documentation.md), the documentation set navigation becomes a component of the [global navigation](global-navigation.md):

* **Documentation set navigation** defines the structure **within** a repository
* **Global navigation** defines **how repositories are organized** relative to each other

Named `toc` sections in `docset.yml` can be referenced and reorganized in the global `navigation.yml` file without affecting the documentation set's internal structure.

## Best practices

### Keep it organized

* Group related content in folders
* Use descriptive folder and file names
* Maintain a logical hierarchy

The folder names and hierarchy are reflected directly in the URL structure.

### Use index files

Always include an `index.md` in folders:

```yaml
- folder: api
  children:
    - file: index.md      # Overview of API documentation
    - file: endpoints.md
    - file: authentication.md
```

### Limit nesting depth

Avoid deeply nested structures (more than three to four levels) to maintain navigation clarity.

### Use toc.yml for large sections

When a section contains many files or becomes complex, extract it to a dedicated `toc.yml`:

```
docs/
├── docset.yml
├── index.md
└── development/
    ├── toc.yml           # Define development section structure here
    ├── index.md
    └── link-validation/
        └── toc.yml       # Nested TOC section
```

### Name TOC sections meaningfully

Use clear, descriptive names for TOC sections:

**Good:**
```yaml
- toc: api-reference
- toc: getting-started
- toc: troubleshooting
```

**Bad:**
```yaml
- toc: section1
- toc: misc
- toc: other
```

These names will end up in the URL structure of the published documentation

## Related concepts

* [Global Navigation](global-navigation.md) - How documentation sets are organized in assembled documentation
* [Content Set Configuration](../configure/content-set/index.md) - Complete `docset.yml` reference
* [Navigation Configuration](../configure/content-set/navigation.md) - Detailed navigation options
