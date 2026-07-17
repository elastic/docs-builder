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
| `:description-visibility: value` | Visibility of changelog **record** descriptions (YAML `description` on each entry) | `auto` |
| `:dropdowns:` | Render breaking changes, deprecations, known issues, and highlights as expandable dropdowns instead of flattened bulleted lists | false |
| `:release-dates:` | Render the bundle `release-date` field as _Released: …_ after the version heading | false |
| `:config: path` | Path to `changelog.yml` configuration | auto-discover |
| `:cdn: [product]` | Render bundles for a product that is declared under `release_notes` in `docset.yml` and prefetched from the public changelog CDN. The product is optional and inferred from the current repository when omitted | (local folder) |
| `:version: target` | Render only the single bundle matching this target/version | (all versions) |

### Example with options

```markdown
:::{changelog} /path/to/bundles
:type: all
:subsections:
:link-visibility: keep-links
:description-visibility: keep-descriptions
:dropdowns:
:release-dates:
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

**General release notes** (default or `:type: all`):

- Section headings are shown when that type has entries.
- Releases with no matching entries are omitted unless the bundle has a `description`; in that case only the version heading and description are shown (no placeholder messages). A `release-date` alone does not preserve an otherwise empty release.

#### `:link-visibility:`

Controls how pull request and issue links are shown when the directive applies source-repo-based privacy.
Bundles whose repo is listed as private in `assembler.yml` hide links by default.

| Value | Behavior |
|-------|----------|
| `auto` | Hide all PR and issue links for bundles from private repos; show links for public repos. When [`:cdn:`](#cdn) is set, **keep** links (CDN bundles are scrubbed for public delivery and assembler private-repo hiding does not apply). |
| `keep-links` | Show PR and issue links even when the bundle source repo is private (does not undo bundle-time private-target sanitization)). |
| `hide-links` | Hide all PR and issue links for this directive block. Refer to [Hiding links](#hide-links). |

This aligns with the `changelog render` command's link visibility controls.

#### `:description-visibility:`

Controls whether the **`description`** text on each **changelog record** appears in output (bullet body text under each item, or the first paragraph inside a breaking-change, deprecation, known-issue, or highlight entry when [`:dropdowns:`](#dropdowns) is enabled). This is **different** from the optional **bundle** `description` field (release intro prose after `_Released:_`), which is always shown when present. See [Rendered output](#rendered-output).

| Value | Behavior |
|-------|----------|
| `auto` | When **every** constituent repository in the bundle’s resolved repo identity is **public** (same private-repo detection as `:link-visibility:` from `assembler.yml`, including `repo1+repo2` merged bundles), **omit** record `description` bodies. When **any** constituent is marked **private**, **show** those bodies. In standalone builds without `assembler.yml`, every repo is treated as public ⇒ record descriptions are omitted under `auto`. |
| `keep-descriptions` | Always render record descriptions when present in the bundle source. Use this on pages such as deprecations or breaking changes when you still want full release-note prose alongside public repos. |
| `hide-descriptions` | Always omit record `description` bodies (titles, PR/issue links, Impact and Action sections, and bundle-level intros are unaffected). |

**Contrast with `:link-visibility:`:** `:link-visibility: auto` hides **links** when a repo is **private**. `:description-visibility: auto` **shows** richer record **description** prose when **any** source repo is **private**, and hides that prose for bundles that resolve to **only public** repositories.

#### `:dropdowns:` [dropdowns]

Controls how the "separated" entry types (`breaking-change`, `deprecation`, `known-issue`, and entries flagged `highlight: true`) are rendered. This option only affects these types; features, enhancements, security, bug fixes, documentation, regressions, and other changes are always rendered as flat bulleted lists.

| Mode | Behavior |
|------|----------|
| (omitted, default) | Flattened: each entry renders as a bullet with its title, links, and (when present) `Impact:` / `Action:` lines as indented continuation. |
| `:dropdowns:` | Dropdowns: each entry renders as an expandable `{dropdown}` with the title as the summary and description, links, `**Impact**`, and `**Action**` inside. |

Use dropdowns when breaking-change and deprecation entries have long `description`, `impact`, or `action` prose that benefits from being collapsed by default. Use the flattened default for compact release-notes pages where the list itself is the primary content.

Entry titles may contain inline markdown markers from changelog YAML (for example, `` `setting.name` ``). Dropdown titles are plain text; see [Plain-text titles](/syntax/dropdowns.md#plain-text-titles).

#### `:release-dates:` [release-dates]

Controls whether the bundle `release-date` field is rendered as italicized _Released: …_ text immediately after each version heading. Defaults to `false`.

| Mode | Behavior |
|------|----------|
| (omitted, default) | Do not render release dates, even when the bundle YAML includes `release-date`. |
| `:release-dates:` | Render `_Released: …_` when the bundle includes a `release-date` field. |

Use this option for semver or agent releases where an explicit release date adds context. Omit it for date-based releases where the version heading already encodes the release date.

This is **render-time** control only. To include or omit `release-date` in bundle YAML at build time, use `bundle.release_dates` in `changelog.yml` or the `--release-date` / `--no-release-date` flags on [`changelog bundle`](/cli/changelog/bundle.md) (option-based mode). The `changelog render` command does not provide an equivalent flag; it always renders release dates when present in the bundle.

#### `:subsections:`

When enabled, entries are grouped by "area" within each section.
By default, entries are listed without area grouping.
If a changelog has multiple area values, only the first one is used.

#### `:config:`

Explicit path to a `changelog.yml` or `changelog.yaml` configuration file, relative to the documentation source directory. If not specified, the directive auto-discovers from these locations (first match wins):

1. `changelog.yml` or `changelog.yaml` in the documentation source directory
2. `changelog.yml` or `changelog.yaml` in the parent directory (typically the repository root)

Both explicit and auto-discovered paths must resolve within the repository checkout directory and must not traverse symlinks.

#### `:cdn:` [cdn]

Renders bundles for a single **product** that the docset sources from the public changelog CDN, so a docset can show release notes without vendoring bundle YAML. The directive is a *selector*: it renders bundles that docs-builder prefetched at startup, so the product must first be declared under [`release_notes`](#declaring-cdn-backed-products) in `docset.yml`.

```yaml
# docset.yml
release_notes:
  - product: elasticsearch
