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

Refer to [changelog.example.yml](https://github.com/elastic/docs-builder/blob/main/config/changelog.example.yml).

By default, the `docs-builder changelog add` command checks the following path: `docs/changelog.yml`.
You can specify a different path with the `--config` command option.

If a configuration file exists, the command validates its values before generating changelog files:

- If the configuration file contains `lifecycles`, `products`, `subtype`, or `type` values that don't match the values in `ChangelogEntryType.cs`, `ChangelogEntrySubtype.cs`, or `Lifecycle.cs`, validation fails.
- If the configuration file contains `areas` values and they don't match what you specify in the `--areas` command option, validation fails.
- If the configuration file contains `lifecycles` or `products` values are a subset of the available values and you try to create a changelog with values outside that subset, validation fails.

In each of these cases where validation fails, a changelog file is not created.

### GitHub label mappings

When you run the `docs-builder changelog add` command with the `--prs` option, it can use label mappings in the changelog configuration file to infer the changelog `type` and `areas` fields from your pull request labels.

Refer to the file layout in [changelog.example.yml](https://github.com/elastic/docs-builder/blob/main/config/changelog.example.yml) and an [example usage](#example-map-label).

### Rules for creation and publishing

If you have pull request labels that indicate a changelog is not required (such as `>non-issue` or `release_note:skip`), you can declare these in the `rules` section of the changelog configuration.

When you run the `docs-builder changelog add` command with the `--prs` option and the PR has one of the identified labels, the command does not create a changelog for that PR.

Likewise, if there are areas or types of changelogs that should not be published, you can declare these in the `rules` section of the changelog configuration.
For example, you might choose to omit `other` or `docs` changelogs.
Or you might want to omit all autoscaling-related changelogs from the Cloud Serverless release docs.

When you run the `docs-builder changelog render` command, changelog entries that match the specified products and areas or types are commented out of the documentation output files.
The command will emit warnings prefixed with `[-exclude]` or `[+include]` indicating which changelog entries were commented out and why.

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
| `any` (default) | Match if ANY item in the entry matches an item in the configured list |
| `all` | Match only if ALL items in the entry match items in the configured list |

#### `rules.create`

Controls which PRs generate changelog entries. Evaluated when running `docs-builder changelog add` with `--prs`.

| Option | Type | Description |
|--------|------|-------------|
| `exclude` | string | Comma-separated labels that prevent changelog creation. A PR with any matching label is skipped. |
| `include` | string | Comma-separated labels required for changelog creation. A PR without any matching label is skipped. |
| `match` | string | Override `rules.match` for create rules. Values: `any`, `all`. |
| `products` | map | Product-specific create rules (see below). |

You cannot specify both `exclude` and `include`.

**Product-specific create rules** (`rules.create.products`):

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

#### `rules.publish`

Controls which entries appear in rendered output. Evaluated when running `docs-builder changelog render` or using the `{changelog}` directive.

| Option | Type | Description |
|--------|------|-------------|
| `exclude_types` | list | Entry types to hide from output |
| `include_types` | list | Only these entry types are shown; all others are hidden |
| `exclude_areas` | list | Entry areas to hide from output |
| `include_areas` | list | Only entries with these areas are shown; all others are hidden |
| `match_areas` | string | Override `rules.match` for area matching. Values: `any`, `all`. |
| `products` | map | Product-specific publish rules (see below). |

You cannot specify both `exclude_types` and `include_types`, or both `exclude_areas` and `include_areas`.
You **can** mix exclude and include across different fields (for example, `exclude_types` with `include_areas`).

**Product-specific publish rules** (`rules.publish.products`):

Product keys can be a single product ID or a comma-separated list.
Each product override supports the same options as the global publish rules (`exclude_types`, `include_types`, `exclude_areas`, `include_areas`, `match_areas`).
Product-specific rules **override** the global publish rules entirely.

```yaml
rules:
  publish:
    exclude_types:
      - docs
    products:
      cloud-serverless:
        include_areas:
          - "Search"
          - "Monitoring"
```

#### Match inheritance

The `match` setting cascades from global to section to product:

```
rules.match (global default, "any" if omitted)
  ├─ rules.create.match → rules.create.products.{id}.match
  └─ rules.publish.match_areas → rules.publish.products.{id}.match_areas
```

If a lower-level `match` or `match_areas` is specified, it overrides the inherited value.

#### Area matching behavior

With `match_areas`, the behavior differs depending on the mode:

| Config | Entry areas | match_areas | Result |
|--------|------------|-------------|--------|
| `exclude_areas: [Internal]` | `[Search, Internal]` | `any` | **Hidden** ("Internal" matches) |
| `exclude_areas: [Internal]` | `[Search, Internal]` | `all` | **Shown** (not all areas are in the exclude list) |
| `include_areas: [Search]` | `[Search, Internal]` | `any` | **Shown** ("Search" matches) |
| `include_areas: [Search]` | `[Search, Internal]` | `all` | **Hidden** ("Internal" is not in the include list) |

#### Validation

The following configurations cause validation errors:

| Condition | Error |
|-----------|-------|
| Old `block:` key found | `'block' is no longer supported. Rename to 'rules'. See changelog.example.yml.` |
| Both `exclude` and `include` in create | `rules.create: cannot have both 'exclude' and 'include'. Use one or the other.` |
| Both `exclude_types` and `include_types` | `rules.publish: cannot have both 'exclude_types' and 'include_types'. Use one or the other.` |
| Both `exclude_areas` and `include_areas` | `rules.publish: cannot have both 'exclude_areas' and 'include_areas'. Use one or the other.` |
| Invalid match value | `rules.match: '{value}' is not valid. Use 'any' or 'all'.` |
| Unknown product ID | `rules.create.products: '{id}' not in available products.` |

## Create changelog files [changelog-add]

You can use the `docs-builder changelog add` command to create a changelog file.

:::{tip}
Ideally this task will be automated such that it's performed by a bot or GitHub action when you create a pull request.
If you run it from the command line, you must precede any special characters (such as backquotes) with a backslash escape character (`\`).
:::

For up-to-date command usage information, use the `-h` option or refer to [](/cli/release/changelog-add.md).

### Authorization

If you use the `--prs` option, the `docs-builder changelog add` command interacts with GitHub services.
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

If you want to use the PR number as the filename instead, add the `--use-pr-number` option:

```sh
docs-builder changelog add \
  --prs https://github.com/elastic/elasticsearch/pull/137431 \
  --products "elasticsearch 9.2.3" \
  --use-pr-number
```

With a single PR, this creates a file named `137431.yaml`. With multiple PRs, the filename aggregates the numbers (e.g., `137431-137432.yaml`).

Use `--use-issue-number` to name the file by issue number(s). When you specify `--issues` without `--prs`, the command fetches the issue from GitHub and derives the title, type, and areas from the issue (using the same label mappings as for PRs). When both `--issues` and `--prs` are specified, `--use-issue-number` still uses the issue number for the filename:

```sh
docs-builder changelog add \
  --issues https://github.com/elastic/elasticsearch/issues/12345 \
  --products "elasticsearch 9.2.3" \
  --config docs/changelog.yml \
  --use-issue-number
```

The command derives the title from the issue title, maps labels to type and areas (if configured), extracts release notes from the issue body, and extracts linked PRs (e.g., "Fixed by #123"). You can omit `--title` and `--type` when the issue has appropriate labels. Multiple issues can be specified comma-separated or via a file path (like `--prs`), creating one changelog per issue.

This creates a file named `12345.yaml` (or `12345-12346.yaml` for multiple issues).

:::{important}
`--use-pr-number` and `--use-issue-number` are mutually exclusive; specify only one. `--use-pr-number` requires `--prs`. `--use-issue-number` requires `--issues`. The numbers are extracted from the URLs or identifiers you provide.
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
```

When you use the `--prs` option to derive information from a pull request, it can make use of those mappings. Similarly, when you use the `--issues` option (without `--prs`), the command derives title, type, and areas from the GitHub issue labels using the same mappings:

```sh
docs-builder changelog add \
  --prs https://github.com/elastic/elasticsearch/pull/139272 \
  --products "elasticsearch 9.3.0" \
  --config test/changelog.yml \
  --strip-title-prefix
```

In this case, the changelog file derives the title, type, and areas from the pull request.
The command also looks for patterns like `Fixes #123`, `Closes owner/repo#456`, `Resolves https://github.com/.../issues/789` in the pull request to derive its issues. Similarly, when using `--issues`, the command extracts linked PRs from the issue body (for example, "Fixed by #123"). You can turn off this behavior in either case with the `--no-extract-issues` flag or by setting `extract.issues: false` in the changelog configuration file. The `extract.issues` setting applies to both directions: issues extracted from PR bodies (when using `--prs`) and PRs extracted from issue bodies (when using `--issues`).

The `--strip-title-prefix` option in this example means that if the PR title has a prefix in square brackets (such as `[ES|QL]` or `[Security]`), it is automatically removed from the changelog title. Multiple square bracket prefixes are also supported (e.g., `[Discover][ESQL] Title` becomes `Title`). If a colon follows the closing bracket, it is also removed.

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

#### Control changelog creation and publishing [example-block-label]

You can prevent changelog creation for certain PRs based on their labels.
You can likewise refrain from publishing changelogs based on their areas and types.

If you run the `docs-builder changelog add` command with the `--prs` option and a PR has a blocking label for any of the products in the `--products` option, that PR will be skipped and no changelog file will be created for it.
A warning message will be emitted indicating which PR was skipped and why.

For example, your configuration file can contain a `rules` section like this:

```yaml
rules:
  # Global match default for multi-valued fields (labels, areas).
  #   any (default) = match if ANY item matches the list
  #   all           = match only if ALL items match the list
  # match: any

  # Create — controls which PRs generate changelog entries.
  create:
    # Labels that block changelog creation (comma-separated string)
    exclude: ">non-issue"
    # Product-specific overrides
    products:
      'cloud-serverless':
        exclude: ">non-issue, >test"

  # Publish — controls which entries appear in rendered output.
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

Those settings affect commands with the `--prs` option, for example:

```sh
docs-builder changelog add --prs "1234, 5678" \
  --products "cloud-serverless" \
  --owner elastic --repo elasticsearch \
  --config test/changelog.yml
```

If PR 1234 has the `>non-issue` or `>test` labels, it will be skipped and no changelog will be created.
If PR 5678 does not have any blocking labels, a changelog is created.

The `rules` settings also affect the publishing stage.
For example, if you run the `docs-builder changelog render` command for a Cloud Serverless bundle, any changelogs that have "Internal", "Autoscaling", or "Watcher" areas are commented out.

You can also use **include** mode instead of **exclude** mode. For example, to only create changelogs for PRs with specific labels or only publish entries with specific areas:

```yaml
rules:
  create:
    include: "@Public, @Notable"
  publish:
    include_areas:
      - "Search"
      - "Monitoring"
```

When subsections are enabled (`:subsections:` in the `{changelog}` directive or `--subsections` in the `changelog render` command), these `include_areas` and `exclude_areas` rules also affect which area label is used for grouping. Entries with multiple areas are grouped under the first area that aligns with the rules — the first included area for `include_areas`, or the first non-excluded area for `exclude_areas`.

#### Create changelogs from a file of PRs [example-file-prs]

You can also provide PRs from a file containing newline-delimited PR URLs or numbers:

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

You can also mix file paths and comma-separated PRs:

```sh
docs-builder changelog add \
  --prs "https://github.com/elastic/elasticsearch/pull/1234" \
  --prs prs.txt \
  --prs "5678, 9012" \
  --products "elasticsearch 9.2.0 ga" \
  --owner elastic --repo elasticsearch \
  --config test/changelog.yml
```

This creates one changelog file for each PR specified, whether from files or directly.

## Create bundles [changelog-bundle]

You can use the `docs-builder changelog bundle` command to create a YAML file that lists multiple changelogs.
For up-to-date details, use the `-h` option or refer to [](/cli/release/changelog-bundle.md).

You can specify only one of the following filter options:

- `--all`: Include all changelogs from the directory.
- `--input-products`: Include changelogs for the specified products. Refer to [Filter by product](#changelog-bundle-product).
- `--prs`: Include changelogs for the specified pull request URLs or numbers, or a path to a newline-delimited file containing PR URLs or numbers. Go to [Filter by pull requests](#changelog-bundle-pr).
- `--issues`: Include changelogs for the specified issue URLs or numbers, or a path to a newline-delimited file containing issue URLs or numbers. Go to [Filter by issues](#changelog-bundle-issues).

By default, the output file contains only the changelog file names and checksums.
You can optionally use the `--resolve` command option to pull all of the content from each changelog into the bundle.

:::{tip}
If you plan to use [changelog directives](#changelog-directive), it is recommended to use the `--resolve` option; otherwise you can't delete your changelogs.
If you likewise want to regenerate your [Asciidoc or Markdown files](#render-changelogs) after deleting your changelogs, it's only possible if you have "resolved" bundles.
:::

When you do not specify `--directory`, the command reads changelog files from `bundle.directory` in your changelog configuration if it is set, otherwise from the current directory.
When you do not specify `--output`, the command writes the bundle to `bundle.output_directory` from your changelog configuration (creating `changelog-bundle.yaml` in that directory) if it is set, otherwise to `changelog-bundle.yaml` in the input directory.

### Filter by product [changelog-bundle-product]

You can use the `--input-products` option to create a bundle of changelogs that match the product details.
When using `--input-products`, you must provide all three parts: product, target, and lifecycle.
Each part can be a wildcard (`*`) to match any value.

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

If you add the `--resolve` option, the contents of each changelog will be included in the output file.

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
2. The repository in the pull request URLs. This option is not required if you specify the short or full PR URLs in the `--prs` option.
3. The owner in the pull request URLs. This option is not required if you specify the short or full PR URLs in the `--prs` option.
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

If you add the `--resolve` option, the contents of each changelog will be included in the output file.

### Filter by issues [changelog-bundle-issues]

You can use the `--issues` option to create a bundle of changelogs that relate to those GitHub issues.
Provide either a comma-separated list of issues (`--issues "https://github.com/owner/repo/issues/123,456"`) or a path to a newline-delimited file (`--issues /path/to/file.txt`).
Issues can be identified by a full URL (such as `https://github.com/owner/repo/issues/123`), a short format (such as `owner/repo#123`), or just a number (in which case you must also provide `--owner` and `--repo` options).

```sh
docs-builder changelog bundle --issues "12345,12346" \
  --repo elasticsearch \
  --owner elastic \
  --output-products "elasticsearch 9.2.2 ga"
```

### Filter by pull request file [changelog-bundle-file]

If you have a file that lists pull requests (such as PRs associated with a GitHub release):

```txt
https://github.com/elastic/elasticsearch/pull/108875
https://github.com/elastic/elasticsearch/pull/135873
https://github.com/elastic/elasticsearch/pull/136886
https://github.com/elastic/elasticsearch/pull/137126
```

You can use the `--prs` option with a file path to create a bundle of the changelogs that relate to those pull requests. You can also combine multiple `--prs` options:

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

### Hide features in bundles [changelog-bundle-hide-features]

You can use the `--hide-features` option to embed feature IDs that should be hidden when the bundle is rendered. This is useful for features that are not yet ready for public documentation.

```sh
docs-builder changelog bundle \
  --input-products "elasticsearch 9.3.0 *" \
  --hide-features "feature:hidden-api,feature:experimental" \ <1>
  --output /path/to/bundles/9.3.0.yaml
```

1. Feature IDs to hide. Entries with matching `feature-id` values will be commented out when rendered.

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

When this bundle is rendered (either via the `changelog render` command or the `{changelog}` directive), entries with `feature-id` values matching any of the listed features will be commented out in the output.

:::{note}
The `--hide-features` option on the `render` command and the `hide-features` field in bundles are **combined**. If you specify `--hide-features` on both the `bundle` and `render` commands, all specified features are hidden. The `{changelog}` directive automatically reads `hide-features` from all loaded bundles and applies them.
:::

### Amend bundles [changelog-bundle-amend]

When you need to add entries to an existing bundle without modifying the original file, you can use the `docs-builder changelog bundle-amend` command to create amend bundles.
Amend bundles follow a specific naming convention: `{parent-bundle-name}.amend-{N}.yaml` where `{N}` is a sequence number.

When bundles are loaded (either via the `changelog render` command or the `{changelog}` directive), amend files are **automatically merged** with their parent bundles.
The entries from all matching amend files are combined with the parent bundle's entries, and the result is rendered as a single release.

:::{warning}
If you explicitly list the amend bundles in the `--input` option of the `docs-builder changelog render` command, you'll get duplicate entries in the output files. List only the original bundles.
:::

For more details and examples, go to [](/cli/release/changelog-bundle-amend.md).

## Create documentation

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

1. Provide information about the changelog bundle(s). The format for each bundle is `"<bundle-file-path>|<changelog-file-path>|<repository>|<link-visibility>"` using pipe (`|`) as delimiter. To merge multiple bundles, separate them with commas (`,`). Only the `<bundle-file-path>` is required for each bundle. The `<changelog-file-path>` is useful if the changelogs are not in the default directory and are not resolved within the bundle. The `<repository>` is necessary if your changelogs do not contain full URLs for the pull requests or issues. The `<link-visibility>` can be `hide-links` or `keep-links` (default) to control whether PR/issue links are hidden for entries from private repositories.
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

When a changelog entry includes multiple values in its `prs` or `issues` arrays, all links are rendered inline for that entry, as shown in the Machine Learning example above.

To comment out the pull request and issue links, for example if they relate to a private repository, add `hide-links` to the `--input` option for that bundle. This allows you to selectively hide links per bundle when merging changelogs from multiple repositories. When `hide-links` is set, all PR and issue links for affected entries are hidden together.

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

The `highlight` field allows you to mark changelog entries that should appear in a dedicated highlights page. Highlights are most commonly used for major or minor version releases to draw attention to the most important changes.

When you set `highlight: true` on a changelog entry:

- The entry appears in both the highlights page (`highlights.md`) and its normal type section (e.g., "Features and enhancements")
- The highlights page is only created when at least one entry has `highlight: true` (unlike other special pages like `known-issues.md` which are always created)
- Highlights can be any type of changelog entry (features, enhancements, bug fixes, etc.)

Example changelog entry with highlight:

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

When rendering changelogs, entries with `highlight: true` are collected from all types and rendered in a dedicated highlights section. In markdown output, this creates a separate `highlights.md` file. In asciidoc output, highlights appear as a dedicated section in the single asciidoc file.

## Remove changelog files [changelog-remove]

A single changelog file might be applicable to multiple releases (for example, it might be delivered in both Stack and {{serverless-short}} releases or {{ech}} and Enterprise releases on different timelines).
After it has been included in all of the relevant bundles, it is reasonable to delete the changelog to keep your repository clean.

:::{important}
If you create docs with changelog directives, run the `docs-builder changelog bundle` command with the `--resolve` option (so that bundle files are self-contained). Otherwise, the build will fail if you remove changelogs that the directive requires.

Likewise, the `docs-builder changelog render` command fails for "unresolved" bundles after you delete the changelogs.
:::

You can use the `docs-builder changelog remove` command to remove changelogs.
It has the same filter options as `changelog bundle` (that is to say, you can remove changelogs based their issues or pull requests, product metadata, or folder).
Exactly one filter option must be specified.

Before deleting, the command automatically scans for bundles that still hold unresolved (`file:`) references to the matching changelog files.
If any are found, the command reports an error for each dependency.
This check prevents the `{changelog}` directive from failing at build time with missing file errors.
To proceed with removal even when unresolved bundle dependencies exist, use `--force`.

To preview what would be removed without deleting anything, use `--dry-run`.
Bundle dependency conflicts are also reported in dry-run mode.

For example:

```sh
docs-builder changelog remove --products "elasticsearch 9.3.0 *" --dry-run
```

For full option details, go to [](/cli/release/changelog-remove.md).
