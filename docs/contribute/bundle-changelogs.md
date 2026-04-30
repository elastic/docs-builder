# Bundle changelogs

You can use `docs-builder changelog` commands to created consolidated data files ("release bundles") that list all the notable changes associated with a particular release.
These files are ultimately used to generate release documentation.

This page describes how to create these files from the command line.
For details about the equivalent GitHub action, refer to the [docs-actions README](https://github.com/elastic/docs-actions/blob/main/changelog/README.md#bundling-changelogs).

## Before you begin

1. Create a changelog configuration file to define all the default behavior and optional profiles and rules. Refer to [](/contribute/configure-changelogs.md).
1. Create changelogs that describe all the notable changes. Refer to [](/contribute/create-changelogs.md).

## Identify your source of truth

To have accurate release notes, there must be a definitive source of truth for what was shipped in each release.
This is a superset of what will appear in the documentation.

The source of truth can be:

- a list of GitHub pull requests
- a list of GitHub issues
- a buildkite promotion report (which contains a list of PRs)
- automated release notes for GitHub releases
- all changelog files that exist in a specific folder
- all changelog files that match specific products, versions, and lifecycles

Deriving the source of truth from the contents of a folder or from the metadata in changelogs are the least accurate options (unless you have additional processes to confirm the validity of that information).
It is recommended to use lists that are generated as part of your release coordination activities.
Consider your options carefully and discuss with your docs team if necessary.

:::{important}
Not everything that was shipped will have a changelog.
For example, you can configure [rules](/contribute/create-changelogs.md#rules) that control changelog creation for work that's not publicly notable or spans multiple PRs.

Your release workflow should not assume there will be a one-to-one mapping between what was shipped and what will be documented.
:::

## Create profiles

The [changelog bundle](/cli/changelog/bundle.md) command has two modes of operation.
You can:

- specify all the command options every time you run the command, or
- define "profiles" in the changelog configuration file

The latter method is more convenient and consistent for repetitive workflows, therefore it's the recommended method described here.

For the most up-to-date changelog configuration options, refer to [changelog.example.yml](https://github.com/elastic/docs-builder/blob/main/config/changelog.example.yml) and [](/contribute/configure-changelogs-ref.md).

:::{note}
You must create profiles that match your chosen source of truth.
:::

### Bundle by report or URL list [profile-url]

If the source of truth for what was shipped in each release is:

- a list of GitHub pull requests
- a list of GitHub issues
- a buildkite promotion report (which contains a list of PRs)

... your profile does not have any mandatory settings.
However it's a good idea to define the [basic bundle settings](/contribute/configure-changelogs-ref.md#bundle-basic) and the [profile settings](/contribute/configure-changelogs-ref.md#bundle-profiles) for the output filename and output products.
For example:

```yaml
bundle:
  directory: docs/changelog # The directory that contains changelog files.
  output_directory: docs/releases # The directory that contains changelog bundles.
  repo: elasticsearch # The default repository for PR and issue links.
  owner: elastic # The default repository owner for PR and issue links.
  profiles:
    # Find changelogs that match a list of PRs
    serverless-report:
      output: "serverless-{version}.yaml" <1>
      output_products: "cloud-serverless {version}" <2>
    elasticsearch-report:
      output: "elasticsearch-{version}.yaml"
      output_products: "elasticsearch {version} {lifecycle}"
```

<1> If the `output` and `output_products` are omitted, the default path and file names are used. This example shows how you can use a `{version}` variable to customize the bundle's filename.
<2> You can likewise set the bundle's product metadata, which affects the rules that are applied and the product and version titles that ultimately appear in the documentation.

### Bundle by GitHub releases [profile-gh-release]

If you have automated GitHub release notes, the `changelog bundle` command can fetch the release from GitHub, parse PR references from the release notes, and uses them as the bundle filter.
Only automated GitHub release notes (the default format or [Release Drafter](https://github.com/release-drafter/release-drafter) format) are supported at this time.

Your profile must contain `source: github_release`.
It's also a good idea to include the [basic bundle settings](/contribute/configure-changelogs-ref.md#bundle-basic) and the [profile settings](/contribute/configure-changelogs-ref.md#bundle-profiles) for the output filename and output products.
For example:

```yaml
bundle:
  profiles:
    agent-gh-release:
      source: github_release <1>
      repo: apm-agent-dotnet
      owner: elastic
      output: "agent-{version}.yaml"
      output_products: "apm-agent-dotnet {version} {lifecycle}" <2>
```

1. This profile fetches the PR list from the GitHub release notes for the version tag specified in the command.
2. For `source: github_release` profiles, the `{lifecycle}` placeholder in `output` and `output_products` is inferred from the release tag returned by GitHub (not the argument you pass to the command). For example, if the release tag is `v1.34.1-preview.1` the lifecycle is `preview`. Refer to [](/cli/changelog/bundle.md#lifecycle-inference) for more details.

### Bundle by folder or changelog product

If the source of truth for what was shipped in each release is:

- the `products` information that exists in each changelog
- all changelog files that exist in a specific folder

... you must include `products` in your profile.
For example:

```yaml
bundle:
  directory: docs/changelog
  output_directory: docs/releases
  resolve: true
  repo: elasticsearch
  owner: elastic
  profiles:
    # Collect all changelogs
    release-all:
      products: "* * *" <1>
      output: "all.yaml"
    # Find changelogs with any lifecycle and a partial date
    serverless-monthly:
      products: "cloud-serverless {version}-* *" <2>
      output: "serverless-{version}.yaml"
      output_products: "cloud-serverless {version}"
    # Find changelogs with a specific lifecycle
    elasticsearch-ga-only:
      products: "elasticsearch {version} ga" <3>
      output: "elasticsearch-{version}.yaml"
    # Infer the lifecycle from the version
    elasticsearch-release:
      products: "elasticsearch {version} {lifecycle}" <4>
      output: "elasticsearch-{version}.yaml"
      output_products: "elasticsearch {version}"
```

1. This profile collects all changelogs from the `directory`.
2. This profile collects any changelogs that have `product: cloud-serverless`, any lifecycle, and the date partially specified in the command.
3. This profile collects any changelogs that have `product: elasticsearch`, `lifecycle: ga`, and the version specified in the command.
4. In this case, the lifecycle is inferred from the version specified in the command. For example, if the version is `9.2.0-beta.1` the lifecycle is `beta`. Refer to [](/cli/changelog/bundle.md#lifecycle-inference).

:::{note}
The `products` field determines which changelog files are gathered for consideration. You can still apply [rules](#rules) afterward to further filter changelogs from the bundle. The input stage and bundle filtering stage are conceptually separate.
:::

## Create bundles

If you created profiles, you can use them with the `changelog bundle` command like this:

```sh
docs-builder changelog bundle <profile> <version|report|url-list>
```

The second argument accepts a version string, a promotion report URL or path, or a URL list file (a plain-text file with one fully-qualified GitHub URL per line).
If you are using a `{version}` placeholder in the `output_products` or `output` fields, you must provide that value as well as your report or URL argument.

If the source of truth for what was shipped in each release is:

- a list of GitHub pull requests or issues:

  ```sh
  # Bundle changelogs from a PR list ({version} → "2026-02-13")
  docs-builder changelog bundle serverless-report 2026-02-13 ./prs.txt
  ```

- a buildkite promotion report:

  ```sh
  # Bundle changelogs from a buildkite report ({lifecycle} → "ga" inferred from "9.2.0")
  docs-builder changelog bundle elasticsearch-report 9.2.0 ./promotion-report.html
  ```

- automated release notes for GitHub releases:

  ```sh
  docs-builder changelog bundle agent-gh-release 1.34.1

  # Use "latest" to fetch the most recent release
  docs-builder changelog bundle agent-gh-release latest
  ```

  Alternatively, use the [changelog gh-release](/cli/changelog/gh-release.md) command, which creates the changelogs and bundles at the same time.

- all changelog files that exist in a specific folder:

  ```sh
  docs-builder changelog bundle release-all '*'
  ```

- all changelog files that match specific products, versions, and lifecycles:

  ```sh
  # Bundle changelogs for a GA release ({lifecycle} → "ga" inferred from "9.2.0")
  docs-builder changelog bundle elasticsearch-release 9.2.0

  # Bundle changelogs for a beta release ({lifecycle} → "beta" inferred from "9.2.0-beta.1")
  docs-builder changelog bundle elasticsearch-release 9.2.0-beta.1

  # Bundle changelogs with partial dates
  docs-builder changelog bundle serverless-monthly 2026-02
  ```

By default all changelogs that match the chosen source of truth are included in the bundle.
To apply additional filtering by the changelog type, areas, or products, add [rules.bundle](/contribute/configure-changelogs-ref.md#rules-bundle) configuration settings.

:::{note}
For profiles that use static patterns (without `{version}` or `{lifecycle}` placeholders), the second argument is still required but serves no functional purpose. You can pass any placeholder value.

```sh
# Profile with static patterns - second argument unused but required
docs-builder changelog bundle release-all '*'
docs-builder changelog bundle release-all 'unused'
docs-builder changelog bundle release-all 'none'
```
:::

If you prefer to specify all the command options every time you run the command refer to [changelog bundle](/cli/changelog/bundle.md#option-based-examples).

## Amend bundles [changelog-bundle-amend]

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

The `output`, `output_products`, `hide_features`, `link_allow_repos`, and `resolve` fields are bundle-specific and are always ignored for removal (along with other bundle-only settings that do not affect which changelog files match the filter).
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

## Examples

### Apply bundle rules [rules]

:::{tip}
Not everything that was shipped in a release and has a changelog necessarily belongs in the release bundle.
:::

If you want to automatically include or exclude changelogs from bundles based on their areas, types, or products, you can accomplish this with rules in your changelog configuration file.
For example, you might choose to omit `other` or `docs` types of changelogs.
Or you might choose to omit all changelogs related to specific features (`areas`) from a specific product's release bundles.

You can define rules at the global level (applies to all products) like this:

```yaml
rules:
  bundle:
    exclude_products: elasticsearch
    exclude_types: deprecation
    exclude_areas:
      - Internal
```

Alternatively, you can define product-specific rules:

```yaml
rules:
  bundle:
    products:
      cloud-serverless:
        include_areas:
          - "Search"
          - "Monitoring"
      elasticsearch:
        exclude_areas:
          - Autoscaling
```

Product-specific rules override the global rules entirely—they do not merge.
For details, refer to [](/contribute/configure-changelogs-ref.md#rules-bundle) and  [](/contribute/configure-changelogs-ref.md#advanced-rule-examples).


### Bundle changelogs from a GitHub release [changelog-bundle-profile-github-release]

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

### Bundle descriptions

You can add introductory text to bundles using the `description` field. This text appears at the top of rendered changelogs, after the release heading but before the entry sections.

**Configuration locations:**

- `bundle.description`: Default description for all profiles
- `bundle.profiles.<name>.description`: Profile-specific description (overrides the default)

**Placeholder support:**

Bundle descriptions support these placeholders:

- `{version}`: The resolved version string
- `{lifecycle}`: The resolved lifecycle (ga, beta, preview, etc.)
- `{owner}`: The GitHub repository owner
- `{repo}`: The GitHub repository name

**Important**: When using `{version}` or `{lifecycle}` placeholders, you must ensure predictable substitution values:

- **Option-based mode**: Requires `--output-products` when using placeholders
- **Profile-based mode**: Requires either a version argument (e.g., `bundle profile 9.2.0`) OR an `output_products` pattern in the profile configuration when using placeholders. If you invoke a profile with only a promotion report (e.g., `bundle profile ./report.html`), placeholders will fail unless `output_products` is configured.

**Multiline descriptions in YAML:**

For complex descriptions with multiple paragraphs, lists, and links, use YAML literal block scalars with the `|` (pipe) syntax:

```yaml
bundle:
  description: |
    This release includes significant improvements:
    
    - Enhanced performance
    - Bug fixes and stability improvements
    - New features for better user experience
    
    For security updates, go to [security announcements](https://example.com/docs).
    
    Download the release binaries: https://github.com/{owner}/{repo}/releases/tag/v{version}
```

The `|` (pipe) preserves line breaks and is ideal for Markdown-formatted text. Avoid using `>` (greater than) for descriptions as it folds line breaks into spaces, making lists and paragraphs difficult to format correctly.

**Command line usage:**

For simple descriptions, use the `--description` option with regular quotes:

```sh
docs-builder changelog bundle --all --description "This release includes new features."
```

For multiline descriptions on the command line, use ANSI-C quoting (`$'...'`) with `\n` for line breaks:

```sh
docs-builder changelog bundle --all --description $'Enhanced release:\n\n- Performance improvements\n- Bug fixes'
```

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

### Option-based bundling [changelog-bundle-options]

You can specify only one of the following filter options:

- `--all`: Include all changelogs from the directory.
- `--input-products`: Include changelogs for the specified products. Refer to [Filter by product](#changelog-bundle-product).
- `--prs`: Include changelogs for the specified pull request URLs, or a path to a newline-delimited file. When using a file, every line must be a fully-qualified GitHub URL such as `https://github.com/owner/repo/pull/123`. Go to [Filter by pull requests](#changelog-bundle-pr).
- `--issues`: Include changelogs for the specified issue URLs, or a path to a newline-delimited file. When using a file, every line must be a fully-qualified GitHub URL such as `https://github.com/owner/repo/issues/123`. Go to [Filter by issues](#changelog-bundle-issues).
- `--release-version`: Bundle changelogs for the pull requests in GitHub release notes. Refer to [Bundle by GitHub release](#changelog-bundle-release-version).
- `--report`: Include changelogs whose pull requests appear in a promotion report. Accepts a URL or a local file path to an HTML report.

`rules.bundle` in `changelog.yml` is **not** mutually exclusive with these options: it runs as a **second stage** after the primary filter matches entries (for example, `--input-products` gathers changelogs, then global or per-product bundle rules may exclude some). The only mutually exclusive pairing is **profile-based** versus **option-based** invocation. See [bundle rules](/contribute/configure-changelogs-ref.md#rules-bundle).

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

To comment out links that are not in your allowlist in all changelogs in your bundles, refer to [changelog bundle](/cli/changelog/bundle.md#link-allowlist).

If you are working in a private repo and do not want any pull request or issue links to appear (even if they target a public repo), you also have the option to configure link visibiblity in the [changelog directive](/syntax/changelog.md) and [changelog render](/cli/changelog/render.md) command.

:::{tip}
You must run the `docs-builder changelog bundle` command with the `--resolve` option or set `bundle.resolve` to `true` in the changelog configuration file (so that bundle files are self-contained) in order to hide the private links.
:::
