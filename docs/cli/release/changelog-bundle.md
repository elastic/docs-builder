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
:   When specified, the second argument is the version, promotion report URL, or URL list file.

`[1] <string?>`
:   Version number, promotion report URL/path, or URL list file.
:   For example, `9.2.0`, `https://buildkite.../promotion-report.html`, or `/path/to/prs.txt`.

`[2] <string?>`
:   Optional: Promotion report URL/path or URL list file when the second argument is a version string.
:   When provided, `[1]` must be a version string and `[2]` is the PR/issue filter source.
:   For example, `docs-builder changelog bundle serverless-release 2026-02 ./promotion-report.html`.

:::{note}
The third argument (`[2]`) is required when your profile uses `{version}` placeholders in `output` or `output_products` patterns and you also want to filter by a promotion report or URL list. Without it, the version defaults to `"unknown"`.
:::

### Profile argument types

The second argument (`[1]`) and optional third argument (`[2]`) accept the following:

- **Version string** — Used for `{version}` substitution in profile patterns. For example, `9.2.0` or `2026-02`.
- **Promotion report URL** — A URL to an HTML promotion report. PR URLs are extracted from it.
- **Promotion report file** — A path to a local `.html` file containing a promotion report.
- **URL list file** — A path to a plain-text file containing one fully-qualified GitHub PR or issue URL per line. For example, `https://github.com/elastic/elasticsearch/pull/123`. The file must contain only PR URLs or only issue URLs, not a mix. Bare numbers and short forms such as `owner/repo#123` are not allowed.

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
:   When not specified, falls back to `bundle.directory` from the changelog configuration, then the current working directory. See [Output files](#output-files) for the full resolution order.

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
:   Filter by issue URLs (comma-separated), or a path to a newline-delimited file. Can be specified multiple times.
:   Only one filter option can be specified: `--all`, `--input-products`, `--prs`, `--issues`, or `--report`.
:   When specifying inline values, comma-separated issue numbers are allowed when `--owner` and `--repo` are also provided.
:   When using a file, every line must be a fully-qualified GitHub issue URL such as `https://github.com/owner/repo/issues/123`. Bare numbers and short forms are not allowed in files.

`--no-resolve`
:   Optional: Explicitly turn off the `resolve` option if it's specified in the changelog configuration file.

`--output <string?>`
:   Optional: The output path for the bundle.
:   Can be either (1) a directory path, in which case `changelog-bundle.yaml` is created in that directory, or (2) a file path ending in `.yml` or `.yaml`.
:   When not specified, falls back to `bundle.output_directory` from the changelog configuration, then the input directory (which is itself resolved from `--directory`, `bundle.directory`, or the current working directory). See [Output files](#output-files) for the full resolution order.

`--output-products <List<ProductInfo>?>`
:   Optional: Explicitly set the products array in the output file in format "product target lifecycle, ...".
:   This value replaces information that would otherwise by derived from changelogs.

`--owner <string?>`
:   Optional: The GitHub repository owner, required when pull requests or issues are specified as numbers.
:   Falls back to `bundle.owner` in `changelog.yml` when not specified.

`--prs <string[]?>`
:   Filter by pull request URLs (comma-separated), or a path to a newline-delimited file. Can be specified multiple times.
:   Only one filter option can be specified: `--all`, `--input-products`, `--prs`, `--issues`, or `--report`.
:   When specifying inline values, comma-separated PR numbers are allowed when `--owner` and `--repo` are also provided.
:   When using a file, every line must be a fully-qualified GitHub PR URL such as `https://github.com/owner/repo/pull/123`. Bare numbers and short forms are not allowed in files.

`--report <string?>`
:   Filter by pull requests extracted from a promotion report. Accepts a URL or a local file path.
:   Only one filter option can be specified: `--all`, `--input-products`, `--prs`, `--issues`, or `--report`.
:   The report can be an HTML page from Buildkite or any file containing GitHub PR URLs.

`--repo <string?>`
:   Optional: The GitHub repository name, required when pull requests or issues are specified as numbers.
:   Also sets the `repo` field in each bundle product entry for correct PR/issue link generation.
:   Falls back to `bundle.repo` in `changelog.yml` when not specified; if that is also absent, the product ID is used.

`--resolve`
:   Optional: Copy the contents of each changelog file into the entries array.
:   By default, the bundle contains only the file names and checksums.

## Output files

Both modes use the same ordered fallback to determine where to write the bundle. The first value that is set wins:

**Output directory** (where the bundle file is placed):

| Priority | Profile-based | Option-based |
|----------|---------------|--------------|
| 1 | — | `--output` (explicit file or directory path) |
| 2 | `bundle.output_directory` in `changelog.yml` | `bundle.output_directory` in `changelog.yml` |
| 3 | `bundle.directory` in `changelog.yml` | `--directory` CLI option |
| 4 | Current working directory | `bundle.directory` in `changelog.yml` |
| 5 | — | Current working directory |

**Input directory** (where changelog YAML files are read from) follows the same fallback for both modes, minus the explicit CLI override that is forbidden in profile mode:

| Priority | Profile-based | Option-based |
|----------|---------------|--------------|
| 1 | `bundle.directory` in `changelog.yml` | `--directory` CLI option |
| 2 | Current working directory | `bundle.directory` in `changelog.yml` |
| 3 | — | Current working directory |

**Bundle filename** is determined by the `bundle.profiles.<name>.output` setting (profile-based) or defaults to `changelog-bundle.yaml` (both modes).
The profile `output` setting can include additional path segments. For example: `"stack/kibana-{version}.yaml"`.

In option-based mode, when you specify `--output`, it supports two formats:

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

:::{note}
"Current working directory" means the directory you are in when you run the command (`pwd`).
Setting `bundle.directory` and `bundle.output_directory` in `changelog.yml` is recommended so you don't need to rely on running the command from a specific directory.
:::

## Repository name in bundles [changelog-bundle-repo]

The repository name is stored in each bundle product entry to ensure that PR and issue links are generated correctly when the bundle is rendered.
It can be set in three ways, in order of precedence:

1. **`--repo` option** (option-based mode only)
2. **`repo` field in the profile** (profile-based mode only; overrides the bundle-level default)
3. **`bundle.repo` in `changelog.yml`** (applies to both modes as a default when neither of the above is set)

Setting `bundle.repo` and `bundle.owner` in your configuration means you rarely need to pass `--repo` and `--owner` on the command line:

```yaml
bundle:
  repo: cloud
  owner: elastic
```

You can still override them per profile if a project has multiple products with different repos:

```yaml
bundle:
  repo: cloud        # default for all profiles
  owner: elastic
  profiles:
    elasticsearch-release:
      products: "elasticsearch {version} {lifecycle}"
      output: "elasticsearch-{version}.yaml"
      repo: elasticsearch  # overrides bundle.repo for this profile only
    serverless-release:
      products: "cloud-serverless {version} *"
      output: "serverless-{version}.yaml"
      # inherits repo: cloud from bundle level
```

The bundle output includes a `repo` field in each product:

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

When rendering, pull request and issue links use `https://github.com/elastic/cloud/...` instead of the product ID.

:::{note}
If no `repo` is set at any level, the product ID is used as a fallback for link generation.
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
:   Optional. The GitHub repository name written to each product entry in the bundle. Used by the `{changelog}` directive to generate correct PR/issue links. Only needed when the product ID doesn't match the GitHub repository name. Overrides `bundle.repo` when set.
:   Example: `repo: cloud` (for the `cloud-serverless` product)

`owner`
:   Optional. The GitHub owner written to each product entry in the bundle. Overrides `bundle.owner` when set.
:   Example: `owner: elastic`

`hide_features`
:   Optional. Feature IDs to mark as hidden in the bundle output (string or list). When the bundle is rendered, entries with matching `feature-id` values are commented out.

## Examples

The following changelog configuration example contains multiple profiles for filtering the bundles:

```yaml
bundle:
  repo: cloud <1>
  owner: elastic
  profiles:
    # Find changelogs with a specific lifecycle
    elasticsearch-ga-only:
      products: "elasticsearch {version} ga" <2>
      output: "elasticsearch-{version}.yaml"
      repo: elasticsearch <3>
    
    # Find changelogs with any lifecycle and a partial date
    serverless-monthly:
      products: "cloud-serverless {version}-* *" <4>
      output: "serverless-{version}.yaml"
      output_products: "cloud-serverless {version}"
      # repo and owner inherited from bundle level
    
    # Infer the lifecycle from the version
    elasticsearch-release:
      hide_features: <5>
        - feature-flag-1
        - feature-flag-2
      products: "elasticsearch {version} {lifecycle}" <6>
      output: "elasticsearch-{version}.yaml"
      output_products: "elasticsearch {version}"
      repo: elasticsearch <3>
```

1. Bundle-level defaults that apply to all profiles. Individual profiles can override these.
2. Bundles any changelogs that have `product: elasticsearch`, `lifecycle: ga`, and the version specified in the command. This is equivalent to the `--input-products` command option.
3. Overrides the bundle-level `repo: cloud` for this profile because the `elasticsearch` product matches its GitHub repository name.
4. Bundles any changelogs that have `product: cloud-serverless`, any lifecycle, and the date partially specified in the command. This is equivalent to the `--input-products` command option's support for wildcards.
5. Adds a `hide-features` array in the bundle. This is equivalent to the `--hide-features` command option.
6. In this case, the lifecycle is inferred from the version.

For example, when the version is:

- `9.2.0` or `9.2.0-rc.1` the inferred lifecycle that is used in the filter is `ga`.
- `9.2.0-beta.1` the inferred lifecycle is `beta`.
- `9.2.0-alpha.1` or `9.2.0-preview.1` the inferred lifecycle is `preview`.

For more information about acceptable product and lifecycle values, go to [Product format](/contribute/changelog.md#product-format).

You can invoke those profiles with commands like this:

```sh
# Bundle changelogs that match a specific version or date
docs-builder changelog bundle elasticsearch-release 9.2.0

# Bundle changelogs with partial dates
docs-builder changelog bundle serverless-monthly 2026-02

# Bundle changelogs that match a list of PRs in a downloaded promotion report
# (version used for {version} substitution; report used as PR filter)
docs-builder changelog bundle serverless-monthly 2026-02 ./promotion-report.html

# Same using a URL list file instead of an HTML promotion report
docs-builder changelog bundle serverless-monthly 2026-02 ./prs.txt
```

For option-based mode, use `--report` to filter by a promotion report:

```sh
# Extract PRs from a downloaded report and use them as the filter
docs-builder changelog bundle \
  --report ./promotion-report.html \
  --directory ./docs/changelog \
  --output ./docs/releases/bundle.yaml
```
