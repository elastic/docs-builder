# changelog bundle

Bundle changelog files.

To create the changelogs, use [](/cli/release/changelog-add.md).
For details and examples, go to [](/contribute/changelog.md).

## Usage

```sh
docs-builder  changelog bundle [arguments...] [options...] [-h|--help]
```

## Profile-based vs. option-based usage [changelog-bundle-invocation-modes]

`changelog bundle` supports two mutually exclusive invocation modes:

- **Profile-based**: `bundle <profile> <version-or-report>` — all paths and filters come from the changelog configuration file. No other options are allowed.
- **Option-based**: `bundle --all` (or `--input-products`, `--prs`, `--issues`) — you supply all filter and output options directly.

You cannot mix the two modes. Passing any option-based flag together with a profile returns an error.

Profile-based commands discover the changelog configuration automatically (no `--config` flag): they look for `changelog.yml` in the current directory, then `docs/changelog.yml`. If neither file is found, the command returns an error with instructions to run `docs-builder changelog init` or to re-run from the folder where the file exists.

## Arguments

These arguments apply to profile-based bundling:

`[0] <string?>`
:   Profile name from `bundle.profiles` in the changelog configuration file.
:   For example, "elasticsearch-release".
:   When it's specified, the second argument is the version or promotion report URL.

`[1] <string?>`
:   Version number or promotion report URL or path.
:   For example, "9.2.0" or "https://buildkite.../promotion-report.html".

## Options (option-based mode only)

The following options are only valid in option-based mode (no profile argument). Using any of them with a profile returns an error.

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
:   Optional: Filter by feature IDs (comma-separated), or a path to a newline-delimited file containing feature IDs.
:   Can be specified multiple times.
:   Entries with matching `feature-id` values will be commented out when the bundle is rendered (by the `changelog render` command or `{changelog}` directive).

`--input-products <List<ProductInfo>?>`
:   Filter by products in format "product target lifecycle, ..."
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
:   Each occurrence can be either comma-separated issues (e.g., `--issues "https://github.com/owner/repo/issues/123,456"`) or a file path (e.g., `--issues /path/to/file.txt`).
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
:   Each occurrence can be either comma-separated PRs (e.g., `--prs "https://github.com/owner/repo/pull/123,6789"`) or a file path (e.g., `--prs /path/to/file.txt`).
:   When specifying PRs directly, provide comma-separated values.
:   When specifying a file path, provide a single value that points to a newline-delimited file.

`--repo <string?>`
:   The GitHub repository name, which is required when pull requests or issues are specified as numbers.

`--resolve`
:   Optional: Copy the contents of each changelog file into the entries array.
:   By default, the bundle contains only the file names and checksums.

## Output file location

When you do not specify `--output`, the command uses `bundle.output_directory` from your changelog configuration if it is set (creating `changelog-bundle.yaml` in that directory), otherwise writes to `changelog-bundle.yaml` in the input directory.

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

Example profile with all fields:

```yaml
bundle:
  profiles:
    elasticsearch-release:
      products: "elasticsearch {version} {lifecycle}"
      output: "elasticsearch-{version}.yaml"
      output_products: "elasticsearch {version} ga"
      repo: elasticsearch
      owner: elastic
      hide_features:
        - feature:my-feature-flag
```
