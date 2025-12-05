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
  --title <string>                  Required: A short, user-facing title (max 80 characters) (Required)
  --type <string>                   Required: Type of change (feature, enhancement, bug-fix, breaking-change, etc.) (Required)
  --products <List<ProductInfo>>    Required: Products affected in format "product target lifecycle, ..." (e.g., "elasticsearch 9.2.0 ga, cloud-serverless 2025-08-05") (Required)
  --subtype <string?>               Optional: Subtype for breaking changes (api, behavioral, configuration, etc.) (Default: null)
  --areas <string[]?>               Optional: Area(s) affected (comma-separated or specify multiple times) (Default: null)
  --pr <string?>                    Optional: Pull request URL (Default: null)
  --issues <string[]?>              Optional: Issue URL(s) (comma-separated or specify multiple times) (Default: null)
  --description <string?>           Optional: Additional information about the change (max 600 characters) (Default: null)
  --impact <string?>                Optional: How the user's environment is affected (Default: null)
  --action <string?>                Optional: What users must do to mitigate (Default: null)
  --feature-id <string?>            Optional: Feature flag ID (Default: null)
  --highlight <bool?>               Optional: Include in release highlights (Default: null)
  --output <string?>                Optional: Output directory for the changelog fragment. Defaults to current directory (Default: null)
  --config <string?>                Optional: Path to the changelog.yml configuration file. Defaults to 'docs/changelog.yml' (Default: null)
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

## Examples

The following command creates a changelog for a bug fix that applies to two products:

```sh
docs-builder changelog add \
  --title "Fixes enrich and lookup join resolution based on minimum transport version" \
  --type bug-fix \ <1>
  --products "elasticsearch 9.2.3, cloud-serverless 2025-12-02" \ <2>
  --areas "ES|QL"
  --pr "https://github.com/elastic/elasticsearch/pull/137431" <3>
```

1. The type values are defined in [ChangelogConfiguration.cs](https://github.com/elastic/docs-builder/blob/main/src/services/Elastic.Documentation.Services/Changelog/ChangelogConfiguration.cs).
2. The product values are defined in [products.yml](https://github.com/elastic/docs-builder/blob/main/config/products.yml).
3. At this time, the PR value can be a number or a URL; it is not validated.

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
