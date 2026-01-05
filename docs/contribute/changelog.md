# Create and bundle changelogs

By adding a file for each notable change and grouping them into bundles, you can ultimately generate release documention with a consistent layout for all your products.

1. Create changelogs with the `docs-builder changelog add` command.
2. [Create changelog bundles](#changelog-bundle) with the `docs-builder changelog bundle` command. For example, create a bundle for the pull requests that are included in a product release.
3. [Create documentation](#render-changelogs) with the `docs-builder changelog render` command.

For more information about running `docs-builder`, go to [Contribute locally](https://www.elastic.co/docs/contribute-docs/locally).

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

## Create bundles [changelog-bundle]

You can use the `docs-builder changelog bundle` command to create a YAML file that lists multiple changelogs.
For up-to-date details, use the `-h` option:

```sh
Bundle changelogs

Options:
  --directory <string?>                     Optional: Directory containing changelog YAML files. Defaults to current directory [Default: null]
  --output <string?>                        Optional: Output file path for the bundled changelog. Defaults to 'changelog-bundle.yaml' in the input directory [Default: null]
  --all                                     Include all changelogs in the directory
  --input-products <List<ProductInfo>?>     Filter by products in format "product target lifecycle, ..." (e.g., "cloud-serverless 2025-12-02, cloud-serverless 2025-12-06") [Default: null]
  --output-products <List<ProductInfo>?>    Explicitly set the products array in the output file in format "product target lifecycle, ...". Overrides any values from changelogs. [Default: null]
  --resolve                                 Copy the contents of each changelog file into the entries array
  --prs <string[]?>                         Filter by pull request URLs or numbers (comma-separated), or a path to a newline-delimited file containing PR URLs or numbers. Can be specified multiple times. [Default: null]
  --owner <string?>                         Optional: GitHub repository owner (used when PRs are specified as numbers) [Default: null]
  --repo <string?>                          Optional: GitHub repository name (used when PRs are specified as numbers) [Default: null]
```

You can specify only one of the following filter options:

`--all`
:   Include all changelogs from the directory.

`--input-products`
:   Include changelogs for the specified products.
:   The format aligns with [](#product-format).
:   For example, `"cloud-serverless 2025-12-02, cloud-serverless 2025-12-06"`.

`--prs`
:   Include changelogs for the specified pull request URLs or numbers, or a path to a newline-delimited file containing PR URLs or numbers. Can be specified multiple times.
:   Each occurrence can be either comma-separated PRs (e.g., `--prs "https://github.com/owner/repo/pull/123,12345"`) or a file path (e.g., `--prs /path/to/file.txt`).
:   When specifying PRs directly, provide comma-separated values.
:   When specifying a file path, provide a single value that points to a newline-delimited file. The file should contain one PR URL or number per line.
:   Pull requests can be identified by a full URL (such as `https://github.com/owner/repo/pull/123`), a short format (such as `owner/repo#123`), or just a number (in which case you must also provide `--owner` and `--repo` options).

By default, the output file contains only the changelog file names and checksums.
You can optionally use the `--resolve` command option to pull all of the content from each changelog into the bundle.

### Filter by product [changelog-bundle-product]

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

### Filter by pull requests [changelog-bundle-pr]

You can use the `--prs` option (with the `--repo` and `--owner` options if you provide only the PR numbers) to create a bundle of the changelogs that relate to those pull requests:

```sh
docs-builder changelog bundle --prs "108875,135873,136886" \ <1>
  --repo elasticsearch \ <2>
  --owner elastic \ <3>
  --output-products "elasticsearch 9.2.2" <4>
```

1. The comma-separated list of pull request numbers to seek. You can also specify multiple `--prs` options, each with comma-separated PRs or a file path.
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
  --output-products "elasticsearch 9.2.2" <3>
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

## Create documentation [render-changelogs]

The `docs-builder changelog render` command creates markdown files from changelog bundles for documentation purposes.
For up-to-date details, use the `-h` command option:

```sh
Render bundled changelog(s) to markdown files

Options:
  --input <List<BundleInput>>    Required: Bundle input(s) in format "bundle-file-path, changelog-file-path, repo". Can be specified multiple times. Only bundle-file-path is required. [Required]
  --output <string?>             Optional: Output directory for rendered markdown files. Defaults to current directory [Default: null]
  --title <string?>              Optional: Title to use for section headers in output markdown files. Defaults to version from first bundle [Default: null]
  --subsections                  Optional: Group entries by area/component in subsections. Defaults to false
  --hide-private-links           Optional: Hide private links by commenting them out in the markdown output. Defaults to false
  --hide-features <string[]?>    Filter by feature IDs (comma-separated), or a path to a newline-delimited file containing feature IDs. Can be specified multiple times. Entries with matching feature-id values will be commented out in the markdown output. [Default: null]
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
  --input "./changelog-bundle.yaml,./changelogs,elasticsearch" \ <1>
  --title 9.2.2 \ <2>
  --output ./release-notes \ <3>
  --subsections \ <4>
```

1. Provide information about the changelog bundle. The format is `"<bundle-file-path>, <changelog-file-path>, <repository>"`. Only the `<bundle-file-path>` is required. The `<changelog-file-path>` is useful if the changelogs are not in the default directory and are not resolved within the bundle. The `<repository>` is necessary if your changelogs do not contain full URLs for the pull requests or issues. You can specify `--input` multiple times to merge multiple bundles.
2. The `--title` value is used for an output folder name and for section titles in the markdown files. If you omit `--title` and the first bundle contains a product `target` value, that value is used. Otherwise, if none of the bundles have product `target` fields, the title defaults to "unknown".
3. By default the command creates the output files in the current directory.
4. By default the changelog areas are not displayed in the output. Add `--subsections` to group changelog details by their `areas`.

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

To comment out the pull request and issue links, for example if they relate to a private repository, use the `--hide-private-links` option.

If you have changelogs with `feature-id` values and you want them to be omitted from the output, use the `--hide-features` option.
For more information, refer to [](/cli/release/changelog-render.md).
