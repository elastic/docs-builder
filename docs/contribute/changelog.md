# Create and bundle changelogs

The `docs-builder changelog add` command creates a new changelog file from command-line input.
The `docs-builder changelog bundle` command creates a consolidated list of changelogs.

By adding a file for each notable change and grouping them into bundles, you can ultimately generate release documention with a consistent layout for all your products.

:::{note}
This command is associated with an ongoing release docs initiative.
Additional workflows are still to come for updating and generating documentation from changelogs.
:::

The changelogs use the following schema:

:::{dropdown} Changelog schema
::::{include} /contribute/_snippets/changelog-fields.md
::::
:::

## Command options

The `changelog add` command creates a single YAML changelog file and supports all of the following options:

```sh
Usage: changelog add [options...] [-h|--help] [--version]

Add a new changelog from command-line input

Options:
  --products <List<ProductInfo>>    Required: Products affected in format "product target lifecycle, ..." (e.g., "elasticsearch 9.2.0 ga, cloud-serverless 2025-08-05") [Required]
  --title <string?>                 Optional: A short, user-facing title (max 80 characters). Required if --pr is not specified. If --pr and --title are specified, the latter value is used instead of what exists in the PR. [Default: null]
  --type <string?>                  Optional: Type of change (feature, enhancement, bug-fix, breaking-change, etc.). Required if --pr is not specified. If mappings are configured, type can be derived from the PR. [Default: null]
  --subtype <string?>               Optional: Subtype for breaking changes (api, behavioral, configuration, etc.) [Default: null]
  --areas <string[]?>               Optional: Area(s) affected (comma-separated or specify multiple times) [Default: null]
  --pr <string?>                    Optional: Pull request URL or PR number (if --owner and --repo are provided). If specified, --title can be derived from the PR. If mappings are configured, --areas and --type can also be derived from the PR. [Default: null]
  --owner <string?>                 Optional: GitHub repository owner (used when --pr is just a number) [Default: null]
  --repo <string?>                  Optional: GitHub repository name (used when --pr is just a number) [Default: null]
  --issues <string[]?>              Optional: Issue URL(s) (comma-separated or specify multiple times) [Default: null]
  --description <string?>           Optional: Additional information about the change (max 600 characters) [Default: null]
  --impact <string?>                Optional: How the user's environment is affected [Default: null]
  --action <string?>                Optional: What users must do to mitigate [Default: null]
  --feature-id <string?>            Optional: Feature flag ID [Default: null]
  --highlight <bool?>               Optional: Include in release highlights [Default: null]
  --output <string?>                Optional: Output directory for the changelog. Defaults to current directory [Default: null]
  --config <string?>                Optional: Path to the changelog.yml configuration file. Defaults to 'docs/changelog.yml' [Default: null]
```

The `changelog bundle` command creates a single YAML bundle file and supports all of the following options:

```sh
Bundle changelogs

Options:
  --directory <string?>                     Optional: Directory containing changelog YAML files. Defaults to current directory [Default: null]
  --output <string?>                        Optional: Output file path for the bundled changelog. Defaults to 'changelog-bundle.yaml' in the input directory [Default: null]
  --all                                     Include all changelogs in the directory
  --input-products <List<ProductInfo>?>     Filter by products in format "product target lifecycle, ..." (e.g., "cloud-serverless 2025-12-02, cloud-serverless 2025-12-06") [Default: null]
  --output-products <List<ProductInfo>?>    Explicitly set the products array in the output file in format "product target lifecycle, ...". Overrides any values from changelogs. [Default: null]
  --resolve                                 Copy the contents of each changelog file into the entries array
  --prs <string[]?>                         Filter by pull request URLs or numbers (can specify multiple times) [Default: null]
  --prs-file <string?>                      Path to a newline-delimited file containing PR URLs or numbers [Default: null]
  --owner <string?>                         Optional: GitHub repository owner (used when PRs are specified as numbers) [Default: null]
  --repo <string?>                          Optional: GitHub repository name (used when PRs are specified as numbers) [Default: null]
```

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

## Changelog configuration

Some of the fields in the changelog files accept only a specific set of values.

:::{important}

