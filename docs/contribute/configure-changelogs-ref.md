---
navigation_title: Configuration reference
---
# Changelog configuration reference

The changelog configuration file contains settings to make the creation of changelog files and bundles more consistent and repeatable. This file should be placed at `docs/changelog.yml` in your repository and defines all aspects of the changelog workflow from file naming to bundle creation.

For the most up-to-date changelog configuration options and examples, refer to [changelog.example.yml](https://github.com/elastic/docs-builder/blob/main/config/changelog.example.yml).

## At a glance [at-a-glance]

These are the main configuration sections available in your `changelog.yml` file:

:::{table}
:widths: description

| Section | Purpose |
| --- | --- |
| `filename` | Controls how `changelog add` names generated files |
| `products` | Defines available products and defaults when `--products` is not specified |
| `extract` | Configures automatic extraction of release notes and issues from PR descriptions |
| `lifecycles` | Specifies allowed lifecycle values (preview, beta, ga) |
| `pivot` | Maps GitHub labels to changelog types, areas, and products |
| `rules` | Filters which PRs create changelogs and which changelogs appear in bundles |
| `bundle` | Sets directory paths, GitHub defaults, and named profiles for bundling |
:::

## Filename [filename]

Controls how the `changelog add` command names generated changelog files.

:::{table}
:widths: description

| Strategy | Description |
| --- | --- |
| `timestamp` (default) | Use Unix timestamp with title slug (e.g., `1735689600-fix-search.yaml`) |
| `pr` | Use the PR number (e.g., `12345.yaml`) |
| `issue` | Use the issue number (e.g., `67890.yaml`) |
:::

You can override this setting per invocation with the `--use-pr-number` or `--use-issue-number` CLI flags.

## Products [products]

Defines which products are available for use and sets defaults when the `--products` option is not specified.

:::{table}
:widths: description

| Setting | Description |
| --- | --- |
| `products.available` | List of allowed product IDs. Empty list or omitted means all products from `products.yml` are allowed |
| `products.default` | List of default products when `--products` is not specified. Each entry contains `product` (required) and `lifecycle` (defaults to `ga`) |
:::

Example:
```yaml
products:
  available: ["elasticsearch", "kibana", "cloud-serverless"]
  default:
    - product: elasticsearch
      lifecycle: ga
```

## Extract [extract]

Configures automatic extraction of information from PR descriptions and titles.

:::{table}
:widths: description

| Setting | Description |
| --- | --- |
| `extract.release_notes` | Auto-extract release notes from PR descriptions (default: `true`). Override with `--no-extract-release-notes` |
| `extract.issues` | Auto-extract linked issues/PRs from descriptions (default: `true`). Override with `--no-extract-issues` |
| `extract.strip_title_prefix` | Remove square-bracket prefixes from PR titles (default: `false`). Override with `--strip-title-prefix` |
:::

When `extract.issues` is enabled, the system looks for patterns like "Fixes #123" in PR bodies when using `--prs`, or "Fixed by #123" in issue bodies when using `--issues`.

## Lifecycles [lifecycles]

Specifies the allowed lifecycle values for changelog entries. These values are strongly typed and validated.

:::{table}
:widths: description

| Value | Description |
| --- | --- |
| `preview` | Technical preview or early access |
| `beta` | Beta release |
| `ga` | General availability |
:::

You can specify lifecycles as a comma-separated string (`"preview, beta, ga"`) or as a YAML list.

## Pivot [pivot]

Configures the mapping between GitHub labels and changelog fields. This is the core configuration that determines how PR labels translate into structured changelog data.

### Types [pivot-types]

Defines changelog types and their associated GitHub labels. At minimum, you must configure `feature`, `bug-fix`, and `breaking-change`.

:::{table}
:widths: description

| Configuration | Description |
| --- | --- |
| Simple string | Comma-separated labels (e.g., `bug-fix: ">bug, >fix"`) |
| YAML list | List of labels (e.g., `[">bug", ">fix"]`) |
| Empty/null | No labels for this type (manual assignment only) |
| Object (breaking-change only) | Contains `labels` and `subtypes` for categorizing breaking changes |
:::

The `breaking-change` type can include subtypes like `api`, `behavioral`, `configuration`, `dependency`, `subscription`, `plugin`, `security`, and `other`.

### Other pivot settings [pivot-other]

:::{table}
:widths: description

| Setting | Description |
| --- | --- |
| `pivot.highlight` | Labels that set the highlight flag on changelog entries |
| `pivot.areas` | Maps area display names to GitHub labels (e.g., `"Search": ":Search/Search"`) |
| `pivot.products` | Maps product spec strings to labels. Product specs can include version and lifecycle (e.g., `"elasticsearch 9.2.0 ga"`) |
:::

Product precedence: CLI `--products` option > pivot label mapping > `products.default` > repository inference.

## Rules

Rules control which pull requests create changelogs (`rules.create`) and which changelogs are included in bundles (`rules.bundle`). Understanding these rules is essential for managing large-scale changelog workflows with multiple products and release patterns.

:::{tip}
For complex bundling scenarios involving multiple products or deployment types, refer to the [bundle rule modes](#bundle-rule-modes) section for detailed mode explanations.
:::

The `rules` section controls two key aspects of the changelog workflow: `rules.create` determines which pull requests or issues generate changelog entries when you run `changelog add`, while `rules.bundle` filters which existing changelogs are included when creating release bundles. Both support global defaults and per-product overrides, with sophisticated matching behavior for multi-product scenarios.

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

- **Product filtering**: `include_products`, `exclude_products`, `match_products` — controls which changelog entries are included based on their product IDs
- **Type/area filtering**: `exclude_types`, `include_types`, `exclude_areas`, `include_areas`, `match_areas` — controls which entries are included based on their type and area fields

When a per-product rule applies to an entry, it is used **instead of** any global `rules.bundle` filters (global bundle keys are not applied in Mode 3). If a filter type is omitted in that per-product block, it is not inherited from global `rules.bundle` — repeat the constraint under the product key if you still need it.

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

## Bundle settings [bundle-settings]

Configures directory paths, GitHub repository defaults, and named profiles for bundle operations. These settings are separate from `rules.bundle` filtering.

### Basic settings [bundle-basic]

:::{table}
:widths: description

| Setting | Description |
| --- | --- |
| `bundle.directory` | Input directory containing changelog YAML files (default: `docs/changelog`) |
| `bundle.output_directory` | Output directory for bundled files (default: `docs/releases`) |
| `bundle.resolve` | Whether to copy changelog contents into bundle entries array (default: `true`) |
| `bundle.description` | Default description template supporting `{version}`, `{lifecycle}`, `{owner}`, and `{repo}` placeholders |
| `bundle.repo` | Default GitHub repository name applied to profiles that don't specify their own |
| `bundle.owner` | Default GitHub repository owner (e.g., `elastic`) |
:::

### Link filtering [bundle-links]

:::{table}
:widths: description

| Setting | Description |
| --- | --- |
| `bundle.link_allow_repos` | List of `owner/repo` pairs whose PR/issue links are preserved. When set (including empty `[]`), links to unlisted repos become `# PRIVATE:` sentinels. Requires `bundle.resolve: true` |
:::

When omitted entirely, no link filtering is applied. To allow links only from your own repository, include your `owner/repo` in the list.

### Bundle profiles [bundle-profiles]

Named profiles simplify bundle creation for different release scenarios. Profiles work with both `changelog bundle` and `changelog remove` commands.

:::{table}
:widths: description

| Profile setting | Description |
| --- | --- |
| `products` | Product filter pattern (e.g., `"elasticsearch {version} {lifecycle}"`) where placeholders are substituted at runtime |
| `output` | Output filename pattern (e.g., `"elasticsearch-{version}.yaml"`) |
| `output_products` | Override the products array in bundle output, supports placeholders |
| `description` | Profile-specific description template (overrides `bundle.description`) |
| `repo` | Profile-specific GitHub repository name |
| `owner` | Profile-specific GitHub owner |
| `hide_features` | List of feature IDs to mark as hidden (commented out) in bundle output |
| `source` | When set to `"github_release"`, fetches PR list from GitHub release instead of filtering changelogs. Mutually exclusive with `products` |
:::

Example profile usage:
```bash
docs-builder changelog bundle elasticsearch-release 9.2.0
docs-builder changelog remove elasticsearch-release 9.2.0
```

When used with `changelog remove`, only the `products` field is applied—other fields like `output` and `hide_features` are ignored.
