---
navigation_title: docset.yml reference
---

# `docset.yml` reference

Every content set has a `docset.yml` file at its root. Larger content sets can also define `toc.yml` files in subfolders. Both files share a subset of keys; `docset.yml` alone supports the full set of content-set configuration options.

For syntax of entries inside the `toc` array, see [TOC reference](toc-reference.md). For layout patterns and trade-offs, see [Navigation layout](navigation.md).

## Key applicability

| Key | `docset.yml` | `toc.yml` |
|-----|:------------:|:---------:|
| `project` | ✓ | ✓ |
| `toc` | ✓ | ✓ |
| `suppress` | ✓ | ✓ |
| `subs` | ✓ | ✓ |
| `max_toc_depth` | ✓ | |
| `dev_docs` | ✓ | |
| `cross_links` | ✓ | |
| `release_notes` | ✓ | |
| `exclude` | ✓ | |
| `extensions` | ✓ | |
| `api` | ✓ | |
| `features` | ✓ | |
| `products` | ✓ | |
| `codex` | ✓ | |
| `branding` | ✓ | |
| `storybook` | ✓ | |
| `cta` | ✓ | |
| `registry` | ✓ | |
| `icon` | ✓ | |
| `display_name` | ✓ (deprecated) | |
| `description` | ✓ (deprecated) | |

## Keys in both `docset.yml` and `toc.yml`

### `project`

**Type:** string

The name of the project or documentation set.

```yaml
project: 'APM Java agent reference'
```

### `toc`

**Type:** list of navigation entries

Defines the table of contents for the content set. See [TOC reference](toc-reference.md) for entry types and sub-keys.

```yaml
toc:
  - file: index.md
  - folder: guides
    children:
      - file: index.md
```

### `suppress`

**Type:** list of hint type names

Suppresses diagnostic hints for this navigation file. Valid values:

| Value | Description |
|-------|-------------|
| `DeepLinkingVirtualFile` | Files with `children` that use a path containing `/` |
| `FolderFileNameMismatch` | `folder` + `file` combinations where the file name does not match the folder name |
| `AutolinkElasticCoDocs` | Bare `https://` URLs pointing at `elastic.co/docs` |

```yaml
suppress:
  - DeepLinkingVirtualFile
  - FolderFileNameMismatch
```

### `subs`

**Type:** map of string → string

Content-set-level [substitutions](../../syntax/substitutions.md) (attributes). Merged with page-level `sub` frontmatter.

```yaml
subs:
  ea: Elastic Agent
  es: Elasticsearch
```

See [Attributes](attributes.md).

## Keys in `docset.yml` only

### `max_toc_depth`

**Type:** integer · **Default:** `2`

Maximum nesting depth for `toc:` entries that reference nested `toc.yml` files. By default, only `docset.yml` may reference `toc.yml` files; increasing this value allows nested `toc.yml` files to reference other `toc.yml` files. Consult the docs team before raising this limit.

```yaml
max_toc_depth: 2
```

### `dev_docs`

**Type:** boolean · **Default:** `false`

Marks the documentation set as developer documentation. Relaxes some restrictions around TOC building and file placement. Developer docsets are not linkable by the assembler.

```yaml
dev_docs: true
```

### `cross_links`

**Type:** list of repository names (or `registry://repository` URIs)

Declares repositories whose [link indexes](../../building-blocks/link-index.md) this content set may link to. Required for `repo://` cross-link syntax in Markdown and navigation.

```yaml
cross_links:
  - docs-content
  - elasticsearch
```

Entries may include an optional registry prefix (`public://` or `internal://`). The docset-level `registry` key sets the default prefix for entries without one.

See [Outbound cross-links](../../building-blocks/outbound-cross-links.md) and [Links](../../syntax/links.md).

### `release_notes`

**Type:** list of product references

Declares products whose changelog bundles are sourced from the public CDN. Required for the `{changelog}` directive's `:cdn:` mode and for `changelog bundle` to source entries from the CDN.

```yaml
release_notes:
  - product: elasticsearch
  - product: edot-java
```

Each `product` must be a product id from `products.yml` that participates in the release-notes system.

