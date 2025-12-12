# Add changelog entries

The `docs-builder changelog add` command creates a new changelog file from command-line input.
By adding a file for each notable change, you can ultimately generate release documention with a consistent layout for all your products.

:::{note}
This command is associated with an ongoing release docs initiative.
Additional workflows are still to come for managing the list of changelogs in each release.
:::

The command generates a YAML file that uses the following schema:

:::{dropdown} Changelog schema
::::{include} /contribute/_snippets/changelog-fields.md
::::
:::

## Command options

The command supports all of the following options, which generally align with fields in the changelog schema:

```sh
Usage: changelog add [options...] [-h|--help] [--version]

Add a new changelog fragment from command-line input

Options:
  --products <List<ProductInfo>>    Required: Products affected in format "product target lifecycle, ..." (e.g., "elasticsearch 9.2.0 ga, cloud-serverless 2025-08-05") [Required]
  --title <string?>                 Optional: A short, user-facing title (max 80 characters). Required if --prs is not specified. If --prs and --title are specified, the latter value is used instead of what exists in the PR. [Default: null]
  --type <string?>                  Optional: Type of change (feature, enhancement, bug-fix, breaking-change, etc.). Required if --prs is not specified. If mappings are configured, type can be derived from the PR. [Default: null]
  --subtype <string?>               Optional: Subtype for breaking changes (api, behavioral, configuration, etc.) [Default: null]
  --areas <string[]?>               Optional: Area(s) affected (comma-separated or specify multiple times) [Default: null]
  --prs <string[]?>                 Optional: Pull request URL(s) or PR number(s) (comma-separated, or if --owner and --repo are provided, just numbers). If specified, --title can be derived from the PR. If mappings are configured, --areas and --type can also be derived from the PR. Creates one changelog file per PR. [Default: null]
  --owner <string?>                 Optional: GitHub repository owner (used when --prs contains just numbers) [Default: null]
  --repo <string?>                  Optional: GitHub repository name (used when --prs contains just numbers) [Default: null]
  --issues <string[]?>              Optional: Issue URL(s) (comma-separated or specify multiple times) [Default: null]
  --description <string?>           Optional: Additional information about the change (max 600 characters) [Default: null]
  --impact <string?>                Optional: How the user's environment is affected [Default: null]
  --action <string?>                Optional: What users must do to mitigate [Default: null]
  --feature-id <string?>            Optional: Feature flag ID [Default: null]
  --highlight <bool?>               Optional: Include in release highlights [Default: null]
  --output <string?>                Optional: Output directory for the changelog fragment. Defaults to current directory [Default: null]
  --config <string?>                Optional: Path to the changelog.yml configuration file. Defaults to 'docs/changelog.yml' [Default: null]
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

Some of the fields in the changelog accept only a specific set of values:

:::{important}

- Product values must exist in [products.yml](https://github.com/elastic/docs-builder/blob/main/config/products.yml). Invalid products will cause the command to fail.
- Type, subtype, and lifecycle values must match the available values defined in [ChangelogConfiguration.cs](https://github.com/elastic/docs-builder/blob/main/src/services/Elastic.Documentation.Services/Changelog/ChangelogConfiguration.cs). Invalid values will cause the command to fail.
:::

If you want to further limit the list of values, you can optionally create a configuration file.
You can also use the configuration file to prevent the creation of changelogs when certain PR labels are present.
Refer to [changelog.yml.example](https://github.com/elastic/docs-builder/blob/main/config/changelog.yml.example).

By default, the command checks the following path: `docs/changelog.yml`.
You can specify a different path with the `--config` command option.

If a configuration file exists, the command validates all its values before generating the changelog file:

- If the configuration file contains `lifecycle`, `product`, `subtype`, or `type` values that don't match the values in `products.yml` and `ChangelogConfiguration.cs`, validation fails. The changelog file is not created.
- If the configuration file contains `areas` values and they don't match what you specify in the `--areas` command option, validation fails. The changelog file is not created.

### GitHub label mappings

You can optionally add `label_to_type` and `label_to_areas` mappings in your changelog configuration.
When you run the command with the `--prs` option, it can use these mappings to fill in the `type` and `areas` in your changelog based on your pull request labels.

Refer to the file layout in [changelog.yml.example](https://github.com/elastic/docs-builder/blob/main/config/changelog.yml.example) and an [example usage](#example-map-label).

### GitHub label blockers

You can also optionally add `product_label_blockers` in your changelog configuration.
When you run the command with the `--prs` and `--products` options and the PR has a label that you've identified as a blocker for that product, the `docs-builder changelog add` command does not create a changelog for that PR.

Refer to the file layout in [changelog.yml.example](https://github.com/elastic/docs-builder/blob/main/config/changelog.yml.example) and an [example usage](#example-block-label).


## Examples

### Create a changelog for multiple products [example-multiple-products]

The following command creates a changelog for a bug fix that applies to two products:

```sh
docs-builder changelog add \
  --title "Fixes enrich and lookup join resolution based on minimum transport version" \ <1>
  --type bug-fix \ <2>
  --products "elasticsearch 9.2.3, cloud-serverless 2025-12-02" \ <3>
  --areas "ES|QL"
  --prs "https://github.com/elastic/elasticsearch/pull/137431" <4>
```

1. This option is required only if you want to override what's derived from the PR title.
2. The type values are defined in [ChangelogConfiguration.cs](https://github.com/elastic/docs-builder/blob/main/src/services/Elastic.Documentation.Services/Changelog/ChangelogConfiguration.cs).
3. The product values are defined in [products.yml](https://github.com/elastic/docs-builder/blob/main/config/products.yml).
4. The `--prs` value can be a full URL (such as `https://github.com/owner/repo/pull/123`, a short format (such as `owner/repo#123`) or just a number (in which case you must also provide `--owner` and `--repo` options). Multiple PRs can be provided comma-separated, and one changelog file will be created for each PR.

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

### Create a changelog with PR label mappings [example-map-label]

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
docs-builder changelog add --prs https://github.com/elastic/elasticsearch/pull/139272 --products "elasticsearch 9.3.0" --config test/changelog.yml
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

### Block changelog creation with PR labels [example-block-label]

You can configure product-specific label blockers to prevent changelog creation for certain PRs based on their labels.

If you run the `docs-builder changelog add` command with the `--prs` option and a PR has a blocking label for any of the products in the `--products` option, that PR will be skipped and no changelog file will be created for it.
A warning message will be emitted indicating which PR was skipped and why.

For example, your configuration file can contain `product_label_blockers` like this:

```yaml
# Product-specific label blockers (optional)
# Maps product IDs to lists of labels that prevent changelog creation for that product
# If you run the changelog add command with the --prs option and a PR has any of these labels, the changelog is not created
product_label_blockers:
  # Example: Skip changelog for cloud.serverless product when PR has "Watcher" label
  cloud-serverless:
    - ":Data Management/Watcher"
    - ">non-issue"
  # Example: Skip changelog creation for elasticsearch product when PR has "skip:releaseNotes" label
  elasticsearch:
    - ">non-issue"
```

Those settings affect commands with the `--prs` option, for example:

```sh
docs-builder changelog add --prs "1234, 5678" --products "cloud-serverless" --owner elastic --repo elasticsearch --config test/changelog.yml
```

If PR 1234 has the `>non-issue` or Watcher label, it will be skipped and no changelog will be created for it.
If PR 5678 does not have any blocking labels, a changelog is created.
