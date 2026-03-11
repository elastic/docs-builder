# Create and bundle changelogs

By adding a file for each notable change and grouping them into bundles, you can ultimately generate release documention with a consistent layout for all your products.

The changelogs use the following schema:

:::{dropdown} Changelog schema
::::{include} /contribute/_snippets/changelog-fields.md
::::
:::

:::{important}
Some of the fields in the schema accept only a specific set of values:

- Product values must exist in [products.yml](https://github.com/elastic/docs-builder/blob/main/config/products.yml). Invalid products will cause the `docs-builder changelog add` command to fail.
- Type, subtype, and lifecycle values must match the available values defined in [ChangelogEntryType.cs](https://github.com/elastic/docs-builder/blob/main/src/Elastic.Documentation/ChangelogEntryType.cs), [ChangelogEntrySubtype.cs](https://github.com/elastic/docs-builder/blob/main/src/Elastic.Documentation/ChangelogEntrySubtype.cs), and [Lifecycle.cs](https://github.com/elastic/docs-builder/blob/main/src/Elastic.Documentation/Lifecycle.cs) respectively. Invalid values will cause the `docs-builder changelog add` command to fail.
:::

To use the `docs-builder changelog` commands in your development workflow:

1. Ensure that your products exist in [products.yml](https://github.com/elastic/docs-builder/blob/main/config/products.yml).
1. Add labels to your GitHub pull requests that map to [changelog types](https://github.com/elastic/docs-builder/blob/main/src/Elastic.Documentation/ChangelogEntryType.cs). At a minimum, create labels for the `feature`, `bug-fix`, and `breaking-change` types.
1. Optional: Choose areas or components that your changes affect and add labels to your GitHub pull requests (such as `:Analytics/Aggregations`).
1. Optional: Add labels to your GitHub pull requests to indicate that they are not notable and should not generate changelogs. For example, `non-issue` or `release_notes:skip`. Alternatively, you can assume that all PRs are *not* notable unless a specific label is present (for example, `@Public`).
1. [Configure changelog settings](#changelog-settings) to correctly interpret your PR labels.
1. [Create changelogs](#changelog-add) with the `docs-builder changelog add` command.
   - Alternatively, if you already have automated release notes for GitHub releases, you can use the `docs-builder changelog gh-release` command to create changelog files and a bundle from your GitHub release notes. Refer to [](/cli/release/changelog-gh-release.md).
1. [Create changelog bundles](#changelog-bundle) with the `docs-builder changelog bundle` command. For example, create a bundle for the pull requests that are included in a product release.
1. [Create documentation](#render-changelogs) with the `docs-builder changelog render` command.

For more information about running `docs-builder`, go to [Contribute locally](https://www.elastic.co/docs/contribute-docs/locally).

:::{note}
This command is associated with an ongoing release docs initiative.
Additional workflows are still to come for updating and generating documentation from changelogs.
:::

## Create a changelog configuration file [changelog-settings]

You can use the `docs-builder changelog init` command to create the changelog configuration file and folder structure automatically.
The command uses an existing docs folder (with or without `docset.yml`) when found, or creates `{path}/docs` when it does not exist.
It places `changelog.yml` in the `docs` folder and creates sub-folders for the changelog and bundle files.
Alternatively, you can create the file and folders manually.

You can create a configuration file to:

- define the acceptable product, type, subtype, and lifecycle values.
- prevent the creation of changelogs when certain PR labels are present.
- set default options, such as whether to extract issues and release note text from pull requests.
- create profiles for simplified bundle creation

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

Refer to the file layout in [changelog.example.yml](https://github.com/elastic/docs-builder/blob/main/config/changelog.example.yml) and an [example usage](#example-map-label).

### Rules for creation and bundling

If you have pull request labels that indicate a changelog is not required (such as `>non-issue` or `release_note:skip`), you can declare these in the `rules.create` section of the changelog configuration.

When you run the `docs-builder changelog add` command with the `--prs` or `--issues` options and the pull request or issue has one of the identified labels, the command does not create a changelog.

Likewise, if you want to exclude changelogs with certain products, areas, or types from the release bundles, you can declare these in the `rules.bundle` section of the changelog configuration.
For example, you might choose to omit `other` or `docs` changelogs.
Or you might want to omit all autoscaling-related changelogs from the Cloud Serverless release bundles.

When you run the `docs-builder changelog render` command, changelogs that match the products, areas, or types in the `rules.publish` section of the changelog configuration file are commented out of the documentation output files.
[Changelog directives](#changelog-directive) also heed these publishing rules and omit matching changelogs.

:::{warning}
`rules.publish` is deprecated. Move your type/area filtering to `rules.bundle` so it applies at bundle time. Using `rules.publish` emits a deprecation warning during configuration loading.
:::

Each field supports **exclude** (block if matches) or **include** (block if doesn't match) semantics. You cannot mix both for the same field.

For multi-valued fields (labels, areas), you can control the matching mode:
- `any` (default): match if ANY item matches the list
- `all`: match only if ALL items match the list

:::{note}
You can define rules at the global level (applies to all products) or for specific products.
Product-specific rules **override** the global rules entirely—they do not merge.
If you define a product-specific `publish` rule, you must re-state any global rules that you also want applied for that product.
:::

Refer to [changelog.example.yml](https://github.com/elastic/docs-builder/blob/main/config/changelog.example.yml) and an [example usage](#example-block-label).

### Rules reference

The `rules:` section supports the following options:

#### `rules.match`

Global match default for multi-valued fields (labels, areas). Inherited by `create`, `publish`, and all product overrides.

| Value | Description |
|-------|-------------|
| `any` (default) | Match if ANY item in the relevant changelog field matches an item in the configured list |
| `all` | Match only if ALL items in the relevant changelog field match items in the configured list |

#### `rules.create`

Filters the pull requests or issues that can generate changelogs.
Evaluated when running `docs-builder changelog add` with `--prs` or `--issues`.

| Option | Type | Description |
|--------|------|-------------|
| `exclude` | string | Comma-separated labels that prevent changelog creation. A PR with any matching label is skipped. |
| `include` | string | Comma-separated labels required for changelog creation. A PR without any matching label is skipped. |
| `match` | string | Override `rules.match` for create rules. Values: `any`, `all`. |
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

The **product filter** (`exclude_products`/`include_products`) is skipped when the primary filter is `--input-products` (or `bundle.profiles.<name>.products`), because the primary filter already constrains by product.
The **type and area filter** (`exclude_types`/`include_types`/`exclude_areas`/`include_areas`) always applies, regardless of the primary filter.

##### Product filtering

| Option | Type | Description |
|--------|------|-------------|
| `exclude_products` | string or list | Product IDs to exclude from the bundle. Cannot be combined with `include_products`. |
| `include_products` | string or list | Only these product IDs are included; all others are excluded. Cannot be combined with `exclude_products`. |
| `match_products` | string | Override `rules.match` for product matching. Values: `any`, `all`. |

##### Type and area filtering

| Option | Type | Description |
|--------|------|-------------|
| `exclude_types` | string or list | Changelog types to exclude from the bundle. |
| `include_types` | string or list | Only changelogs with these types are kept; all others are excluded. |
| `exclude_areas` | string or list | Changelog areas to exclude from the bundle. |
| `include_areas` | string or list | Only changelogs with these areas are kept; all others are excluded. |
| `match_areas` | string | Override `rules.match` for area matching. Values: `any`, `all`. |
| `products` | map | Per-product type/area filter overrides (see below). |

You cannot specify both `exclude_products` and `include_products`, both `exclude_types` and `include_types`, or both `exclude_areas` and `include_areas`. You can mix exclude and include across different fields (for example, `exclude_types` with `include_areas`).

When a changelog is excluded by `rules.bundle`, the bundling service emits a warning with a `[-bundle-exclude]`, `[-bundle-include]`, or `[-bundle-type-area]` prefix.

##### Product-specific bundle rules (`rules.bundle.products`)

Product keys can be a single product ID or a comma-separated list.
Each product override supports the same type/area options (`exclude_types`, `include_types`, `exclude_areas`, `include_areas`, `match_areas`).
Product-specific rules **override** the global bundle type/area rules entirely for entries matching that product.

```yaml
rules:
  bundle:
    exclude_products: cloud-enterprise
    exclude_types: deprecation
    exclude_areas:
      - Internal
    products:
      cloud-serverless:
        include_areas:
          - "Search"
          - "Monitoring"
```

##### Per-product rule resolution for multi-product entries [changelog-bundle-multi-product-rules]

When a changelog entry belongs to more than one product, the applicable per-product rule is chosen using an *intersection + alphabetical first-match* algorithm:

1. Compute the **intersection** of the bundle's product context and the entry's own products.
   - The bundle context is the set of product IDs from `--output-products` (if specified), or the entry's own products when `--output-products` is not set.
   - The intersection restricts rule lookup to only the products the entry actually claims to belong to.
2. **Sort the intersection alphabetically** (case-insensitive, ascending) for a deterministic result.
3. Use the per-product rule for the **first product ID** in the sorted intersection that has a configured rule.
4. If the intersection is empty (the entry's products are disjoint from the bundle context), fall back to the entry's own product list sorted alphabetically, then to the global `rules.bundle` blocker. This prevents context-only rules from being applied to unrelated entries.

For example, with `--output-products "kibana 9.3.0" "security 9.3.0"`:

| Entry's `products` | Intersection with context | Sorted | Rule used |
|--------------------|--------------------------|--------|-----------|
| `[kibana]` | `{kibana}` | `[kibana]` | `kibana` rule |
| `[security]` | `{security}` | `[security]` | `security` rule |
| `[kibana, security]` | `{kibana, security}` | `[kibana, security]` | `kibana` rule (k < s) |

When `--output-products` is not set, the entry's own product list is used as the context, so each single-product entry naturally picks its own rule. For shared entries without `--output-products`, the alphabetically-first product with a configured rule wins. To avoid ambiguity for shared entries, configure per-product rules that agree on the shared entry, or use `--output-products` to make the bundle's product context explicit.

#### `rules.publish`

:::{warning}
`rules.publish` is deprecated. Move your type/area filtering to `rules.bundle` so it applies at bundle time rather than render time. Using `rules.publish` emits a deprecation warning during configuration loading.
:::

`rules.publish` still works for backward compatibility, but will be removed in a future release. The migration is straightforward — copy the same fields from `rules.publish` into `rules.bundle`.

**Before (deprecated):**

```yaml
rules:
  publish:
    exclude_types: docs
    exclude_areas:
      - Internal
```

**After:**

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

With `match_products`, the behavior differs depending on the mode:

| Config | Changelog `products` | `match_products` | Result |
|--------|----------------|----------------|--------|
| `exclude_products: [cloud-enterprise]` | `[cloud-enterprise, kibana]` | `any` | **Excluded** ("cloud-enterprise" matches) |
| `exclude_products: [cloud-enterprise]` | `[cloud-enterprise, kibana]` | `all` | **Included** (not all products are in the exclude list) |
| `include_products: [elasticsearch]` | `[elasticsearch, kibana]` | `any` | **Included** ("elasticsearch" matches) |
| `include_products: [elasticsearch]` | `[elasticsearch, kibana]` | `all` | **Excluded** ("kibana" is not in the include list) |

In practice, most changelogs have a single product, so `any` (the default) and `all` behave identically for them.
The difference only matters for changelogs with multiple products.

#### Area matching behavior

With `match_areas` (applies to both `rules.bundle` and `rules.publish`), the behavior differs depending on the mode:

| Config | Changelog `areas` | `match_areas` | Result |
|--------|------------|-------------|--------|
| `exclude_areas: [Internal]` | `[Search, Internal]` | `any` | **Excluded** ("Internal" matches) |
| `exclude_areas: [Internal]` | `[Search, Internal]` | `all` | **Included** (not all areas are in the exclude list) |
| `include_areas: [Search]` | `[Search, Internal]` | `any` | **Included** ("Search" matches) |
| `include_areas: [Search]` | `[Search, Internal]` | `all` | **Excluded** ("Internal" is not in the include list) |

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
| Invalid match value | `rules.match: '{value}' is not valid. Use 'any' or 'all'.` |
| Unknown product ID in bundle | `rules.bundle.exclude_products: '{id}' is not in the list of available products.` |
| Unknown product ID | `rules.create.products: '{id}' not in available products.` |

## Create changelog files [changelog-add]

You can use the `docs-builder changelog add` command to create a changelog file.

If you specify `--prs` or `--issues`, the command tries to fetch information from GitHub. It derives the changelog `title` from the pull request or issue title, maps labels to areas, products, and type (if configured), and extracts linked references.
With `--issues`, it extracts linked PRs from the issue body (for example, "Fixed by #123").
With `--prs`, it extracts linked issues from the PR body (for example, "Fixes #123").

When `--repo`, `--owner`, or `--output` are not specified, the command reads them from the `bundle` section of `changelog.yml` (`bundle.repo`, `bundle.owner`, `bundle.directory`). This applies to all modes — `--prs`, `--issues`, and `--release-version` alike. If no config value is available, `--owner` defaults to `elastic` and `--output` defaults to the current directory.

:::{tip}
Ideally this task will be automated such that it's performed by a bot or GitHub action when you create a pull request.
If you run it from the command line, you must precede any special characters (such as backquotes) with a backslash escape character (`\`).
:::

For up-to-date command usage information, use the `-h` option or refer to [](/cli/release/changelog-add.md).

### Authorization

If you use the `--prs`, `--issues`, or `--release-version` options, the `docs-builder changelog add` command interacts with GitHub services.
The `--release-version` option on the `docs-builder changelog add`, `bundle`, and `remove` commands also interacts with GitHub services.
Log into GitHub or set the `GITHUB_TOKEN` (or `GH_TOKEN` ) environment variable with a sufficient personal access token (PAT).
Otherwise, there will be fetch failures when you access private repositories and you might also encounter GitHub rate limiting errors.

For example, to create a new token with the minimum authority to read pull request details:

1. Go to **GitHub Settings** > **Developer settings** > **Personal access tokens** > [Fine-grained tokens](https://github.com/settings/personal-access-tokens).
2. Click **Generate new token**.
3. Give your token a descriptive name (such as "docs-builder changelog").
4. Under **Resource owner** if you're an Elastic employee, select **Elastic**.
5. Set an expiration date.
6. Under **Repository access**, select **Only select repositories** and choose the repositories you want to access.
7. Under **Permissions** > **Repository permissions**, set **Pull requests** to **Read-only**. If you want to be able to read issue details, do the same for **Issues**.
8. Click **Generate token**.
9. Copy the token to a safe location and use it in the `GITHUB_TOKEN` environment variable.

### Product format

The `docs-builder changelog add` has a `--products` option and the `docs-builder changelog bundle` has `--input-products` and `--output-products` options that all use the same format.

They accept values with the format `"product target lifecycle, ..."` where:

- `product` is the product ID from [products.yml](https://github.com/elastic/docs-builder/blob/main/config/products.yml) (required)
- `target` is the target version or date (optional)
- `lifecycle` is one of: `preview`, `beta`, or `ga` (optional)

Examples:

- `"kibana 9.2.0 ga"`
- `"cloud-serverless 2025-08-05"`
- `"cloud-enterprise 4.0.3, cloud-hosted 2025-10-31"`

### Filenames

By default, the `docs-builder changelog add` command generates filenames using a timestamp and a sanitized version of the title:
`{timestamp}-{sanitized-title}.yaml`

For example: `1735689600-fixes-enrich-and-lookup-join-resolution.yaml`

If you want to use PR numbers for filenames, add the `--use-pr-number` option. With both `--prs` (which creates one changelog per specified PR) and `--issues` (which creates one changelog per specified issue), each changelog filename will be derived from its PR numbers:

```sh
docs-builder changelog add \
  --prs https://github.com/elastic/elasticsearch/pull/137431 \
  --products "elasticsearch 9.2.3" \
  --use-pr-number

docs-builder changelog add \
  --issues https://github.com/elastic/docs-builder/issues/2571 \
  --products "elasticsearch 9.3.0" \
  --config docs/changelog.yml \
  --use-pr-number
```

For filenames that match issue numbers instead of PR numbers, specify `--use-issue-number`.

:::{important}
`--use-pr-number` and `--use-issue-number` are mutually exclusive; specify only one. Each requires `--prs` or `--issues`. The numbers are extracted from the URLs or identifiers you provide, or from linked references in the issue or PR body when extraction is enabled.
:::

### Examples

#### Create a changelog for multiple products [example-multiple-products]

```sh
docs-builder changelog add \
  --title "Fixes enrich and lookup join resolution based on minimum transport version" \ <1>
  --type bug-fix \ <2>
  --products "elasticsearch 9.2.3, cloud-serverless 2025-12-02" \ <3>
  --areas "ES|QL"
  --prs "https://github.com/elastic/elasticsearch/pull/137431" <4>
```

1. This option is required only if you want to override what's derived from the PR title.
2. The type values are defined in [ChangelogEntryType.cs](https://github.com/elastic/docs-builder/blob/main/src/Elastic.Documentation/ChangelogEntryType.cs).
3. The product values are defined in [products.yml](https://github.com/elastic/docs-builder/blob/main/config/products.yml).
4. The `--prs` value can be a full URL (such as `https://github.com/owner/repo/pull/123`), a short format (such as `owner/repo#123`), just a number (in which case you must also provide `--owner` and `--repo` options), or a path to a file containing newline-delimited PR URLs or numbers. Multiple PRs can be provided comma-separated, or you can specify a file path. You can also mix both formats by specifying `--prs` multiple times. One changelog file will be created for each PR.

#### Create a changelog with PR label mappings [example-map-label]

You can configure label mappings in your changelog configuration file:

```yaml
pivot:
  # Keys are type names, values can be:
  #   - simple string: comma-separated label list (e.g., ">bug, >fix")
  #   - empty/null: no labels for this type
  #   - object: { labels: "...", subtypes: {...} } for breaking-change type only
  types:
    # Example mappings - customize based on your label naming conventions
    breaking-change:
      labels: ">breaking, >bc"
    bug-fix: ">bug"
    enhancement: ">enhancement"
  
  # Area definitions with labels
  # Keys are area display names, values are label strings
  # Multiple labels can be comma-separated
  areas:
    # Example mappings - customize based on your label naming conventions
    Autoscaling: ":Distributed Coordination/Autoscaling"
    "ES|QL": ":Search Relevance/ES|QL"

  # Product definitions with labels (optional)
  # Keys are product spec strings; values are label strings or lists.
  # A product spec string is: "<product-id> [<target-version>] [<lifecycle>]"
  products:
    'elasticsearch':
      - ":stack/elasticsearch"
    'kibana':
      - ":stack/kibana"
    # Include a target version if known:
    # 'cloud-serverless 2025-06 ga':
    #   - ":cloud/serverless"
```

When you use the `--prs` option to derive information from a pull request, it can make use of those mappings. Similarly, when you use the `--issues` option (without `--prs`), the command derives title, type, areas, and products from the GitHub issue labels using the same mappings.

The following example omits `--products`, so the command derives them from the PR labels:

```sh
docs-builder changelog add \
  --prs https://github.com/elastic/elasticsearch/pull/139272 \
  --config test/changelog.yml \
  --strip-title-prefix
```

In this case, the changelog file derives the title, type, areas, and products from the pull request. If none of the PR's labels match `pivot.products`, the command falls back to `products.default` or repository name inference from `--repo` (refer to [Products resolution](/cli/release/changelog-add.md#products-resolution) for more details).
The command also looks for patterns like `Fixes #123`, `Closes owner/repo#456`, `Resolves https://github.com/.../issues/789` in the pull request to derive its issues. Similarly, when using `--issues`, the command extracts linked PRs from the issue body (for example, "Fixed by #123"). You can turn off this behavior in either case with the `--no-extract-issues` flag or by setting `extract.issues: false` in the changelog configuration file. The `extract.issues` setting applies to both directions: issues extracted from PR bodies (when using `--prs`) and PRs extracted from issue bodies (when using `--issues`).

The `--strip-title-prefix` option in this example means that if the PR title has a prefix in square brackets (such as `[ES|QL]` or `[Security]`), it is automatically removed from the changelog title. Multiple square bracket prefixes are also supported (for example `[Discover][ESQL] Title` becomes `Title`). If a colon follows the closing bracket, it is also removed.

:::{note}
The `--strip-title-prefix` option only applies when the title is derived from the PR (when `--title` is not explicitly provided). If you specify `--title` explicitly, that title is used as-is without any prefix stripping.
:::

#### Extract release notes from PR descriptions [example-extract-release-notes]

When you use the `--prs` option, by default the `docs-builder changelog add` command automatically extracts text from the PR descriptions and use it in your changelog.

In particular, it looks for content in these formats in the PR description:

- `Release Notes: This is the extracted sentence.`
- `Release-Notes: This is the extracted sentence.`
- `release notes: This is the extracted sentence.`
- `Release Note: This is the extracted sentence.`
- `Release Notes - This is the extracted sentence.`
- `## Release Note` (as a markdown header)

The extracted content is handled differently based on its length:

- **Short release notes (≤120 characters, single line)**: Used as the changelog title (only if `--title` is not explicitly provided)
- **Long release notes (>120 characters or multi-line)**: Used as the changelog description (only if `--description` is not explicitly provided)
- **No release note found**: No changes are made to the title or description

:::{note}
If you explicitly provide `--title` or `--description`, those values take precedence over extracted release notes.
You can turn off the release note extraction in the changelog configuration file or by using the `--no-extract-release-notes` option.
:::

#### Control changelog creation [example-block-label]

You can prevent changelog creation for certain PRs based on their labels.

If you run the `docs-builder changelog add` command with the `--prs` option and a PR has a blocking label for any of the resolved products (from `--products`, `pivot.products` label mapping, or `products.default`), that PR will be skipped and no changelog file will be created for it.
A warning message will be emitted indicating which PR was skipped and why.

For example, your configuration file can contain a `rules` section like this:

```yaml
rules:
  # Global match default for multi-valued fields (labels, areas).
  #   any (default) = match if ANY item matches the list
  #   all           = match only if ALL items match the list
  # match: any

  # Create — controls which PRs generate changelogs.
  create:
    # Labels that block changelog creation (comma-separated string)
    exclude: ">non-issue"
    # Product-specific overrides
    products:
      'cloud-serverless':
        exclude: ">non-issue, >test"
```

Those settings affect commands with the `--prs` or `--issues` options, for example:

```sh
docs-builder changelog add --prs "1234, 5678" \
  --products "cloud-serverless" \
  --owner elastic --repo elasticsearch \
  --config test/changelog.yml
```

If PR 1234 has the `>non-issue` or `>test` labels, it will be skipped and no changelog will be created.
If PR 5678 does not have any blocking labels, a changelog is created.

You can also use **include** mode instead of **exclude** mode.
For example, to only create changelogs for PRs with specific labels:

```yaml
rules:
  create:
    include: "@Public, @Notable"
```

#### Create changelogs from a file [example-file-add]

You can create multiple changelogs in a single command by providing a newline-delimited file that contains pull requests or issues.
For example:

```sh
# Create a file with PRs (one per line)
cat > prs.txt << EOF
https://github.com/elastic/elasticsearch/pull/1234
https://github.com/elastic/elasticsearch/pull/5678
EOF

# Use the file with --prs
docs-builder changelog add --prs prs.txt \
  --products "elasticsearch 9.2.0 ga" \
  --config test/changelog.yml
```

In this example, the command creates one changelog for each pull request in the list.

#### Create changelogs from GitHub release notes [changelog-add-release-version]

If you have GitHub releases with automated release notes (the default format or [Release Drafter](https://github.com/release-drafter/release-drafter) format), the changelog commands can derive the PR list from those release notes with the `--release-version` option.
For example:

```sh
docs-builder changelog add \
  --release-version v1.34.0 \
  --repo apm-agent-dotnet --owner elastic
```

This command creates one changelog file per PR found in the `v1.34.0` GitHub release notes.
The product, target version, and lifecycle in each changelog are inferred automatically from the release tag and the repository name.
For example, a tag of `v1.34.0` in the `apm-agent-dotnet` repo creates changelogs with `product: apm-agent-dotnet`, `target: 1.34.0`, and `lifecycle: ga`.

:::{note}
`--release-version` requires `--repo` (or `bundle.repo` set in `changelog.yml`) and is mutually exclusive with `--prs` and `--issues`.
The option precedence is: CLI option > `changelog.yml` bundle section > built-in default. This applies to `--repo`, `--owner`, and `--output` for all `changelog add` modes.
:::

You can use the `docs-builder changelog gh-release` command as a one-shot alternative to `changelog add` and `changelog bundle` commands.
The command parses the release notes, creates one changelog file per pull request found, and creates a `changelog-bundle.yaml` file — all in a single step. Refer to [](/cli/release/changelog-gh-release.md)

:::{note}
This command requires a `GITHUB_TOKEN` or `GH_TOKEN` environment variable (or an active `gh` login) to fetch release details from the GitHub API. Refer to [Authorization](#authorization) for details.
:::

## Create bundles [changelog-bundle]

You can use the `docs-builder changelog bundle` command to create a YAML file that lists multiple changelogs.
The command has two modes of operation: you can specify all the command options or you can define "profiles" in the changelog configuration file.
The latter is more convenient and consistent for repetitive workflows.
For up-to-date details, use the `-h` option or refer to [](/cli/release/changelog-bundle.md).

The command supports two mutually exclusive usage modes:

- **Option-based** — you provide filter and output options directly on the command line.
- **Profile-based** — you specify a named profile from your `changelog.yml` configuration file.

You cannot mix these two modes: when you use a profile name, no filter or output options are accepted on the command line.

### Option-based bundling [changelog-bundle-options]

You can specify only one of the following filter options:

- `--all`: Include all changelogs from the directory.
- `--input-products`: Include changelogs for the specified products. Refer to [Filter by product](#changelog-bundle-product).
- `--prs`: Include changelogs for the specified pull request URLs, or a path to a newline-delimited file. When using a file, every line must be a fully-qualified GitHub URL such as `https://github.com/owner/repo/pull/123`. Go to [Filter by pull requests](#changelog-bundle-pr).
- `--issues`: Include changelogs for the specified issue URLs, or a path to a newline-delimited file. When using a file, every line must be a fully-qualified GitHub URL such as `https://github.com/owner/repo/issues/123`. Go to [Filter by issues](#changelog-bundle-issues).
- `--release-version`: Bundle changelogs for the pull requests in GitHub release notes. Refer to [Bundle by GitHub release](#changelog-bundle-release-version).
- `--report`: Include changelogs whose pull requests appear in a promotion report. Accepts a URL or a local file path to an HTML report.

By default, the output file contains only the changelog file names and checksums.
To change this behavior, set `bundle.resolve` to `true` in the changelog configuration file or use the `--resolve` command option.

:::{tip}
If you plan to use [changelog directives](#changelog-directive), it is recommended to pull all of the content from each changelog into the bundle; otherwise you can't delete your changelogs.
If you likewise want to regenerate your [Asciidoc or Markdown files](#render-changelogs) after deleting your changelogs, it's only possible if you have "resolved" bundles.
:::

<!--
TBD: This feels like TMI in this context. Remove after confirming it's covered in the CLI reference
When you do not specify `--directory`, the command reads changelog files from `bundle.directory` in your changelog configuration if it is set, otherwise from the current directory.
When you do not specify `--output`, the command writes the bundle to `bundle.output_directory` from your changelog configuration (creating `changelog-bundle.yaml` in that directory) if it is set, otherwise to `changelog-bundle.yaml` in the input directory.
When you do not specify `--repo` or `--owner`, the command falls back to `bundle.repo` and `bundle.owner` in the changelog configuration, so you rarely need to pass these on the command line.
-->

### Profile-based bundling [changelog-bundle-profile]

If your `changelog.yml` configuration file defines `bundle.profiles`, you can run a bundle by profile name instead of supplying individual options:

```sh
docs-builder changelog bundle <profile> <version|report|url-list>
```

The second argument accepts a version string, a promotion report URL/path, or a URL list file (a plain-text file with one fully-qualified GitHub URL per line). When your profile uses `{version}` in its `output` or `output_products` pattern and you also want to filter by a report, pass both.
For example:

```sh
# Standard profile: lifecycle is inferred from the version string
docs-builder changelog bundle elasticsearch-release 9.2.0        # {lifecycle} → "ga"
docs-builder changelog bundle elasticsearch-release 9.2.0-beta.1 # {lifecycle} → "beta"

# Standard profile: filter by a promotion report (version used for {version})
docs-builder changelog bundle elasticsearch-release ./promotion-report.html
docs-builder changelog bundle elasticsearch-release 9.2.0 ./promotion-report.html
```

<!--
TBD: This feels like TMI in this context, since it's covered in the CLI reference
The command automatically discovers `changelog.yml` by checking `./changelog.yml` then `./docs/changelog.yml` relative to your current directory.
If no configuration file is found, the command returns an error with advice to create one (using `docs-builder changelog init`) or to run from the directory where the file exists.

You can set `bundle.repo` and `bundle.owner` directly under `bundle:` as defaults that apply to all profiles.
Individual profiles can override them when needed.
-->

Top-level `bundle` fields:

| Field | Description |
|---|---|
| `repo` | Default GitHub repository name applied to all profiles. Falls back to product ID if not set at any level. |
| `owner` | Default GitHub repository owner applied to all profiles. |

Profile configuration fields in `bundle.profiles`:

| Field | Description |
|---|---|
| `source` | Optional. Set to `github_release` to fetch the PR list from a GitHub release. Mutually exclusive with `products`. Requires `repo` at the profile or `bundle` level. |
| `products` | Product filter pattern with `{version}` and `{lifecycle}` placeholders. Used to match changelog files. Required when filtering by product metadata. Not used when the filter comes from a promotion report, URL list file, or `source: github_release`. |
| `output` | Output file path pattern with `{version}` and `{lifecycle}` placeholders. |
| `output_products` | Optional override for the products array written to the bundle. Useful when the bundle should have a single product ID though it's filtered from many or have a different lifecycle or version than the filter. |
| `repo` | Optional. Overrides `bundle.repo` for this profile only. Required when `source: github_release` is used and no `bundle.repo` is set. |
| `owner` | Optional. Overrides `bundle.owner` for this profile only. |
| `hide_features` | List of feature IDs to embed in the bundle as hidden. |

Example profile configuration:

```yaml
bundle:
  repo: elasticsearch # The default repository for PR and issue links.
  owner: elastic # The default repository owner for PR and issue links.
  directory: docs/changelog # The directory that contains changelog files.
  output_directory: docs/releases # The directory that contains changelog bundles.
  profiles:
    elasticsearch-release:
      products: "elasticsearch {version} {lifecycle}"
      output: "elasticsearch/{version}.yaml"
      output_products: "elasticsearch {version}"
      hide_features:
        - feature:experimental-api
    serverless-release:
      products: "cloud-serverless {version} *"
      output: "serverless/{version}.yaml"
      output_products: "cloud-serverless {version}"
      # inherits repo: elasticsearch and owner: elastic from bundle level
```

#### Bundle changelogs from a GitHub release [changelog-bundle-profile-github-release]

Set `source: github_release` on a profile to make `changelog bundle` fetch the PR list directly from a published GitHub release.

This is equivalent to running `changelog bundle --release-version <version>`, but fully configured in `changelog.yml` so you don't have to remember command-line flags.

```yaml
bundle:
  owner: elastic
  profiles:
    agent-gh-release:
      source: github_release
      repo: apm-agent-dotnet
      output: "my-agents-{version}.yaml"
      output_products: "apm-agent-dotnet {version} {lifecycle}"
```

Invoke the profile with a version tag or `latest`:

```sh
docs-builder changelog bundle agent-gh-release 1.34.0
docs-builder changelog bundle agent-gh-release latest
```

The `{version}` placeholder is substituted with the clean base version extracted from the release tag (for example, `v1.34.0` → `1.34.0`, `v1.34.0-beta.1` → `1.34.0`). The `{lifecycle}` placeholder is inferred from the **release tag** returned by GitHub, not from the argument you pass to the command:

| Release tag | `{version}` | `{lifecycle}` |
|-------------|-------------|---------------|
| `v1.2.3` | `1.2.3` | `ga` |
| `v1.2.3-beta.1` | `1.2.3` | `beta` |
| `v1.2.3-preview.1` | `1.2.3` | `preview` |

This differs from standard profiles, where `{lifecycle}` is inferred from the version string you type at the command line.

`output_products` is optional. When omitted, the bundle products array is derived from the matched changelog files' own `products` fields — the same fallback used by all other profile types. Set `output_products` when you want a single clean product entry that reflects the release identity rather than the diverse metadata across individual changelog files, or to hardcode a lifecycle that cannot be inferred from the tag format:

```yaml
# Produce one authoritative product entry instead of inheriting from changelog files
agent-gh-release:
  source: github_release
  repo: apm-agent-dotnet
  output: "apm-agent-dotnet-{version}.yaml"
  output_products: "apm-agent-dotnet {version} {lifecycle}"

# Or hardcode the lifecycle when the tag format doesn't encode it
agent-gh-release-preview:
  source: github_release
  repo: apm-agent-dotnet
  output: "apm-agent-dotnet-{version}-preview.yaml"
  output_products: "apm-agent-dotnet {version} preview"
```

`source: github_release` is mutually exclusive with `products`, and a third positional argument (promotion report or URL list) is not accepted by this profile type.

### Filter by product [changelog-bundle-product]

You can use the `--input-products` option to create a bundle of changelogs that match the product details.
When using `--input-products`, you must provide all three parts: product, target, and lifecycle.
Each part can be a wildcard (`*`) to match any value.

:::{tip}
If you use profile-based bundling, provide this information in the `bundle.profiles.<name>.products` field.
:::

```sh
docs-builder changelog bundle \
  --input-products "cloud-serverless 2025-12-02 ga, cloud-serverless 2025-12-06 beta" <1>
```

1. Include all changelogs that have the `cloud-serverless` product identifier with target dates of either December 2 2025 (lifecycle `ga`) or December 6 2025 (lifecycle `beta`). For more information about product values, refer to [](#product-format).

You can use wildcards in any of the three parts:

```sh
# Bundle any changelogs that have exact matches for either of these clauses
docs-builder changelog bundle --input-products "cloud-serverless 2025-12-02 ga, elasticsearch 9.3.0 beta"

# Bundle all elasticsearch changelogs regardless of target or lifecycle
docs-builder changelog bundle --input-products "elasticsearch * *"

# Bundle all cloud-serverless 2025-12-02 changelogs with any lifecycle
docs-builder changelog bundle --input-products "cloud-serverless 2025-12-02 *"

# Bundle any cloud-serverless changelogs with target starting with "2025-11-" and "ga" lifecycle
docs-builder changelog bundle --input-products "cloud-serverless 2025-11-* ga"

# Bundle all changelogs (equivalent to --all)
docs-builder changelog bundle --input-products "* * *"
```

If you have changelog files that reference those product details, the command creates a file like this:

```yaml
products: <1>
- product: cloud-serverless
  target: 2025-12-02
- product: cloud-serverless
  target: 2025-12-06
entries:
- file:
    name: 1765495972-fixes-enrich-and-lookup-join-resolution-based-on-m.yaml
    checksum: 6c3243f56279b1797b5dfff6c02ebf90b9658464
- file:
    name: 1765507778-break-on-fielddata-when-building-global-ordinals.yaml
    checksum: 70d197d96752c05b6595edffe6fe3ba3d055c845
```

1. By default these values match your `--input-products` (even if the changelogs have more products).
To specify different product metadata, use the `--output-products` option.

### Filter by pull requests [changelog-bundle-pr]

You can use the `--prs` option to create a bundle of the changelogs that relate to those pull requests.
You can provide either a comma-separated list of PRs (`--prs "https://github.com/owner/repo/pull/123,12345"`) or a path to a newline-delimited file (`--prs /path/to/file.txt`).
In the latter case, the file should contain one PR URL or number per line.

Pull requests can be identified by a full URL (such as `https://github.com/owner/repo/pull/123`), a short format (such as `owner/repo#123`), or just a number (in which case you must also provide `--owner` and `--repo` options).

```sh
docs-builder changelog bundle --prs "108875,135873,136886" \ <1>
  --repo elasticsearch \ <2>
  --owner elastic \ <3>
  --output-products "elasticsearch 9.2.2 ga" <4>
```

1. The comma-separated list of pull request numbers to seek.
2. The repository in the pull request URLs. Not required when using full PR URLs, or when `bundle.repo` is set in the changelog configuration.
3. The owner in the pull request URLs. Not required when using full PR URLs, or when `bundle.owner` is set in the changelog configuration.
4. The product metadata for the bundle. If it is not provided, it will be derived from all the changelog product values.

If you have changelog files that reference those pull requests, the command creates a file like this:

```yaml
products:
- product: elasticsearch
  target: 9.2.2
  lifecycle: ga
entries:
- file:
    name: 1765507819-fix-ml-calendar-event-update-scalability-issues.yaml
    checksum: 069b59edb14594e0bc3b70365e81626bde730ab7
- file:
    name: 1765507798-convert-bytestransportresponse-when-proxying-respo.yaml
    checksum: c6dbd4730bf34dbbc877c16c042e6578dd108b62
- file:
    name: 1765507839-use-ivf_pq-for-gpu-index-build-for-large-datasets.yaml
    checksum: 451d60283fe5df426f023e824339f82c2900311e
```

### Filter by issues [changelog-bundle-issues]

You can use the `--issues` option to create a bundle of changelogs that relate to those GitHub issues.
Provide either a comma-separated list of issues (`--issues "https://github.com/owner/repo/issues/123,456"`) or a path to a newline-delimited file (`--issues /path/to/file.txt`).
Issues can be identified by a full URL (such as `https://github.com/owner/repo/issues/123`), a short format (such as `owner/repo#123`), or just a number (in which case `--owner` and `--repo` are required — or set via `bundle.owner` and `bundle.repo` in the configuration).

```sh
docs-builder changelog bundle --issues "12345,12346" \
  --repo elasticsearch \
  --owner elastic \
  --output-products "elasticsearch 9.2.2 ga"
```

### Filter by pull request or issue file [changelog-bundle-file]

If you have a file that lists pull requests (such as PRs associated with a GitHub release), you can pass it to `--prs`.
For example, if you have a file that contains full pull request URLs like this:

```txt
https://github.com/elastic/elasticsearch/pull/108875
https://github.com/elastic/elasticsearch/pull/135873
https://github.com/elastic/elasticsearch/pull/136886
https://github.com/elastic/elasticsearch/pull/137126
```

You can use the `--prs` option with the file path to create a bundle of the changelogs that relate to those pull requests.
You can also combine multiple `--prs` options:

```sh
./docs-builder changelog bundle \
  --prs "https://github.com/elastic/elasticsearch/pull/108875,135873" \ <1>
  --prs test/9.2.2.txt \ <2>
  --output-products "elasticsearch 9.2.2 ga" <3>
  --resolve <4>
```

1. Comma-separated list of pull request URLs or numbers.
2. The path for the file that lists the pull requests. If the file contains only PR numbers, you must add `--repo` and `--owner` command options.
3. The product metadata for the bundle. If it is not provided, it will be derived from all the changelog product values.
4. Optionally include the contents of each changelog in the output file.

:::{tip}
You can use these files with profile-based bundling too. Refer to [](/cli/release/changelog-bundle.md).
:::

If you have changelog files that reference those pull requests, the command creates a file like this:

```yaml
products:
- product: elasticsearch
  target: 9.2.2
  lifecycle: ga
entries:
- file:
    name: 1765507778-break-on-fielddata-when-building-global-ordinals.yaml
    checksum: 70d197d96752c05b6595edffe6fe3ba3d055c845
  type: bug-fix
  title: Break on FieldData when building global ordinals
  products:
  - product: elasticsearch
  areas:
  - Aggregations
  prs:
  - https://github.com/elastic/elasticsearch/pull/108875
...
```

:::{note}
When a changelog matches multiple `--input-products` filters, it appears only once in the bundle. This deduplication applies even when using `--all` or `--prs`.
:::

### Filter by GitHub release notes [changelog-bundle-release-version]

If you have GitHub releases with automated release notes (the default format or [Release Drafter](https://github.com/release-drafter/release-drafter) format), you can use the `--release-version` option to derive the PR list from those release notes.
For example:

```sh
docs-builder changelog bundle \
  --release-version v1.34.0 \
  --repo apm-agent-dotnet --owner elastic <1>
```

1. The repo and repo owner are used to fetch the release and follow these rules of precedence:

- Repo: `--repo` flag > `bundle.repo` in `changelog.yml` (one source is required)
- Owner: `--owner` flag > `bundle.owner` in `changelog.yml` > `elastic`

This command creates a bundle of changelogs that match the list of PRs found in the `v1.34.0` GitHub release notes.

The bundle's product metadata is inferred automatically from the release tag and repository name; you can override that behavior with the `--output-products` option.

:::{tip}
If you are not creating changelogs when you create your pull requests, consider the `docs-builder changelog gh-release` command as a one-shot alternative to the `changelog add` and `changelog bundle` commands.
It parses the release notes, creates one changelog file per pull request found, and creates a `changelog-bundle.yaml` file — all in a single step. Refer to [](/cli/release/changelog-gh-release.md)
:::

### Hide features in bundles [changelog-bundle-hide-features]

You can use the `--hide-features` option to embed feature IDs that should be hidden when the bundle is rendered. This is useful for features that are not yet ready for public documentation.

```sh
docs-builder changelog bundle \
  --input-products "elasticsearch 9.3.0 *" \
  --hide-features "feature:hidden-api,feature:experimental" \ <1>
  --output /path/to/bundles/9.3.0.yaml
```

1. Feature IDs to hide. Changelogs with matching `feature-id` values will be commented out when rendered.

<!--
TO-DO: Add info about how to do this in bundle.
:::{tip}
You can do this with profile-based bundling too. Refer to [](/cli/release/changelog-bundle.md).
::: -->

The bundle output will include a `hide-features` field:

```yaml
products:
- product: elasticsearch
  target: 9.3.0
hide-features:
  - feature:hidden-api
  - feature:experimental
entries:
- file:
    name: 1765495972-new-feature.yaml
    checksum: 6c3243f56279b1797b5dfff6c02ebf90b9658464
```

When this bundle is rendered (either via the `changelog render` command or the `{changelog}` directive), changelogs with `feature-id` values matching any of the listed features will be commented out in the output.

:::{note}
The `--hide-features` option on the `render` command and the `hide-features` field in bundles are **combined**. If you specify `--hide-features` on both the `bundle` and `render` commands, all specified features are hidden. The `{changelog}` directive automatically reads `hide-features` from all loaded bundles and applies them.
:::

### Amend bundles [changelog-bundle-amend]

When you need to add changelogs to an existing bundle without modifying the original file, you can use the `docs-builder changelog bundle-amend` command to create amend bundles.
Amend bundles follow a specific naming convention: `{parent-bundle-name}.amend-{N}.yaml` where `{N}` is a sequence number.

When bundles are loaded (either via the `changelog render` command or the `{changelog}` directive), amend files are **automatically merged** with their parent bundles.
The changelogs from all matching amend files are combined with the parent bundle's changelogs and the result is rendered as a single release.

:::{warning}
If you explicitly list the amend bundles in the `--input` option of the `docs-builder changelog render` command, you'll get duplicate entries in the output files. List only the original bundles.
:::

For more details and examples, go to [](/cli/release/changelog-bundle-amend.md).

## Create documentation

### Control changelog publishing [example-rules-publishing]

:::{warning}
This functionality is deprecated. Perform the filtering at bundle time instead. Using `rules.publish` emits a deprecation warning during configuration loading.
:::

You can use rules in the changelog configuration file to refrain from publishing changelogs based on their areas and types.

For example, your configuration file can contain a `rules` section like this:

```yaml
rules:
  # Global match default for multi-valued fields (labels, areas).
  #   any (default) = match if ANY item matches the list
  #   all           = match only if ALL items match the list
  # match: any
  # Publish — controls which changelogs appear in rendered output.
  publish:
    exclude_types:
      - docs
    exclude_areas:
      - "Internal"
    products:
      'cloud-serverless':
        exclude_areas:
          - "Internal"
          - "Autoscaling"
          - "Watcher"
      'elasticsearch, kibana':
        exclude_types:
          - docs
          - other
```

For example, if you run the `docs-builder changelog render` command for a Cloud Serverless bundle, any changelogs that have "Internal", "Autoscaling", or "Watcher" areas are commented out.

You can also use **include** mode instead of **exclude** mode.
For example, to only publish changelogs with specific areas:

```yaml
rules:
  publish:
    include_areas:
      - "Search"
      - "Monitoring"
```

When subsections are enabled (`:subsections:` in the `{changelog}` directive or `--subsections` in the `changelog render` command), these `include_areas` and `exclude_areas` rules also affect which area label is used for grouping.
Changelogs with multiple areas are grouped under the first area that aligns with the rules — the first included area for `include_areas`, or the first non-excluded area for `exclude_areas`.

### Include changelogs inline [changelog-directive]

You can use the [`{changelog}` directive](../syntax/changelog.md) to render all changelog bundles directly in your documentation pages, without needing to run the `changelog render` command first.

```markdown
:::{changelog}
:::
```

By default, the directive renders all bundles from `changelog/bundles/` (relative to the docset root), ordered by semantic version (newest first). For full documentation and examples, see the [{changelog} directive syntax reference](../syntax/changelog.md).

### Generate markdown or asciidoc [render-changelogs]

The `docs-builder changelog render` command creates markdown or asciidoc files from changelog bundles for documentation purposes.
For up-to-date details, use the `-h` command option or refer to [](/cli/release/changelog-render.md).

Before you can use this command you must create changelog files and collect them into bundles.
For example, the `docs-builder changelog bundle` command creates a file like this:

```yaml
products:
- product: elasticsearch
  target: 9.2.2
entries:
- file:
    name: 1765581721-convert-bytestransportresponse-when-proxying-respo.yaml
    checksum: d7e74edff1bdd3e23ba4f2f88b92cf61cc7d490a
- file:
    name: 1765581721-fix-ml-calendar-event-update-scalability-issues.yaml
    checksum: dfafce50c9fd61c3d8db286398f9553e67737f07
- file:
    name: 1765581651-break-on-fielddata-when-building-global-ordinals.yaml
    checksum: 704b25348d6daff396259216201053334b5b3c1d
```

To create markdown files from this bundle, run the `docs-builder changelog render` command:

```sh
docs-builder changelog render \
  --input "/path/to/changelog-bundle.yaml|/path/to/changelogs|elasticsearch|keep-links,/path/to/other-bundle.yaml|/path/to/other-changelogs|kibana|hide-links" \ <1>
  --title 9.2.2 \ <2>
  --output /path/to/release-notes \ <3>
  --subsections <4>
```

1. Provide information about the changelog bundle(s). The format for each bundle is `"<bundle-file-path>|<changelog-file-path>|<repository>|<link-visibility>"` using pipe (`|`) as delimiter. To merge multiple bundles, separate them with commas (`,`). Only the `<bundle-file-path>` is required for each bundle. The `<changelog-file-path>` is useful if the changelogs are not in the default directory and are not resolved within the bundle. The `<repository>` is necessary if your changelogs do not contain full URLs for the pull requests or issues. The `<link-visibility>` can be `hide-links` or `keep-links` (default) to control whether PR/issue links are hidden for changelogs from private repositories.
2. The `--title` value is used for an output folder name and for section titles in the output files. If you omit `--title` and the first bundle contains a product `target` value, that value is used. Otherwise, if none of the bundles have product `target` fields, the title defaults to "unknown".
3. By default the command creates the output files in the current directory.
4. By default the changelog areas are not displayed in the output. Add `--subsections` to group changelog details by their `areas`. For breaking changes that have a `subtype` value, the subsections will be grouped by subtype instead of area.

:::{important}
Paths in the `--input` option must be absolute paths or use environment variables. Tilde (`~`) expansion is not supported.
:::

For example, the `index.md` output file contains information derived from the changelogs:

```md
## 9.2.2 [elastic-release-notes-9.2.2]

### Fixes [elastic-9.2.2-fixes]

**Network**
* Convert BytesTransportResponse when proxying response from/to local node. [#135873](https://github.com/elastic/elastic/pull/135873) 

**Machine Learning**
* Fix ML calendar event update scalability issues. [#136886](https://github.com/elastic/elastic/pull/136886) [#136900](https://github.com/elastic/elastic/pull/136900)

**Aggregations**
* Break on FieldData when building global ordinals. [#108875](https://github.com/elastic/elastic/pull/108875) 
```

When a changelog includes multiple values in its `prs` or `issues` arrays, all its links are rendered inline, as shown in the Machine Learning example above.

To comment out the pull request and issue links, for example if they relate to a private repository, add `hide-links` to the `--input` option for that bundle.
This allows you to selectively hide links per bundle when merging changelogs from multiple repositories.
When `hide-links` is set, all PR and issue links for affected changelogs are hidden together.

If you have changelogs with `feature-id` values and you want them to be omitted from the output, use the `--hide-features` option. Feature IDs specified via `--hide-features` are **merged** with any `hide-features` already present in the bundle files. This means both CLI-specified and bundle-embedded features are hidden in the output.

To create an asciidoc file instead of markdown files, add the `--file-type asciidoc` option:

```sh
docs-builder changelog render \
  --input "./changelog-bundle.yaml,./changelogs,elasticsearch" \
  --title 9.2.2 \
  --output ./release-notes \
  --file-type asciidoc \ <1>
  --subsections
```

1. Generate a single asciidoc file instead of multiple markdown files.

#### Release highlights

The `highlight` field allows you to mark changelogs that should appear in a dedicated highlights page.
Highlights are most commonly used for major or minor version releases to draw attention to the most important changes.

When you set `highlight: true` in a changelog:

- It appears in both the highlights page (`highlights.md`) and its normal type section (for example "Features and enhancements")
- The highlights page is only created when at least one changelog has `highlight: true` (unlike other special pages like `known-issues.md` which are always created)
- Highlights can be any type of changelog (features, enhancements, bug fixes, etc.)

Example changelog with highlight:

```yaml
type: feature
products:
- product: elasticsearch
  target: 9.3.0
  lifecycle: ga
title: New Cloud Connect UI for self-managed installations
description: Adds Cloud Connect functionality to Kibana, which allows you to use cloud solutions like AutoOps and Elastic Inference Service in your self-managed Elasticsearch clusters.
highlight: true
```

When rendering, changelogs with `highlight: true` are collected from all types and rendered in a dedicated highlights section.
In markdown output, this creates a separate `highlights.md` file.
In asciidoc output, highlights appear as a dedicated section in the single asciidoc file.

## Remove changelog files [changelog-remove]

A single changelog file might be applicable to multiple releases (for example, it might be delivered in both Stack and {{serverless-short}} releases or {{ech}} and Enterprise releases on different timelines).
After it has been included in all of the relevant bundles, it is reasonable to delete the changelog to keep your repository clean.

:::{important}
If you create docs with changelog directives, run the `docs-builder changelog bundle` command with the `--resolve` option or set `bundle.resolve` to `true` in the changelog configuration file (so that bundle files are self-contained).
Otherwise, the build will fail if you remove changelogs that the directive requires.

Likewise, the `docs-builder changelog render` command fails for "unresolved" bundles after you delete the changelogs.
:::

You can use the `docs-builder changelog remove` command to remove changelogs.
It supports the same two modes as `changelog bundle`: you can specify all the command options or you can define "profiles" in the changelog configuration file.
In the command option mode, exactly one filter option must be specified: `--all`, `--products`, `--prs`, `--issues`, `--release-version`, or `--report`.

Before deleting, the command automatically scans for bundles that still hold unresolved (`file:`) references to the matching changelog files.
If any are found, the command reports an error for each dependency.
This check prevents the `{changelog}` directive from failing at build time with missing file errors.
To proceed with removal even when unresolved bundle dependencies exist, use `--force`.

To preview what would be removed without deleting anything, use `--dry-run`.
Bundle dependency conflicts are also reported in dry-run mode.

### Removal with profiles [changelog-remove-profile]

If your `changelog.yml` configuration file defines `bundle.profiles`, you can use those profiles with `changelog remove`.
This is the easiest way to remove exactly the changelogs that were included in a profile-based bundle.
The command syntax is:

```sh
docs-builder changelog remove <profile> <version|report|url-list>
```

For example, if you bundled with:

```sh
docs-builder changelog bundle elasticsearch-release 9.2.0
```

You can remove the same changelogs with:

```sh
docs-builder changelog remove elasticsearch-release 9.2.0 --dry-run
```

The command automatically discovers `changelog.yml` by checking `./changelog.yml` then `./docs/changelog.yml` relative to your current directory.
If no configuration file is found, the command returns an error with advice to create one or to run from the directory where the file exists.

The `output`, `output_products`, and `hide_features` fields are bundle-specific and are always ignored for removal.
Which other fields are used depends on the profile type:

- Standard profiles: only the `products` field is used. The `repo` and `owner` fields are ignored (they only affect bundle output metadata).
- GitHub release profiles (`source: github_release`): `source`, `repo`, and `owner` are all used. The command fetches the PR list from the GitHub release identified by the version argument and removes changelogs whose `prs` field matches.

For example, given a GitHub release profile:

```sh
docs-builder changelog remove agent-gh-release v1.34.0 --dry-run
```

This fetches the PR list from the `v1.34.0` release (using the profile's `repo`/`owner` settings) and removes matching changelogs.

:::{note}
`source: github_release` profiles require a `GITHUB_TOKEN` or `GH_TOKEN` environment variable (or an active `gh` login) to fetch release details from the GitHub API.
:::

Profile-based removal is mutually exclusive with command options.
The only options allowed alongside a profile name are `--dry-run` and `--force`.

You can also pass a promotion report URL, file path, or URL list file as the second argument, and the command removes changelogs whose pull request or issue URLs appear in the report:

```sh
docs-builder changelog remove elasticsearch-release https://buildkite.../promotion-report.html
docs-builder changelog remove serverless-release 2026-02 ./promotion-report.html
docs-builder changelog remove serverless-release 2026-02 ./prs.txt
```

### Removal with command options [changelog-remove-raw]

You can alternatively remove changelogs based on their issues, pull requests, product metadata, or remove all changelogs from a folder.
Exactly one filter option must be specified: `--all`, `--products`, `--prs`, `--issues`, `--release-version` or `--report`.
When using a file for `--prs` or `--issues`, every line must be a fully-qualified GitHub URL.

```sh
docs-builder changelog remove --products "elasticsearch 9.3.0 *" --dry-run
```

For full option details, go to [](/cli/release/changelog-remove.md).
