# changelog bundle

Bundle changelog files.

To create the changelogs, use [](/cli/release/changelog-add.md).
For details and examples, go to [](/contribute/changelog.md).

## Usage

```sh
docs-builder  changelog bundle [arguments...] [options...] [-h|--help]
```

`changelog bundle` supports two mutually exclusive invocation modes:

- **Profile-based**: All paths and filters come from the changelog configuration file. No other options are allowed. For example, `bundle <profile> <version-or-report>`.
- **Option-based**: You supply all filter and output options directly. For example, `bundle --all` (or `--input-products`, `--prs`, `--issues`).

You cannot mix the two modes. Passing any option-based flag together with a profile returns an error.

Profile-based commands discover the changelog configuration automatically (no `--config` flag): they look for `changelog.yml` in the current directory, then `docs/changelog.yml`.
If neither file is found, the command returns an error with instructions to run `docs-builder changelog init` or to re-run from the folder where the file exists.

## Arguments

These arguments apply to profile-based bundling:

`[0] <string?>`
:   Profile name from `bundle.profiles` in the changelog configuration file.
:   For example, "elasticsearch-release".
:   When it's specified, the second argument is the version or promotion report URL.

`[1] <string?>`
:   Version number or promotion report URL or path.
:   For example, "9.2.0" or `https://buildkite.../promotion-report.html`.

:::{note}
Only the profile-based method currently supports buildkite promotion reports.
There is no equivalent command option.
:::

<!-- 
TBD: Does the promotion report need to have a specific format? Can we provide more details or an example?
-->

## Options

The following options are only valid in option-based mode (no profile argument).
Using any of them with a profile returns an error.

`--all`
:   Include all changelogs from the directory.
:   Only one filter option can be specified: `--all`, `--input-products`, `--prs`, or `--issues`.

`--config <string?>`
:   Optional: Path to the changelog.yml configuration file.
:   Defaults to `docs/changelog.yml`.

`--directory <string?>`
:   Optional: The directory that contains the changelog YAML files.
:   When not specified, uses `bundle.directory` from the changelog configuration if set, otherwise the current directory.

`--hide-features <string[]?>`
:   Optional: A list of feature IDs (comma-separated), or a path to a newline-delimited file containing feature IDs.
:   Can be specified multiple times.
:   Adds a `hide-features` list to the bundle.
:   When the bundle is rendered (by the `changelog render` command or `{changelog}` directive), changelogs with matching `feature-id` values will be commented out of the documentation.

