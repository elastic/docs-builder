# changelog bundle

Bundle changelog files.

To create the changelogs, use [](/cli/changelog/add.md).
For details and examples, go to [](/contribute/changelog.md).

## Usage

```sh
docs-builder  changelog bundle [arguments...] [options...] [-h|--help]
```

`changelog bundle` supports two mutually exclusive invocation modes:

- **Profile-based**: All paths and filters come from the changelog configuration file. No other options are allowed. For example, `bundle <profile> <version> <report>`.
- **Option-based**: You supply all filter and output options directly. For example, `bundle --all` (or `--input-products`, `--prs`, `--issues`).

You cannot mix the two modes. Passing any option-based flag together with a profile returns an error.

Profile-based commands discover the changelog configuration automatically (no `--config` flag): they look for `changelog.yml` in the current directory, then `docs/changelog.yml`.
If neither file is found, the command returns an error with instructions to run `docs-builder changelog init` or to re-run from the folder where the file exists.

Option-based commands ignore the `bundle.profiles` section of the changelog configuration file.

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
- **Promotion report file** — A path to a downloaded `.html` file containing a promotion report.
- **URL list file** — A path to a plain-text file containing one fully-qualified GitHub PR or issue URL per line. For example, `https://github.com/elastic/elasticsearch/pull/123`. The file must contain only PR URLs or only issue URLs, not a mix. Bare numbers and short forms such as `owner/repo#123` are not allowed.

## General options

These options work with both profile-based and option-based modes.

`--plan`
:   Output a structured set of CI step outputs (`needs_network`, `needs_github_token`, `output_path`) describing Docker flags, network requirements, and the resolved output path, then exit without generating the bundle. Intended for CI actions that need to determine container configuration before running the actual bundle step. When running outside GitHub Actions, the output is written to stdout.

## Options

The following options are only valid in option-based mode (no profile argument).
Using any of them with a profile returns an error.
You must choose one method for determining what's in the bundle (`--all`, `--input-products`, `--prs`, `--issues`, `--release-version`, or `--report`).

`--all`
:   Include all changelogs from the directory.

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
:   When specified, all three parts (product, target, lifecycle) are required but can be wildcards (`*`). Multiple comma-separated values are combined with OR: a changelog is included if it matches any of the specified product/target/lifecycle combinations. For example:

- `"cloud-serverless 2025-12-02 ga, cloud-serverless 2025-12-06 beta"` — include changelogs for either cloud-serverless 2025-12-02 ga or cloud-serverless 2025-12-06 beta
- `"cloud-serverless 2025-12-02 *"` - match cloud-serverless 2025-12-02 with any lifecycle
- `"elasticsearch * *"` - match all elasticsearch changelogs
- `"* 9.3.* *"` - match any product with target starting with "9.3."
- `"* * *"` - match all changelogs (equivalent to `--all`)

