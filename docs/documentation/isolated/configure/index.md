---
navigation_title: Configuration
---

# docset.yml reference

The `docset.yml` file is the configuration file for a documentation set. At minimum, a documentation set needs a `docset.yml` and an `index.md` in the same folder.

For an overview of navigation concepts and common patterns, see [Navigation](../navigation.md).

## `project`

The name of the project.

```yaml
project: 'APM Java agent reference'
```

## `toc`

Defines the table of contents (navigation) for the content set:

```yaml
toc:
  - file: index.md
```

### `file:`

Adds a page to the navigation:

```yaml
toc:
  - file: index.md
  - file: getting-started.md
```

A `file` can include `children` to create a virtual grouping. Children must be siblings (same directory) or deeper:

```yaml
- file: getting-started.md
  children:
    - file: installation.md
    - file: configuration.md
```

### `folder:`

Groups pages under a directory. Without `children`, all markdown files in the folder are included automatically:

```yaml
- folder: api
```

With explicit `children`, all markdown files in the folder must be listed:

```yaml
- folder: api
  children:
    - file: index.md
    - file: authentication.md
```

#### `sort`

Controls sort order when auto-discovering files (no explicit `children`):

```yaml
- folder: api-versions
  sort: desc
```

Valid values: `asc`, `ascending`, `desc`, `descending`. Default is ascending. `index.md` is always first regardless of sort order.

#### `exclude`

Excludes specific files from auto-discovery:

```yaml
- folder: subsection
  exclude:
    - draft.md
    - internal-notes.md
```

### `hidden:`

Includes a page in the build but hides it from the navigation:

```yaml
- hidden: developer-notes.md
```

### `toc:`

References a separate `toc.yml` file for modularity:

```yaml
toc:
  - file: index.md
  - toc: elastic-basics
  - toc: solutions
```

## `cross_links`

Declares repositories whose link indexes should be fetched for cross-link validation:

```yaml
cross_links:
  - apm-server
  - cloud
  - docs-content
```

Use cross-link syntax in Markdown: `[text](docs-content://directory/file.md)` or with anchors: `[text](docs-content://directory/file.md#section-id)`.

Cross-links can also appear in navigation:

```yaml
toc:
  - file: index.md
  - title: External Documentation
    crosslink: docs-content://directory/file.md
```

## `exclude`

Files to exclude from the build. Supports glob patterns:

```yaml
exclude:
  - '_*.md'
```

## `subs`

Defines substitution variables as key-value pairs. Use `{{name}}` in Markdown to reference them:

```yaml
subs:
  es: "Elasticsearch"
  kib: "Kibana"
  agent: "Elastic Agent"
```

See [Substitutions](/syntax/substitutions.md) for the full syntax including [mutations](/syntax/substitutions.md#mutations).

## `api`

Configures API Explorer sections from OpenAPI specifications. Only valid in `docset.yml`, not `toc.yml`:

```yaml
api:
  elasticsearch: elasticsearch-openapi.json
  kibana:
    - file: kibana-intro.md
    - spec: kibana-openapi.json
```

See [API Explorer](/data/openapi/api-explorer.md) for full details.

## `cta`

Defines named call-to-action templates for the right-hand sidebar. See [CTA](../cta.md).

## `default_cta`

Registers a named CTA template as the default for every page listed in this navigation file. Available on both `docset.yml` and nested `toc.yml` files. The template must be declared under the `cta` map in `docset.yml`.

See [CTA](../cta.md).

## `suppress`

Suppresses specific diagnostic hints:

```yaml
suppress:
  - DeepLinkingVirtualFile
  - FolderFileNameMismatch
  - AutolinkElasticCoDocs
```

### `DeepLinkingVirtualFile`

Suppresses hints about files with children that use deep-linking (paths containing `/`). Prefer `folder:` structures instead.

### `FolderFileNameMismatch`

Suppresses hints about file names not matching folder names. Prefer matching names or `index.md`.

### `AutolinkElasticCoDocs`

Suppresses hints about bare URLs pointing to elastic.co/docs. Prefer cross-links or relative links.