`--input-products <List<ProductInfo>?>`
:   Filter by products in the format "product target lifecycle, ...".
:   For more information about the valid product and lifecycle values, go to [Product format](/contribute/changelog.md#product-format).
:   Only one filter option can be specified: `--all`, `--input-products`, `--prs`, or `--issues`.
:   When specified, all three parts (product, target, lifecycle) are required but can be wildcards (`*`). Multiple comma-separated values are combined with OR: a changelog is included if it matches any of the specified product/target/lifecycle combinations. For example:

- `"cloud-serverless 2025-12-02 ga, cloud-serverless 2025-12-06 beta"` — include changelogs for either cloud-serverless 2025-12-02 ga or cloud-serverless 2025-12-06 beta
- `"cloud-serverless 2025-12-02 *"` - match cloud-serverless 2025-12-02 with any lifecycle
- `"elasticsearch * *"` - match all elasticsearch changelogs
- `"* 9.3.* *"` - match any product with target starting with "9.3."
- `"* * *"` - match all changelogs (equivalent to `--all`)

`--issues <string[]?>`
:   Filter by issue URLs or numbers (comma-separated), or a path to a newline-delimited file containing issue URLs or numbers. Can be specified multiple times.
:   Only one filter option can be specified: `--all`, `--input-products`, `--prs`, or `--issues`.
:   Each occurrence can be either comma-separated issues ( `--issues "https://github.com/owner/repo/issues/123,456"`) or a file path (for example `--issues /path/to/file.txt`).
:   When specifying issues directly, provide comma-separated values.
:   When specifying a file path, provide a single value that points to a newline-delimited file.

`--no-resolve`
:   Optional: Explicitly turn off the `resolve` option if it's specified in the changelog configuration file.

`--output <string?>`
:   Optional: The output path for the bundle.
:   Can be either (1) a directory path, in which case `changelog-bundle.yaml` is created in that directory, or (2) a file path ending in `.yml` or `.yaml`.
:   When not specified, uses `bundle.output_directory` from the changelog configuration (creating `changelog-bundle.yaml` in that directory) if set, otherwise `changelog-bundle.yaml` in the input directory.

`--output-products <List<ProductInfo>?>`
:   Optional: Explicitly set the products array in the output file in format "product target lifecycle, ...".
:   This value replaces information that would otherwise by derived from changelogs.

`--owner <string?>`
:   The GitHub repository owner, which is required when pull requests or issues are specified as numbers.

`--prs <string[]?>`
:   Filter by pull request URLs or numbers (comma-separated), or a path to a newline-delimited file containing PR URLs or numbers. Can be specified multiple times.
:   Only one filter option can be specified: `--all`, `--input-products`, `--prs`, or `--issues`.
:   Each occurrence can be either comma-separated PRs (for example `--prs "https://github.com/owner/repo/pull/123,6789"`) or a file path (for example `--prs /path/to/file.txt`).
:   When specifying PRs directly, provide comma-separated values.
:   When specifying a file path, provide a single value that points to a newline-delimited file.

`--repo <string?>`
:   The GitHub repository name, which is required when pull requests or issues are specified as numbers.

`--resolve`
:   Optional: Copy the contents of each changelog file into the entries array.
:   By default, the bundle contains only the file names and checksums.

## Output files

Profile-based bundles are created in `bundle.output_directory`.
If `output_directory` is not set, they are created in the `bundle.directory` alongside the changelog files.
Bundle names are determined by the `bundle.profiles.<name>.output` setting, which can optionally include additional profile-specific paths. For example: `"stack/kibana-{version}.yaml"`.
If that setting is absent, the default name is `changelog-bundle.yaml`

In the option-based mode, when you do not specify `--output`, the command uses `bundle.output_directory` or defaults to the input directory.
When you specify `--output`, it supports two formats:

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

## Repository name in bundles [changelog-bundle-repo]

When you specify the `--repo` option (option-based mode) or the `repo` field in a profile (profile-based mode), the repository name is stored in the bundle's product metadata.
This ensures that PR and issue links are generated correctly when the bundle is rendered.

```sh
docs-builder changelog bundle \
  --input-products "cloud-serverless 2025-12-02 *" \
  --repo cloud \ <1>
  --output /path/to/bundles/2025-12-02.yaml
```

1. The GitHub repository name. This is stored in each product entry in the bundle.

The bundle output will include a `repo` field in each product:

```yaml
products:
- product: cloud-serverless
  target: 2025-12-02
  repo: cloud
entries:
- file:
    name: 1765495972-new-feature.yaml
    checksum: 6c3243f56279b1797b5dfff6c02ebf90b9658464
```

When rendering, pull request and issue links will use `https://github.com/elastic/cloud/...` instead of the product ID in the URL.

:::{note}
If the `repo` field is not specified, the product ID is used as a fallback for link generation.
This may result in broken links if the product ID doesn't match the GitHub repository name (for example, `cloud-serverless` vs `cloud`).
:::

## Profile configuration fields [changelog-bundle-profile-config]

Bundle profiles in `changelog.yml` support the following fields:

`products`
:   Required. The product filter pattern for input changelogs. Supports `{version}` and `{lifecycle}` placeholders that are substituted at runtime.
:   Example: `"elasticsearch {version} {lifecycle}"`

`output`
:   Required for bundling. The output filename pattern. `{version}` is substituted at runtime.
:   Example: `"elasticsearch-{version}.yaml"`

`output_products`
:   Optional. Overrides the products array written to the bundle output. Supports `{version}` and `{lifecycle}` placeholders. Useful when the bundle should advertise a different lifecycle than was used for filtering — for example, when filtering by `preview` changelogs to produce a `ga` bundle.
:   Example: `"elasticsearch {version} ga"`

`repo`
:   Optional. The GitHub repository name written to each product entry in the bundle. Used by the `{changelog}` directive to generate correct PR/issue links. Only needed when the product ID doesn't match the GitHub repository name.
:   Example: `repo: cloud` (for the `cloud-serverless` product)

`owner`
:   Optional. The GitHub owner written to each product entry in the bundle. Defaults to `elastic` when not specified.
:   Example: `owner: elastic`

`hide_features`
:   Optional. Feature IDs to mark as hidden in the bundle output (string or list). When the bundle is rendered, entries with matching `feature-id` values are commented out.

## Examples

The following changelog configuration example contains multiple profiles for filtering the bundles:

```yaml
bundle:
  profiles:
    # Find changelogs with a specific lifecycle
    elasticsearch-ga-only:
      products: "elasticsearch {version} ga" <1>
      output: "elasticsearch-{version}.yaml"
    
    # Find changelogs with any lifecycle (wildcard)
    serverless-release:
      products: "cloud-serverless {version} *" <2>
      output_products: "cloud-serverless {version}"
      output: "serverless-{version}.yaml"
      repo: elasticsearch
      owner: elastic
      
    # Find changelogs that match a promotion report
    serverless-release-report:
      output_products: "cloud-serverless {version}"
      output: "serverless-{version}.yaml"
      repo: elasticsearch
      owner: elastic
    
    # Infer the lifecycle from the version
    elasticsearch-release:
      hide_features: <3>
        - feature-flag-1
        - feature-flag-2
      products: "elasticsearch {version} {lifecycle}" <4>
      output: "elasticsearch-{version}.yaml"
      output_products: "elasticsearch {version}"
```

1. Bundles any changelogs that have `product: elasticsearch`, `lifecycle: ga`, and the version specified in the command. This is equivalent to the `--input-products` command option.
2. Bundles any changelogs that have `product: cloud-serverless`, any lifecycle, and the version specified in the command. This is equivalent to the `--input-products` command option's support for wildcards.
3. Adds a `hide-features` array in the bundle. This is equivalent to the `--hide-features` command option.
4. In this case, the lifecycle is inferred from the version.

For example, when the version is:

- `9.2.0` or `9.2.0-rc.1` the inferred lifecycle that is used in the filter is `ga`.
- `9.2.0-beta.1` the inferred lifecycle is `beta`.
- `9.2.0-alpha.1` or `9.2.0-preview.1` the inferred lifecycle is `preview`.

For more information about acceptable product and lifecycle values, go to [Product format](/contribute/changelog.md#product-format).

You can invoke those profiles with commands like this:

```sh
# Bundle changelogs that match a specific version or date
docs-builder changelog bundle elasticsearch-release 9.2.0

# Bundle changelogs with wildcards
docs-builder changelog bundle serverless-releases 2026-02-*

# Bundle changelogs that match a list of PRs in a promotion report
docs-builder changelog bundle serverless-release-report 2026-02 https://buildkite.../promotion-report.html

# Bundle changelogs that match a list of PRs in a downloaded promotion report
docs-builder changelog bundle serverless-release-report 2026-02 ./promotion-report.html
```