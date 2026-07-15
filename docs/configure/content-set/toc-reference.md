---
navigation_title: TOC reference
---

# TOC reference

The `toc` key in `docset.yml` and `toc.yml` defines the navigation tree for a content set. This page documents the syntax for each entry type. For layout patterns and when to choose each type, see [Documentation set navigation](../../building-blocks/documentation-set-navigation.md).

File paths in `toc` entries are relative to the documentation set root (the folder containing `docset.yml`), not relative to the `toc.yml` file that declares them.

## Entry types

### `file`

References a single Markdown file.

| Sub-key | Type | Description |
|---------|------|-------------|
| `children` | list | Nested navigation entries. Children must be siblings or deeper files within the parent's subtree on disk. |

```yaml
toc:
  - file: index.md
  - file: getting-started.md
    children:
      - file: installation.md
      - file: configuration.md
```

A `file` entry with `children` creates a virtual grouping that does not require a matching folder on disk. The builder may emit a `DeepLinkingVirtualFile` hint when the file path contains `/` and has children.

### `hidden`

References a page that is built and linkable but omitted from the navigation menu. Cannot have `children`.

```yaml
toc:
  - hidden: developer-notes.md
  - hidden: _search.md
```

### `folder`

References a directory of Markdown files.

| Sub-key | Type | Description |
|---------|------|-------------|
| `children` | list | Explicit list of files. When set, every `.md` file in the folder must be listed or the build errors. |
| `sort` | string | Sort order for auto-discovered files: `asc`, `ascending`, `desc`, or `descending`. Default: ascending. Uses natural sort (`3_2_0` before `3_10_0`). `index.md` is always first. Ignored when `children` is set. |
| `exclude` | list | File names to omit during auto-discovery (case-insensitive). Ignored when `children` is set. |
| `file` | string | Entry-point file for the folder (folder + file combination). Creates a `FolderIndexFileRef`. |

**Auto-discovery** (no `children`): all `.md` files in the folder are included automatically.

```yaml
toc:
  - folder: api
  - folder: api-versions
    sort: desc
    exclude:
      - draft.md
```

**Explicit children:**

```yaml
toc:
  - folder: api
    children:
      - file: index.md
      - file: authentication.md
```

**Folder + file combination:**

```yaml
toc:
  - folder: getting-started
    file: getting-started.md
    children:
      - file: prerequisites.md
      - file: installation.md
```

The builder may emit a `FolderFileNameMismatch` hint when `file` does not match the folder name (except for `index.md`).

### `toc`

References a nested `toc.yml` file in a subdirectory. Only `docset.yml` may reference `toc.yml` by default; nested `toc.yml` → `toc.yml` references require `max_toc_depth` > 2 in `docset.yml`.

| Sub-key | Type | Description |
|---------|------|-------------|
| `children` | list | Optional children appended after the referenced TOC |

```yaml
toc:
  - file: index.md
  - toc: development
  - toc: api-reference
```

Loads `development/toc.yml` and `api-reference/toc.yml`. The folder name is not repeated inside the nested `toc.yml`.

### `crosslink`

References a page in another documentation set. Requires the target repository in `cross_links`.

| Sub-key | Type | Description |
|---------|------|-------------|
| `title` | string | Navigation label (required when using crosslink as a standalone entry) |
| `hidden` | boolean | Omit from the navigation menu |
| `children` | list | Nested entries under this cross-link |

```yaml
toc:
  - file: index.md
  - title: External Documentation
    crosslink: docs-content://get-started/introduction.md
  - folder: local-section
    children:
      - file: index.md
      - title: API Reference
        crosslink: elasticsearch://api/index.html
```

See [Outbound cross-links](../../building-blocks/outbound-cross-links.md) and [Links](../../syntax/links.md).

### `cli`

Embeds auto-generated CLI reference navigation from a JSON schema file.

| Sub-key | Type | Description |
|---------|------|-------------|
| `cli` | string | Path to the CLI schema JSON, relative to `docset.yml` (required) |
| `folder` | string | Supplemental Markdown docs folder |
| `title` | string | Navigation title |
| `navigation_title` | string | Short navigation label |

```yaml
toc:
  - cli: cli-schema.json
    folder: cli
```

See [CLI reference how-to](../../cli/cli-reference-how-to.md).

### `detection_rules`

Generates navigation from detection rule TOML files. Requires `extensions: [detection-rules]` in `docset.yml`.

| Sub-key | Type | Description |
|---------|------|-------------|
| `file` | string | Overview Markdown page (required) |
| `detection_rules` | list | Paths to folders containing rule TOML files (required) |
| `deprecated_file` | string | Optional page for deprecated rules |
| `children` | list | Additional navigation entries |

```yaml
toc:
  - file: index.md
    detection_rules:
      - '../rules'
```

See [Extensions](extensions.md).

## `suppress`

Top-level key (not a `toc` entry type). Suppresses diagnostic hints for the navigation file. Available in both `docset.yml` and `toc.yml`.

| Value | Triggers when |
|-------|---------------|
| `DeepLinkingVirtualFile` | A `file` with `children` uses a path containing `/` |
| `FolderFileNameMismatch` | A `folder` + `file` pair has mismatched names |
| `AutolinkElasticCoDocs` | A bare URL autolink points at `elastic.co/docs` |

```yaml
suppress:
  - DeepLinkingVirtualFile
  - AutolinkElasticCoDocs
```

See [`docset.yml` reference](docset-reference.md#suppress) for details.

## Related topics

* [`docset.yml` reference](docset-reference.md) — all top-level configuration keys
* [Navigation layout](navigation.md) — patterns and trade-offs for structuring navigation
* [Documentation set navigation](../../building-blocks/documentation-set-navigation.md) — worked examples