```

```markdown
:::{changelog}
:cdn: elasticsearch
:::
```

The value names a product defined in [`products.yml`](https://github.com/elastic/docs-builder/blob/main/config/products.yml) (syntactically it must match `[a-zA-Z0-9_-]+`). The value is **optional**: leave it blank to infer the product from the repository that holds the doc. The repository name is mapped to its canonical product id via `products.yml` (for example the `elastic-otel-java` repo renders the `edot-java` product).

```markdown
:::{changelog}
:cdn:
:::
```

If the product cannot be inferred, or is not declared under `release_notes`, the block emits an error rather than rendering empty. When `:cdn:` is set, the local-folder argument is ignored. All other options (`:type:`, `:link-visibility:`, `:description-visibility:`, `:dropdowns:`, `:release-dates:`, `:subsections:`) and `hide-features` apply identically to CDN-sourced bundles.

With `:link-visibility: auto` (the default), PR and issue links from CDN bundles are shown as-is. Public CDN copies are scrubbed before delivery, so the directive does not re-hide links based on `assembler.yml` private repositories. Explicit `:link-visibility: hide-links` still hides links for CDN-sourced bundles.

The CDN base URL is build configuration, not authored per page: it defaults to the public changelog bundles distribution and can be overridden with the `DOCS_BUILDER_CHANGELOG_CDN` environment variable (an absolute `http`/`https` URL) for staging or local testing.

Bundles are fetched **once at build startup** for every declared product, not per directive. If a declared product's registry cannot be fetched the build fails; an individual bundle that is missing from the CDN is skipped with a warning. For the full design — including the manifest format and infrastructure — see [Changelog bundle registry and CDN delivery](/development/changelog-bundle-registry.md).

##### Declaring CDN-backed products [declaring-cdn-backed-products]

List each CDN-sourced product under `release_notes` in `docset.yml`. Every entry must reference a product id from `products.yml` that participates in the release-notes system:

```yaml
# docset.yml
release_notes:
  - product: elasticsearch
  - product: edot-java
