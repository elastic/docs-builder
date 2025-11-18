# Navigation

Two types of nav files are supported: `docset.yml` and `toc.yml`.

## `docset.yml`

Example:

```yaml
project: 'PROJECT_NAME'

exclude:
  - 'EXCLUDED_FILES'

toc:
  - file: index.md
  - toc: elastic-basics
  - folder: top-level-bucket-a
    children:
      - file: index.md
      - file: file-a.md
      - file: file-b.md
  - folder: top-level-bucket-b
    children:
      - file: index.md
      - folder: second-level-bucket-c
        children:
          - file: index.md
```

### `project`

The name of the project.

Example:

```yaml
project: 'APM Java agent reference'
```

### `cross_links`

Defines repositories that contain documentation sets you want to link to. The purpose of this feature is to avoid using absolute links that require time-consuming crawling and checking.

Consider a docset repository called `bazinga`. The following example adds three docset repositories to its `docset.yml` file:

```yaml
cross_links:
  - apm-server
  - cloud
  - docs-content
```

#### Adding cross-links in Markdown content

To link to a document in the `docs-content` repository, you would write the link as follows:

```markdown
[Link to docs-content doc](docs-content://directory/another-directory/file.md)
```

You can also link to specific anchors within the document:

```markdown
[Link to specific section](docs-content://directory/file.md#section-id)
```

#### Adding cross-links in navigation

Cross-links can also be included in navigation structures. When creating a `toc.yml` file or defining navigation in `docset.yml`, you can add cross-links as follows:

```yaml
toc:
  - file: index.md
  - title: External Documentation
    crosslink: docs-content://directory/file.md
  - folder: local-section
    children:
      - file: index.md
      - title: API Reference
        crosslink: elasticsearch://api/index.html
```

Cross-links in navigation will be automatically resolved during the build process, maintaining consistent linking between related documentation across repositories.

### `exclude`

Files to exclude from the TOC. Supports glob patterns.

The following example excludes all markdown files beginning with `_`:

```yaml
exclude:
  - '_*.md'
```

### `toc`

Defines the table of contents (navigation) for the content set. A minimal toc is:

```yaml
toc:
  - file: index.md
```

The TOC in principle follows the directory structure on disk.

#### `folder:`

```yaml
  ...
  - folder: subsection
```

If a folder does not explicitly define `children` all markdown files within that folder are included automatically 

If a folder does define `children` all markdown files within that folder have to be included. `docs-builder` will error if it detects dangling documentation files.

```yaml
  ...
  - folder: subsection
    children:
      - file: index.md
      - file: page-one.md
      - file: page-two.md
```

#### Virtual grouping

A `file` element may include children to create a virtual grouping that 
does not match the directory structure. 

```yaml
  ...
  - file: subsection/index.md
    children:
      - file: subsection/page-one.md
      - file: subsection/page-two.md
```

A `file` may only select siblings and more deeply nested files as its children. It may not select files outside its own subtree on disk.

#### Hidden files

A hidden file can be declared in the TOC. 
```yaml
  - hidden: developer-pages.md
```

It may not have any children and won't show up in the navigation.

It [may be linked to locally however](../../developer-notes.md)

#### Nesting `toc`

The `toc` key can include nested `toc.yml` files.

The following example includes two sub-`toc.yml` files located in directories named `elastic-basics` and `solutions`:

```yml
toc:
  - file: index.md
  - toc: elastic-basics
  - toc: solutions
```

### Attributes

Example:

```yml
subs:
  attr-name:   "attr value"
  ea:   "Elastic Agent"
  es:   "Elasticsearch"
```

See [Attributes](./attributes.md) to learn more.

### `suppress`

Optional list of diagnostic hint types to suppress for this navigation file. Available in both `docset.yml` and `toc.yml` files.

Valid suppression values:

- `DeepLinkingVirtualFile`
- `FolderFileNameMismatch`

Example:

```yaml
suppress:
  - DeepLinkingVirtualFile
  - FolderFileNameMismatch
```