- Product values must exist in [products.yml](https://github.com/elastic/docs-builder/blob/main/config/products.yml). Invalid products will cause the `docs-builder changelog add` command to fail.
- Type, subtype, and lifecycle values must match the available values defined in [ChangelogConfiguration.cs](https://github.com/elastic/docs-builder/blob/main/src/services/Elastic.Documentation.Services/Changelog/ChangelogConfiguration.cs). Invalid values will cause the `docs-builder changelog add` command to fail.
:::

If you want to further limit the list of acceptable values, you can create a changelog configuration file.
Refer to [changelog.yml.example](https://github.com/elastic/docs-builder/blob/main/config/changelog.yml.example).

By default, the `docs-builder changelog add` command checks the following path: `docs/changelog.yml`.
You can specify a different path with the `--config` command option.

If a configuration file exists, the command validates all its values before generating the changelog file:

- If the configuration file contains `lifecycle`, `product`, `subtype`, or `type` values that don't match the values in `products.yml` and `ChangelogConfiguration.cs`, validation fails. The changelog file is not created.
- If the configuration file contains `areas` values and they don't match what you specify in the `--areas` command option, validation fails. The changelog file is not created.

### GitHub label mappings

You can optionally add `label_to_type` and `label_to_areas` mappings in your changelog configuration.
When you run the command with the `--pr` option, it can use these mappings to fill in the `type` and `areas` in your changelog based on your pull request labels.

Refer to [changelog.yml.example](https://github.com/elastic/docs-builder/blob/main/config/changelog.yml.example).

## Bundle creation

You can use the `docs-builder changelog bundle` command to create a YAML file that lists multiple changelogs.
By default, the file contains only the changelog file names and checksums.
For example:

```yaml
products:
- product: elasticsearch
  target: 9.2.2
entries:
- file:
    name: 1765507819-fix-ml-calendar-event-update-scalability-issues.yaml
    checksum: 069b59edb14594e0bc3b70365e81626bde730ab7
```

You can optionally use the `--resolve` command option to pull all of the content from each changelog into the bundle.
For example:

```yaml
products:
- product: elasticsearch
  target: 9.2.2
entries:
- file:
    name: 1765507819-fix-ml-calendar-event-update-scalability-issues.yaml
    checksum: 069b59edb14594e0bc3b70365e81626bde730ab7
  type: bug-fix
  title: Fix ML calendar event update scalability issues
  products:
  - product: elasticsearch
  areas:
  - Machine Learning
  pr: https://github.com/elastic/elasticsearch/pull/136886
```

When you run the `docs-builder changelog bundle` command, you can specify only one of the following filter options:

`--all`
:   Include all changelogs from the directory.

`--input-products`
:   Include changelogs for the specified products.
:   The format aligns with [](#product-format).
:   For example, `"cloud-serverless 2025-12-02, cloud-serverless 2025-12-06"`.

`--prs`
:   Include changelogs for the specified pull request URLs or numbers.
:   Pull requests can be identified by a full URL (such as `https://github.com/owner/repo/pull/123`, a short format (such as `owner/repo#123`) or just a number (in which case you must also provide `--owner` and `--repo` options).

`--prs-file`
:   Include changelogs for the pull request URLs or numbers specified in a newline-delimited file.
:   Pull requests can be identified by a full URL (such as `https://github.com/owner/repo/pull/123`, a short format (such as `owner/repo#123`) or just a number (in which case you must also provide `--owner` and `--repo` options).

## Examples

### Create a changelog for multiple products

The following command creates a changelog for a bug fix that applies to two products:

```sh
docs-builder changelog add \
  --title "Fixes enrich and lookup join resolution based on minimum transport version" \ <1>
  --type bug-fix \ <2>
  --products "elasticsearch 9.2.3, cloud-serverless 2025-12-02" \ <3>
  --areas "ES|QL"
  --pr "https://github.com/elastic/elasticsearch/pull/137431" <4>
```

1. This option is required only if you want to override what's derived from the PR title.
2. The type values are defined in [ChangelogConfiguration.cs](https://github.com/elastic/docs-builder/blob/main/src/services/Elastic.Documentation.Services/Changelog/ChangelogConfiguration.cs).
3. The product values are defined in [products.yml](https://github.com/elastic/docs-builder/blob/main/config/products.yml).
4. The `--pr` value can be a full URL (such as `https://github.com/owner/repo/pull/123`, a short format (such as `owner/repo#123`) or just a number (in which case you must also provide `--owner` and `--repo` options).

The output file has the following format:

```yaml
pr: https://github.com/elastic/elasticsearch/pull/137431
type: bug-fix
products:
- product: elasticsearch
  target: 9.2.3
- product: cloud-serverless
  target: 2025-12-02
title: Fixes enrich and lookup join resolution based on minimum transport version
areas:
- ES|QL
```

### Create a changelog with PR label mappings

You can update your changelog configuration file to contain GitHub label mappings, for example:

```yaml
# Available areas (optional - if not specified, all areas are allowed)
available_areas:
  - search
  - security
  - machine-learning
  - observability
  - index-management
  - ES|QL
  # Add more areas as needed

# GitHub label mappings (optional - used when --pr option is specified)
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

When you use the `--pr` option to derive information from a pull request, it can make use of those mappings:

```sh
docs-builder changelog add \
  --pr https://github.com/elastic/elasticsearch/pull/139272 \
  --products "elasticsearch 9.3.0" --config test/changelog.yml
```

In this case, the changelog file derives the title, type, and areas:

```yaml
pr: https://github.com/elastic/elasticsearch/pull/139272
type: enhancement
products:
- product: elasticsearch
  target: 9.3.0
areas:
- ES|QL
title: '[ES|QL] Take TOP_SNIPPETS out of snapshot'
```

### Create a changelog bundle by product

You can use the `--input-products` option to create a bundle of changelogs that match the product details:

```sh
docs-builder changelog bundle \
  --input-products "cloud-serverless 2025-12-02, cloud-serverless 2025-12-06" <1>
```

1. Include all changelogs that have the `cloud-serverless` product identifier and target dates of either December 2 2025 or December 12 2025. For more information about product values, refer to [](#product-format).

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

1. By default these values match your `--input-products` (even if the changelogs have more products). To specify different product metadata, use the `--output-products` option.

If you add the `--resolve` option, the contents of each changelog will be included in the output file.
Refer to [](#bundle-creation).

### Create a changelog bundle by PR list

You can use the `--prs` option (with the `--repo` and `--owner` options if you provide only the PR numbers) to create a bundle of the changelogs that relate to those pull requests:

```sh
docs-builder changelog bundle --prs 108875,135873,136886 \ <1>
  --repo elasticsearch \ <2>
  --owner elastic \ <3>
  --output-products "elasticsearch 9.2.2" <4>
```

1. The list of pull request numbers to seek.
2. The repository in the pull request URLs. This option is not required if you specify the short or full PR URLs in the `--prs` option.
3. The owner in the pull request URLs. This option is not required if you specify the short or full PR URLs in the `--prs` option.
4. The product metadata for the bundle. If it is not provided, it will be derived from all the changelog product values.

If you have changelog files that reference those pull requests, the command creates a file like this:

```yaml
products:
- product: elasticsearch
  target: 9.2.2
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
Refer to [](#bundle-creation).


### Create a changelog bundle by PR file

If you have a file that lists pull requests (such as PRs associated with a GitHub release):

```txt
https://github.com/elastic/elasticsearch/pull/108875
https://github.com/elastic/elasticsearch/pull/135873
https://github.com/elastic/elasticsearch/pull/136886
https://github.com/elastic/elasticsearch/pull/137126
```

You can use the `--prs-file` option to create a bundle of the changelogs that relate to those pull requests:

```sh
./docs-builder changelog bundle --prs-file test/9.2.2.txt \ <1>
  --output-products "elasticsearch 9.2.2" <3>
  --resolve <3>
```

1. The path for the file that lists the pull requests. If the file contains only PR numbers, you must add `--repo` and `--owner` command options.
2. The product metadata for the bundle. If it is not provided, it will be derived from all the changelog product values.
3. Optionally include the contents of each changelog in the output file.

If you have changelog files that reference those pull requests, the command creates a file like this:

```yaml
products:
- product: elasticsearch
  target: 9.2.2
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