```

docs-builder prefetches the registry and bundles for each declared product at startup. A `:cdn:` directive that names an undeclared product is an error, which keeps the set of network sources auditable in one place rather than discovered dynamically across pages.

#### `:version:` [version]

Renders only the **single** bundle whose target matches the given value, instead of every bundle for the source. A bundle matches when the value equals its declared `target` (for example `9.4.0`, or a date like `2026-04-09`) or its file name (with or without extension). Matching is case-insensitive.

```markdown
:::{changelog}
:version: 9.4.0
:::
```

This works for both local-folder and `:cdn:` sources. In `:cdn:` mode it filters the prefetched bundles down to the matching target at render time.

```markdown
:::{changelog}
:cdn: elasticsearch
:version: 9.4.0
:::
```

If no bundle matches, the directive renders nothing and emits a warning (it does not fall back to showing all versions).

## Filtering entries with bundle rules

You can filter changelog entries at bundle time using the `rules.bundle` configuration in your `changelog.yml` file. This is evaluated during `changelog bundle` and `changelog gh-release`, before the bundle is written. Entries that don't match are excluded from the bundle entirely.

The `{changelog}` directive and the `changelog render` command both do not apply `rules.publish`. To filter entries, use `rules.bundle` at bundle time so entries are excluded before bundling. Both receive only the bundled entries. See the [changelog bundle documentation](/cli/changelog/bundle.md) for full syntax.

`rules.bundle` supports product, type, and area filtering, and per-product overrides.
For full syntax, refer to the [rules for filtered bundles](/cli/changelog/bundle.md).

## Hiding features

When bundles contain a `hide-features` field, entries with matching `feature-id` values are automatically filtered out from the rendered output. This allows you to hide unreleased or experimental features without modifying the bundle at render time.

```yaml
# Example bundle with release-date, description, and hide-features
products:
  - product: elasticsearch
    target: 9.3.0
