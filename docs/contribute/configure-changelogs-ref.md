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

| Section                   | Purpose                                                                           |
| ------------------------- | --------------------------------------------------------------------------------- |
| [bundle](#bundle)         | Sets directory paths, GitHub defaults, and named profiles for bundling.           |
| [extract](#extract)       | Configures automatic extraction of release notes and issues from PR descriptions. |
| [filename](#filename)     | Controls how `changelog add` names generated files.                               |
| [lifecycles](#lifecycles) | Specifies allowed lifecycle values.                                               |
| [pivot](#pivot)           | Maps GitHub labels to changelog types, areas, and products.                       |
| [products](#products)     | Defines available products and defaults.                                          |
| [rules](#rules)           | Filters which PRs create changelogs and which changelogs appear in bundles.       |

:::

## Bundle

Configures directory paths, GitHub repository defaults, and named profiles for bundle operations.
These settings are separate from `rules.bundle` filtering.

### Basic settings [bundle-basic]

Controls bundle-level behavior.
These settings are relevant to one or all of the `changelog bundle`, `changelog gh-release`, and `changelog remove` commands.

:::{table}
:widths: description


| Setting                   | Description                                                                                                                                                                            |
| ------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `bundle.description`      | Default template for bundle descriptions. Supports `{version}`, `{lifecycle}`, `{owner}`, and `{repo}` placeholders.                                                                   |
| `bundle.directory`        | Input directory containing changelog YAML files (default: `docs/changelog`).                                                                                                           |
| `bundle.link_allow_repos` | List of `owner/repo` pairs whose PR/issue links are preserved. When set (including empty `[]`), links to unlisted repos become `# PRIVATE:` sentinels. Requires `bundle.resolve: true` |
| `bundle.output_directory` | Output directory for bundled files (default: `docs/releases`).                                                                                                                         |
| `bundle.owner`            | Default GitHub repository owner (for example, `elastic`).                                                                                                                              |
| `bundle.release_dates`    | When `true`, bundles include a `release-date` field (default: true).                                                                                                                   |
| `bundle.repo`             | Default GitHub repository name (for example, `elasticsearch`).                                                                                                                         |
| `bundle.resolve`          | When `true`, changelog contents are copied into bundle (default: `true`).                                                                                                              |


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


| Profile setting   | Description                                                                                                                              |
| ----------------- | ---------------------------------------------------------------------------------------------------------------------------------------- |
| `description`     | Profile-specific description template. Overrides `bundle.description`.                                                                   |
| `hide_features`   | List of feature IDs to mark as hidden (commented out) in bundle output.                                                                  |
| `output`          | Output filename pattern (for example, `"elasticsearch/{version}.yaml"`).                                                                 |
| `output_products` | Products list in bundle metadata; supports placeholders.                                                                                 |
| `owner`           | Profile-specific GitHub owner. Overrides `bundle.owner`.                                                                                 |
| `products`        | Product filter pattern (for example, `"elasticsearch {version} {lifecycle}"`) where placeholders are substituted at runtime.             |
| `release_dates`   | When `true`, bundles include a `release-date` field. Overrides `bundle.release_dates`.                                                   |
| `repo`            | Profile-specific GitHub repository name. Overrides `bundle.repo`.                                                                        |
| `source`          | When set to `"github_release"`, fetches PR list from GitHub release instead of filtering changelogs. Mutually exclusive with `products`. |


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


| Setting                      | Description                                                         |
| ---------------------------- | ------------------------------------------------------------------- |
| `extract.issues`             | Auto-extract linked issues/PRs from descriptions (default: `true`). |
| `extract.release_notes`      | Auto-extract descriptions from GitHub (default: `true`).  |
| `extract.strip_title_prefix` | Remove square-bracket prefixes from PR titles (default: `false`).   |

When `extract.issues` is `true`, the system looks for patterns like "Fixes #123" in PR bodies (when you're creating changelogs from PRs) or "Fixed by #123" in issue bodies (when you're creating changelogs from issues).

When `extract.release_notes` is `true`, the system looks for content like this in the PR or issue description:

- `Release Notes: ...`
- `Release-Notes: ...`
- `release notes: ...`
- `Release Note: ...`
- `Release Notes - ...`
- `## Release Note` (as a markdown header)

The extracted release note text is used in the changelog `description`.

When `extract.strip_title_prefix` is `true` and PR or issue titles have a prefix in square brackets (such as `[ES|QL]` or `[Security]`), they are automatically removed from the changelog title.
Multiple square bracket prefixes are also supported (for example `[Discover][ESQL] Title` becomes `Title`).
If a colon follows the closing bracket, it is also removed.

:::{note}
The title cleanup only occurs when the title is derived from GitHub. If you specify `--title` explicitly, that title is used as-is without any prefix stripping.
:::

## Filename [filename]

Controls how the `changelog add` command generates changelog file names.

:::{table}
:widths: description

| Value                 | Description                                                                    |
| --------------------- | ------------------------------------------------------------------------------ |
| `timestamp` (default) | Use Unix timestamp with title slug (for example, `1735689600-fix-search.yaml`) |
| `pr`                  | Use the PR number (for example, `12345.yaml`)                                  |
| `issue`               | Use the issue number (for example, `67890.yaml`)                               |

:::

## Lifecycles [lifecycles]

Specifies the allowed lifecycle values for your changelogs.

:::{table}
:widths: description

| Value     | Description                       |
| --------- | --------------------------------- |
| `preview` | Technical preview or early access |
| `beta`    | Beta release                      |
| `ga`      | General availability              |

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

| Setting           | Description                                |
| ----------------- | ------------------------------------------ |
| `pivot.areas`     | Lists the valid area values. Optionally maps area names to GitHub labels (for example, `"Search": ":Search/Search"`). |
| `pivot.highlight` | Defines labels that set the `highlight` flag on changelogs. |
| `pivot.products`  | Maps product IDs (and optionally version and lifecycle) to GitHub labels. |
| `pivot.types`     | Lists the valid type values (at a minimum, `feature`, `bug-fix`, and `breaking-change`). Optionally maps types to GitHub labels (for example, `bug-fix: ">bug"`). You can also optionally define breaking change subtypes. |

:::

:::{note}
Type and subtype values must match the available values defined in [ChangelogEntryType.cs](https://github.com/elastic/docs-builder/blob/main/src/Elastic.Documentation/ChangelogEntryType.cs) and [ChangelogEntrySubtype.cs](https://github.com/elastic/docs-builder/blob/main/src/Elastic.Documentation/ChangelogEntrySubtype.cs).
:::

The following example demonstrates some GitHub label mappings in the `pivot` section of the changelog configuration file:
```yaml
pivot:
  types:
    # Example mappings - customize based on your label naming conventions
    breaking-change:
      labels: ">breaking, >bc"
    bug-fix: ">bug"
    enhancement: ">enhancement"
  areas:
    # Example mappings - customize based on your label naming conventions
    Autoscaling: ":Distributed Coordination/Autoscaling"
    "ES|QL": ":Search Relevance/ES|QL"
  products:
    'elasticsearch':
      - ":product/elasticsearch"
    'cloud-serverless':
      - ":product/serverless"
```

## Products [products]

Specifies the allowed product values and the default values used by the `changelog add` command.

:::{note}
The product values must exist in [products.yml](https://github.com/elastic/docs-builder/blob/main/config/products.yml). All products in the catalog are valid for changelogs, including those that have `public-reference` disabled.
:::

:::{table}
:widths: description

| Setting              | Description                                                                                           |
| -------------------- | ----------------------------------------------------------------------------------------------------- |
| `products.available` | List of allowed product IDs. Empty list or omitted means all products from `products.yml` are allowed |
| `products.default`   | List of default products when `--products` is not specified.                                          |

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

Provides two key ways to customize the changelog workflow:

- `rules.create` defines GitHub labels that turn changelog creation on or off
- `rules.bundle` defines changelog types, areas, or product combinations that are included or excluded from release bundles

Understanding these rules is essential for managing large-scale changelog workflows with multiple products and release patterns.

### `rules.match` [rules-match]

Defines global default match behavior for all rules.

| Value           | Description |
| --------------- | -------------------------------------- |
| `any` (default) | Match if ANY item in the relevant changelog field matches an item in the configured list |
| `all`           | Match only if ALL items in the relevant changelog field match items in the configured list (subset semantics; see product/area tables) |
| `conjunction`   | Match only if every item in the configured list appears in the relevant changelog field (logical AND over the list) |

If a lower-level `match` or `match_areas` is specified (for example, `rules.create.match` or `rules.bundle.match_areas`), it overrides the inherited value.

### `rules.create` [rules-create]

Defines the PR or issue labels that affect the creation of changelogs.
These rules affect the `docs-builder changelog add` command when it's pulling information from GitHub (for example using `--prs` or `--issues` command options).

These settings are located in the `rules.create` section of the configuration file:

| Setting    | Type   | Description               |
| ---------- | ------ | ------------------------- |
| `exclude`  | string | Comma-separated list of labels that prevent changelog creation. |
| `include`  | string | Comma-separated list of labels that enable changelog creation. |
| `match`    | string | Override `rules.match` for create rules. Values: `any`, `all`, `conjunction`. |
| `products` | map    | Product-specific create rules. |

:::{important}
You cannot specify both `exclude` and `include`.
:::

Product keys in `rules.create.products` can be a single product ID or a comma-separated list of product IDs (for example, `'elasticsearch, kibana'`).
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

Controls which changelogs are included in bundles.
These rules are applied by the `docs-builder changelog bundle` and `docs-builder changelog gh-release` commands **after** the primary filter (`--prs`, `--issues`, `--all`, or `--input-products`) has identified the relevant changelogs.

These settings are located in the `rules.bundle` section of the configuration file:

| Setting            | Type           | Description |
| ------------------ | -------------- | ----------- |
| `exclude_areas`    | string or list | Changelog areas to exclude from the bundle. |
| `exclude_products` | string or list | Changelog products to exclude from the bundle. |
| `exclude_types`    | string or list | Changelog types to exclude from the bundle. |
| `include_areas`    | string or list | Only changelogs with these areas are included. |
| `include_products` | string or list | Only changelogs with these product IDs are included. |
| `include_types`    | string or list | Only changelogs with these types are included. |
| `match_areas`      | string         | Override `rules.match` for area matching. Values: `any`, `all`, `conjunction`. |
| `match_products`   | string         | Override `rules.match` for product matching. Values: `any`, `all`, `conjunction`. |
| `products`         | map            | Per-product type/area filter overrides. Refer to [](#rules-bundle-products).|

:::{important}
You cannot specify both `exclude_products` and `include_products`, both `exclude_types` and `include_types`, or both `exclude_areas` and `include_areas`. You can mix exclude and include across different fields (for example, `exclude_types` with `include_areas`).
:::

For examples, go to [](#example-product-matching).

The way bundle rules are applied can be broken down into three "modes":

1 — No filtering
:   This mode applies when there is no effective bundle rule.
:   No changelogs are filtered out of the bundle based on their product, type, or area.  

2 — Global rules
:   This mode applies when there's at least one global bundle rule set and the `rules.bundle.products` section is absent or empty (`products: {}`).
:   Global rules are evaluated against each changelog's own `products`, `type`, and `areas`.
:   There is no single product used for the rule context and no disjoint exclusion.
:   Changelogs with missing or empty `products` are included with a warning; global product "include" or "exclude" lists are skipped for those entries, global type and area rules still apply.

3 — Product-specific rules
:   This mode applies when the `rules.bundle.products` section is not absent or empty.
:   The bundle uses a single product for its rule context.
:   Disjoint changelogs are excluded and changelogs with missing or empty `products` are excluded with a warning.
:   Global `rules.bundle` are not used for filtering.
:   Refer to [](#rules-bundle-products).

:::{important}
In **Mode 2** (global-only `rules.bundle`), `match_products: any` with `include_products` is meaningful: each changelog is evaluated on its own, so OR-style inclusion (for example, elasticsearch **or** kibana) works without per-product rules.

In **Mode 3** (non-empty `rules.bundle.products`), `include_products` can **only** affect changelogs that already contain the rule context product.
It cannot pull in changelogs that are disjoint from that context.
Refer to [](#rules-bundle-products).
:::

Changelog commands emit a hint when both global bundle fields and a non-empty `products` map are present because global keys are ignored in Mode 3.
The `changelog bundle` and `changelog gh-release` commands also emit informational messages when rules cause changelogs to be omitted from the bundle.

## Advanced rule examples

The following sections provide details and examples for some of the more complicated bundle rule scenarios.

### Area matching behavior

The following table demonstates the impact of area-related bundle rules:

| Config | Changelog `areas` | `match_areas` | Result |
|--------|------------|-------------|--------|
| `exclude_areas: [Internal]` | `[Search, Internal]` | `any` | **Excluded** ("Internal" matches) |
| `exclude_areas: [Internal]` | `[Search, Internal]` | `all` | **Included** (not all areas are in the exclude list) |
| `exclude_areas: [Search, Internal]` | `[Search]` | `conjunction` | **Included** ("Internal" is not in the changelog) |
| `exclude_areas: [Search, Internal]` | `[Search, Internal, Monitoring]` | `conjunction` | **Excluded** (every listed exclude area is in the changelog) |
| `include_areas: [Search]` | `[Search, Internal]` | `any` | **Included** ("Search" matches) |
| `include_areas: [Search]` | `[Search, Internal]` | `all` | **Excluded** ("Internal" is not in the include list) |
| `include_areas: [Search, Internal]` | `[Search, Internal]` | `conjunction` | **Included** (every listed include area is in the changelog) |
| `include_areas: [Search, Internal]` | `[Search]` | `conjunction` | **Excluded** ("Internal" is missing from the changelog) |

As described in [match settings](#rules-match), the `conjunction` value means every area in the config list must appear in the changelog.

:::{tip}
There is one difference between how these rules are applied in global rules ("mode 2") and product-specific rules ("mode 3"). In the latter case, there's an exceptional "pass-through" scenario that skips per-product "type" and "area" rules and does not fall back to global rules. Refer to [](#rules-bundle-products).
:::

### Product matching behavior [example-product-matching]

The following table demonstates the impact of global ("Mode 2") bundle rule [match settings](#rules-match):

| `rules.bundle` setting | Changelog `products` | `match_products` value | Result |
|--------|----------------|----------------|--------|
| `exclude_products: [cloud-enterprise]` | `[cloud-enterprise, kibana]` | `any` | **Excluded** ("cloud-enterprise" matches) |
| `exclude_products: [cloud-enterprise]` | `[cloud-enterprise, kibana]` | `all` | **Included** (not all products are in the exclude list) |
| `exclude_products: [kibana, observability]` | `[kibana]` | `conjunction` | **Included** (not every exclude list item is in the changelog) |
| `exclude_products: [kibana, observability]` | `[kibana, observability]` | `conjunction` | **Excluded** (every exclude list item is in the changelog) |
| `include_products: [elasticsearch]` | `[elasticsearch, kibana]` | `any` | **Included** ("elasticsearch" matches) |
| `include_products: [elasticsearch]` | `[elasticsearch, kibana]` | `all` | **Excluded** ("kibana" is not in the include list) |
| `include_products: [elasticsearch, security]` | `[elasticsearch, security, kibana]` | `conjunction` | **Included** (every listed include ID is on the changelog) |
| `include_products: [elasticsearch, security]` | `[elasticsearch]` | `conjunction` | **Excluded** ("security" is missing from the changelog) |

The impact of the `match_products` setting differs depending on the mode.
For product-specific ("Mode 3") details, refer to [](#rules-bundle-products).

In practice, most changelogs have a single product, so `any` (the default) and `all` behave identically for them.
The difference only matters for changelogs with multiple products.

### Product-specific bundle rules [rules-bundle-products]

This section provides more detailed information about "mode 3" product-specific bundle rules.

Product keys in `rules.bundle.products` can be a single product ID or a comma-separated list of product IDs (for example, `'elasticsearch, kibana'`) if you want an identical set of rules for multiple products.

When you create a bundle, only a single product-specific bundle rule can apply (we cannot combine multiple products' rules).
A single _rule-context product_ must therefore be determined for each bundle.
In cases where a bundle is associated with multiple products, a single rule-context product is automatically determined as follows:

- If the `--output-products` (CLI) or `output_products` (profile) option is specified, it's the first product alphabetically from that list
- Otherwise, it's the first product alphabetically from all products aggregated from changelogs that passed the primary filter (`--all`, `--prs`, `--issues`, or `--input-products`) to be candidates for inclusion in the bundle.

The bundle's rule-context product affects how rules are applied to each changelog, as follows:

- If a changelog contains the rule-context product in its `products` list and product-specific rules exist for that product, the rules are applied and the changelog is included or excluded accordingly.
- If a changelog does not contain the rule-context product in its `products` list it is _disjoint_ from the rule context and **excluded** from the bundle.
- If there are no `products` in the changelog or it's empty, a warning occurs and it's **excluded** the changelog.

:::{warning}
There is another possible scenario where a changelog contains the rule-context product in its `products` but there is no product-specific rule for that product in the changelog configuration file. In that case the changelog is **included** without applying global `rules.bundle` filters. This is referred to as a "pass-through" scenario.
It is recommended that you avoid this situation by:

- Creating bundles with only a single product ID in `--output-products` (CLI) or `output_products` (profile) and ensuring that you have an appropriate product-specific bundle rule, or
- Ensuring that you have product-specific bundle rules for all the possible product values in your changelogs.
:::

For example, if you create a bundle with `--output-products "kibana 9.3.0, security 9.3.0"` the derived rule-context product is `kibana` and it affects the changelogs as follows:

| Changelog `products` | Contains `kibana`? | Result (Mode 3)            |
| -------------------- | ------------------ | -------------------------- |
| `[kibana]`           | Yes                | Use `kibana` per-product rules if defined; otherwise pass-through |
| `[security]`         | No                 | **Excluded** (disjoint) |
| `[kibana, security]` | Yes                | Use `kibana` per-product rules if defined; otherwise pass-through |
| `[elasticsearch]`    | No                 | **Excluded** (disjoint) |
| `[]` (empty/missing) | No                 | **Excluded** with warning |

For release notes that need different rule behavior per product, run separate `changelog bundle` invocations with a single product in `--output-products` (or a profile whose `output_products` resolves to one product).
Alternatively, use **Mode 2** (global-only `rules.bundle`) when you need OR-style product inclusion without disjoint exclusion.

You can define all the same area, product, and type rules (such as `include_products`, `exclude_types`, `include_areas`, and `match_areas`) as described in [](#rules-bundle).
However, in this context, `include_products` can **only** affect changelogs that already contain the rule context product.
It cannot pull in changelogs that are disjoint from that context.

:::{important}
Product-specific rules override the global bundle rules entirely.
You must repeat global constraints in the product-specific bundle rules section if you still need them in that context.
:::

The following example demonstrates how per-product rules replace global rules:

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

- Changelogs that have only `elasticsearch` product IDs are excluded as disjoint.
- Changelogs that have `kibana` product IDs have the appropriate product-specific rules applied. Global `exclude_types` and `exclude_areas` rules are not applied. Add `exclude_types` under `kibana` if you still need to exclude `docs` types.
