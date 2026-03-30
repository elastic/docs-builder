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
| `:link-visibility: value` | Visibility of pull request (PR) and issue links | `auto` |
| `:config: path` | Path to `changelog.yml` configuration (reserved for future use) | auto-discover |

### Example with options

```markdown
:::{changelog} /path/to/bundles
:type: all
:subsections:
:link-visibility: keep-links
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

#### `:link-visibility:`

Controls how pull request and issue links are shown when the directive applies source-repo-based privacy.
Bundles whose repo is listed as private in `assembler.yml` hide links by default.

| Value | Behavior |
|-------|----------|
| `auto` | Hide all PR and issue links for bundles from private repos; show links for public repos. |
| `keep-links` | Show PR and issue links even when the bundle source repo is private (does not undo bundle-time private-target sanitization)). |
| `hide-links` | Hide all PR and issue links for this directive block. Refer to [Hiding links](#hide-links). |

This aligns with the `changelog render` command's link visibility controls.

#### `:subsections:`

When enabled, entries are grouped by "area" within each section.
By default, entries are listed without area grouping.
If a changelog has multiple area values, only the first one is used.

#### `:config:`

Explicit path to a `changelog.yml` configuration file. If not specified, the directive auto-discovers from:
1. `changelog.yml` in the docset root
2. `docs/changelog.yml` relative to docset root

Reserved for future configuration use. The directive does not currently load or apply configuration from this file.

## Filtering entries with bundle rules

You can filter changelog entries at bundle time using the `rules.bundle` configuration in your `changelog.yml` file. This is evaluated during `changelog bundle` and `changelog gh-release`, before the bundle is written. Entries that don't match are excluded from the bundle entirely.

The `{changelog}` directive and the `changelog render` command both do not apply `rules.publish`. To filter entries, use `rules.bundle` at bundle time so entries are excluded before bundling. Both receive only the bundled entries. See the [changelog bundle documentation](/cli/release/changelog-bundle.md#changelog-bundle-rules) for full syntax.

`rules.bundle` supports product, type, and area filtering, and per-product overrides.
For full syntax, refer to the [rules for filtered bundles](/cli/release/changelog-bundle.md#changelog-bundle-rules).

## Hiding features

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

To add `hide-features` to a bundle, use the `--hide-features` option when running `changelog bundle`.
For more details, go to [Hide features in bundles](../contribute/changelog.md#changelog-bundle-hide-features).

## Hiding private links [hide-links]

A changelog can reference multiple pull requests and issues in the `prs` and `issues` array fields.

PR and issue links are automatically hidden (commented out) for bundles from private repositories.
When links are hidden, **all** PR and issue links for an affected entry are hidden together.
This is determined by checking the `assembler.yml` configuration:

- Repositories marked with `private: true` in `assembler.yml` will have their links hidden
- For merged bundles (for example, `elasticsearch+kibana`), links are hidden if ANY component repository is private
- In standalone builds without `assembler.yml`, all links are shown by default

Use `:link-visibility: keep-links` or `hide-links` on the `{changelog}` directive to override this behavior.

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

The `bundle.directory` and `bundle.output_directory` settings in `changelog.yml` apply to the `changelog bundle` and `changelog gh-release` CLI commands. The directive's bundles folder is controlled by its first argument or defaults to `changelog/bundles/` relative to the docset root.

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