release-date: "2026-04-09"
description: |
  This release includes new features and bug fixes.
  
  For more information, see the [release notes](https://example.com/docs).
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
For more details, go to [Hide features in bundles](../contribute/bundle-changelogs.md#changelog-bundle-hide-features).

## Hiding private links [hide-links]

A changelog can reference multiple pull requests and issues in the `prs` and `issues` array fields.

PR and issue links are automatically hidden (commented out) for bundles from private repositories when loading from a **local** bundles folder.
When links are hidden, **all** PR and issue links for an affected entry are hidden together.
This is determined by checking the `assembler.yml` configuration:

- Repositories marked with `private: true` in `assembler.yml` will have their links hidden
- For merged bundles loaded locally (for example, `elasticsearch+kibana`), links are hidden if ANY component repository is private
- In standalone builds without `assembler.yml`, all links are shown by default
- When [`:cdn:`](#cdn) is set, `:link-visibility: auto` keeps links (CDN bundles are already scrubbed for public delivery). You do not need `:link-visibility: keep-links` on CDN pages for this reason alone.

Use `:link-visibility: keep-links` or `hide-links` on the `{changelog}` directive to override this behavior. For local merged bundles where a private repo's entries were already sanitized at bundle time with [`link_allow_repos`](/contribute/configure-changelogs-ref.md#bundle-basic), use `:link-visibility: keep-links` so public constituents' links are not hidden with the private repo's.

## Bundle merging

Bundles with the same target version/date are automatically merged into a single section. This is useful for Cloud Serverless releases where multiple repositories (e.g., Elasticsearch, Kibana) contribute to a single dated release like `2025-08-05`.

### Amend bundle merging

Bundles can have associated **amend files** that follow the naming pattern `{bundle-name}.amend-{N}.yaml` (e.g., `9.3.0.amend-1.yaml`). When loading bundles, the directive automatically discovers and merges amend files with their parent bundles.

This allows you to add or remove late changes to a release without modifying the original bundle file:

```
bundles/
├── 9.3.0.yaml           # Parent bundle
├── 9.3.0.amend-1.yaml   # First amend (auto-merged with parent)
└── 9.3.0.amend-2.yaml   # Second amend (auto-merged with parent)
```

Amend files may contain `entries` (additions) and `exclude-entries` (removals). Within each amend file, exclusions are applied before additions. Amend files are processed in numeric order.

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

Each bundle renders as a `## {version}` section with optional release date, description, and subsections beneath:

```markdown
## 0.100.0

_Released: April 9, 2026_

This release includes new features and bug fixes.

Download the release binaries: https://github.com/elastic/elasticsearch/releases/tag/v0.100.0

### Features and enhancements
...
### Fixes
...

## 2025-08-05
### Features and enhancements
...
```

When a bundle includes a `release-date` field, the directive renders it as italicized text (for example, `_Released: April 9, 2026_`) immediately after the version heading **only when** [`:release-dates:`](#release-dates) is set. This is informative for end-users and is especially useful for components released outside the usual stack lifecycle, such as APM agents and EDOT agents.

**Bundle time:** set `release_dates: false` at the bundle or profile level in `changelog.yml`, or use `--no-release-date` on [`changelog bundle`](/cli/changelog/bundle.md), to omit `release-date` from bundle YAML when bundling. Defaults to auto-population when omitted.

**Render time:** omit `:release-dates:` (default) to hide `_Released:_` even when the bundle YAML contains a date — for example, when the version heading already shows a release date.

Bundle descriptions are rendered when present in the bundle YAML file. The description appears after the release date (if any) but before any entry sections. Descriptions support Markdown formatting including links, lists, and multiple paragraphs.

**Record descriptions:** Each changelog entry may have its own `description` field in YAML (shown as body text under list items or as the introductory paragraph inside dropdowns). Visibility of **these** descriptions is controlled with `:description-visibility:` (defaults to `auto`; see Option details section). Do not confuse bundle `description` (intro prose) with per-record `description` (entry bodies).

### Section types

| Section | Entry type | Rendering |
|---------|------------|-----------|
| Features and enhancements | `feature`, `enhancement` | Grouped by area |
| Fixes | `bug-fix`, `security` | Grouped by area |
| Documentation | `docs` | Grouped by area |
| Regressions | `regression` | Grouped by area |
| Other changes | `other` | Grouped by area |
| Breaking changes | `breaking-change` | Flattened bullets by default; expandable dropdowns with [`:dropdowns:`](#dropdowns) |
| Highlights | Entries with `highlight: true` | Flattened bullets by default; expandable dropdowns with [`:dropdowns:`](#dropdowns) |
| Deprecations | `deprecation` | Flattened bullets by default; expandable dropdowns with [`:dropdowns:`](#dropdowns) |
| Known issues | `known-issue` | Flattened bullets by default; expandable dropdowns with [`:dropdowns:`](#dropdowns) |

**Note about highlights:**
- Highlights only appear when using `:type: all` (they are excluded from the default view)
- When rendered, highlighted entries appear in BOTH the "Highlights" section AND their original type section (for example, a highlighted feature appears in both "Highlights" and "Features and enhancements")
- The "Highlights" section is only created when at least one entry has `highlight: true`
- When using `:type: highlight`, only highlighted entries are shown (no section headers or other content)

Sections with no entries of that type are omitted from the output. Releases with no entries after the `:type:` filter are omitted entirely, except on general release-notes pages (`:type: all` or default) when the bundle has a `description`.

## Error behavior for missing files [changelog-missing-files]

Bundles created without the `--resolve` option store `file:` references (filenames and checksums) instead of embedding entry content inline.
When the directive loads such a bundle, it looks up each referenced file to read its content.
If a referenced file cannot be found on disk, the directive emits an error and the build fails.
This prevents silent data loss where changelog entries would be quietly omitted from rendered release notes without any indication that something was missing.

To fix this, either:

- Restore the missing changelog files, or
- Re-create the bundle with `--resolve` to embed entry content directly (making the bundle self-contained).

`bundle-amend --remove` only applies when the source changelog file is still available (for example, to drop an entry from the effective bundle before you delete the file with `changelog remove`).

:::{tip}
In general, if you want to be able to remove changelog files after your releases, create your bundles with the `--resolve` option or set `bundle.resolve` to `true` in the changelog configuration file.
For more command syntax details, go to [Remove changelog files](../contribute/bundle-changelogs.md#changelog-remove).
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

The `{changelog}` directive is ideal for release notes pages that should always show the complete changelog history. For more selective workflows or external publishing, use the [`changelog render`](/cli/changelog/render.md) command.

## Related

- [Create and bundle changelogs](/contribute/changelog.md) — Overview, workflow, and links to detailed guides
- [`changelog add`](/cli/changelog/add.md) — CLI command to create changelog entries
- [`changelog bundle`](/cli/changelog/bundle.md) — CLI command to bundle changelog entries
- [`changelog remove`](/cli/changelog/remove.md) — CLI command to remove changelog files
- [`changelog render`](/cli/changelog/render.md) — CLI command to render changelogs to markdown files
