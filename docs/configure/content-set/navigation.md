# Navigation

Two types of nav files are supported: `docset.yml` and `toc.yml`.

## `docset.yml`

Example:

```yaml
project: 'PROJECT_NAME'
soft_line_endings: true

external_hosts:
  - EXTERNAL_LINKS_HERE

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

### `soft_line_endings`

Optional key. Defaults to `false`. When enabled turns soft line endings in the markdown to hard HTML breaks `<br />`.


### `external_hosts`

% Or remove this section altogether?
```{applies_to}
product: deprecated 0.11.0
```

All links to external hosts must be declared in this section of `docset.yml`.

Example:

```yaml
external_hosts:
  - google.com
  - github.com
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

To link to a document in the `docs-content` repository, you would write the link as follows:

```
[Link to docs-content doc](docs-content://directory/another-directory/file.md)
```

### `exclude`

Files to exclude from the TOC. Supports glob patterns.

The following example excludes all markdown files beginning with `_`:

```yaml
exclude:
  - '_*.md'
```

### `products`

Products that are covered in all pages in the doc set.

:::{include} /_snippets/products-list.md
:::

`products` can also be defined in the [page level frontmatter](/syntax/frontmatter.md#products).
If you define `products` in a page's Markdown file and the `docset.yml` file also includes `products`, docs-builder will combine the two lists.
You can _not_ override doc set level `products` at the page level.

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

## `toc.yml`

As a rule, each `docset.yml` file can only be included once in the assembler. This prevents us from accidentally duplicating pages in the docs. However, there are times when you want to split content sets and include them partially in different areas of the TOC. That's what `toc.yml` files are for. These files split one documentation set into multiple “sub-TOCs,” each mapped to a different navigation node.

A `toc.yml` file may only declare a nested [TOC](#toc), other options are ignored.

A `toc.yml` may not link to further nested `toc.yml` files. Doing so will result in an error
