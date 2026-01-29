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
| `:hide-features: id1, id2` | Hide entries with specified feature IDs | (none) |
| `:subsections:` | Group entries by area/component | false |
| `:config: path` | Path to changelog.yml configuration | auto-discover |

### Example with options

```markdown
:::{changelog} /path/to/bundles
:hide-features: experimental-api, internal-feature
:subsections:
:::
```

### Option details

#### `:hide-features:`

A comma-separated list of feature IDs. Entries with matching `feature-id` values will be filtered out from the rendered output. Feature ID matching is case-insensitive.

#### `:subsections:`

When enabled, entries are grouped by their area/component within each section. By default, entries are listed without area grouping (matching CLI behavior).

#### `:config:`

Explicit path to a `changelog.yml` configuration file. If not specified, the directive auto-discovers from:
1. `changelog.yml` in the docset root
2. `docs/changelog.yml` relative to docset root

The configuration can include publish blockers to filter entries by type or area.

## Private repository link hiding

PR and issue links are automatically hidden (commented out) for bundles from private repositories. This is determined by checking the `assembler.yml` configuration:

- Repositories marked with `private: true` in `assembler.yml` will have their links hidden
- For merged bundles (e.g., `elasticsearch+kibana`), links are hidden if ANY component repository is private
- In standalone builds without `assembler.yml`, all links are shown by default

## Bundle merging

Bundles with the same target version/date are automatically merged into a single section. This is useful for Cloud Serverless releases where multiple repositories (e.g., Elasticsearch, Kibana) contribute to a single dated release like `2025-08-05`.

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
| Deprecations | `deprecation` | Expandable dropdowns |
| Known issues | `known-issue` | Expandable dropdowns |

Sections with no entries of that type are omitted from the output.

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
- [`changelog render`](../cli/release/changelog-render.md) — CLI command to render changelogs to markdown files
