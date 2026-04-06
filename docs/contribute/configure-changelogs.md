# Configure changelogs

Before you can use the `docs-builder changelog` commands in your development workflow, you must make some decisions and do some setup steps:

1. Ensure that your products exist in [products.yml](https://github.com/elastic/docs-builder/blob/main/config/products.yml).
1. Add labels to your GitHub pull requests that map to [changelog types](https://github.com/elastic/docs-builder/blob/main/src/Elastic.Documentation/ChangelogEntryType.cs). At a minimum, create labels for the `feature`, `bug-fix`, and `breaking-change` types.
1. Optional: Choose areas or components that your changes affect and add labels to your GitHub pull requests (such as `:Analytics/Aggregations`).
1. Optional: Add labels to your GitHub pull requests to indicate that they are not notable and should not generate changelogs. For example, `non-issue` or `release_notes:skip`. Alternatively, you can assume that all PRs are *not* notable unless a specific label is present (for example, `@Public`).

After you collect all this information, you can use it to make the changelog process more automated and repeatable by setting up a changelog configuration file.

## Create a changelog configuration file [changelog-settings]

The changelog configuration file:

- Defines acceptable product, type, subtype, and lifecycle values.
- Sets default options, such as whether to extract issues and release note text from pull requests.
- Defines profiles for simplified bundle creation.
- Prevents the creation of changelogs when certain labels are present.
- Excludes changelogs from bundles based on their areas, types, or products.

:::{tip}
Only one configuration file is required for each repository.
You must maintain the file if your repo labels change over time.
:::

You can use the [docs-builder changelog init](/cli/changelog/init.md) command to create the changelog configuration file and folder structure automatically.
The command uses an existing docs folder or creates `{path}/docs` when it does not exist.
It creates a `changelog.yml` file in the `docs` folder and creates sub-folders for the changelog and bundle files.
Alternatively, you can create the file and folders manually.


Refer to [changelog.example.yml](https://github.com/elastic/docs-builder/blob/main/config/changelog.example.yml).

By default, the changelog commands check the following path: `docs/changelog.yml`.
You can specify a different path with the `--config` command option.

If a configuration file exists, the command validates its values before generating changelog files:

- If the configuration file contains `lifecycles`, `products`, `subtype`, or `type` values that don't match the values in `ChangelogEntryType.cs`, `ChangelogEntrySubtype.cs`, or `Lifecycle.cs`, validation fails.
- If the configuration file contains `areas` values and they don't match what you specify in the `--areas` command option, validation fails.
- If the configuration file contains `lifecycles` or `products` values are a subset of the available values and you try to create a changelog with values outside that subset, validation fails.

In each of these cases where validation fails, a changelog file is not created.

### GitHub label mappings

When you run the `docs-builder changelog add` command with the `--prs` or `--issues` options, it can use label mappings in the changelog configuration file to infer the changelog `type`, `areas`, and `products` fields from your GitHub labels.

Refer to the file layout in [changelog.example.yml](https://github.com/elastic/docs-builder/blob/main/config/changelog.example.yml) and an [example usage](/contribute/create-changelogs.md#example-map-label).

### Rules for creation and bundling

If you have pull request labels that indicate a changelog is not required (such as `>non-issue` or `release_note:skip`), you can declare these in the `rules.create` section of the changelog configuration.

When you run the `docs-builder changelog add` command with the `--prs` or `--issues` options and the pull request or issue has one of the identified labels, the command does not create a changelog.

Likewise, if you want to exclude changelogs with certain products, areas, or types from the release bundles, you can declare these in the `rules.bundle` section of the changelog configuration.
For example, you might choose to omit `other` or `docs` changelogs.
Or you might want to omit all autoscaling-related changelogs from the Cloud Serverless release bundles.

The `changelog render` command does **not** apply `rules.publish`; filtering must be done at bundle time via `rules.bundle`.
[Changelog directives](/contribute/publish-changelogs.md#changelog-directive) also do not apply `rules.publish`.

:::{warning}
`rules.publish` is deprecated and no longer used by the `changelog render` command. Move your type/area filtering to `rules.bundle` so it applies at bundle time. Using `rules.publish` emits a deprecation warning during configuration loading.
:::

Each field supports **exclude** (block if matches) or **include** (block if doesn't match) semantics. You cannot mix both for the same field.

For multi-valued fields (labels, areas), you can control the matching mode:
- `any` (default): match if ANY item matches the list
- `all`: match only if ALL items match the list

:::{note}
You can define rules at the global level (applies to all products) or for specific products.
Product-specific rules **override** the global rules entirely—they do not merge.
If you define a product-specific `publish` rule, you must re-state any global rules that you also want applied for that product.
For `rules.bundle`, global versus per-product behavior is described under [bundle rule modes](#bundle-rule-modes).
:::

Refer to [changelog.example.yml](https://github.com/elastic/docs-builder/blob/main/config/changelog.example.yml) and an [example usage](/contribute/create-changelogs.md#example-block-label).

### Rules reference

The `rules:` section supports the following options:

#### `rules.match`

Global match default for multi-valued fields (labels, areas). Inherited by `create`, `publish`, and all product overrides.

| Value | Description |
|-------|-------------|
| `any` (default) | Match if ANY item in the relevant changelog field matches an item in the configured list |
| `all` | Match only if ALL items in the relevant changelog field match items in the configured list (subset semantics; see product/area tables) |
| `conjunction` | Match only if EVERY item in the configured list appears in the relevant changelog field (logical AND over the list) |

#### `rules.create`

Filters the pull requests or issues that can generate changelogs.
Evaluated when running `docs-builder changelog add` with `--prs` or `--issues`.

| Option | Type | Description |
|--------|------|-------------|
| `exclude` | string | Comma-separated labels that prevent changelog creation. A PR with any matching label is skipped. |
| `include` | string | Comma-separated labels required for changelog creation. A PR without any matching label is skipped. |
| `match` | string | Override `rules.match` for create rules. Values: `any`, `all`, `conjunction`. |
| `products` | map | Product-specific create rules (see below). |

You cannot specify both `exclude` and `include`.

##### Product-specific create rules (`rules.create.products`)

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

#### `rules.bundle`

Filters the changelogs that are included in a bundle file.
Applied during `docs-builder changelog bundle` and `docs-builder changelog gh-release` after the primary filter (`--prs`, `--issues`, `--all`) has matched entries.

Input stage (gathering entries) and bundle filtering stage (filtering for output) are conceptually separate.
Which global fields take effect depends on the [bundle rule mode](#bundle-rule-modes).

##### Bundle rule modes [bundle-rule-modes]

The bundler chooses one of three modes from your parsed `rules.bundle` configuration:

| Mode | When it applies | Filtering behavior |
|------|-----------------|-------------------|
| **1 — No filtering** | There is no effective bundle rule: no `exclude_products` / `include_products`, no type/area blocker, and no non-empty `products` map. | No product, type, or area filtering from `rules.bundle`. |
| **2 — Global content** | At least one global filter or blocker is set, and **`products` is absent or empty** (including `products: {}`). | Global lists are evaluated against **each changelog’s own** `products`, `type`, and `areas`. There is **no** single rule-context product and **no** disjoint exclusion. Changelogs with **missing or empty** `products` are **included** with a warning; product include/exclude lists are skipped for those entries. Type/area rules still apply. |
| **3 — Per-product context** | **`products` has at least one key** with a rule block. | **Global** `exclude_products`, `include_products`, `match_products`, and global type/area fields under `rules.bundle` are **not used for filtering** — configure what you need under each product key (or use Mode 2 by removing product keys). The bundle uses a **single rule-context product**, **disjoint** changelogs are excluded, and **missing/empty** `products` are excluded with a warning. If the rule-context product has no per-product block, the entry **passes through** without global fallback. |

Configuration loading may emit a **hint** when both global bundle fields and a non-empty `products` map are present, because global keys are ignored in Mode 3.

##### Product filtering

| Option | Type | Description |
|--------|------|-------------|
| `exclude_products` | string or list | Product IDs to exclude from the bundle. Cannot be combined with `include_products`. |
| `include_products` | string or list | Only these product IDs are included; all others are excluded. Cannot be combined with `exclude_products`. |
| `match_products` | string | Override `rules.match` for product matching. Values: `any`, `all`, `conjunction`. |

##### Type and area filtering

| Option | Type | Description |
|--------|------|-------------|
| `exclude_types` | string or list | Changelog types to exclude from the bundle. |
| `include_types` | string or list | Only changelogs with these types are kept; all others are excluded. |
| `exclude_areas` | string or list | Changelog areas to exclude from the bundle. |
| `include_areas` | string or list | Only changelogs with these areas are kept; all others are excluded. |
| `match_areas` | string | Override `rules.match` for area matching. Values: `any`, `all`, `conjunction`. |
| `products` | map | Per-product type/area filter overrides (see below). |

You cannot specify both `exclude_products` and `include_products`, both `exclude_types` and `include_types`, or both `exclude_areas` and `include_areas`. You can mix exclude and include across different fields (for example, `exclude_types` with `include_areas`).

When a changelog is excluded by `rules.bundle`, the bundling service emits a warning with a `[-bundle-exclude]`, `[-bundle-include]`, `[-bundle-type-area]`, or mode-specific prefix such as `[-bundle-global]`.

:::{important}
In **Mode 3** (non-empty `rules.bundle.products`), per-product `include_products` can **only** affect changelogs that already contain the rule context product.
It cannot pull in changelogs that are disjoint from that context.
Under **Mode 2** (global-only `rules.bundle`), `match_products: any` with `include_products` is meaningful: each changelog is evaluated on its own, so OR-style inclusion (for example, elasticsearch **or** kibana) works without per-product blocks.
:::

##### Product-specific bundle rules (`rules.bundle.products`)

This section applies to **Mode 3 — Per-product context** (at least one key under `rules.bundle.products` with rules).

Product keys can be a single product ID or a comma-separated list.
Each product override supports:

- **Product filtering**: `include_products`, `exclude_products`, `match_products` — controls which changelog entries are included based on their product IDs
- **Type/area filtering**: `exclude_types`, `include_types`, `exclude_areas`, `include_areas`, `match_areas` — controls which entries are included based on their type and area fields

When a per-product rule applies to an entry, it is used **instead of** any global `rules.bundle` filters (**global bundle keys are not applied** in Mode 3). If a filter type is omitted in that per-product block, it is not inherited from global `rules.bundle` — repeat the constraint under the product key if you still need it.

If the changelog contains the rule context product but **no** block exists for that product ID, the entry is **included** without applying global bundle filters (**pass-through**).

**Example (Mode 3): per-product rule replaces global keys for matching entries**

```yaml
rules:
  bundle:
    exclude_types: [docs]
    exclude_areas: [Internal]
    products:
      kibana:
        include_areas: [UI]
```

**Result** when the rule context product is `kibana`:

- Elasticsearch-only entries: **excluded** as disjoint (they never use the `kibana` block).
- Kibana entries: only the `kibana` block applies — global `exclude_types` / `exclude_areas` do not apply. Add `exclude_types` under `kibana` if you still need to drop `docs` entries.

###### Single-product rule resolution (Mode 3 only) [changelog-bundle-rule-resolution]

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

#### `rules.publish`

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

#### Match inheritance

The `match` setting cascades from global to section to product:

```
rules.match (global default, "any" if omitted)
  ├─ rules.create.match → rules.create.products.{id}.match
  ├─ rules.bundle.match_products
  ├─ rules.bundle.match_areas → rules.bundle.products.{id}.match_areas
  └─ rules.publish.match_areas → rules.publish.products.{id}.match_areas  (deprecated)
```

If a lower-level `match` or `match_areas` is specified, it overrides the inherited value.

#### Product matching behavior

The following applies to **global** `rules.bundle` product lists (**Mode 2**). In **Mode 3**, product matching uses the per-product block for the rule context product instead.

With `match_products`, the behavior differs depending on the mode. The keyword **`conjunction`** means *every product ID in the config list must appear on the changelog* (logical AND). It does not refer to the `include_products` / `exclude_products` field names — those choose **which** list and **include vs exclude**; `match_products` chooses **how** that list is interpreted.

| Config | Changelog `products` | `match_products` | Result |
|--------|----------------|----------------|--------|
| `exclude_products: [cloud-enterprise]` | `[cloud-enterprise, kibana]` | `any` | **Excluded** ("cloud-enterprise" matches) |
| `exclude_products: [cloud-enterprise]` | `[cloud-enterprise, kibana]` | `all` | **Included** (not all products are in the exclude list) |
| `exclude_products: [kibana, observability]` | `[kibana]` | `conjunction` | **Included** (not every listed exclude ID is on the changelog) |
| `exclude_products: [kibana, observability]` | `[kibana, observability]` | `conjunction` | **Excluded** (every listed exclude ID is on the changelog) |
| `include_products: [elasticsearch]` | `[elasticsearch, kibana]` | `any` | **Included** ("elasticsearch" matches) |
| `include_products: [elasticsearch]` | `[elasticsearch, kibana]` | `all` | **Excluded** ("kibana" is not in the include list) |
| `include_products: [elasticsearch, security]` | `[elasticsearch, security, kibana]` | `conjunction` | **Included** (every listed include ID is on the changelog) |
| `include_products: [elasticsearch, security]` | `[elasticsearch]` | `conjunction` | **Excluded** ("security" is missing from the changelog) |

In practice, most changelogs have a single product, so `any` (the default) and `all` behave identically for them.
The difference only matters for changelogs with multiple products.

#### Area matching behavior

With `match_areas` (applies to both `rules.bundle` and `rules.publish`), the behavior differs depending on the mode. As with products, **`conjunction`** means every area in the config list must appear on the changelog.

| Config | Changelog `areas` | `match_areas` | Result |
|--------|------------|-------------|--------|
| `exclude_areas: [Internal]` | `[Search, Internal]` | `any` | **Excluded** ("Internal" matches) |
| `exclude_areas: [Internal]` | `[Search, Internal]` | `all` | **Included** (not all areas are in the exclude list) |
| `exclude_areas: [Search, Internal]` | `[Search]` | `conjunction` | **Included** ("Internal" is not on the changelog) |
| `exclude_areas: [Search, Internal]` | `[Search, Internal, Monitoring]` | `conjunction` | **Excluded** (every listed exclude area is on the changelog) |
| `include_areas: [Search]` | `[Search, Internal]` | `any` | **Included** ("Search" matches) |
| `include_areas: [Search]` | `[Search, Internal]` | `all` | **Excluded** ("Internal" is not in the include list) |
| `include_areas: [Search, Internal]` | `[Search, Internal]` | `conjunction` | **Included** (every listed include area is on the changelog) |
| `include_areas: [Search, Internal]` | `[Search]` | `conjunction` | **Excluded** ("Internal" is missing from the changelog) |

#### Validation

The following configurations cause validation errors:

| Condition | Error |
|-----------|-------|
| Old `block:` key found | `'block' is no longer supported. Rename to 'rules'. See changelog.example.yml.` |
| Both `exclude` and `include` in create | `rules.create: cannot have both 'exclude' and 'include'. Use one or the other.` |
| Both `exclude_types` and `include_types` in bundle | `rules.bundle: cannot have both 'exclude_types' and 'include_types'. Use one or the other.` |
| Both `exclude_areas` and `include_areas` in bundle | `rules.bundle: cannot have both 'exclude_areas' and 'include_areas'. Use one or the other.` |
| Both `exclude_products` and `include_products` in bundle | `rules.bundle: cannot have both 'exclude_products' and 'include_products'. Use one or the other.` |
| Both `exclude_types` and `include_types` in publish | `rules.publish: cannot have both 'exclude_types' and 'include_types'. Use one or the other.` |
| Both `exclude_areas` and `include_areas` in publish | `rules.publish: cannot have both 'exclude_areas' and 'include_areas'. Use one or the other.` |
| `rules.publish` present | Deprecation warning: `rules.publish is deprecated. Move type/area filtering to rules.bundle.` |
| Invalid match value | `rules.match: '{value}' is not valid. Use 'any', 'all', or 'conjunction'.` |
| Unknown product ID in bundle | `rules.bundle.exclude_products: '{id}' is not in the list of available products.` |
| Unknown product ID | `rules.create.products: '{id}' not in available products.` |



### Ineffective configuration patterns [ineffective-configuration-patterns]

Some `rules.bundle` combinations are valid YAML but do not filter the way you might expect:

- **`match_products: any` with `include_products` in a per-product rule** provides no selective filtering in the common case. Consider `match_products: all` for strict filtering or `exclude_products` for exclusion-based filtering.
- **Disjoint products in `include_products`** in a per-product rule: If you list more than one product ID and some are disjoint from the rule context product, those changelogs cannot be included. Use separate bundles (each with a single product in `--output-products` or profile `output_products`), or multi-product changelogs instead.

For how global `rules.bundle` fields interact with `products` keys, see [Bundle rule modes](#bundle-rule-modes).
