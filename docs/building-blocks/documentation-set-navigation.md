---
navigation_title: Documentation set navigation
---

# Documentation set navigation

**Documentation set navigation** defines how files within a single documentation set are organized and structured. Each documentation set is responsible for its own internal navigation hierarchy.

## Purpose

Documentation set navigation allows repository maintainers to:

* **Organize content**: Define the logical structure of their documentation.
* **Control hierarchy**: Determine which pages are nested under others.
* **Create sections**: Group related content together.
* **Maintain autonomy**: Structure documentation independently of other repositories.

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

* **Modularity**: Each section can be maintained independently.
* **Cleaner docset.yml**: Keep the main file focused and readable.
* **Easier refactoring**: Rename folders without updating TOC references.
* **Team ownership**: Different teams can manage different TOC sections.

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

* **Documentation set navigation** defines the structure **within** a repository.
* **Global navigation** defines **how repositories are organized** relative to each other.

Named `toc` sections in `docset.yml` can be referenced and reorganized in the global `navigation.yml` file without affecting the documentation set's internal structure.

## Common navigation patterns

Understanding the different ways to structure navigation helps you choose the right pattern for your documentation. Each pattern serves a specific purpose and has its own trade-offs.

### Pattern: Single file

The simplest pattern - just reference individual markdown files:

```yaml
toc:
  - file: index.md
  - file: getting-started.md
  - file: faq.md
```

**When to use**: For flat documentation with few pages or when pages don't naturally group together.

**Advantages**: Simple and explicit. No hidden files or automatic inclusion.

**Considerations**: Doesn't scale well for large documentation sets.

### Pattern: File with children (virtual grouping)

Group related files under a parent without creating a physical folder structure:

```yaml
toc:
  - file: getting-started.md
    children:
      - file: installation.md
      - file: configuration.md
      - file: first-steps.md
```

**When to use**: When you want to create a logical grouping without reorganizing files on disk, typically for sibling files in the same directory.

**Advantages**: Creates navigation hierarchy without changing the file system. Useful for grouping related pages that share a parent.

**Considerations**: Children must be siblings of the parent file. The parent can't select files from different directories. Avoid deep-linking (using paths with `/` in the file reference when it has children) - the builder will emit hints suggesting you use folder structures instead.

**Example scenario**: You have several setup guides at the root level that you want to group under a "Getting Started" parent page:

```
docs/
├── getting-started.md
├── installation.md
├── configuration.md
└── first-steps.md
```

### Pattern: Folder without children

Let the builder automatically include all markdown files in a folder:

```yaml
toc:
  - folder: tutorials
  - folder: api
```

**When to use**: During active development when content is still evolving, or for folders where file order doesn't matter.

**Advantages**: Zero maintenance - new files are automatically included. Perfect for development.

**Considerations**: No control over file order. All markdown files in the folder will be included.

### Pattern: Folder with explicit children

Define exactly which files appear and in what order:

```yaml
toc:
  - folder: api
    children:
      - file: index.md
      - file: authentication.md
      - file: endpoints.md
      - file: errors.md
```

**When to use**: When file order matters or when you need precise control over what's included.

**Advantages**: Complete control over structure and ordering. The builder validates that all files are accounted for.

**Considerations**: Requires maintenance. The builder will error if files exist in the folder that aren't listed in children.

### Pattern: Folder with entry file

Combine a folder reference with a specific entry file:

```yaml
toc:
  - folder: getting-started
    file: getting-started.md
    children:
      - file: prerequisites.md
      - file: installation.md
```

**When to use**: When you want a folder with a main overview file that's not named `index.md`.

**Advantages**: Clear entry point. Works well when the folder name and overview file name match.

**Considerations**: The builder will hint if the file name doesn't match the folder name (unless you use `index.md`). This pattern works best when names align:

**Good examples**:
```yaml
# File name matches folder name
- folder: getting-started
  file: getting-started.md

# Using index.md always works
- folder: api-reference
  file: index.md
```

**Triggers hints**:
```yaml
# File name doesn't match folder name
- folder: getting-started
  file: overview.md  # Hint: Consider naming this getting-started.md
```

### Pattern: Nested toc references

Split large documentation into separate `toc.yml` files:

**In `docset.yml`**:
```yaml
toc:
  - file: index.md
  - toc: getting-started
  - toc: api-reference
  - toc: guides
```

**In `getting-started/toc.yml`**:
```yaml
toc:
  - file: index.md
  - file: installation.md
  - file: configuration.md
```

**When to use**: For large documentation sets, when different teams own different sections, or when you want to keep `docset.yml` focused and readable.

