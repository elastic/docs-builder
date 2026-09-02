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

#### `source`

Reads the page's content from somewhere else in the repository, so documentation can live next to the code it describes:

```yaml
toc:
  - file: index.md
  - file: feedback.md
    source: ../packages/kbn-ui/feedback/feedback.md
```

`file:` stays the page's position in the documentation set — it drives the URL, the navigation entry, the output path and the cross-repository link reference. `source:` is only where the content is read from, resolved relative to the directory holding the `docset.yml` or `toc.yml` that declares the entry.

Relative links, images and includes inside a sourced page resolve from its `file:` position, not from where the file sits on disk.

Also works on a `hidden:` entry, and on the `folder:` + `file:` form:

```yaml
- folder: feedback
  file: index.md
  source: ../packages/kbn-ui/feedback/readme.md
```

Constraints:

- Single markdown files only — there is no folder or glob equivalent.
- The source must resolve outside the documentation set root; inside it, use a plain `file:` entry.
- The source must stay inside the repository checkout, and neither it nor any directory on the way to it may be a symlink.
- The `file:` position must resolve inside the documentation set root and be free — no file of that name on disk, no page generated there by an extension, and no other entry sourcing it.

On the assembled documentation site, repositories are cloned with a partial checkout of `docs` only. A source outside that path needs the repository's `sparse_paths` or `checkout_strategy` widened in [`config/assembler.yml`](https://github.com/elastic/docs-builder/blob/main/config/assembler.yml).

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
  elasticsearch:
    - spec: elasticsearch-openapi.json
      product: elasticsearch
  kibana:
    - spec: kibana-openapi.json
      product: kibana
      children:
        - file: kibana-api-overview.md
```

See [API Explorer](/data/openapi/api-explorer.md) for full details.

## `cta`

Defines named call-to-action templates for the right-hand sidebar. See [CTA](../cta.md).

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
