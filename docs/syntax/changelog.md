# Changelog

The `{changelog}` directive renders all changelog bundles from a folder directly in your documentation pages. This is designed for release notes pages that primarily consist of changelog content.

## Syntax

```markdown
:::{changelog}
:::
```

Or with a custom bundles folder:

```markdown
:::{changelog} /path/to/bundles
:::
```

## Options

The directive supports the following options:

| Option | Description | Default |
|--------|-------------|---------|
| `:type: value` | Filter entries by type | Excludes separated types |
| `:subsections:` | Group entries by area/component | false |
| `:config: path` | Path to changelog.yml configuration | auto-discover |
| `:product: id` | Product ID for product-specific publish rules | auto from docset |

### Example with options

```markdown
:::{changelog} /path/to/bundles
:type: all
:subsections:
:product: kibana
:::
```

### Option details

#### `:type:`

Controls which entry types are displayed. By default, the directive excludes "separated types" (known issues, breaking changes, deprecations, and highlights) which are typically shown on their own dedicated pages.

| Value | Description |
|-------|-------------|
| (omitted) | Default: shows all types EXCEPT known issues, breaking changes, deprecations, and highlights |
| `all` | Shows all entry types including known issues, breaking changes, deprecations, and highlights |
| `breaking-change` | Shows only breaking change entries |
| `deprecation` | Shows only deprecation entries |
| `known-issue` | Shows only known issue entries |
| `highlight` | Shows only highlighted entries |

This allows you to create separate pages for different entry types:

```markdown
# Release Notes

:::{changelog}
:::
```

```markdown
# Breaking Changes

:::{changelog}
:type: breaking-change
:::
```

```markdown
# Known Issues

:::{changelog}
:type: known-issue
:::
```

```markdown
# Deprecations

:::{changelog}
:type: deprecation
:::
```

```markdown
# Highlights

:::{changelog}
:type: highlight
:::
```

To show all entries on a single page (previous default behavior):

```markdown
:::{changelog}
:type: all
:::
```

#### `:subsections:`

When enabled, entries are grouped by their area/component within each section. By default, entries are listed without area grouping (matching CLI behavior).

If publish rules with `include_areas` or `exclude_areas` are active, the grouping uses the first area in the entry's `areas` list that is consistent with those rules — the first included area for `include_areas` rules, or the first non-excluded area for `exclude_areas` rules. When no publish rules are configured, the first area in the list is used.

#### `:config:`

Explicit path to a `changelog.yml` configuration file. If not specified, the directive auto-discovers from:
1. `changelog.yml` in the docset root
2. `docs/changelog.yml` relative to docset root

The configuration can include publish rules to filter entries by type or area.

#### `:product:`

Product ID for loading product-specific publish rules from `changelog.yml`. The directive resolves the product ID in this order:

1. **Explicit `:product:` option** - if specified, uses that product ID
2. **Docset's single product** - if the docset has exactly one product configured in `docset.yml`, uses that product ID automatically
3. **Global fallback** - uses the global `rules.publish` configuration

This automatic fallback means most single-product docsets don't need to specify `:product:` explicitly - the directive will automatically use the docset's product for publish rule lookup.

**Example docset with single product:**

```yaml
# docset.yml
products:
  - id: kibana
toc:
  - file: release-notes.md
```

```yaml
# changelog.yml
rules:
  publish:
    products:
      kibana:
        exclude_types:
          - docs
        exclude_areas:
          - "Elastic Observability solution"
          - "Elastic Security solution"
```

With this configuration, the directive will automatically use the `kibana` product rules:

```markdown
:::{changelog}
:::
```

**Explicit override:**

You can override the automatic product detection by specifying `:product:` explicitly:

```markdown
:::{changelog}
:product: elasticsearch
:::
```

This is useful when:
- The docset has multiple products and you want a specific one
- You want to use a different product's rules than the docset default

The product ID matching is case-insensitive.

## Filtering entries with publish rules

You can filter changelog entries from the rendered output using the `rules.publish` configuration in your `changelog.yml` file. This is useful for hiding entries that shouldn't appear in public documentation, such as internal changes or documentation-only updates.

