# Create and bundle changelogs

The `docs-builder changelog add` command creates a new changelog file from command-line input.
The `docs-builder changelog bundle` command creates a consolidated list of changelogs.

By adding a file for each notable change and grouping them into bundles, you can ultimately generate release documention with a consistent layout for all your products.

:::{note}
This command is associated with an ongoing release docs initiative.
Additional workflows are still to come for managing the list of changelogs in each release.
:::

The changelogs use the following schema:

:::{dropdown} Changelog schema
::::{include} /contribute/_snippets/changelog-fields.md
::::
:::

## Command options

The `changelog add` command supports all of the following options, which generally align with fields in the changelog schema:

```sh
Usage: changelog add [options...] [-h|--help] [--version]

Add a new changelog fragment from command-line input

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
  --output <string?>                Optional: Output directory for the changelog fragment. Defaults to current directory [Default: null]
  --config <string?>                Optional: Path to the changelog.yml configuration file. Defaults to 'docs/changelog.yml' [Default: null]
```

The `changelog bundle` command supports all of the following options, which provide multiple methods for collecting the changelogs:

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

The `--products` parameter accepts products in the format `"product target lifecycle, ..."` where:

- `product` is the product ID from [products.yml](https://github.com/elastic/docs-builder/blob/main/config/products.yml) (required)
- `target` is the target version or date (optional)
- `lifecycle` is one of: `preview`, `beta`, or `ga` (optional)

Examples:

- `"kibana 9.2.0 ga"`
- `"cloud-serverless 2025-08-05"`
- `"cloud-enterprise 4.0.3, cloud-hosted 2025-10-31"`

## Changelog configuration

Some of the fields in the changelog accept only a specific set of values.

:::{important}

- Product values must exist in [products.yml](https://github.com/elastic/docs-builder/blob/main/config/products.yml). Invalid products will cause the command to fail.
- Type, subtype, and lifecycle values must match the available values defined in [ChangelogConfiguration.cs](https://github.com/elastic/docs-builder/blob/main/src/services/Elastic.Documentation.Services/Changelog/ChangelogConfiguration.cs). Invalid values will cause the command to fail.
:::

If you want to further limit the list of values, you can optionally create a configuration file.
Refer to [changelog.yml.example](https://github.com/elastic/docs-builder/blob/main/config/changelog.yml.example).

By default, the command checks the following path: `docs/changelog.yml`.
You can specify a different path with the `--config` command option.

If a configuration file exists, the command validates all its values before generating the changelog file:

- If the configuration file contains `lifecycle`, `product`, `subtype`, or `type` values that don't match the values in `products.yml` and `ChangelogConfiguration.cs`, validation fails. The changelog file is not created.
- If the configuration file contains `areas` values and they don't match what you specify in the `--areas` command option, validation fails. The changelog file is not created.

### GitHub label mappings

You can optionally add `label_to_type` and `label_to_areas` mappings in your changelog configuration.
When you run the command with the `--pr` option, it can use these mappings to fill in the `type` and `areas` in your changelog based on your pull request labels.

Refer to [changelog.yml.example](https://github.com/elastic/docs-builder/blob/main/config/changelog.yml.example).

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
docs-builder changelog add --pr https://github.com/elastic/elasticsearch/pull/139272 --products "elasticsearch 9.3.0" --config test/changelog.yml
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