#### `DeepLinkingVirtualFile`

Suppresses hints about files with children that use deep-linking (path contains `/`).

By default, the builder emits a hint when a file reference includes a path separator and has children:

```yaml
toc:
  - file: a/b/c/getting-started.md
    children:
      - file: a/b/c/setup.md
      - file: a/b/c/install.md
```

**Hint message**: "File 'a/b/c/getting-started.md' uses deep-linking with children. Consider using 'folder' instead of 'file' for better navigation structure. Virtual files are primarily intended to group sibling files together."

**Why this is discouraged**: Virtual files (files with children) are intended to group sibling files together, not to create deep navigation hierarchies. Using `folder` structures provides clearer organization.

**Preferred alternative**:

```yaml
toc:
  - folder: a
    children:
      - folder: b
        children:
          - folder: c
            children:
              - file: index.md
              - file: getting-started.md
                children:
                  - file: setup.md
                  - file: install.md
```

Or use nested `toc.yml` files for better maintainability.

#### `FolderFileNameMismatch`

Suppresses hints about file names not matching folder names in folder+file combinations.

By default, when using the folder+file pattern, the builder emits a hint if the file name doesn't match the folder name:

```yaml
toc:
  - folder: getting-started
    file: overview.md
```

**Hint message**: "File name 'overview.md' does not match folder name 'getting-started'. Best practice is to name the file the same as the folder (e.g., 'folder: getting-started, file: getting-started.md')."

**Why this is discouraged**: Mismatched names can cause confusion about which file represents the folder's main content. Consistent naming makes navigation structure more predictable.

**Preferred alternatives**:

```yaml
# Option 1: Match file name to folder name
toc:
  - folder: getting-started
    file: getting-started.md

# Option 2: Use index.md (always allowed)
toc:
  - folder: getting-started
    file: index.md

# Option 3: Use folder only with children
toc:
  - folder: getting-started
    children:
      - file: index.md
```

## Navigation configuration patterns

### Single file reference

Reference a standalone file:

```yaml
toc:
  - file: index.md
  - file: getting-started.md
  - file: api-reference.md
```

### File with children (virtual grouping)

Group related sibling files under a parent file without creating a folder on disk:

```yaml
toc:
  - file: getting-started.md
    children:
      - file: installation.md
      - file: configuration.md
      - file: first-steps.md
```

All children must be siblings of the parent file (in the same directory). The parent file may not select files outside its own subtree.

### Folder without explicit children

Include all markdown files in a folder automatically:

```yaml
toc:
  - folder: api
```

All `.md` files in the `api/` folder will be included automatically. This is useful during development when the structure is still evolving.

### Folder with explicit children

Define exact files and their order within a folder:

```yaml
toc:
  - folder: api
    children:
      - file: index.md
      - file: authentication.md
      - file: endpoints.md
      - file: errors.md
```

When `children` is defined, all markdown files in the folder must be listed. The builder will error on dangling documentation files.

### Folder and file combination

Specify both a folder and a file to create a folder with a non-index entry point:

```yaml
toc:
  - folder: getting-started
    file: overview.md
    children:
      - file: installation.md
      - file: configuration.md
```

Best practice: Use `index.md` or match the file name to the folder name to avoid `FolderFileNameMismatch` hints.

### Nested toc reference

Include a dedicated `toc.yml` file for large sections:

```yaml
toc:
  - file: index.md
  - toc: api-reference
  - toc: tutorials
  - toc: guides
```

Each `toc` reference loads the corresponding `toc.yml` file from that directory (e.g., `api-reference/toc.yml`).

### Mixed patterns

Combine different patterns as needed:

```yaml
toc:
  - file: index.md
  - file: quick-start.md
  - folder: guides
    children:
      - file: index.md
      - folder: installation
        file: installation.md
        children:
          - file: prerequisites.md
          - file: steps.md
  - toc: api-reference
  - folder: troubleshooting
```
