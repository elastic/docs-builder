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

For the most up-to-date changelog configuration options, refer to [changelog.example.yml](https://github.com/elastic/docs-builder/blob/main/config/changelog.example.yml).

## Rules for creation and bundling [rules]

If you have pull request labels that indicate a changelog is not required (such as `>non-issue` or `release_note:skip`), you can declare these in the `rules.create` section of the changelog configuration.

When you run the `docs-builder changelog add` command with the `--prs` or `--issues` options and the pull request or issue has one of the identified labels, the command does not create a changelog.

Likewise, if you want to exclude changelogs with certain products, areas, or types from the release bundles, you can declare these in the `rules.bundle` section of the changelog configuration.
For example, you might choose to omit `other` or `docs` changelogs.
Or you might want to omit all autoscaling-related changelogs from the Cloud Serverless release bundles.

:::{warning}
`rules.publish` is deprecated and no longer used by the `changelog render` command. Move your type/area filtering to `rules.bundle` so it applies at bundle time. Using `rules.publish` emits a deprecation warning during configuration loading.
:::

You can define rules at the global level (applies to all products) or for specific products.
Product-specific rules **override** the global rules entirely—they do not merge.
For more information about the global versus per-product behavior of `rules.bundle`, refer to [bundle rule modes](/contribute/configure-changelogs-ref.md#bundle-rule-modes).

### Product matching behavior

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

### Area matching behavior

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

### Validation

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

For how global `rules.bundle` fields interact with `products` keys, see [Bundle rule modes](/contribute/configure-changelogs-ref.md#bundle-rule-modes).

## Use a changelog configuration file

By default, changelog commands check the following path: `docs/changelog.yml`.
You can specify a different path with the `--config` command option.

For specific details about the usage and impact of the configuration file, refer to the [changelog commands](/cli/changelog/index.md).