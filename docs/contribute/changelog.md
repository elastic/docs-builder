# Add changelog entries

The `docs-builder changelog add` command creates a new changelog fragment file from command-line input. This is useful for creating changelog entries as part of your development workflow.

## `docs-builder changelog add`

Create a new changelog fragment with required and optional fields. For example:

```sh
docs-builder changelog add \
  --title "Fixes enrich and lookup join resolution based on minimum transport version" \
  --type bug-fix \
  --products "elasticsearch 9.2.3, cloud-serverless 2025-12-02" \
  --areas "ES|QL"
  --pr "https://github.com/elastic/elasticsearch/pull/137431"
```

:::{important}

- Product values must exist in [products.yml](https://github.com/elastic/docs-builder/blob/main/config/products.yml). Invalid products will cause the command to fail.
- Type, subtype, and lifecycle values must match the available values defined in [ChangelogConfiguration.cs](https://github.com/elastic/docs-builder/blob/main/src/services/Elastic.Documentation.Services/Changelog/ChangelogConfiguration.cs). Invalid values will cause the command to fail.
- The command validates all values before generating the changelog file. If validation fails, no file is created.
- At this time, the PR value can be a number or a URL; it is not validated.
:::

## Product format

The `--products` parameter accepts products in the format `"product target lifecycle, ..."` where:

- `product` is the product ID from [products.yml](https://github.com/elastic/docs-builder/blob/main/config/products.yml) (required)
- `target` is the target version or date (optional)
- `lifecycle` is one of: `preview`, `beta`, or `ga` (optional)

Examples:

- `"kibana 9.2.0 ga"`
- `"cloud-serverless 2025-08-05"`
- `"cloud-enterprise 4.0.3, cloud-hosted 2025-10-31"`

## {{dbuild}} changelog add --help

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