**Advantages**: Modularity. Each section can evolve independently. Easier folder renames (the folder name isn't repeated in its own toc.yml). Better for team ownership.

**Considerations**: `toc.yml` files can't nest other `toc.yml` files - only `docset.yml` can reference them.

**Example scenario**: You're building product documentation with multiple major sections:

```
docs/
├── docset.yml
├── index.md
├── getting-started/
│   ├── toc.yml
│   └── ...
├── api-reference/
│   ├── toc.yml
│   └── ...
└── guides/
    ├── toc.yml
    └── ...
```

### Mixing patterns

In practice, you'll combine patterns based on your needs:

```yaml
toc:
  - file: index.md                    # Single file
  - file: quick-start.md              # Single file
  - folder: tutorials                 # Auto-include during development
  - folder: api
    children:                         # Explicit control for stability
      - file: index.md
      - file: authentication.md
  - toc: guides                       # Large section in separate file
```

## Suppressing diagnostic hints

As you build navigation, the docs-builder may emit hints suggesting improvements to your structure. These hints help maintain best practices but can be suppressed when you have valid reasons to deviate.

### Available suppressions

Add a `suppress` section to either `docset.yml` or `toc.yml`:

```yaml
suppress:
  - DeepLinkingVirtualFile
  - FolderFileNameMismatch

toc:
  - file: index.md
  # ... rest of your navigation
```

### DeepLinkingVirtualFile

**What it detects**: Files with children that use paths containing `/`:

```yaml
toc:
  - file: guides/advanced/performance.md
    children:
      - file: guides/advanced/caching.md
      - file: guides/advanced/optimization.md
```

**Why it hints**: Virtual files (files with children) work best for grouping sibling files together. Using deep paths suggests you might benefit from proper folder structures.

**When to suppress**: Rarely. This usually indicates a structural issue. Consider refactoring to use folders or nested toc files instead.

**Better alternative**:
```yaml
toc:
  - folder: guides
    children:
      - folder: advanced
        children:
          - file: index.md
          - file: performance.md
            children:
              - file: caching.md
              - file: optimization.md
```

### FolderFileNameMismatch

**What it detects**: Folder and file combinations where names don't match:

```yaml
toc:
  - folder: getting-started
    file: overview.md      # Doesn't match folder name
```

**Why it hints**: Matching names create predictable, consistent navigation. When a folder is named "getting-started," readers expect the main file to be either `getting-started.md` or `index.md`.

**When to suppress**: When you have legacy documentation with established naming conventions, or when the file name is intentionally different for clarity.

**Better alternatives**:
```yaml
# Option 1: Match the names
- folder: getting-started
  file: getting-started.md

# Option 2: Use index.md (conventional and always appropriate)
- folder: getting-started
  file: index.md

# Option 3: Just use folder with children
- folder: getting-started
  children:
    - file: index.md
    - file: prerequisites.md
```

### When to use suppressions

Suppressions are escape hatches, not defaults. Use them when:

* **Migrating legacy content**: Existing documentation has established patterns that can't be changed immediately
* **Valid architectural reasons**: Your specific use case genuinely benefits from the flagged pattern
* **Temporary transitions**: You're in the middle of restructuring and need to suppress hints during the migration

**Example of justified suppression**:
```yaml
# This section uses an established URL structure we can't change
# without breaking external links. Suppressing the hint until we
# can implement proper redirects.
suppress:
  - FolderFileNameMismatch

toc:
  - folder: install
    file: setup.md  # External links point to /install/setup
    children:
      - file: prerequisites.md
```

## Best practices

### Keep it organized

* **Group related content** in folders that reflect logical sections
* **Use descriptive names** - folder and file names become URLs
* **Maintain hierarchy** - think about how users navigate from general to specific

The folder names and hierarchy translate directly to URL structure. `folder: api/authentication` becomes `/docs/api/authentication/` in the browser.

### Start simple, evolve structure

Begin with automatic folder inclusion during development:

```yaml
toc:
  - file: index.md
  - folder: guides        # Auto-includes everything
  - folder: api           # Auto-includes everything
```

As content stabilizes, add explicit children for control:

```yaml
toc:
  - file: index.md
  - folder: guides
    children:             # Now you control the order
      - file: index.md
      - file: getting-started.md
      - file: advanced-topics.md
  - toc: api              # Extract to separate toc.yml
```

### Use index files consistently

Every folder should have an `index.md` that introduces the section:

```yaml
- folder: api
  children:
    - file: index.md        # Overview of the API section
    - file: authentication.md
    - file: endpoints.md
```

The index file provides context before users dive into specific topics. It's also what users see when they navigate to `/docs/api/`.

### Limit nesting depth

Deep navigation hierarchies overwhelm readers. Aim for three to four levels maximum:

**Good** (3 levels):
```
Documentation
  └── Guides
      └── Installation
          └── Prerequisites
```

**Too deep** (6 levels):
```
Documentation
  └── Guides
      └── Getting Started
          └── Installation
              └── Linux
                  └── Ubuntu
                      └── Prerequisites
```

If you need more depth, consider splitting into separate documentation sets or using virtual file grouping for minor subdivisions.

### Extract large sections to toc.yml

When a section grows beyond 5-10 files or has its own internal structure, move it to a dedicated `toc.yml`:

```
docs/
├── docset.yml          # High-level structure only
├── index.md
└── api-reference/
    ├── toc.yml         # API section structure
    ├── index.md
    └── ...
```

**Benefits**:
* Keeps `docset.yml` focused on top-level organization
* Teams can own their section's navigation
* Easier to refactor individual sections
* Folder renames don't require updating the toc.yml (since the folder name isn't repeated inside it)

### Name TOC sections meaningfully

TOC section names become part of URLs and navigation structure:

**Good** (clear and descriptive):
```yaml
- toc: api-reference
- toc: getting-started
- toc: troubleshooting
- toc: user-guide
```

**Bad** (vague and uninformative):
```yaml
- toc: section1
- toc: misc
- toc: other
- toc: stuff
```

Choose names that:
* Describe the content clearly
* Work well in URLs (lowercase, hyphenated)
* Match user expectations

## Related concepts

* [Global Navigation](global-navigation.md) - How documentation sets are organized in assembled documentation.
* [Content Set Configuration](../configure/content-set/index.md) - Complete `docset.yml` reference.
* [Navigation Configuration](../configure/content-set/navigation.md) - Detailed navigation options.
