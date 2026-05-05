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

`--description <string?>`
:   Optional: Bundle description text with placeholder support.
:   Supports `{version}`, `{lifecycle}`, `{owner}`, and `{repo}` placeholders. Overrides `bundle.description` from config.
:   When using `{version}` or `{lifecycle}` placeholders, predictable substitution values are required:
:   - **Option-based mode**: Requires `--output-products` to be explicitly specified
:   - **Profile-based mode**: Requires either a version argument OR `output_products` in the profile configuration

`--hide-features <string[]?>`
:   Optional: A list of feature IDs (comma-separated) or a path to a newline-delimited file containing feature IDs.
:   Can be specified multiple times.
:   Adds a `hide-features` list to the bundle.
:   When the bundle is rendered (by the `changelog render` command or `{changelog}` directive), changelogs with matching `feature-id` values will be commented out of the documentation.

:::{note}
The `--hide-features` option on the `render` command and the `hide-features` field in bundles are **combined**. If you specify `--hide-features` on both the `bundle` and `render` commands, all specified features are hidden. The `{changelog}` directive automatically reads `hide-features` from all loaded bundles and applies them.
:::

`--input-products <List<ProductInfo>?>`
:   Filter by products in the format "product target lifecycle, ...".
:   For more information about the valid product and lifecycle values, go to [Product format](#product-format).
:   When specified, all three parts (product, target, lifecycle) are required but can be wildcards (`*`). Multiple comma-separated values are combined with OR: a changelog is included if it matches any of the specified product/target/lifecycle combinations. For example:

- `"cloud-serverless 2025-12-02 ga, cloud-serverless 2025-12-06 beta"` — include changelogs for either cloud-serverless 2025-12-02 ga or cloud-serverless 2025-12-06 beta
- `"cloud-serverless 2025-12-02 *"` - match cloud-serverless 2025-12-02 with any lifecycle
- `"elasticsearch * *"` - match all elasticsearch changelogs
- `"* 9.3.* *"` - match any product with target starting with "9.3."
- `"* * *"` - match all changelogs (equivalent to `--all`)

:::{note}
The `--input-products` option determines which changelog files are gathered for consideration.
Bundle rules are not turned off when you use `--input-products`-- they run **after** matching, unless your configuration is in no-filtering mode per [Bundle rules](/contribute/configure-changelogs-ref.md#rules-bundle).
:::

`--issues <string[]?>`
:   Include changelogs for the specified issue URLs (comma-separated), or a path to a newline-delimited file. Can be specified multiple times.
:   Each occurrence can be either comma-separated issues ( `--issues "https://github.com/owner/repo/issues/123,456"`) or a file path (for example `--issues /path/to/file.txt`).
:   When using a file, every line must be a fully-qualified GitHub issue URL such as `https://github.com/owner/repo/issues/123`. Bare numbers and short forms are not allowed in files.

`--no-resolve`
:   Optional: Explicitly turn off the `resolve` option if it's specified in the changelog configuration file.

`--output <string?>`
:   Optional: The output path for the bundle.
:   Can be either (1) a directory path, in which case `changelog-bundle.yaml` is created in that directory, or (2) a file path ending in `.yml` or `.yaml`.
:   When not specified, falls back to `bundle.output_directory` from the changelog configuration, then the input directory (which is itself resolved from `--directory`, `bundle.directory`, or the current working directory). See [Output files](#output-files) for the full resolution order.

`--output-products <List<ProductInfo>?>`
:   Optional: Explicitly set the products array in the output file in format "product target lifecycle, ...".
:   This value replaces information that would otherwise be derived from changelogs.
:   For more information about the valid product and lifecycle values, go to [Product format](#product-format).
:   When `rules.bundle.products` per-product overrides are configured, `--output-products` also supplies the product IDs used to determine the **rule context product** (if there are multiple, the first ID alphabetically is used). Refer to [Product-specific bundle rules](/contribute/configure-changelogs-ref.md#rules-bundle-products).

:::{tip}
Though technically optional, it is strongly recommended to set `--output-products` ( or `output_products` for profiles) so that you have a single clean product entry that reflects the context of the release.
:::

`--no-release-date`
:   Optional: Skip auto-population of release date in the bundle.
:   By default, bundles are created with a `release-date` field set to today's date (UTC) or the GitHub release published date when using `--release-version`.
:   Mutually exclusive with `--release-date`.

`--release-date <string?>`
:   Optional: Explicit release date for the bundle in YYYY-MM-DD format.
:   Overrides the default auto-population behavior (today's date or GitHub release published date).
:   Mutually exclusive with `--no-release-date`.

`--owner <string?>`
:   Optional: The GitHub repository owner, required when pull requests or issues are specified as numbers.
:   Precedence: `--owner` flag > `bundle.owner` in `changelog.yml` > `elastic`.

`--prs <string[]?>`
:   Include changelogs for the specified pull request URLs (comma-separated) or a path to a newline-delimited file. Can be specified multiple times.
:   Each occurrence can be either comma-separated PRs (for example `--prs "https://github.com/owner/repo/pull/123,6789"`) or a file path (for example `--prs /path/to/file.txt`).
:   When using a file, every line must be a fully-qualified GitHub PR URL such as `https://github.com/owner/repo/pull/123`. Bare numbers and short forms are not allowed in files.

`--release-version <tag>`
:   Bundle changelogs for the pull requests in GitHub release notes. For example, the tag can be `"v9.2.0"` or `"latest"`.
:   When specified, the command fetches the release from GitHub, parses PR references from the release notes, and uses them as the bundle filter. Only automated GitHub release notes (the default format or [Release Drafter](https://github.com/release-drafter/release-drafter) format) are supported at this time.
:   Requires repo (`--repo` or `bundle.repo` in `changelog.yml`) and owner (`--owner` flag > `bundle.owner` in `changelog.yml` > `elastic`) details.
:   When `--output-products` is not specified, the products array in the bundle is derived from the matched changelog files' own `products` fields, consistent with all other filter options.

`--repo <string?>`
:   Optional: The GitHub repository name.
:   Falls back to `bundle.repo` in `changelog.yml` when not specified; if that is also absent, the product ID is used.

`--report <string?>`
:   Include changelogs based on the pull requests in a promotion report. Accepts a URL or a local file path.
:   The report can be an HTML page from Buildkite or any file containing GitHub PR URLs.

`--resolve`
:   Optional: Copy the contents of each changelog file into the entries array.
:   By default, the bundle contains only the file names and checksums.

## File paths and filenames [output-files]

**Input directory** (where changelog YAML files are read from) follows the same fallback for both modes, minus the explicit CLI override that is forbidden in profile mode:

| Priority | Profile-based | Option-based |
|----------|---------------|--------------|
| 1 | `bundle.directory` in `changelog.yml` | `--directory` CLI option |
| 2 | Current working directory | `bundle.directory` in `changelog.yml` |
| 3 | — | Current working directory |

Both modes use the same ordered fallback to determine where to write the bundle. The first value that is set wins:

**Output directory** (where the bundle file is placed):

| Priority | Profile-based | Option-based |
|----------|---------------|--------------|
| 1 | — | `--output` (explicit file or directory path) |
| 2 | `bundle.output_directory` in `changelog.yml` | `bundle.output_directory` in `changelog.yml` |
| 3 | `bundle.directory` in `changelog.yml` | `--directory` CLI option |
| 4 | Current working directory | `bundle.directory` in `changelog.yml` |
| 5 | — | Current working directory |

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

## Product format

The `changelog bundle` command has `--input-products` and `--output-products` options that accept values with the format `"product target lifecycle, ..."` where:

- `product` is the product ID from [products.yml](https://github.com/elastic/docs-builder/blob/main/config/products.yml) (required)
- `target` is the target version or date (optional)
- `lifecycle` exists in [Lifecycle.cs](https://github.com/elastic/docs-builder/blob/main/src/Elastic.Documentation/Lifecycle.cs) (optional)

You can further limit the possible values with the [products](/contribute/configure-changelogs-ref.md#products) and [lifecycles](/contribute/configure-changelogs-ref.md#lifecycles) options in the changelog configuration file.

For example:

- `"kibana 9.2.0 ga"`
- `"cloud-serverless 2025-08-05"`
- `"cloud-enterprise 4.0.3, cloud-hosted 2025-10-31"`

If you use `"* * *"` in the `--input-products` command option or `bundle.profiles.<name>.products` configuration setting, it's equivalent to the `--all` command option.

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

## PR and issue link allowlist [link-allowlist]

A changelog in a public repository might contain links to pull requests or issues in repositories that should not appear in published documentation.

Set `bundle.link_allow_repos` in `changelog.yml` to an explicit list of `owner/repo` strings (for example, `elastic/elasticsearch`). When this key is present (including as an empty list), PR and issue references are filtered at bundle time: only links whose resolved repository is in the list are kept; others are rewritten to quoted `# PRIVATE:` sentinel strings in the bundle YAML.

:::{important}
`bundle.link_allow_repos` requires a **resolved** bundle. Set `bundle.resolve: true` or pass `--resolve`. Unresolved bundles that only store `file:` pointers are not rewritten.
:::

When [`assembler.yml`](/configure/site/content.md) is available, docs-builder emits **warnings** (non-fatal) if an allowlisted repo is missing from `references` or is marked `private: true`, so you can verify the registry before publishing.

The `changelog bundle`, `changelog gh-release`, and `changelog bundle-amend` commands apply the same rules. The changelog directive and `changelog render` command omit `# PRIVATE:` sentinels from rendered documentation.

:::{warning}
Sentinel values are omitted from rendered documentation but remain in bundle files; they are not cryptographic redaction.
:::

`bundle.repo` must name a **single** GitHub repository (do not use `repo1+repo2` merged-repo syntax).

## Option-based examples

### Bundle by GitHub release [changelog-bundle-release-version]

You can use `--release-version` to fetch pull request references directly from GitHub release notes and use them as the bundle filter.
This is equivalent to building a PR list file manually and passing it with `--prs`, but without any file management.

:::{important}
Only automated GitHub release notes (the default format or [Release Drafter](https://github.com/release-drafter/release-drafter) format) are supported at this time.
:::

```sh
docs-builder changelog bundle \
  --release-version v1.34.0 \ <1>
  --repo apm-agent-dotnet \ <2>
  --owner elastic <3>
  --output-products "apm-agent-dotnet 1.34.0 ga" <4>
```

1. The tag value that is used in the `GET /repos/{owner}/{repo}/releases/tags/{tag}` releases API.
2. You must specify `--repo` or set `bundle.repo` in the changelog configuration file.
3. If you don't specify `--owner`, it uses `bundle.owner` in the changelog configuration or else defaults to `elastic`.
4. The bundle's product metadata is inferred automatically from the release tag and repository name; you can override that behavior with the `--output-products` option.

:::{note}
`--release-version` requires a `GITHUB_TOKEN` or `GH_TOKEN` environment variable (or an active `gh` login) to fetch release details from the GitHub API.
:::

By default all changelogs that match PRs in the GitHub release notes are included in the bundle.
To apply additional filtering by the changelog type, areas, or products, add [rules.bundle](/contribute/configure-changelogs-ref.md#rules-bundle) configuration settings.

:::{tip}
If you are not creating changelogs when you create your pull requests, consider the `docs-builder changelog gh-release` command as a one-shot alternative to the `changelog add` and `changelog bundle` commands.
It parses the release notes, creates one changelog file per pull request found, and creates a `changelog-bundle.yaml` file — all in a single step. Refer to [](/cli/changelog/gh-release.md)
:::

### Bundle by issues [changelog-bundle-issues]

You can use the `--issues` option to create a bundle of changelogs that relate to those GitHub issues.
Issues can be identified by a full URL (such as `https://github.com/owner/repo/issues/123`), a short format (such as `owner/repo#123`), or just a number (in which case `--owner` and `--repo` are required — or set via `bundle.owner` and `bundle.repo` in the configuration).

```sh
docs-builder changelog bundle --issues "12345,12346" \
  --repo elasticsearch \
  --owner elastic \
  --output-products "elasticsearch 9.2.2 ga"
```

Alternatively, you can specify a path to a newline-delimited file that contains the issue URLS (for example, `--issues /path/to/file.txt`).
In this case, you cannot use short URLs or numbers, each line must have a full URL.

By default all changelogs that match issues in the list are included in the bundle.
To apply additional filtering by the changelog type, areas, or products, add [rules.bundle](/contribute/configure-changelogs-ref.md#rules-bundle) configuration settings.


### Bundle by pull requests [changelog-bundle-pr]

You can use the `--prs` option to create a bundle of the changelogs that relate to those pull requests.

Pull requests can be identified by a full URL (such as `https://github.com/owner/repo/pull/123`), a short format (such as `owner/repo#123`), or just a number.

```sh
docs-builder changelog bundle --prs "108875,135873,136886" \ <1>
  --repo elasticsearch \ <2>
  --owner elastic \ <3>
  --output-products "elasticsearch 9.2.2 ga" <4>
```

1. The comma-separated list of pull request numbers to seek.
2. The repository in the pull request URLs. Not required when using full PR URLs, or when `bundle.repo` is set in the changelog configuration.
3. The owner in the pull request URLs. Not required when using full PR URLs, or when `bundle.owner` is set in the changelog configuration.
4. The product metadata for the bundle. If it is not provided, it will be derived from all the changelog product values.

Alternatively, you can specify a path to a newline-delimited file that contains the PR URLS (for example, `--prs /path/to/file.txt`).
In this case, you cannot use short URLs or numbers, each line must have a full URL.
For example:

```txt
https://github.com/elastic/elasticsearch/pull/108875
https://github.com/elastic/elasticsearch/pull/135873
https://github.com/elastic/elasticsearch/pull/136886
https://github.com/elastic/elasticsearch/pull/137126
```

By default all changelogs that match PRs in the list are included in the bundle.
To apply additional filtering by the changelog type, areas, or products, add [rules.bundle](/contribute/configure-changelogs-ref.md#rules-bundle) configuration settings.

If you have changelog files that reference those pull requests, the command creates a file like this:

```yaml
products:
- product: elasticsearch
  target: 9.2.2
  lifecycle: ga
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

### Bundle by product [changelog-bundle-product]

You can use the `--input-products` option to create a bundle of changelogs that match the product details.
When using `--input-products`, you must provide all three parts: product, target, and lifecycle.
Each part can be a wildcard (`*`) to match any value.

:::{tip}
If you use profile-based bundling, provide this information in the `bundle.profiles.<name>.products` field.
:::

```sh
docs-builder changelog bundle \
  --input-products "cloud-serverless 2025-12-02 ga, cloud-serverless 2025-12-06 beta" <1>
```

1. Include all changelogs that have the `cloud-serverless` product identifier with target dates of either December 2 2025 (lifecycle `ga`) or December 6 2025 (lifecycle `beta`). For more information about product values, refer to [Product format](/cli/changelog/bundle.md#product-format).

You can use wildcards in any of the three parts:

```sh
# Bundle any changelogs that have exact matches for either of these clauses
docs-builder changelog bundle --input-products "cloud-serverless 2025-12-02 ga, elasticsearch 9.3.0 beta"

# Bundle all elasticsearch changelogs regardless of target or lifecycle
docs-builder changelog bundle --input-products "elasticsearch * *"

# Bundle all cloud-serverless 2025-12-02 changelogs with any lifecycle
docs-builder changelog bundle --input-products "cloud-serverless 2025-12-02 *"

# Bundle any cloud-serverless changelogs with target starting with "2025-11-" and "ga" lifecycle
docs-builder changelog bundle --input-products "cloud-serverless 2025-11-* ga"

# Bundle all changelogs (equivalent to --all)
docs-builder changelog bundle --input-products "* * *"
```

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

1. By default these values match your `--input-products` (even if the changelogs have more products).
To specify different product metadata, use the `--output-products` option.

:::{note}
When a changelog matches multiple `--input-products` filters, it appears only once in the bundle. This deduplication applies even when using `--all` or `--prs`.
:::

### Bundle by report

You can use `--report` to filter by a promotion report:

```sh
# Extract PRs from a downloaded report and use them as the filter
docs-builder changelog bundle \
  --report ./promotion-report.html \
  --directory ./docs/changelog \
  --output ./docs/releases/bundle.yaml
```

By default all changelogs that match PRs in the promotion report are included in the bundle.
To apply additional filtering by the changelog type, areas, or products, add [rules.bundle](/contribute/configure-changelogs-ref.md#rules-bundle) configuration settings.

### Bundle descriptions

You can add a description to bundles using the `--description` option. For simple descriptions, use regular quotes:

```sh
docs-builder changelog bundle \
  --all \
  --description "This release includes new features and bug fixes."
```

For multiline descriptions with multiple paragraphs, lists, and links, use ANSI-C quoting (`$'...'`) with `\n` for line breaks:

```sh
docs-builder changelog bundle \
  --all \
  --description $'This release includes significant improvements:\n\n- Enhanced performance\n- Bug fixes and stability improvements\n\nFor security updates, go to [security announcements](https://example.com/docs).'
```

When using placeholders in option-based mode, you must explicitly specify `--output-products` for predictable substitution:

```sh
docs-builder changelog bundle \
  --all \
  --output-products "elasticsearch 9.1.0 ga" \
  --description "Elasticsearch {version} includes performance improvements. Download: https://github.com/{owner}/{repo}/releases/tag/v{version}"
```

### Bundle release dates

You can add a `release-date` field directly to a bundle YAML file. This field is optional and purely informative for end-users. It is especially useful for components released outside the usual stack lifecycle, such as APM agents and EDOT agents.

```yaml
products:
  - product: apm-agent-dotnet
    target: 1.34.0
release-date: "April 9, 2026"
description: |
  This release includes tracing improvements and bug fixes.
entries:
  - file:
      name: tracing-improvement.yaml
      checksum: abc123
```

When the bundle is rendered (by the `changelog render` command or `{changelog}` directive), the release date appears immediately after the version heading as italicized text: `_Released: April 9, 2026_`.

## Profile-based examples

When the changelog configuration file defines [bundle.profiles](/contribute/configure-changelogs-ref.md#bundle-profiles), you can use those profiles with the `changelog bundle` command.

Refer to [](/contribute/bundle-changelogs.md#create-profiles) for examples.

### Lifecycle inference [lifecycle-inference]

The way that lifecycle values are inferred varies between [GitHub release profiles](#lifecycles-github) and [standard profiles](#lifecycles-standard).

#### GitHub release profiles [lifecycles-github]

For `source: github_release` profiles, the `{lifecycle}` placeholder in `output` and `output_products` is derived from the full release tag name and `{version}` is the base version extracted from that same tag.
For example:

| Release tag | `{version}` | `{lifecycle}` |
|-------------|-------------|---------------|
| `v1.2.3` | `1.2.3` | `ga` |
| `v1.2.3-beta.1` | `1.2.3` | `beta` |
| `v1.2.3-preview.1` | `1.2.3` | `preview` |

If the lifecycle you want to advertise cannot be inferred from the tag format — for example, because your team uses clean tags like `v1.34.1` even for pre-releases — hardcode the lifecycle directly in `output_products` instead of using the `{lifecycle}` placeholder:

```yaml
# Instead of relying on {lifecycle} inference, hardcode the lifecycle
gh-release:
  source: github_release
  repo: apm-agent-dotnet
  output: "apm-agent-dotnet-{version}.yaml"
  output_products: "apm-agent-dotnet {version} preview"
```

You can invoke the profile with commands like this:

```sh
# Bundle changelogs using the PR list from a GitHub release (source: github_release)
docs-builder changelog bundle gh-release v1.2.3

# Use "latest" to fetch the most recent release
docs-builder changelog bundle gh-release latest
```

#### Standard profiles [lifecycles-standard]

If your configuration file defines a standard profile (that is to say, not a GitHub release profile), the `{version}` is copied verbatim from your command argument and the `{lifecycle}` is derived from that value.
For example:

| Version argument | `{version}` | `{lifecycle}` |
|------------------|-------------|---------------|
| `9.2.0` | `9.2.0` | `ga` |
| `9.2.0-rc.1` | `9.2.0-rc.1` | `ga` |
| `9.2.0-beta.1` | `9.2.0-beta.1` | `beta` |
| `9.2.0-alpha.1` | `9.2.0-alpha.1` | `preview` |
| `9.2.0-preview.1` | `9.2.0-preview.1` | `preview` |

For more information about acceptable product and lifecycle values, go to [Product format](#product-format).

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
```

For profiles that use static patterns (without `{version}` or `{lifecycle}` placeholders), the second argument is still required but serves no functional purpose. You can pass any placeholder value:

```sh
# Profile with static patterns - second argument unused but required
docs-builder changelog bundle release-all '*'
docs-builder changelog bundle release-all 'unused'
docs-builder changelog bundle release-all 'none'
```
