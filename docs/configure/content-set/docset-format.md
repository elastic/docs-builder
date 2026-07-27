---
navigation_title: docset.yml format
---

# docset.yml file format

The `docset.yml` file (or `_docset.yml`) configures a content set's structure and metadata. It defines navigation, cross-links, substitutions, and other content set-level settings. The file is typically located at the root of the documentation folder (for example, `docs/docset.yml` or `docs/_docset.yml`).

For navigation structure and TOC syntax, see [Navigation](./navigation.md). For attributes and substitutions, see [Attributes](./attributes.md).

## Field reference

| Field | Required | Default | Description |
|-------|----------|---------|-------------|
| `project` | Optional | — | The name of the project. |
| `toc` | Required | — | Table of contents defining the navigation structure. A minimal docset needs at least one file or folder reference. |
| `suppress` | Optional | `[]` | Diagnostic hint types to suppress (for example, `DeepLinkingVirtualFile`, `FolderFileNameMismatch`). |
| `max_toc_depth` | Optional | `2` | Maximum depth of the table of contents. |
| `dev_docs` | Optional | `false` | When `true`, marks this documentation set as development docs that are not linkable by the assembler. |
| `cross_links` | Optional | `[]` | Repositories to link to (for example, `docs-content`, `apm-server`). |
| `exclude` | Optional | `[]` | Glob patterns for files to exclude from the TOC. |
| `extensions` | Optional | `[]` | Extension configuration. |
| `subs` | Optional | `{}` | Substitution key-value pairs for consistent terminology across the docset. |
| `display_name` | Optional | — | **Deprecated.** Use the `index.md` H1 heading instead. |
| `description` | Optional | — | **Deprecated.** Use the `index.md` frontmatter description instead. |
| `icon` | Optional | — | Icon identifier for the documentation set. |
| `registry` | Optional | — | Link index registry (for example, `Public`). |
| `features` | Optional | `{}` | Feature flags (for example, `primary-nav`, `disable-github-edit-link`). |
| `api` | Optional | `{}` | OpenAPI specification paths keyed by name. |
| `products` | Optional | `[]` | Product IDs that this documentation set documents. Used for versioning and other product-aware features. Resolved against [products.yml](https://github.com/elastic/docs-builder/blob/main/config/products.yml). |
| `codex` | Optional | — | Codex-specific metadata (for example, `group` for navigation grouping). |

## Field details

### `project`

The project name. Used for identification and display.

```yaml
project: 'APM Java agent reference'
```

### `toc`

Defines the table of contents. Required for a valid docset. See [Navigation](./navigation.md) for full TOC syntax and structure.

```yaml
toc:
  - file: index.md
  - folder: getting-started
    children:
      - file: index.md
      - file: install.md
```

### `suppress`

Suppresses specific diagnostic hints. Valid values include `DeepLinkingVirtualFile`, `FolderFileNameMismatch`, and `AutolinkElasticCoDocs`.

```yaml
suppress:
  - DeepLinkingVirtualFile
  - FolderFileNameMismatch
```

### `cross_links`

Repositories you want to link to. Enables cross-repository links without absolute URLs. See [Navigation](./navigation.md#cross_links) for usage details.

```yaml
cross_links:
  - docs-content
  - apm-server
  - cloud
```

### `exclude`

Glob patterns for files to exclude from the TOC.

```yaml
exclude:
  - '_*.md'
```

### `subs`

Substitution key-value pairs. Values can be referenced in Markdown as `{{key}}`. See [Attributes](./attributes.md) for details.

```yaml
subs:
  ea: "Elastic Agent"
  es: "Elasticsearch"
```

### `features`

Feature flags that control docset behavior.

| Sub-field | Description |
|-----------|-------------|
| `primary-nav` | Controls primary navigation display. |
| `disable-github-edit-link` | Hides the GitHub edit link. |

```yaml
features:
  primary-nav: false
  disable-github-edit-link: false
```

### `api`

OpenAPI specification file paths, keyed by API name. Paths are relative to the documentation source directory.

```yaml
api:
  elasticsearch: elasticsearch-openapi.json
  kibana: kibana-openapi.json
```

### `products`

Product IDs that this documentation set documents. Each ID must exist in the global [products.yml](https://github.com/elastic/docs-builder/blob/main/config/products.yml) configuration. When omitted or empty, the docset has no product association.

```yaml
products:
  - id: kibana
```

For a docset that documents multiple products:

```yaml
products:
  - id: elasticsearch
  - id: kibana
```

### `codex`

Codex-specific metadata. The `group` field controls navigation grouping in codex environments.

```yaml
codex:
  group: observability
```

## Related

* [Navigation](./navigation.md) — TOC structure and cross-links
* [Attributes](./attributes.md) — Substitutions and attributes
* [File structure](./file-structure.md) — How content set structure maps to URLs
