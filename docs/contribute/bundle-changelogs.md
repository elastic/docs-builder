# Bundle changelogs

## Create bundles [changelog-bundle]

You can use the `docs-builder changelog bundle` command to create a YAML file that lists multiple changelogs.
The command has two modes of operation: you can specify all the command options or you can define "profiles" in the changelog configuration file.
The latter is more convenient and consistent for repetitive workflows.
For up-to-date details, use the `-h` option or refer to [](/cli/changelog/bundle.md).

The command supports two mutually exclusive usage modes:

- **Option-based** — you provide filter and output options directly on the command line.
- **Profile-based** — you specify a named profile from your `changelog.yml` configuration file.

You cannot mix these two modes: when you use a profile name, no filter or output options are accepted on the command line.

### Option-based bundling [changelog-bundle-options]

You can specify only one of the following filter options:

- `--all`: Include all changelogs from the directory.
- `--input-products`: Include changelogs for the specified products. Refer to [Filter by product](#changelog-bundle-product).
- `--prs`: Include changelogs for the specified pull request URLs, or a path to a newline-delimited file. When using a file, every line must be a fully-qualified GitHub URL such as `https://github.com/owner/repo/pull/123`. Go to [Filter by pull requests](#changelog-bundle-pr).
- `--issues`: Include changelogs for the specified issue URLs, or a path to a newline-delimited file. When using a file, every line must be a fully-qualified GitHub URL such as `https://github.com/owner/repo/issues/123`. Go to [Filter by issues](#changelog-bundle-issues).
- `--release-version`: Bundle changelogs for the pull requests in GitHub release notes. Refer to [Bundle by GitHub release](#changelog-bundle-release-version).
- `--report`: Include changelogs whose pull requests appear in a promotion report. Accepts a URL or a local file path to an HTML report.

`rules.bundle` in `changelog.yml` is **not** mutually exclusive with these options: it runs as a **second stage** after the primary filter matches entries (for example, `--input-products` gathers changelogs, then global or per-product bundle rules may exclude some). The only mutually exclusive pairing is **profile-based** versus **option-based** invocation. See [bundle rule modes](/contribute/configure-changelogs.md#bundle-rule-modes).

By default, the output file contains only the changelog file names and checksums.
To change this behavior, set `bundle.resolve` to `true` in the changelog configuration file or use the `--resolve` command option.

:::{tip}
If you plan to use [changelog directives](/contribute/publish-changelogs.md#changelog-directive), it is recommended to pull all of the content from each changelog into the bundle; otherwise you can't delete your changelogs.
If you likewise want to regenerate your [Asciidoc or Markdown files](/contribute/publish-changelogs.md#render-changelogs) after deleting your changelogs, it's only possible if you have "resolved" bundles.
:::

<!--
TBD: This feels like TMI in this context. Remove after confirming it's covered in the CLI reference
When you do not specify `--directory`, the command reads changelog files from `bundle.directory` in your changelog configuration if it is set, otherwise from the current directory.
When you do not specify `--output`, the command writes the bundle to `bundle.output_directory` from your changelog configuration (creating `changelog-bundle.yaml` in that directory) if it is set, otherwise to `changelog-bundle.yaml` in the input directory.
When you do not specify `--repo` or `--owner`, the command falls back to `bundle.repo` and `bundle.owner` in the changelog configuration, so you rarely need to pass these on the command line.
-->

### Profile-based bundling [changelog-bundle-profile]

If your `changelog.yml` configuration file defines `bundle.profiles`, you can run a bundle by profile name instead of supplying individual options:

```sh
docs-builder changelog bundle <profile> <version|report|url-list>
```

The second argument accepts a version string, a promotion report URL/path, or a URL list file (a plain-text file with one fully-qualified GitHub URL per line). When your profile uses `{version}` in its `output` or `output_products` pattern and you also want to filter by a report, pass both.
For example:

```sh
# Standard profile: lifecycle is inferred from the version string
docs-builder changelog bundle elasticsearch-release 9.2.0        # {lifecycle} → "ga"
docs-builder changelog bundle elasticsearch-release 9.2.0-beta.1 # {lifecycle} → "beta"

# Standard profile: filter by a promotion report (version used for {version})
docs-builder changelog bundle elasticsearch-release ./promotion-report.html
docs-builder changelog bundle elasticsearch-release 9.2.0 ./promotion-report.html
```

<!--
TBD: This feels like TMI in this context, since it's covered in the CLI reference
The command automatically discovers `changelog.yml` by checking `./changelog.yml` then `./docs/changelog.yml` relative to your current directory.
If no configuration file is found, the command returns an error with advice to create one (using `docs-builder changelog init`) or to run from the directory where the file exists.

You can set `bundle.repo` and `bundle.owner` directly under `bundle:` as defaults that apply to all profiles.
Individual profiles can override them when needed.
-->

Top-level `bundle` fields:

| Field | Description |
|---|---|
| `repo` | Default GitHub repository name applied to all profiles. Falls back to product ID if not set at any level. |
| `owner` | Default GitHub repository owner applied to all profiles. |
| `resolve` | When `true`, embeds full changelog entry content in the bundle (same as `--resolve`). Required when `sanitize_private_links` is enabled. |
| `sanitize_private_links` | When `true`, rewrites PR/issue references that target private repositories (per `assembler.yml` `references`) to quoted `# PRIVATE:` sentinel strings in bundle YAML. Requires `resolve: true` and a non-empty `references` section in `assembler.yml`. Default `false`. Refer to  [Private link sanitization at bundle time](/cli/changelog/bundle.md#private-link-sanitization). |

Profile configuration fields in `bundle.profiles`:

| Field | Description |
|---|---|
| `source` | Optional. Set to `github_release` to fetch the PR list from a GitHub release. Mutually exclusive with `products`. Requires `repo` at the profile or `bundle` level. |
| `products` | Product filter pattern with `{version}` and `{lifecycle}` placeholders. Used to match changelog files. Required when filtering by product metadata. Not used when the filter comes from a promotion report, URL list file, or `source: github_release`. |
| `output` | Output file path pattern with `{version}` and `{lifecycle}` placeholders. |
| `output_products` | Optional override for the products array written to the bundle. Useful when the bundle should have a single product ID though it's filtered from many or have a different lifecycle or version than the filter. With multiple product IDs, Mode 3 rule resolution uses the first alphabetically; use separate profiles or bundle runs with a single product in `output_products` when you need a different rule context. |
| `repo` | Optional. Overrides `bundle.repo` for this profile only. Required when `source: github_release` is used and no `bundle.repo` is set. |
| `owner` | Optional. Overrides `bundle.owner` for this profile only. |
| `hide_features` | List of feature IDs to embed in the bundle as hidden. |
| `sanitize_private_links` | Optional. Overrides `bundle.sanitize_private_links` for this profile. |

Example profile configuration:

```yaml
bundle:
  repo: elasticsearch # The default repository for PR and issue links.
  owner: elastic # The default repository owner for PR and issue links.
  directory: docs/changelog # The directory that contains changelog files.
  output_directory: docs/releases # The directory that contains changelog bundles.
  profiles:
    elasticsearch-release:
      products: "elasticsearch {version} {lifecycle}"
      output: "elasticsearch/{version}.yaml"
      output_products: "elasticsearch {version}"
      hide_features:
        - feature:experimental-api
    serverless-release:
      products: "cloud-serverless {version} *"
      output: "serverless/{version}.yaml"
      output_products: "cloud-serverless {version}"
      # inherits repo: elasticsearch and owner: elastic from bundle level
    # Multi-product profile: rule context for Mode 3 is the first product alphabetically (here: kibana).
    # For security-specific rules only, use a separate profile with output_products listing only security.
    kibana-security-release:
      products: "kibana {version} {lifecycle}, security {version} {lifecycle}"
      output: "kibana-security/{version}.yaml"
      output_products: "kibana {version}, security {version}"
```

#### Bundle changelogs from a GitHub release [changelog-bundle-profile-github-release]

Set `source: github_release` on a profile to make `changelog bundle` fetch the PR list directly from a published GitHub release.

This is equivalent to running `changelog bundle --release-version <version>`, but fully configured in `changelog.yml` so you don't have to remember command-line flags.

```yaml
bundle:
  owner: elastic
  profiles:
    agent-gh-release:
      source: github_release
      repo: apm-agent-dotnet
      output: "my-agents-{version}.yaml"
      output_products: "apm-agent-dotnet {version} {lifecycle}"
```

Invoke the profile with a version tag or `latest`:

```sh
docs-builder changelog bundle agent-gh-release 1.34.0
docs-builder changelog bundle agent-gh-release latest
```

The `{version}` placeholder is substituted with the clean base version extracted from the release tag (for example, `v1.34.0` → `1.34.0`, `v1.34.0-beta.1` → `1.34.0`). The `{lifecycle}` placeholder is inferred from the **release tag** returned by GitHub, not from the argument you pass to the command:

| Release tag | `{version}` | `{lifecycle}` |
|-------------|-------------|---------------|
| `v1.2.3` | `1.2.3` | `ga` |
| `v1.2.3-beta.1` | `1.2.3` | `beta` |
| `v1.2.3-preview.1` | `1.2.3` | `preview` |

This differs from standard profiles, where `{lifecycle}` is inferred from the version string you type at the command line.

`output_products` is optional. When omitted, the bundle products array is derived from the matched changelog files' own `products` fields — the same fallback used by all other profile types. Set `output_products` when you want a single clean product entry that reflects the release identity rather than the diverse metadata across individual changelog files, or to hardcode a lifecycle that cannot be inferred from the tag format:

```yaml
# Produce one authoritative product entry instead of inheriting from changelog files
agent-gh-release:
  source: github_release
  repo: apm-agent-dotnet
  output: "apm-agent-dotnet-{version}.yaml"
  output_products: "apm-agent-dotnet {version} {lifecycle}"

# Or hardcode the lifecycle when the tag format doesn't encode it
agent-gh-release-preview:
  source: github_release
  repo: apm-agent-dotnet
  output: "apm-agent-dotnet-{version}-preview.yaml"
  output_products: "apm-agent-dotnet {version} preview"
```

`source: github_release` is mutually exclusive with `products`, and a third positional argument (promotion report or URL list) is not accepted by this profile type.

### Filter by product [changelog-bundle-product]

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

1. Include all changelogs that have the `cloud-serverless` product identifier with target dates of either December 2 2025 (lifecycle `ga`) or December 6 2025 (lifecycle `beta`). For more information about product values, refer to [Product format](/contribute/create-changelogs.md#product-format).

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

### Filter by pull requests [changelog-bundle-pr]

You can use the `--prs` option to create a bundle of the changelogs that relate to those pull requests.
You can provide either a comma-separated list of PRs (`--prs "https://github.com/owner/repo/pull/123,12345"`) or a path to a newline-delimited file (`--prs /path/to/file.txt`).
In the latter case, the file should contain one PR URL or number per line.

Pull requests can be identified by a full URL (such as `https://github.com/owner/repo/pull/123`), a short format (such as `owner/repo#123`), or just a number (in which case you must also provide `--owner` and `--repo` options).

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

In Mode 3, the **rule context product** is the first alphabetically from `--output-products` (or from aggregated changelog products if omitted). To apply a different product's per-product rules, use a bundle whose `output_products` contains only that product (separate command or profile).

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

### Filter by issues [changelog-bundle-issues]

You can use the `--issues` option to create a bundle of changelogs that relate to those GitHub issues.
Provide either a comma-separated list of issues (`--issues "https://github.com/owner/repo/issues/123,456"`) or a path to a newline-delimited file (`--issues /path/to/file.txt`).
Issues can be identified by a full URL (such as `https://github.com/owner/repo/issues/123`), a short format (such as `owner/repo#123`), or just a number (in which case `--owner` and `--repo` are required — or set via `bundle.owner` and `bundle.repo` in the configuration).

```sh
docs-builder changelog bundle --issues "12345,12346" \
  --repo elasticsearch \
  --owner elastic \
  --output-products "elasticsearch 9.2.2 ga"
```

### Filter by pull request or issue file [changelog-bundle-file]

If you have a file that lists pull requests (such as PRs associated with a GitHub release), you can pass it to `--prs`.
For example, if you have a file that contains full pull request URLs like this:

```txt
https://github.com/elastic/elasticsearch/pull/108875
https://github.com/elastic/elasticsearch/pull/135873
https://github.com/elastic/elasticsearch/pull/136886
https://github.com/elastic/elasticsearch/pull/137126
```

You can use the `--prs` option with the file path to create a bundle of the changelogs that relate to those pull requests.
You can also combine multiple `--prs` options:

```sh
./docs-builder changelog bundle \
  --prs "https://github.com/elastic/elasticsearch/pull/108875,135873" \ <1>
  --prs test/9.2.2.txt \ <2>
  --output-products "elasticsearch 9.2.2 ga" <3>
  --resolve <4>
```

1. Comma-separated list of pull request URLs or numbers.
2. The path for the file that lists the pull requests. If the file contains only PR numbers, you must add `--repo` and `--owner` command options.
3. The product metadata for the bundle. If it is not provided, it will be derived from all the changelog product values.
4. Optionally include the contents of each changelog in the output file.

:::{tip}
You can use these files with profile-based bundling too. Refer to [](/cli/changelog/bundle.md).
:::

If you have changelog files that reference those pull requests, the command creates a file like this:

```yaml
products:
- product: elasticsearch
  target: 9.2.2
  lifecycle: ga
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
  prs:
  - https://github.com/elastic/elasticsearch/pull/108875
...
```

:::{note}
When a changelog matches multiple `--input-products` filters, it appears only once in the bundle. This deduplication applies even when using `--all` or `--prs`.
:::

### Filter by GitHub release notes [changelog-bundle-release-version]

If you have GitHub releases with automated release notes (the default format or [Release Drafter](https://github.com/release-drafter/release-drafter) format), you can use the `--release-version` option to derive the PR list from those release notes.
For example:

```sh
docs-builder changelog bundle \
  --release-version v1.34.0 \
  --repo apm-agent-dotnet --owner elastic <1>
```

1. The repo and repo owner are used to fetch the release and follow these rules of precedence:

- Repo: `--repo` flag > `bundle.repo` in `changelog.yml` (one source is required)
- Owner: `--owner` flag > `bundle.owner` in `changelog.yml` > `elastic`

This command creates a bundle of changelogs that match the list of PRs found in the `v1.34.0` GitHub release notes.

The bundle's product metadata is inferred automatically from the release tag and repository name; you can override that behavior with the `--output-products` option.

:::{tip}
If you are not creating changelogs when you create your pull requests, consider the `docs-builder changelog gh-release` command as a one-shot alternative to the `changelog add` and `changelog bundle` commands.
It parses the release notes, creates one changelog file per pull request found, and creates a `changelog-bundle.yaml` file — all in a single step. Refer to [](/cli/changelog/gh-release.md)
:::

### Hide features [changelog-bundle-hide-features]

You can use the `--hide-features` option to embed feature IDs that should be hidden when the bundle is rendered. This is useful for features that are not yet ready for public documentation.

```sh
docs-builder changelog bundle \
  --input-products "elasticsearch 9.3.0 *" \
  --hide-features "feature:hidden-api,feature:experimental" \ <1>
  --output /path/to/bundles/9.3.0.yaml
```

1. Feature IDs to hide. Changelogs with matching `feature-id` values will be commented out when rendered.

<!--
TO-DO: Add info about how to do this in bundle.
:::{tip}
You can do this with profile-based bundling too. Refer to [](/cli/changelog/bundle.md).
::: -->

The bundle output will include a `hide-features` field:

```yaml
products:
- product: elasticsearch
  target: 9.3.0
hide-features:
  - feature:hidden-api
  - feature:experimental
entries:
- file:
    name: 1765495972-new-feature.yaml
    checksum: 6c3243f56279b1797b5dfff6c02ebf90b9658464
```

When this bundle is rendered (either via the `changelog render` command or the `{changelog}` directive), changelogs with `feature-id` values matching any of the listed features will be commented out in the output.

:::{note}
The `--hide-features` option on the `render` command and the `hide-features` field in bundles are **combined**. If you specify `--hide-features` on both the `bundle` and `render` commands, all specified features are hidden. The `{changelog}` directive automatically reads `hide-features` from all loaded bundles and applies them.
:::

### Hide private links

A changelog can reference multiple pull requests and issues in the `prs` and `issues` array fields.

To comment out the private links in all changelogs in your bundles, refer to [changelog bundle](/cli/changelog/bundle.md#private-link-sanitization).

If you are working in a private repo and do not want any pull request or issue links to appear (even if they target a public repo), you also have the option to configure link visibiblity in the [changelog directive](/syntax/changelog.md) and [changelog render](/cli/changelog/render.md) command.

:::{tip}
You must run the `docs-builder changelog bundle` command with the `--resolve` option or set `bundle.resolve` to `true` in the changelog configuration file (so that bundle files are self-contained) in order to hide the private links.
:::

### Amend bundles [changelog-bundle-amend]

When you need to add changelogs to an existing bundle without modifying the original file, you can use the `docs-builder changelog bundle-amend` command to create amend bundles.
Amend bundles follow a specific naming convention: `{parent-bundle-name}.amend-{N}.yaml` where `{N}` is a sequence number.

When bundles are loaded (either via the `changelog render` command or the `{changelog}` directive), amend files are **automatically merged** with their parent bundles.
The changelogs from all matching amend files are combined with the parent bundle's changelogs and the result is rendered as a single release.

:::{warning}
If you explicitly list the amend bundles in the `--input` option of the `docs-builder changelog render` command, you'll get duplicate entries in the output files. List only the original bundles.
:::

For more details and examples, go to [](/cli/changelog/bundle-amend.md).

## Remove changelog files [changelog-remove]

A single changelog file might be applicable to multiple releases (for example, it might be delivered in both Stack and {{serverless-short}} releases or {{ech}} and Enterprise releases on different timelines).
After it has been included in all of the relevant bundles, it is reasonable to delete the changelog to keep your repository clean.

:::{important}
If you create docs with changelog directives, run the `docs-builder changelog bundle` command with the `--resolve` option or set `bundle.resolve` to `true` in the changelog configuration file (so that bundle files are self-contained).
Otherwise, the build will fail if you remove changelogs that the directive requires.

Likewise, the `docs-builder changelog render` command fails for "unresolved" bundles after you delete the changelogs.
:::

You can use the `docs-builder changelog remove` command to remove changelogs.
It supports the same two modes as `changelog bundle`: you can specify all the command options or you can define "profiles" in the changelog configuration file.
In the command option mode, exactly one filter option must be specified: `--all`, `--products`, `--prs`, `--issues`, `--release-version`, or `--report`.

Before deleting, the command automatically scans for bundles that still hold unresolved (`file:`) references to the matching changelog files.
If any are found, the command reports an error for each dependency.
This check prevents the `{changelog}` directive from failing at build time with missing file errors.
To proceed with removal even when unresolved bundle dependencies exist, use `--force`.

To preview what would be removed without deleting anything, use `--dry-run`.
Bundle dependency conflicts are also reported in dry-run mode.

### Removal with profiles [changelog-remove-profile]

If your `changelog.yml` configuration file defines `bundle.profiles`, you can use those profiles with `changelog remove`.
This is the easiest way to remove exactly the changelogs that were included in a profile-based bundle.
The command syntax is:

```sh
docs-builder changelog remove <profile> <version|report|url-list>
```

For example, if you bundled with:

```sh
docs-builder changelog bundle elasticsearch-release 9.2.0
```

You can remove the same changelogs with:

```sh
docs-builder changelog remove elasticsearch-release 9.2.0 --dry-run
```

The command automatically discovers `changelog.yml` by checking `./changelog.yml` then `./docs/changelog.yml` relative to your current directory.
If no configuration file is found, the command returns an error with advice to create one or to run from the directory where the file exists.

The `output`, `output_products`, `hide_features`, `sanitize_private_links`, and `resolve` fields are bundle-specific and are always ignored for removal (along with other bundle-only settings that do not affect which changelog files match the filter).
Which other fields are used depends on the profile type:

- Standard profiles: only the `products` field is used. The `repo` and `owner` fields are ignored (they only affect bundle output metadata).
- GitHub release profiles (`source: github_release`): `source`, `repo`, and `owner` are all used. The command fetches the PR list from the GitHub release identified by the version argument and removes changelogs whose `prs` field matches.

For example, given a GitHub release profile:

```sh
docs-builder changelog remove agent-gh-release v1.34.0 --dry-run
```

This fetches the PR list from the `v1.34.0` release (using the profile's `repo`/`owner` settings) and removes matching changelogs.

:::{note}
`source: github_release` profiles require a `GITHUB_TOKEN` or `GH_TOKEN` environment variable (or an active `gh` login) to fetch release details from the GitHub API.
:::

Profile-based removal is mutually exclusive with command options.
The only options allowed alongside a profile name are `--dry-run` and `--force`.

You can also pass a promotion report URL, file path, or URL list file as the second argument, and the command removes changelogs whose pull request or issue URLs appear in the report:

```sh
docs-builder changelog remove elasticsearch-release https://buildkite.../promotion-report.html
docs-builder changelog remove serverless-release 2026-02 ./promotion-report.html
docs-builder changelog remove serverless-release 2026-02 ./prs.txt
```

### Removal with command options [changelog-remove-raw]

You can alternatively remove changelogs based on their issues, pull requests, product metadata, or remove all changelogs from a folder.
Exactly one filter option must be specified: `--all`, `--products`, `--prs`, `--issues`, `--release-version` or `--report`.
When using a file for `--prs` or `--issues`, every line must be a fully-qualified GitHub URL.

```sh
docs-builder changelog remove --products "elasticsearch 9.3.0 *" --dry-run
```

For full option details, go to [](/cli/changelog/remove.md).
