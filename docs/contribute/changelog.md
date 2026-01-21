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
- Type, subtype, and lifecycle values must match the available values defined in [ChangelogConfiguration.cs](https://github.com/elastic/docs-builder/blob/main/src/services/Elastic.Changelog/Configuration/ChangelogConfiguration.cs). Invalid values will cause the `docs-builder changelog add` command to fail.
:::

To use the `docs-builder changelog` commands in your development workflow:

1. Ensure that your products exist in [products.yml](https://github.com/elastic/docs-builder/blob/main/config/products.yml).
1. Add labels to your GitHub pull requests to represent the types defined in [ChangelogConfiguration.cs](https://github.com/elastic/docs-builder/blob/main/src/services/Elastic.Changelog/Configuration/ChangelogConfiguration.cs). For example, `>bug` and `>enhancement` labels.
1. Optional: Choose areas or components that your changes affect and add labels to your GitHub pull requests (such as `:Analytics/Aggregations`).
1. Optional: Add labels to your GitHub pull requests to indicate that they are not notable and should not generate changelogs. For example, `non-issue` or `release_notes:skip`.
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

You can create a configuration file to limit the acceptable product, type, subtype, and lifecycle values.
You can also use it to prevent the creation of changelogs when certain PR labels are present.
Refer to [changelog.yml.example](https://github.com/elastic/docs-builder/blob/main/config/changelog.yml.example).

By default, the `docs-builder changelog add` command checks the following path: `docs/changelog.yml`.
You can specify a different path with the `--config` command option.

If a configuration file exists, the command validates its values before generating changelog files:

- If the configuration file contains `lifecycle`, `product`, `subtype`, or `type` values that don't match the values in `products.yml` and `ChangelogConfiguration.cs`, validation fails. The changelog file is not created.
- If the configuration file contains `areas` values and they don't match what you specify in the `--areas` command option, validation fails. The changelog file is not created.

The `available_types`, `available_subtypes`, and `available_lifecycles` fields are optional in the configuration file.
If not specified, all default values from `ChangelogConfiguration.cs` are used.

### GitHub label mappings

You can optionally add `label_to_type` and `label_to_areas` mappings in your changelog configuration.
When you run the `docs-builder changelog add` command with the `--prs` option, it can use these mappings to fill in the `type` and `areas` in your changelog based on your pull request labels.

Refer to the file layout in [changelog.yml.example](https://github.com/elastic/docs-builder/blob/main/config/changelog.yml.example) and an [example usage](#example-map-label).

### Add blockers

You can optionally use `add_blockers` in your changelog configuration to prevent the creation of some changelogs.
When you run the `docs-builder changelog add` command with the `--prs` and `--products` options and the PR has a label that you've identified as a blocker for that product, the command does not create a changelog for that PR.

You can use comma-separated product IDs to share the same list of labels across multiple products.

Refer to the file layout in [changelog.yml.example](https://github.com/elastic/docs-builder/blob/main/config/changelog.yml.example) and an [example usage](#example-block-label).

### Render blockers [render-blockers]

You can optionally add `render_blockers` in your changelog configuration to prevent the rendering of some changelogs.
When you run the `docs-builder changelog render` command, changelog entries that match the specified products and areas/types will be commented out of the documentation output files.

By default, the `docs-builder changelog render` command checks the following path: `docs/changelog.yml`.
You can specify a different path with the `--config` command option.

The `render_blockers` configuration uses a dictionary format where:

- The key can be a single product ID or comma-separated product IDs (e.g., `"elasticsearch, cloud-serverless"`)
- The value contains `areas` and/or `types` that should be blocked for those products

An entry is blocked if any product in the changelog entry matches any product key in `render_blockers` AND (any area matches OR any type matches).
If a changelog entry has multiple products, all matching products in `render_blockers` are checked.

The `types` values in `render_blockers` must exist in the `available_types` list (or in the default types if `available_types` is not specified).

Example configuration:

```yaml
render_blockers:
  "cloud-hosted, cloud-serverless":
    areas: # List of area values that should be blocked (commented out) during render
      - Autoscaling
      - Watcher
    types: # List of type values that should be blocked (commented out) during render
      - docs
  elasticsearch: # Another single product case
    areas:
      - Security
```

When rendering, entries with:

- Product `cloud-hosted` or `cloud-serverless` AND (area `Autoscaling` or `Watcher` OR type `docs`) will be commented out
- Product `elasticsearch` AND area `Security` will be commented out

The command will emit warnings indicating which changelog entries were commented out and why.

Refer to [changelog.yml.example](https://github.com/elastic/docs-builder/blob/main/config/changelog.yml.example).

## Create changelog files [changelog-add]

You can use the `docs-builder changelog add` command to create a changelog file.

:::{tip}
Ideally this task will be automated such that it's performed by a bot or GitHub action when you create a pull request. More details to come as we refine the workflows.
:::

For up-to-date command usage information, use the `-h` option:

```sh
Add a new changelog from command-line input

Options:
  --products <List<ProductInfo>>    Required: Products affected in format "product target lifecycle, ..." (e.g., "elasticsearch 9.2.0 ga, cloud-serverless 2025-08-05") [Required]
  --action <string?>                Optional: What users must do to mitigate [Default: null]
  --areas <string[]?>               Optional: Area(s) affected (comma-separated or specify multiple times) [Default: null]
  --config <string?>                Optional: Path to the changelog.yml configuration file. Defaults to 'docs/changelog.yml' [Default: null]
  --description <string?>           Optional: Additional information about the change (max 600 characters) [Default: null]
  --extract-release-notes           Optional: When used with --prs, extract release notes from PR descriptions. Short release notes (≤120 characters, single line) are used as the title, long release notes (>120 characters or multi-line) are used as the description. Looks for content in formats like "Release Notes: ...", "Release-Notes: ...", "## Release Note", etc.
  --feature-id <string?>            Optional: Feature flag ID [Default: null]
  --highlight <bool?>               Optional: Include in release highlights [Default: null]
  --impact <string?>                Optional: How the user's environment is affected [Default: null]
  --issues <string[]?>              Optional: Issue URL(s) (comma-separated or specify multiple times) [Default: null]
  --owner <string?>                 Optional: GitHub repository owner (used when --prs contains just numbers) [Default: null]
  --output <string?>                Optional: Output directory for the changelog. Defaults to current directory [Default: null]
  --prs <string[]?>                 Optional: Pull request URL(s) or PR number(s) (comma-separated), or a path to a newline-delimited file containing PR URLs or numbers. Can be specified multiple times. Each occurrence can be either comma-separated PRs (e.g., `--prs "https://github.com/owner/repo/pull/123,6789"`) or a file path (e.g., `--prs /path/to/file.txt`). When specifying PRs directly, provide comma-separated values. When specifying a file path, provide a single value that points to a newline-delimited file. If --owner and --repo are provided, PR numbers can be used instead of URLs. If specified, --title can be derived from the PR. If mappings are configured, --areas and --type can also be derived from the PR. Creates one changelog file per PR. [Default: null]
  --repo <string?>                  Optional: GitHub repository name (used when --prs contains just numbers) [Default: null]
  --strip-title-prefix              Optional: When used with --prs, remove square brackets and text within them from the beginning of PR titles, and also remove a colon if it follows the closing bracket (e.g., "[Inference API] Title" becomes "Title", "[ES|QL]: Title" becomes "Title")
  --subtype <string?>               Optional: Subtype for breaking changes (api, behavioral, configuration, etc.) [Default: null]
  --title <string?>                 Optional: A short, user-facing title (max 80 characters). Required if --prs is not specified. If --prs and --title are specified, the latter value is used instead of what exists in the PR. [Default: null]
  --type <string?>                  Optional: Type of change (feature, enhancement, bug-fix, breaking-change, etc.). Required if --prs is not specified. If mappings are configured, type can be derived from the PR. [Default: null]
  --use-pr-number                   Optional: Use the PR number as the filename instead of generating it from a unique ID and title
```

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
7. Under **Permissions** > **Repository permissions**, set **Pull requests** to **Read-only**.
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
  --pr https://github.com/elastic/elasticsearch/pull/137431 \
  --products "elasticsearch 9.2.3" \
  --use-pr-number
```

This creates a file named `137431.yaml` instead of the default timestamp-based filename.

:::{important}
When using `--use-pr-number`, you must also provide the `--pr` option. The PR number is extracted from the PR URL or number you provide.
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
2. The type values are defined in [ChangelogConfiguration.cs](https://github.com/elastic/docs-builder/blob/main/src/services/Elastic.Changelog/Configuration/ChangelogConfiguration.cs).
3. The product values are defined in [products.yml](https://github.com/elastic/docs-builder/blob/main/config/products.yml).
4. The `--prs` value can be a full URL (such as `https://github.com/owner/repo/pull/123`), a short format (such as `owner/repo#123`), just a number (in which case you must also provide `--owner` and `--repo` options), or a path to a file containing newline-delimited PR URLs or numbers. Multiple PRs can be provided comma-separated, or you can specify a file path. You can also mix both formats by specifying `--prs` multiple times. One changelog file will be created for each PR.

#### Create a changelog with PR label mappings [example-map-label]

You can configure label mappings in your changelog configuration file:

```yaml
# GitHub label mappings (optional - used when --prs option is specified)
# Maps GitHub PR labels to changelog type values
# When a PR has a label that matches a key, the corresponding type value is used
label_to_type:
  # Example mappings - customize based on your label naming conventions
  ">enhancement": enhancement
  ">breaking": breaking-change

# Maps GitHub PR labels to changelog area values
# Multiple labels can map to the same area, and a single label can map to multiple areas (comma-separated)
label_to_areas:
  # Example mappings - customize based on your label naming conventions
  ":Search Relevance/ES|QL": "ES|QL"
```

When you use the `--prs` option to derive information from a pull request, it can make use of those mappings:

```sh
docs-builder changelog add \
  --prs https://github.com/elastic/elasticsearch/pull/139272 \
  --products "elasticsearch 9.3.0" \
  --config test/changelog.yml \
  --strip-title-prefix
```

In this case, the changelog file derives the title, type, and areas from the pull request.

The `--strip-title-prefix` option in this example means that if the PR title has a prefix in square brackets (such as `[ES|QL]` or `[Security]`), it is automatically removed from the changelog title. If a colon follows the closing bracket, it is also removed.

:::{note}
The `--strip-title-prefix` option only applies when the title is derived from the PR (when `--title` is not explicitly provided). If you specify `--title` explicitly, that title is used as-is without any prefix stripping.
:::

#### Extract release notes from PR descriptions [example-extract-release-notes]

When you use the `--prs` option, you can also add the `--extract-release-notes` option to automatically extract text from the PR descriptions and use them in your changelog.

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
:::

#### Block changelog creation with PR labels [example-block-label]

You can configure product-specific label blockers to prevent changelog creation for certain PRs based on their labels.

If you run the `docs-builder changelog add` command with the `--prs` option and a PR has a blocking label for any of the products in the `--products` option, that PR will be skipped and no changelog file will be created for it.
A warning message will be emitted indicating which PR was skipped and why.

For example, your configuration file can contain `add_blockers` like this:

```yaml
# Product-specific label blockers (optional)
# Maps product IDs to lists of labels that prevent changelog creation for that product
# If you run the changelog add command with the --prs option and a PR has any of these labels, the changelog is not created
# Product IDs can be comma-separated to share the same list of labels across multiple products
add_blockers:
  # Example: Skip changelog for cloud.serverless product when PR has "Watcher" label
  cloud-serverless:
    - ":Data Management/Watcher"
    - ">non-issue"
  # Example: Skip changelog creation for elasticsearch product when PR has "skip:releaseNotes" label
  elasticsearch:
    - ">non-issue"
  # Example: Share the same blockers across multiple products using comma-separated product IDs
  elasticsearch, cloud-serverless:
    - ">non-issue"
```

Those settings affect commands with the `--prs` option, for example:

```sh
docs-builder changelog add --prs "1234, 5678" \
  --products "cloud-serverless" \
  --owner elastic --repo elasticsearch \
  --config test/changelog.yml
```

If PR 1234 has the `>non-issue` or Watcher label, it will be skipped and no changelog will be created for it.
If PR 5678 does not have any blocking labels, a changelog is created.

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
For up-to-date details, use the `-h` option:

```sh
Bundle changelogs

Options:
  --all                                     Include all changelogs in the directory. Only one filter option can be specified: `--all`, `--input-products`, or `--prs`.
  --directory <string?>                     Optional: Directory containing changelog YAML files. Defaults to current directory [Default: null]
  --input-products <List<ProductInfo>?>     Filter by products in format "product target lifecycle, ..." (e.g., "cloud-serverless 2025-12-02 ga, cloud-serverless 2025-12-06 beta"). When specified, all three parts (product, target, lifecycle) are required but can be wildcards (*). Examples: "elasticsearch * *" matches all elasticsearch changelogs, "cloud-serverless 2025-12-02 *" matches cloud-serverless 2025-12-02 with any lifecycle, "* 9.3.* *" matches any product with target starting with "9.3.", "* * *" matches all changelogs (equivalent to --all). Only one filter option can be specified: `--all`, `--input-products`, or `--prs`. [Default: null]
  --output <string?>                        Optional: Output path for the bundled changelog. Can be either (1) a directory path, in which case 'changelog-bundle.yaml' is created in that directory, or (2) a file path ending in .yml or .yaml. Defaults to 'changelog-bundle.yaml' in the input directory [Default: null]
  --output-products <List<ProductInfo>?>    Optional: Explicitly set the products array in the output file in format "product target lifecycle, ...". Overrides any values from changelogs. [Default: null]
  --owner <string?>                         GitHub repository owner (required only when PRs are specified as numbers) [Default: null]
  --prs <string[]?>                         Filter by pull request URLs or numbers (comma-separated), or a path to a newline-delimited file containing PR URLs or numbers. Can be specified multiple times. Only one filter option can be specified: `--all`, `--input-products`, or `--prs`. [Default: null]
  --repo <string?>                          GitHub repository name (required only when PRs are specified as numbers) [Default: null]
  --resolve                                 Optional: Copy the contents of each changelog file into the entries array. By default, the bundle contains only the file names and checksums.
```

You can specify only one of the following filter options:

- `--all`: Include all changelogs from the directory.
- `--input-products`: Include changelogs for the specified products. Refer to [Filter by product](#changelog-bundle-product).
- `--prs`: Include changelogs for the specified pull request URLs or numbers, or a path to a newline-delimited file containing PR URLs or numbers. Go to [Filter by pull requests](#changelog-bundle-pr).

By default, the output file contains only the changelog file names and checksums.
You can optionally use the `--resolve` command option to pull all of the content from each changelog into the bundle.

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
  pr: https://github.com/elastic/elasticsearch/pull/108875
...
```

:::{note}
When a changelog matches multiple `--input-products` filters, it appears only once in the bundle. This deduplication applies even when using `--all` or `--prs`.
:::

### Output file location

The `--output` option supports two formats:

1. **Directory path**: If you specify a directory path (without a filename), the command creates `changelog-bundle.yaml` in that directory:

   ```sh
   docs-builder changelog bundle --all --output /path/to/output/dir
   # Creates /path/to/output/dir/changelog-bundle.yaml
   ```

2. **File path**: If you specify a file path ending in `.yml` or `.yaml`, the command uses that exact path:

   ```sh
   docs-builder changelog bundle --all --output /path/to/custom-bundle.yaml
   # Creates /path/to/custom-bundle.yaml
   ```

If you specify a file path with a different extension (not `.yml` or `.yaml`), the command returns an error.

## Create documentation [render-changelogs]

The `docs-builder changelog render` command creates markdown or asciidoc files from changelog bundles for documentation purposes.
For up-to-date details, use the `-h` command option:

```sh
Render bundled changelog(s) to markdown or asciidoc files

Options:
  --input <string[]?>            Required: Bundle input(s) in format "bundle-file-path|changelog-file-path|repo|link-visibility" (use pipe as delimiter). To merge multiple bundles, separate them with commas. Only bundle-file-path is required. link-visibility can be "hide-links" or "keep-links" (default). Paths must be absolute or use environment variables; tilde (~) expansion is not supported. [Default: null]
  --config <string?>             Optional: Path to the changelog.yml configuration file. Defaults to 'docs/changelog.yml' [Default: null]
  --file-type <string?>          Optional: Output file type. Valid values: "markdown" or "asciidoc". Defaults to "markdown" [Default: @"markdown"]
  --hide-features <string[]?>    Filter by feature IDs (comma-separated), or a path to a newline-delimited file containing feature IDs. Can be specified multiple times. Entries with matching feature-id values will be commented out in the output. [Default: null]
  --output <string?>             Optional: Output directory for rendered files. Defaults to current directory [Default: null]
  --subsections                  Optional: Group entries by area/component in subsections. For breaking changes with a subtype, groups by subtype instead of area. Defaults to false
  --title <string?>              Optional: Title to use for section headers in output files. Defaults to version from first bundle [Default: null]
```

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
* Fix ML calendar event update scalability issues. [#136886](https://github.com/elastic/elastic/pull/136886) 

**Aggregations**
* Break on FieldData when building global ordinals. [#108875](https://github.com/elastic/elastic/pull/108875) 
```

To comment out the pull request and issue links, for example if they relate to a private repository, add `hide-links` to the `--input` option for that bundle. This allows you to selectively hide links per bundle when merging changelogs from multiple repositories.

If you have changelogs with `feature-id` values and you want them to be omitted from the output, use the `--hide-features` option.
For more information, refer to [](/cli/release/changelog-render.md).

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