Each field supports **exclude** (block if matches) or **include** (block if doesn't match) semantics. You cannot mix both for the same field (for example, you cannot specify both `exclude_types` and `include_types`).

For areas, you can control the matching mode with `match_areas`:
- `any` (default): block if ANY entry area matches the list
- `all`: block only if ALL entry areas match the list

The `match_areas` setting inherits from the global `rules.match` if not specified. Product-level `match_areas` inherits from the parent `publish.match_areas`:

```
rules.match → publish.match_areas → publish.products.{id}.match_areas
```

### Configuration syntax

Create a `changelog.yml` file in your docset root (or `docs/changelog.yml`):

```yaml
rules:
  # Global publish rules (applies to all products)
  publish:
    # match_areas: any
    exclude_types:
      - docs           # Hide documentation entries
      - regression     # Hide regression entries
    exclude_areas:
      - Internal       # Hide entries with "Internal" area
      - Experimental   # Hide entries with "Experimental" area

    # Product-specific rules (override global rules)
    products:
      kibana:
        exclude_types:
          - docs
        exclude_areas:
          - "Elastic Observability solution"
          - "Elastic Security solution"
      cloud-serverless:
        exclude_types:
          - docs
        exclude_areas:
          - "Snapshot and restore"
```

Product-specific rules are applied automatically when your docset has a single product configured. For docsets with multiple products or to override the automatic detection, specify the `:product:` option:

```markdown
:::{changelog}
:product: kibana
:::
```

### Filtering by type

The `exclude_types` or `include_types` list filters entries based on their changelog entry type. Matching is **case-insensitive**.

| Type | Description |
|------|-------------|
| `feature` | New features |
| `enhancement` | Improvements to existing features |
| `security` | Security advisories and fixes |
| `bug-fix` | Bug fixes |
| `breaking-change` | Breaking changes |
| `deprecation` | Deprecated functionality |
| `known-issue` | Known issues |
| `docs` | Documentation changes |
| `regression` | Regressions |
| `other` | Other changes |

Example - hide documentation and regression entries:

```yaml
rules:
  publish:
    exclude_types:
      - docs
      - regression
```

Example - only show feature and bug-fix entries:

```yaml
rules:
  publish:
    include_types:
      - feature
      - bug-fix
```

### Filtering by area

The `exclude_areas` or `include_areas` list filters entries based on their area/component tags. By default (`match_areas: any`), an entry is blocked if **any** of its areas match. With `match_areas: all`, an entry is blocked only if **all** of its areas match. Matching is **case-insensitive**.

Example - hide internal and experimental entries:

```yaml
rules:
  publish:
    exclude_areas:
      - Internal
      - Experimental
      - Testing
```

Example - only show entries with specific areas, requiring all areas to match:

```yaml
rules:
  publish:
    match_areas: all
    include_areas:
      - "Search"
      - "Monitoring"
```

The `match_areas` setting controls how areas are compared. Here is a quick reference:

| Config | Entry areas | match_areas | Result |
|--------|------------|-------------|--------|
| `exclude_areas: [Internal]` | `[Search, Internal]` | `any` | **Hidden** ("Internal" matches) |
| `exclude_areas: [Internal]` | `[Search, Internal]` | `all` | **Shown** (not all areas are in the exclude list) |
| `include_areas: [Search]` | `[Search, Internal]` | `any` | **Shown** ("Search" matches) |
| `include_areas: [Search]` | `[Search, Internal]` | `all` | **Hidden** ("Internal" is not in the include list) |

Product-specific rules can override `match_areas`:

```yaml
rules:
  match: any
  publish:
    # inherits match_areas: any from rules.match
    exclude_areas:
      - Internal
    products:
      cloud-serverless:
        match_areas: all
        include_areas:
          - "Search"
          - "Monitoring"
```

### Combining type and area filters

You can combine both type and area filters. An entry is blocked if it matches **either** a blocked type **or** a blocked area. You can mix exclude and include across fields (for example, `exclude_types` with `include_areas`).

```yaml
rules:
  publish:
    exclude_types:
      - docs
      - deprecation
    exclude_areas:
      - Internal
```

This configuration will hide:
- All entries with type `docs` or `deprecation`
- All entries with the `Internal` area tag (regardless of type)

### Example: Cloud Serverless configuration

For Cloud Serverless releases where you want to hide certain entry types:

```yaml
# changelog.yml
rules:
  publish:
    exclude_types:
      - docs           # Documentation changes handled separately
      - deprecation    # Deprecations shown on dedicated page
      - known-issue    # Known issues shown on dedicated page
```

## Feature hiding from bundles

When bundles contain a `hide-features` field, entries with matching `feature-id` values are automatically filtered out from the rendered output. This allows you to hide unreleased or experimental features without modifying the bundle at render time.

```yaml
# Example bundle with hide-features
products:
  - product: elasticsearch
    target: 9.3.0
hide-features:
  - feature:hidden-api
  - feature:experimental
entries:
  - file:
      name: new-feature.yaml
      checksum: abc123
```

When the directive loads multiple bundles, `hide-features` from **all bundles are aggregated** and applied to all entries. This means if bundle A hides `feature:x` and bundle B hides `feature:y`, both features are hidden in the combined output.

To add `hide-features` to a bundle, use the `--hide-features` option when running `changelog bundle`. For more details, see [Hide features in bundles](../contribute/changelog.md#changelog-bundle-hide-features).

## Private repository link hiding

Changelog entries can reference multiple pull requests and issues via the `prs` and `issues` array fields. When an entry is rendered, all of its links are shown inline:

```md
* Fix ML calendar event update scalability issues. [#136886](https://github.com/elastic/elastic/pull/136886) [#136900](https://github.com/elastic/elastic/pull/136900)
```

PR and issue links are automatically hidden (commented out) for bundles from private repositories. When links are hidden, **all** PR and issue links for an affected entry are hidden together. This is determined by checking the `assembler.yml` configuration:

- Repositories marked with `private: true` in `assembler.yml` will have their links hidden
- For merged bundles (e.g., `elasticsearch+kibana`), links are hidden if ANY component repository is private
- In standalone builds without `assembler.yml`, all links are shown by default

## Bundle merging

Bundles with the same target version/date are automatically merged into a single section. This is useful for Cloud Serverless releases where multiple repositories (e.g., Elasticsearch, Kibana) contribute to a single dated release like `2025-08-05`.

### Amend bundle merging

Bundles can have associated **amend files** that follow the naming pattern `{bundle-name}.amend-{N}.yaml` (e.g., `9.3.0.amend-1.yaml`). When loading bundles, the directive automatically discovers and merges amend files with their parent bundles.

This allows you to add late additions to a release without modifying the original bundle file:

```
bundles/
├── 9.3.0.yaml           # Parent bundle
├── 9.3.0.amend-1.yaml   # First amend (auto-merged with parent)
└── 9.3.0.amend-2.yaml   # Second amend (auto-merged with parent)
```

All entries from the parent and amend bundles are rendered together as a single release section. The parent bundle's metadata (products, hide-features, repo) is preserved.

## Default folder structure

The directive expects bundles in `changelog/bundles/` relative to the docset root:

```
docs/
├── _docset.yml
├── changelog/
│   ├── feature-x.yaml        # Individual changelog entries
│   ├── bugfix-y.yaml
│   └── bundles/
│       ├── 0.99.0.yaml       # Bundled changelogs (by version)
│       └── 0.100.0.yaml
└── release-notes.md          # Page with :::{changelog}
```

To override these expectations, set the `bundle.directory` and `bundle.output_directory` in the changelog configuration file.

## Version ordering

Bundles are automatically sorted by **semantic version** (descending - newest first). This means:

- `0.100.0` sorts after `0.99.0` (not lexicographically)
- `1.0.0` sorts after `0.100.0`
- `1.0.0` sorts after `1.0.0-beta`

The version is extracted from the first product's `target` field in each bundle file, or from the filename if not specified.

## Rendered output

Each bundle renders as a `## {version}` section with subsections beneath:

```markdown
## 0.100.0
### Features and enhancements
...
### Fixes
...

## 0.99.0
### Features and enhancements
...
```

### Section types

| Section | Entry type | Rendering |
|---------|------------|-----------|
| Features and enhancements | `feature`, `enhancement` | Grouped by area |
| Fixes | `bug-fix`, `security` | Grouped by area |
| Documentation | `docs` | Grouped by area |
| Regressions | `regression` | Grouped by area |
| Other changes | `other` | Grouped by area |
| Breaking changes | `breaking-change` | Expandable dropdowns |
| Highlights | Entries with `highlight: true` | Expandable dropdowns |
| Deprecations | `deprecation` | Expandable dropdowns |
| Known issues | `known-issue` | Expandable dropdowns |

**Note about highlights:**
- Highlights only appear when using `:type: all` (they are excluded from the default view)
- When rendered, highlighted entries appear in BOTH the "Highlights" section AND their original type section (for example, a highlighted feature appears in both "Highlights" and "Features and enhancements")
- The "Highlights" section is only created when at least one entry has `highlight: true`
- When using `:type: highlight`, only highlighted entries are shown (no section headers or other content)

Sections with no entries of that type are omitted from the output.

## Error behavior for missing files [changelog-missing-files]

Bundles created without the `--resolve` option store `file:` references (filenames and checksums) instead of embedding entry content inline.
When the directive loads such a bundle, it looks up each referenced file to read its content.
If a referenced file cannot be found on disk, the directive emits an error and the build fails.
This prevents silent data loss where changelog entries would be quietly omitted from rendered release notes without any indication that something was missing.

To fix this, either:

- Restore the missing changelog files, or
- Re-create the bundle with `--resolve` to embed entry content directly (making the bundle self-contained), or
- Remove the unresolvable entry from the bundle file.

:::{tip}
In general, if you want to be able to remove changelog files after your releases, create your bundles with the `--resolve` option or set `bundle.resolve` to `true` in the changelog configuration file.
For more command syntax details, go to [Remove changelog files](../contribute/changelog.md#changelog-remove).
:::

## Example

The following renders all changelog bundles from the default `changelog/bundles/` folder:

```markdown
:::{changelog}
:::
```

### Result

:::{changelog}
:::

## When to use the directive vs render command

| Use case | Recommended approach |
|----------|---------------------|
| Release notes page for a product | `{changelog}` directive |
| Generating static markdown files for external use | `changelog render` command |
| Selective rendering of specific versions | `changelog render` command |

The `{changelog}` directive is ideal for release notes pages that should always show the complete changelog history. For more selective workflows or external publishing, use the [`changelog render`](../cli/release/changelog-render.md) command.

## Related

- [Create and bundle changelogs](../contribute/changelog.md) — Learn how to create changelog entries and bundles
- [`changelog add`](../cli/release/changelog-add.md) — CLI command to create changelog entries
- [`changelog bundle`](../cli/release/changelog-bundle.md) — CLI command to bundle changelog entries
- [`changelog remove`](../cli/release/changelog-remove.md) — CLI command to remove changelog files
- [`changelog render`](../cli/release/changelog-render.md) — CLI command to render changelogs to markdown files