See [Declaring CDN-backed products](../../syntax/changelog.md#declaring-cdn-backed-products) and [Changelog bundle registry](../../development/changelog-bundle-registry.md).

### `exclude`

**Type:** list of glob patterns

Excludes files from the content set. Patterns starting with `!` re-include files that would otherwise be excluded.

```yaml
exclude:
  - '_*.md'
  - '!_search.md'
```

### `extensions`

**Type:** list of extension names

Opts into content-set extensions that are disabled by default.

```yaml
extensions:
  - detection-rules
```

See [Extensions](extensions.md).

### `api`

**Type:** map of product name → OpenAPI spec path or sequence

Configures [API Explorer](api-explorer.md) sections. Paths are relative to the folder containing `docset.yml`. Not valid in `toc.yml`.

```yaml
api:
  elasticsearch: elasticsearch-openapi.json
  kibana:
    - file: kibana-intro.md
    - spec: kibana-openapi.json
```

### `features`

**Type:** map of feature flags

| Key | Type | Description |
|-----|------|-------------|
| `primary-nav` | boolean | Enables the primary navigation dropdown (requires Elastic global navigation; incompatible with `branding`) |
| `disable-github-edit-link` | boolean | Hides the **Edit this page on GitHub** link |

```yaml
features:
  primary-nav: false
  disable-github-edit-link: true
```

### `products`

**Type:** list of product references

Default products for the documentation set. Merged with page-level `products` frontmatter.

```yaml
products:
  - id: apm-agent
  - id: edot-sdk
```

See [Products frontmatter](../../syntax/frontmatter.md#products).

### `codex`

**Type:** map

Codex-specific metadata for internal documentation environments.

| Key | Type | Description |
|-----|------|-------------|
| `group` | string | Navigation group id for codex landing pages |

```yaml
codex:
  group: observability
```

### `branding`

**Type:** map

White-label branding overrides for isolated builds. When present, Elastic-specific chrome is suppressed. Image paths are relative to the folder containing `docset.yml`.

| Key | Type | Description |
|-----|------|-------------|
| `icon` | string | Site icon image path |
| `header-bg` | string | CSS colour for the header background (default: `#000000`) |
| `og-image` | string | Open Graph image path |
| `favicon` | string | Browser favicon path (auto-discovered from `favicon.ico`, `favicon.png`, or `favicon.svg` when omitted) |
| `apple-touch-icon` | string | Apple touch icon path (auto-discovered from `apple-touch-icon.png` when omitted) |

```yaml
branding:
  icon: images/logo.svg
  header-bg: '#1a1a2e'
  favicon: favicon.ico
```

Cannot be used together with `features.primary-nav: true`.

### `storybook`

**Type:** map

Configures the Storybook registry for the `{storybook}` directive.

| Key | Type | Description |
|-----|------|-------------|
| `registry` | string | URL to a Kibana `docs_registry.json` file. Supports `${KIBANA_STORYBOOK_REGISTRY:-default}` interpolation. |

```yaml
storybook:
  registry: https://ci-artifacts.kibana.dev/storybooks/main/storybook-docs/docs_registry.json
```

See [Storybook](../../syntax/storybook.md).

### `cta`

**Type:** map of template name → definition

Named right-gutter call-to-action templates. Pages select a template via `cta.id` frontmatter.

```yaml
cta:
  beta:
    button:
      label: Join the private beta
      url: https://example.com/beta
    benefits:
      - Early access to new features
```

See [CTA](cta.md).

### `registry`

**Type:** string · **Default:** `public`

Default link-index registry for `cross_links` entries that omit a registry prefix.

| Value | Description |
|-------|-------------|
| `public` | S3-based public link index (default) |
| `internal` | Codex internal link index |

```yaml
registry: public
```

Cross-link entries may override the default: `internal://my-docset`.

### `icon`

**Type:** string

Optional icon identifier for codex documentation-set cards. Distinct from `branding.icon`, which applies to white-label site chrome.

### `display_name` (deprecated)

**Type:** string

Deprecated. Use the `index.md` H1 heading instead. This field will be removed in a future version.

### `description` (deprecated)

**Type:** string

Deprecated. Use page frontmatter `description` instead. This field will be removed in a future version.

## Related topics

* [TOC reference](toc-reference.md) — syntax for `toc` array entries
* [Navigation layout](navigation.md) — patterns for organizing navigation
* [File structure](file-structure.md) — how directories map to URLs