:::{note}
The `--input-products` option determines which changelog files are gathered for consideration. **`rules.bundle` is not disabled** when you use `--input-products` — global `include_products` / `exclude_products`, type/area rules, and (when configured) per-product rules still run **after** matching, unless your configuration is in [no-filtering mode](/contribute/changelog.md#bundle-rule-modes). The only “mutually exclusive” pairing on this command is **profile-based** bundling versus **option-based** flags (see [Usage](#usage)), not `--input-products` versus `rules.bundle`.
:::

`--issues <string[]?>`
:   Filter by issue URLs (comma-separated), or a path to a newline-delimited file. Can be specified multiple times.
:   Each occurrence can be either comma-separated issues ( `--issues "https://github.com/owner/repo/issues/123,456"`) or a file path (for example `--issues /path/to/file.txt`).
:   When using a file, every line must be a fully-qualified GitHub issue URL such as `https://github.com/owner/repo/issues/123`. Bare numbers and short forms are not allowed in files.

`--no-sanitize-private-links`
:   Optional: Explicitly turn off the `sanitize_private_links` option if it's specified in the changelog configuration file.

`--no-resolve`
:   Optional: Explicitly turn off the `resolve` option if it's specified in the changelog configuration file.

`--output <string?>`
:   Optional: The output path for the bundle.
:   Can be either (1) a directory path, in which case `changelog-bundle.yaml` is created in that directory, or (2) a file path ending in `.yml` or `.yaml`.
:   When not specified, falls back to `bundle.output_directory` from the changelog configuration, then the input directory (which is itself resolved from `--directory`, `bundle.directory`, or the current working directory). See [Output files](#output-files) for the full resolution order.

`--output-products <List<ProductInfo>?>`
:   Optional: Explicitly set the products array in the output file in format "product target lifecycle, ...".
:   This value replaces information that would otherwise be derived from changelogs.
:   When `rules.bundle.products` per-product overrides are configured, `--output-products` also supplies the product IDs used to choose the **rule context product** (first alphabetically) for Mode 3. To use a different product's rules, run a separate bundle with only that product in `--output-products`. For details, refer to [Single-product rule resolution algorithm](/contribute/changelog.md#changelog-bundle-rule-resolution).

`--owner <string?>`
:   Optional: The GitHub repository owner, required when pull requests or issues are specified as numbers.
:   Precedence: `--owner` flag > `bundle.owner` in `changelog.yml` > `elastic`.

`--prs <string[]?>`
:   Filter by pull request URLs (comma-separated) or a path to a newline-delimited file. Can be specified multiple times.
:   Each occurrence can be either comma-separated PRs (for example `--prs "https://github.com/owner/repo/pull/123,6789"`) or a file path (for example `--prs /path/to/file.txt`).
:   When using a file, every line must be a fully-qualified GitHub PR URL such as `https://github.com/owner/repo/pull/123`. Bare numbers and short forms are not allowed in files.

`--release-version <string?>`
:   GitHub release tag to use as a source of pull requests (for example, `"v9.2.0"` or `"latest"`).
:   When specified, the command fetches the release from GitHub, parses PR references from the release notes, and uses them as the bundle filter. Only automated GitHub release notes (the default format or [Release Drafter](https://github.com/release-drafter/release-drafter) format) are supported at this time.
:   Requires repo (`--repo` or `bundle.repo` in `changelog.yml`) and owner (`--owner` flag > `bundle.owner` in `changelog.yml` > `elastic`) details.
:   When `--output-products` is not specified, the products array in the bundle is derived from the matched changelog files' own `products` fields, consistent with all other filter options.

`--repo <string?>`
:   Optional: The GitHub repository name.
:   Falls back to `bundle.repo` in `changelog.yml` when not specified; if that is also absent, the product ID is used.

`--report <string?>`
:   Filter by pull requests extracted from a promotion report. Accepts a URL or a local file path.
:   The report can be an HTML page from Buildkite or any file containing GitHub PR URLs.

`--resolve`
:   Optional: Copy the contents of each changelog file into the entries array.
:   By default, the bundle contains only the file names and checksums.

`--sanitize-private-links`
:   Optional: Turn on [private link sanitization](#private-link-sanitization).
:   Pull requests and issues that target repositories marked `private: true` in the `references` section of `assembler.yml` are rewritten as quoted `# PRIVATE:` sentinel strings in the bundle file.
:   This option requires a resolved bundle: use `--resolve` or set `bundle.resolve: true` in the `changelog.yml`.
:   If sanitization is enabled and the bundle is not resolved, the command fails.
:   When you omit this option, it defaults to `bundle.sanitize_private_links` in your changelog configuration file, which defaults to `false`.

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
  repo: elasticsearch
  owner: elastic
```

You can still override them per profile if a project has multiple products with different repos.

The bundle output includes a `repo` field in each product:

```yaml
products:
- product: cloud-serverless
  target: 2025-12-02
  repo: elasticsearch
  owner: elastic
entries:
- file:
    name: 1765495972-new-feature.yaml
    checksum: 6c3243f56279b1797b5dfff6c02ebf90b9658464
```

When rendering, pull request and issue links use `https://github.com/elastic/elasticsearch/...` instead of the product ID.

:::{note}
If no `repo` is set at any level, the product ID is used as a fallback for link generation.
This may result in broken links if the product ID doesn't match the GitHub repository name (for example, `cloud-serverless` product ID in the `elasticsearch` repo).
:::

## Rules for filtered bundles [changelog-bundle-rules]

The `rules.bundle` section in the changelog configuration file lets you filter entries during bundling. It applies to both `changelog bundle` and `changelog gh-release`, after entries are matched by the primary filter (`--prs`, `--issues`, `--all`, **`--input-products`**, and so on) and before the bundle is written.

Which `rules.bundle` fields take effect depends on the [bundle rule modes](/contribute/changelog.md#bundle-rule-modes) (no filtering, global rules against each changelog’s content, or per-product rule context). Input stage (gathering entries) and bundle filtering stage (filtering for output) are conceptually separate.

The following fields are supported:

`exclude_products`
:   A product ID or list of product IDs to exclude from the bundle. Cannot be combined with `include_products`.

`include_products`
:   A product ID or list of product IDs to include in the bundle (all others are excluded). Cannot be combined with `exclude_products`.

`match_products`
:   Match mode for the product filter (`any`, `all`, or `conjunction`). Inherits from `rules.match` when not specified.

`exclude_types`
:   A changelog type or list of types to exclude from the bundle.

`include_types`
:   Only changelogs with these types are kept; all others are excluded.

`exclude_areas`
:   A changelog area or list of areas to exclude from the bundle.

`include_areas`
:   Only changelogs with these areas are kept; all others are excluded.

`match_areas`
:   Match mode for the area filter (`any`, `all`, or `conjunction`). Inherits from `rules.match` when not specified.

`products`
:   Per-product filter overrides for **all filter types** (product, type, area). Keys are product IDs (or comma-separated lists). When this map is **non-empty**, the bundler uses **per-product rule context** mode: global `rules.bundle` product/type/area fields are **not** used for filtering (repeat constraints under each product key if you still need them). For details, refer to [Bundle rule modes](/contribute/changelog.md#bundle-rule-modes) and [Single-product rule resolution (Mode 3 only)](/contribute/changelog.md#changelog-bundle-rule-resolution).

```yaml
rules:
  bundle:
    exclude_products: cloud-enterprise
    exclude_types: deprecation
    exclude_areas:
      - Internal
    products:
      cloud-serverless:
        include_areas:
          - "Search"
          - "Monitoring"
```

## Private link sanitization [private-link-sanitization]

A changelog in a public repository might contain links to pull requests or issues in private repositories.
To prevent that information from appearing in the documentation, use `bundle.sanitize_private_links` in the changelog configuration file (or a product-specific profile override) or the `--sanitize-private-links` command option.

This feature relies on the [`assembler.yml`](/configure/site/content.md) file and the existence of `private: true` to determine which repo links should be sanitized.
Every repository that appears in a PR or issue link must be listed under `assembler.yml` `references`. References to unknown repositories fail the command so you can fix the registry.
Repos are assumed to be `private: false` unless you specify otherwise.

:::{important}
When you use these options, you must also set `bundle.resolve: true` or specify `--resolve`.
Unresolved bundles that only store `file:` pointers do not get this rewrite; if you need private link sanitization, you must use a resolved bundle.
:::

The `changelog bundle`, `changelog gh-release`, and `changelog bundle-amend` commands rewrite PR and issue references that **target** private repositories into quoted sentinel strings such as `"# PRIVATE: …"` in the bundle file.
The changelog directive and `changelog render` command then omit these sentinels from the documentation.

:::{warning}
Sentinel values are omitted from rendered documentation but remain in bundle files; they are not cryptographic redaction.
:::

## Option-based examples

### Bundle by report or URL list

You can use `--report` to filter by a promotion report:

```sh
# Extract PRs from a downloaded report and use them as the filter
docs-builder changelog bundle \
  --report ./promotion-report.html \
  --directory ./docs/changelog \
  --output ./docs/releases/bundle.yaml
```

By default all changelogs that match PRs in the promotion report are included in the bundle.
To apply additional filtering by the changelog type, areas, or products, add `rules.bundle` [filters](#changelog-bundle-rules).

### Bundle by GitHub release [changelog-bundle-release-version]

You can use `--release-version` to fetch pull request references directly from GitHub release notes and use them as the bundle filter.
This is equivalent to building a PR list file manually and passing it with `--prs`, but without any file management.

:::{important}
Only automated GitHub release notes (the default format or [Release Drafter](https://github.com/release-drafter/release-drafter) format) are supported at this time.
:::

```sh
docs-builder changelog bundle \
  --release-version v1.34.0 \
  --repo apm-agent-dotnet \ <1>
  --owner elastic <2>
```

1. You must specify `--repo` or set `bundle.repo` in the changelog configuration file.
2. If you don't specify `--owner`, it uses `bundle.owner` in the changelog configuration or else defaults to `elastic`.

Without `--output-products`, the products array in the bundle is derived from the matched changelog files' own `products` fields — the same behavior as `--prs`, `--issues`, `--report`, and `--all`.
Use `--output-products` when you need a single, authoritative product entry that reflects the release identity rather than the diverse metadata across individual changelog files.
For example:

```sh
docs-builder changelog bundle \
  --release-version v1.34.0 \
  --output-products "apm-agent-dotnet 1.34.0 ga"
```

:::{note}
`--release-version` requires a `GITHUB_TOKEN` or `GH_TOKEN` environment variable (or an active `gh` login) to fetch release details from the GitHub API.
:::

By default all changelogs that match PRs in the GitHub release notes are included in the bundle.
To apply additional filtering by the changelog type, areas, or products, add `rules.bundle` [filters](#changelog-bundle-rules).

## Profile-based examples

When the changelog configuration file defines `bundle.profiles`, you can use those profiles with the `changelog bundle` command.

### Profile configuration fields [changelog-bundle-profile-config]

If you're using profile-based commands, they're affected by the following fields in the `bundle.profiles` section of the changelog configuration file:

`source`
:   Optional. When set to `github_release`, the PR list is fetched automatically from the GitHub release identified by the version argument. Requires `repo` to be set at the profile or `bundle` level. Mutually exclusive with `products`.
:   Example: `source: github_release`

`products`
:   Required when filtering by product metadata (equivalent to the `--input-products` command option).
:   The value `"* * *"` is equivalent to the `--all` command option.
:   Not used when the filter comes from a promotion report, URL list file, or `source: github_release` — in those cases the PR or issue list determines what's included and `products` is ignored.
:   Supports `{version}` and `{lifecycle}` placeholders that are substituted at runtime.
:   Example: `"elasticsearch {version} {lifecycle}"`

:::{note}
The `products` field determines which changelog files are gathered for consideration. **`rules.bundle` still applies** afterward (see the note under [`--input-products`](#options)). Input stage and bundle filtering stage are conceptually separate.
:::

`output`
:   Optional. The output filename pattern for the bundle file. Supports `{version}` and `{lifecycle}` placeholders.
:   When not set, the output path falls back in order to: `bundle.output_directory/changelog-bundle.yaml` (if `bundle.output_directory` is configured), then `changelog-bundle.yaml` in the input directory.
:   Setting this is recommended so each profile produces a distinctly named file rather than overwriting the default.
:   Example: `"elasticsearch-{version}.yaml"`

`output_products`
:   Optional. Overrides the products array written to the bundle output. Supports `{version}` and `{lifecycle}` placeholders.
:   When **not set**, the products array is derived from the individual changelog files matched by the filter. This often produces multiple product entries (one per unique product/target/lifecycle combination across all matched files), which may not reflect a single clean release identity.
:   When **set**, the products array in the bundle is exactly the value you specify, replacing anything that would be derived from the matched changelogs. Use this to publish a single, authoritative product entry with a specific version and lifecycle.
:   The `{lifecycle}` placeholder is substituted at runtime with the inferred lifecycle. For `source: github_release` profiles this comes from the release tag suffix. For standard profiles it comes from the version argument. Refer to [](#changelog-bundle-standard-profile-lifecycle) and [](#changelog-bundle-github-release-profile) for details.
:   If you omit lifecycle from the pattern (for example, `"elasticsearch {version}"`), the lifecycle field is omitted from the products array entirely.
:   Example: `"elasticsearch {version} {lifecycle}"` or `"elasticsearch {version} ga"` to hardcode GA regardless of tag.

`repo`
:   Optional. The GitHub repository name written to each product entry in the bundle. Used by the `{changelog}` directive to generate correct PR/issue links. Only needed when the product ID doesn't match the GitHub repository name. Overrides `bundle.repo` when set. Required when `source: github_release` is used and no `bundle.repo` default is set.
:   Example: `repo: elasticsearch`.

`owner`
:   Optional. The GitHub owner written to each product entry in the bundle. Overrides `bundle.owner` when set.
:   Example: `owner: elastic`

`hide_features`
:   Optional. Feature IDs to mark as hidden in the bundle output (string or list). When the bundle is rendered, entries with matching `feature-id` values are commented out.

### Lifecycle inference for standard profiles [changelog-bundle-standard-profile-lifecycle]

If your configuration file defines a standard profile (that is to say, not a GitHub release profile), the lifecycle is inferred from the version string you pass as the second argument:

| Version argument | Inferred lifecycle |
|------------------|--------------------|
| `9.2.0` | `ga` |
| `9.2.0-rc.1` | `ga` |
| `9.2.0-beta.1` | `beta` |
| `9.2.0-alpha.1` | `preview` |
| `9.2.0-preview.1` | `preview` |

For more information about acceptable product and lifecycle values, go to [Product format](/contribute/changelog.md#product-format).

You can invoke those profiles with commands like this:

```sh
# Bundle changelogs for a GA release ({lifecycle} → "ga" inferred from "9.2.0")
docs-builder changelog bundle elasticsearch-release 9.2.0

# Bundle changelogs for a beta release ({lifecycle} → "beta" inferred from "9.2.0-beta.1")
docs-builder changelog bundle elasticsearch-release 9.2.0-beta.1

# Bundle changelogs with partial dates
docs-builder changelog bundle serverless-monthly 2026-02

# Bundle changelogs that match a list of PRs in a downloaded promotion report
# (version used for {version} substitution; report used as PR filter)
docs-builder changelog bundle serverless-report 2026-02-13 ./promotion-report.html

# Same using a URL list file instead of an HTML promotion report
docs-builder changelog bundle serverless-report 2026-02-13 ./prs.txt

# Bundle changelogs using the PR list from a GitHub release (source: github_release)
docs-builder changelog bundle elasticsearch-gh-release 9.2.0

# Use "latest" to fetch the most recent release
docs-builder changelog bundle elasticsearch-gh-release latest
```

### Bundle by product

You can create profiles that are equivalent to the `--input-products` filter option, that is to say the bundle will contain only changelogs with matching `products`.
For example:

```yaml
bundle:
  # Input directory containing changelog YAML files
  directory: docs/changelog
  # Output directory for bundles
  output_directory: docs/releases
  # Whether to resolve (copy contents) by default
  resolve: true
  repo: elasticsearch <1>
  owner: elastic
  profiles:
    # Collect all changelogs
    release-all:
      products: "* * *" <2>
      output: "all.yaml"
    # Find changelogs with any lifecycle and a partial date
    serverless-monthly:
      products: "cloud-serverless {version}-* *" <3>
      output: "serverless-{version}.yaml"
      output_products: "cloud-serverless {version}"

    # Find changelogs with a specific lifecycle
    elasticsearch-ga-only:
      products: "elasticsearch {version} ga" <4>
      output: "elasticsearch-{version}.yaml"

    # Infer the lifecycle from the version
    elasticsearch-release:
      hide_features: <5>
        - feature-flag-1
        - feature-flag-2
      products: "elasticsearch {version} {lifecycle}" <6>
      output: "elasticsearch-{version}.yaml"
      output_products: "elasticsearch {version}"
```

1. Bundle-level defaults that apply to all profiles. Individual profiles can override these.
2. Collects all changelogs from the `directory`. This is equivalent to the `--all` command.
3. Collects any changelogs that have `product: cloud-serverless`, any lifecycle, and the date partially specified in the command. This is equivalent to the `--input-products` command option's support for wildcards.
4. Collects any changelogs that have `product: elasticsearch`, `lifecycle: ga`, and the version specified in the command.
5. Adds a `hide-features` array in the bundle. This is equivalent to the `--hide-features` command option.
6. In this case, the lifecycle is inferred from the version string passed as the second command argument (for example, `9.2.0-beta.1` → `beta`).

`output_products: "elasticsearch {version} {lifecycle}"` produces a single, authoritative product entry in the bundle derived from the release tag — for example, tag `v9.2.0` gives `elasticsearch 9.2.0 ga` and tag `v9.2.0-beta.1` gives `elasticsearch 9.2.0 beta`. Without `output_products`, the bundle products array is instead derived from the matched changelog files' own `products` fields, which is the consistent fallback for all profile types. Set `output_products` when you need a single clean product entry that reflects the release identity rather than the diverse metadata across individual changelog files.

:::{note}
The `products` field determines which changelog files are gathered for consideration. **`rules.bundle` still applies** afterward (see the note under [`--input-products`](#options)). Input stage and bundle filtering stage are conceptually separate.
:::

For profiles that use static patterns (without `{version}` or `{lifecycle}` placeholders), the second argument is still required but serves no functional purpose. You can pass any placeholder value. For example:

```sh
# Profile with static patterns - second argument unused but required
docs-builder changelog bundle release-all '*'
docs-builder changelog bundle release-all 'unused'
docs-builder changelog bundle release-all 'none'
```

If you are using the `{version}` placeholder in the `output_products` or `output` fields, you must provide an appropriate value even though it's not used by the `products` filter.

### Bundle by report or URL list [profile-bundle-report-examples]

You can also create profiles that are equivalent to the `--prs`, `--issues`, and `--report` filter options.
That is to say you can create bundles that contain only changelogs with matching `prs` or `issues`.
For example:

```yaml
bundle:
  repo: elasticsearch <1>
  owner: elastic
  profiles:
    # Find changelogs that match a list of PRs
    serverless-report: <2>
      output: "serverless-{version}.yaml"
      output_products: "cloud-serverless {version}"
```

1. Bundle-level defaults that apply to all profiles. Individual profiles can override these.
2. If a profile is intended for use with a promotion report or a newline delimited file that lists the issues or pull requests, it does not need a `products` filter. If the `output` and `output_products` are omitted, the default path and file names are used. This example shows how you can use a `{version}` variable to customize the bundle's filename and product metadata.

By default all changelogs that match PRs or issues in the list or report are included in the bundle.
To apply additional filtering by the changelog type, areas, or products, add `rules.bundle` [filters](#changelog-bundle-rules).

### Bundle by GitHub release profiles [changelog-bundle-github-release-profile]

To make bundling by GitHub release more easily repeatable, create a profile with `source: github_release` in your changelog configuration file.
For example:

```yaml
bundle:
  profiles:
    # Fetch the PR list directly from a GitHub release
    agent-gh-release:
      source: github_release <1>
      repo: apm-agent-dotnet   <2>
      output: "agent-{version}.yaml"
      output_products: "apm-agent-dotnet {version} {lifecycle}"
```

1. Instead of filtering pre-existing changelog files by product, this profile fetches the PR list from the GitHub release notes for the given version. Mutually exclusive with `products`.
2. The repository to fetch the release from. Overrides `bundle.repo` for this profile.

For `source: github_release` profiles, the `{lifecycle}` placeholder in `output` and `output_products` is inferred from the **release tag** returned by GitHub (not the argument you pass to the command).
This means the pre-release suffix on the tag drives the lifecycle value:

| Release tag | `{version}` | `{lifecycle}` |
|-------------|-------------|---------------|
| `v9.2.0` | `9.2.0` | `ga` |
| `v9.2.0-beta.1` | `9.2.0` | `beta` |
| `v9.2.0-preview.1` | `9.2.0` | `preview` |
| `v1.34.1` | `1.34.1` | `ga` |
| `v1.34.1-preview.1` | `1.34.1` | `preview` |

This differs from standard profiles, where lifecycle is inferred from the version argument you type. For `source: github_release`, the `{version}` placeholder always uses the clean base version (stripped of any pre-release suffix), while `{lifecycle}` reflects the actual tag format.

If the lifecycle you want to advertise cannot be inferred from the tag format — for example, because your team uses clean tags like `v1.34.1` even for pre-releases — hardcode the lifecycle directly in `output_products` instead of using the `{lifecycle}` placeholder:

```yaml
# Instead of relying on {lifecycle} inference, hardcode the lifecycle
gh-release:
  source: github_release
  repo: apm-agent-dotnet
  output: "apm-agent-dotnet-{version}.yaml"
  output_products: "apm-agent-dotnet {version} preview"
```

By default all changelogs that match PRs in the GitHub release notes are included in the bundle.
To apply additional filtering by the changelog type, areas, or products, add `rules.bundle` [filters](#changelog-bundle-rules).