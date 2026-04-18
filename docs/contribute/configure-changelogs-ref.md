---
navigation_title: Configuration reference
---
# Changelog configuration reference

The changelog configuration file contains settings to make the creation of changelog files and bundles more consistent and repeatable.
By default, it's named `docs/changelog.yml`.

For the most up-to-date changelog configuration options and examples, refer to [changelog.example.yml](https://github.com/elastic/docs-builder/blob/main/config/changelog.example.yml).

These are the main configuration sections:

:::{table}
:widths: description

| Section | Purpose |
| --- | --- |
| `bundle` | Sets directory paths, GitHub defaults, and named profiles for bundling. |
| `extract` | Configures automatic extraction of release notes and issues from PR descriptions. |
| `filename` | Controls how `changelog add` names generated files. |
| `lifecycles` | Specifies allowed lifecycle values. |
| `pivot` | Maps GitHub labels to changelog types, areas, and products. |
| `products` | Defines available products and defaults. |
| `rules` | Filters which PRs create changelogs and which changelogs appear in bundles. |

:::

## Bundle

Configures directory paths, GitHub repository defaults, and named profiles for bundle operations.
These settings are separate from `rules.bundle` filtering.

### Basic settings [bundle-basic]

Controls bundle-level behavior.
These settings are relevant to one or all of the `changelog bundle`, `changelog gh-release`, and `changelog remove` commands.

:::{table}
:widths: description

| Setting | Description |
| --- | --- |
| `bundle.description` | Default template for bundle descriptions. Supports `{version}`, `{lifecycle}`, `{owner}`, and `{repo}` placeholders. |
| `bundle.directory` | Input directory containing changelog YAML files (default: `docs/changelog`). |
| `bundle.link_allow_repos` | List of `owner/repo` pairs whose PR/issue links are preserved. When set (including empty `[]`), links to unlisted repos become `# PRIVATE:` sentinels. Requires `bundle.resolve: true` |
| `bundle.output_directory` | Output directory for bundled files (default: `docs/releases`). |
| `bundle.owner` | Default GitHub repository owner (for example, `elastic`). |
| `bundle.release_dates` | When `true`, bundles include a `release-date` field (default: true). |
| `bundle.repo` | Default GitHub repository name (for example, `elasticsearch`). |
| `bundle.resolve` | When `true`, changelog contents are copied into bundle (default: `true`). |

:::

:::{important}
When `bundle.link_allow_repos` is omitted, no link filtering occurs.

- For private repos, set it to `[]` or add related public repos to the list.
- For public repos, add your `owner/repo` to the list at a minimum.
:::

### Bundle profiles [bundle-profiles]

Named profiles simplify bundle creation for different release scenarios.
Profiles work with both `changelog bundle` and `changelog remove` commands.

These settings are located in the `bundle.profiles.<name>` section of the configuration file.

:::{table}
:widths: description

| Profile setting | Description |
| --- | --- |
| `description` | Profile-specific description template. Overrides `bundle.description`. |
| `hide_features` | List of feature IDs to mark as hidden (commented out) in bundle output. |
| `output` | Output filename pattern (for example, `"elasticsearch/{version}.yaml"`). |
| `output_products` | Products list in bundle metadata; supports placeholders. |
| `owner` | Profile-specific GitHub owner. Overrides `bundle.owner`. |
| `products` | Product filter pattern (for example, `"elasticsearch {version} {lifecycle}"`) where placeholders are substituted at runtime. |
| `release_dates` | When `true`, bundles include a `release-date` field. Overrides `bundle.release_dates`. |
| `repo` | Profile-specific GitHub repository name. Overrides `bundle.repo`. |
| `source` | When set to `"github_release"`, fetches PR list from GitHub release instead of filtering changelogs. Mutually exclusive with `products`. |

:::

Example profile usage:

```bash
docs-builder changelog bundle elasticsearch-release 9.2.0
docs-builder changelog remove elasticsearch-release 9.2.0
```

## Extract [extract]

Controls how the `changelog add` command extracts information from PR descriptions and titles.

:::{table}
:widths: description

| Setting | Description |
| --- | --- |
| `extract.issues` | Auto-extract linked issues/PRs from descriptions (default: `true`). |
| `extract.release_notes` | Auto-extract release notes from PR descriptions (default: `true`). |
| `extract.strip_title_prefix` | Remove square-bracket prefixes from PR titles (default: `false`). |
:::

When `extract.issues` is `true`, the system looks for patterns like "Fixes #123" in PR bodies (when you're creating changelogs from PRs) or "Fixed by #123" in issue bodies (when you're creating changelogs from issues).

When `extract.release_notes` is `true`, the system looks for content like this in the PR or issue description:

- `Release Notes: ...`
- `Release-Notes: ...`
- `release notes: ...`
- `Release Note: ...`
- `Release Notes - ...`
- `## Release Note` (as a markdown header)

The extracted release note text is used in the changelog `description`.

## Filename [filename]

Controls how the `changelog add` command generates changelog file names.

:::{table}
:widths: description

| Value | Description |
| --- | --- |
| `timestamp` (default) | Use Unix timestamp with title slug (for example, `1735689600-fix-search.yaml`) |
| `pr` | Use the PR number (for example, `12345.yaml`) |
| `issue` | Use the issue number (for example, `67890.yaml`) |

:::

## Lifecycles [lifecycles]

Specifies the allowed lifecycle values for your changelogs.

:::{table}
:widths: description

| Value | Description |
| --- | --- |
| `preview` | Technical preview or early access |
| `beta` | Beta release |
| `ga` | General availability |

:::

:::{note}
The full list of possible values is defined in [Lifecycle.cs](https://github.com/elastic/docs-builder/blob/main/src/Elastic.Documentation/Lifecycle.cs).
:::

You can specify lifecycles as a comma-separated string (`"preview, beta, ga"`) or as a YAML list.

## Pivot [pivot]

Specifies the allowed area, type, and subtype values for your changelogs.
Also controls how the `changelog add` command maps GitHub labels to various changelog fields.

:::{table}
:widths: description

| Setting | Description |
| --- | --- |
| `pivot.areas` | Lists the valid area values. Optionally maps area names to GitHub labels (for example, `"Search": ":Search/Search"`). |
| `pivot.highlight` | Defines labels that set the `highlight` flag on changelogs. |
| `pivot.products` | Maps product IDs (and optionally version and lifecycle) to GitHub labels. |
| `pivot.types` | Lists the valid type values (at a minimum, `feature`, `bug-fix`, and `breaking-change`). Optionally maps types to GitHub labels (for example, `bug-fix: ">bug"`). You can also optionally define breaking change subtypes. |

:::

:::{note}
Type and subtype values must match the available values defined in [ChangelogEntryType.cs](https://github.com/elastic/docs-builder/blob/main/src/Elastic.Documentation/ChangelogEntryType.cs) and [ChangelogEntrySubtype.cs](https://github.com/elastic/docs-builder/blob/main/src/Elastic.Documentation/ChangelogEntrySubtype.cs).
:::

## Products [products]

Specifies the allowed product values and the default values used by the `changelog add` command.

:::{note}
The product values must exist in [products.yml](https://github.com/elastic/docs-builder/blob/main/config/products.yml). All products in the catalog are valid for changelogs, including those that have `public-reference` disabled.
:::

:::{table}
:widths: description

| Setting | Description |
| --- | --- |
| `products.available` | List of allowed product IDs. Empty list or omitted means all products from `products.yml` are allowed |
| `products.default` | List of default products when `--products` is not specified. |
:::

Example:
```yaml
products:
  available: ["elasticsearch", "cloud-serverless"]
  default:
    - product: elasticsearch
      lifecycle: ga
```

## Rules

Rules control which pull requests create changelogs (`rules.create`) and which changelogs are included in bundles (`rules.bundle`). Understanding these rules is essential for managing large-scale changelog workflows with multiple products and release patterns.

:::{tip}
For complex bundling scenarios involving multiple products or deployment types, refer to the [bundle rule modes](#bundle-rule-modes) section for detailed mode explanations.
:::

The `rules` section controls two key aspects of the changelog workflow: `rules.create` determines which pull requests or issues generate changelogs when you run `changelog add`, while `rules.bundle` filters which existing changelogs are included when creating release bundles. Both support global defaults and per-product overrides, with sophisticated matching behavior for multi-product scenarios.

### `rules.match` [rules-match]

Global match default for multi-valued fields (labels, areas).

| Value | Description |
|-------|-------------|
| `any` (default) | Match if ANY item in the relevant changelog field matches an item in the configured list |
| `all` | Match only if ALL items in the relevant changelog field match items in the configured list (subset semantics; see product/area tables) |
| `conjunction` | Match only if EVERY item in the configured list appears in the relevant changelog field (logical AND over the list) |

The `match` setting cascades from global to section to product:

```
rules.match (global default, "any" if omitted)
  ├─ rules.create.match → rules.create.products.{id}.match
  ├─ rules.bundle.match_products
  ├─ rules.bundle.match_areas → rules.bundle.products.{id}.match_areas
  └─ rules.publish.match_areas → rules.publish.products.{id}.match_areas  (deprecated)
```

If a lower-level `match` or `match_areas` is specified, it overrides the inherited value.

### `rules.create` [rules-create]

Filters the pull requests or issues that can generate changelogs.
Evaluated when running `docs-builder changelog add` with `--prs` or `--issues`.

| Option | Type | Description |
|--------|------|-------------|
| `exclude` | string | Comma-separated labels that prevent changelog creation. A PR with any matching label is skipped. |
| `include` | string | Comma-separated labels required for changelog creation. A PR without any matching label is skipped. |
| `match` | string | Override `rules.match` for create rules. Values: `any`, `all`, `conjunction`. |
| `products` | map | Product-specific create rules (per following section). |

You cannot specify both `exclude` and `include`.

#### Product-specific create rules (`rules.create.products`) [rules-create-products]

Product keys can be a single product ID or a comma-separated list of product IDs (for example, `'elasticsearch, kibana'`).
Each product override supports the same `exclude`, `include`, and `match` options.
Product-specific rules **override** the global create rules entirely.

```yaml
rules:
  create:
    exclude: ">non-issue"
    products:
      'elasticsearch, kibana':
        exclude: ">test"
      cloud-serverless:
        exclude: ">non-issue, ILM"
```

### `rules.bundle` [rules-bundle]

Filters the changelogs that are included in a bundle file.
Applied during `docs-builder changelog bundle` and `docs-builder changelog gh-release` after the primary filter (`--prs`, `--issues`, `--all`) has matched entries.

Input stage (gathering entries) and bundle filtering stage (filtering for output) are conceptually separate.
Which global fields take effect depends on the [bundle rule mode](#bundle-rule-modes).

#### Bundle rule modes [bundle-rule-modes]

The bundler chooses one of three modes from your parsed `rules.bundle` configuration:

| Mode | When it applies | Filtering behavior |
|------|-----------------|-------------------|
| **1 — No filtering** | There is no effective bundle rule: no `exclude_products` / `include_products`, no type/area blocker, and no non-empty `products` map. | No product, type, or area filtering from `rules.bundle`. |
| **2 — Global content** | At least one global filter or blocker is set, and `products` section is absent or empty (including `products: {}`). | Global lists are evaluated against each changelog’s own `products`, `type`, and `areas`. There is no single rule-context product and no disjoint exclusion. Changelogs with missing or empty `products` are included with a warning; global product include or exclude lists are skipped for those entries, global type and area rules still apply. |
| **3 — Per-product context** | `products` section has at least one key with a rule block. | Global `exclude_products`, `include_products`, `match_products`, and global type and area fields under `rules.bundle` are not used for filtering. configure what you need under each product key (or use Mode 2 by removing product keys). The bundle uses a single rule-context product, disjoint changelogs are excluded, and changelogs with missing or empty `products` are excluded with a warning. If the rule-context product has no per-product block, the entry passes through without global fallback. |

Configuration loading emits a hint when both global bundle fields and a non-empty `products` map are present because global keys are ignored in Mode 3.

#### Product filtering

| Option | Type | Description |
|--------|------|-------------|
| `exclude_products` | string or list | Product IDs to exclude from the bundle. Cannot be combined with `include_products`. |
| `include_products` | string or list | Only these product IDs are included; all others are excluded. Cannot be combined with `exclude_products`. |
| `match_products` | string | Override `rules.match` for product matching. Values: `any`, `all`, `conjunction`. |

#### Type and area filtering

| Option | Type | Description |
|--------|------|-------------|
| `exclude_types` | string or list | Changelog types to exclude from the bundle. |
| `include_types` | string or list | Only changelogs with these types are kept; all others are excluded. |
| `exclude_areas` | string or list | Changelog areas to exclude from the bundle. |
| `include_areas` | string or list | Only changelogs with these areas are kept; all others are excluded. |
| `match_areas` | string | Override `rules.match` for area matching. Values: `any`, `all`, `conjunction`. |
| `products` | map | Per-product type/area filter overrides (per following section). |

You cannot specify both `exclude_products` and `include_products`, both `exclude_types` and `include_types`, or both `exclude_areas` and `include_areas`. You can mix exclude and include across different fields (for example, `exclude_types` with `include_areas`).

When a changelog is excluded by `rules.bundle`, the bundling service emits a warning with a `[-bundle-exclude]`, `[-bundle-include]`, `[-bundle-type-area]`, or mode-specific prefix such as `[-bundle-global]`.

:::{important}
In **Mode 3** (non-empty `rules.bundle.products`), per-product `include_products` can **only** affect changelogs that already contain the rule context product.
It cannot pull in changelogs that are disjoint from that context.
Under **Mode 2** (global-only `rules.bundle`), `match_products: any` with `include_products` is meaningful: each changelog is evaluated on its own, so OR-style inclusion (for example, elasticsearch **or** kibana) works without per-product blocks.
:::

#### Product-specific bundle rules (`rules.bundle.products`) [rules-bundle-products]

This section applies to **Mode 3 — Per-product context** (at least one key under `rules.bundle.products` with rules).

Product keys can be a single product ID or a comma-separated list.
Each product override supports:

- **Product filtering**: `include_products`, `exclude_products`, `match_products` — controls which changelogs are included based on their product IDs
- **Type/area filtering**: `exclude_types`, `include_types`, `exclude_areas`, `include_areas`, `match_areas` — controls which changelogs are included based on their type and area fields

When a per-product rule applies to a changelog, it is used **instead of** any global `rules.bundle` filters (global bundle keys are not applied in Mode 3). If a filter type is omitted in that per-product block, it is not inherited from global `rules.bundle` — repeat the constraint under the product key if you still need it.

If the changelog contains the rule context product but **no** block exists for that product ID, the entry is **included** without applying global bundle filters (**pass-through**).

**Example (Mode 3)**

Per-product rule replaces global keys for matching entries:

```yaml
rules:
  bundle:
    exclude_types: [docs]
    exclude_areas: [Internal]
    products:
      kibana:
        include_areas: [UI]
```

The result when the rule context product is `kibana` is as follows:

- Elasticsearch-only entries: **excluded** as disjoint (they never use the `kibana` block).
- Kibana entries: only the `kibana` block applies — global `exclude_types` / `exclude_areas` do not apply. Add `exclude_types` under `kibana` if you still need to drop `docs` entries.

**Mode 3** uses a **single rule-context product** for each bundle:

1. **Rule context product**:
   - **With `--output-products`** (CLI) or profile **`output_products`**: First product alphabetically from that list
   - **Without** those: First product alphabetically from all products aggregated from matched changelog entries (regardless of input method: `--all`, `--prs`, `--issues`, etc.)

2. **For each changelog**:
   - **Contains rule context product** and a per-product block exists for that product: apply that block (product rules, then type/area for that block).
   - **Contains rule context product** and **no** per-product block for that product: **include** without applying global `rules.bundle` filters (pass-through).
   - **Disjoint** from rule context: **exclude** the changelog.
   - **No products** in the changelog: emit a warning and **exclude** the changelog.

**Input method independence**: Bundle filtering still applies regardless of how entries were gathered (`--input-products`, `--prs`, `--all`, etc.).

For example, with `--output-products "kibana 9.3.0, security 9.3.0"` (rule context: `kibana`):

| Changelog `products` | Contains `kibana`? | Result (Mode 3) |
|--------------------|-------------------|---------|
| `[kibana]` | Yes | Use `kibana` per-product rules if defined; otherwise pass-through |
| `[security]` | No | **Excluded** (disjoint) |
| `[kibana, security]` | Yes | Use `kibana` per-product rules if defined; otherwise pass-through |
| `[elasticsearch]` | No | **Excluded** (disjoint) |
| `[]` (empty/missing) | No | **Excluded** with warning |

**Multi-product bundles**: For release notes that need different rule behavior per product, run **separate** `changelog bundle` invocations with a **single** product in `--output-products` (or a profile whose `output_products` resolves to one product), or use **Mode 2** (global-only `rules.bundle`) when you need OR-style product inclusion without disjoint exclusion.

### `rules.publish`

:::{warning}
`rules.publish` is deprecated and **no longer used by the `changelog render` command**. The command now only supports `rules.bundle` for type/area filtering at bundle time. Using `rules.publish` emits a deprecation warning during configuration loading.
:::

`rules.publish` is ignored by the `changelog render` command and will be removed in a future release. If you have `rules.publish` configured, move your type/area filtering to `rules.bundle` so it applies at bundle time.

**Deprecated (no longer used):**

```yaml
rules:
  publish:
    exclude_types: docs
    exclude_areas:
      - Internal
```

**Use instead:**

```yaml
rules:
  bundle:
    exclude_types: docs
    exclude_areas:
      - Internal
```
