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

You must create profiles that match your chosen source of truth.

:::{tip}
It is strongly recommended to set `output_products` in your profile so your bundles have a single top-level product entry that provides the context of the release. This context is particularly important if you'll be [applying bundle rules](#rules).
:::

For the most up-to-date changelog configuration options, refer to [changelog.example.yml](https://github.com/elastic/docs-builder/blob/main/config/changelog.example.yml) and [](/contribute/configure-changelogs-ref.md).

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
  directory: docs/changelog <1>
  output_directory: docs/releases <2>
  repo: elasticsearch 
  owner: elastic
  resolve: true <3>
  profiles:
    serverless-report:
      output: "serverless/{version}.yaml" <4>
      output_products: "cloud-serverless {version}" <5>
    elasticsearch-release:
      output: "elasticsearch/{version}.yaml"
      output_products: "elasticsearch {version} {lifecycle}"
```

1. The directory that contains changelog files.
2. The directory that contains changelog bundles.
3. Resolve the changelog files in the bundle rather than just referencing them. Otherwise, when you move or remove changelog files the bundle cannot be rendered.
4. If `output` is omitted, the default path and file names are used. This example shows how you can use a `{version}` variable to customize the bundle's filename.
5. The bundle's product metadata, which affects the rules that are applied and the product and version titles that ultimately appear in the documentation. If omitted, it's derived from all the changelogs in the bundle.

### Bundle by GitHub releases [profile-gh-release]

If you have automated GitHub release notes, the `changelog bundle` command can fetch the release from GitHub, parse PR references from the release notes, and uses them as the bundle filter.
Only automated GitHub release notes (the default format or [Release Drafter](https://github.com/release-drafter/release-drafter) format) are supported at this time.

Your profile must contain `source: github_release`.
It's also a good idea to include the [basic bundle settings](/contribute/configure-changelogs-ref.md#bundle-basic) and the [profile settings](/contribute/configure-changelogs-ref.md#bundle-profiles) for the output filename and output products.
For example:

```yaml
bundle:
  resolve: true
  profiles:
    agent-gh-release:
      source: github_release <1>
      repo: apm-agent-dotnet
      owner: elastic
      output: "agent-{version}.yaml"
      output_products: "apm-agent-dotnet {version} {lifecycle}" <2>
```

1. This profile fetches the PR list from the GitHub release notes for the version tag specified in the command.
2. For `source: github_release` profiles, the `{lifecycle}` placeholder in `output` and `output_products` is inferred from full release tag name. For example, if the release tag is `v1.34.1-preview.1` the lifecycle is `preview`. Refer to [](/cli/changelog/bundle.md#lifecycle-inference) for more details.

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
    elasticsearch-with-lifecycle:
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

For example, if the source of truth for what was shipped in each release is:

- a list of GitHub pull requests or issues:

  ```sh
  # Bundle changelogs from a PR list ({lifecycle} → "ga" inferred from "9.2.0")
  docs-builder changelog bundle elasticsearch-release 9.2.0 ./prs.txt
  ```

  ... where `prs.txt` is a newline delimited file with PR or issue URLs like this this:

  ```txt
  https://github.com/elastic/kibana/pull/123
  https://github.com/elastic/kibana/pull/456
  ```

| Field | Description |
|---|---|
| `repo` | Default GitHub repository name applied to all profiles. Falls back to product ID if not set at any level. |
| `owner` | Default GitHub repository owner applied to all profiles. |
| `resolve` | When `true`, embeds full changelog entry content in the bundle (same as `--resolve`). Required when `link_allow_repos` is set. |
| `link_allow_repos` | When set (including an empty list), only PR/issue links whose resolved repository is in this `owner/repo` list are kept; others are rewritten to `# PRIVATE:` sentinels in bundle YAML. When absent, no link filtering is applied. Requires `resolve: true`. Refer to [PR and issue link allowlist](/cli/changelog/bundle.md). |

  ```sh
  # Bundle changelogs from a buildkite report ({version} → "2026-02-13")
  docs-builder changelog bundle serverless-report 2026-02-13 ./promotion-report.html
  ```

- automated release notes for GitHub releases:

  ```sh
  # Bundle changelogs from the release notes of a specific GitHub tag
  docs-builder changelog bundle agent-gh-release v1.34.1

  # Use "latest" to fetch the most recent release
  docs-builder changelog bundle agent-gh-release latest
  ```

  Alternatively, use the [changelog gh-release](/cli/changelog/gh-release.md) command, which creates the changelogs and bundles at the same time.

  :::{note}
  This method requires a `GITHUB_TOKEN` or `GH_TOKEN` environment variable (or an active `gh` login) to fetch release details from the GitHub API.
  :::

- all changelog files that exist in a specific folder:

  ```sh
  docs-builder changelog bundle release-all '*'
  ```

- all changelog files that match specific products, versions, and lifecycles:

  ```sh
  # Bundle changelogs with partial dates
  docs-builder changelog bundle serverless-monthly 2026-02
  
  # Bundle changelogs for a GA release ({lifecycle} → "ga" inferred from "9.2.0")
  docs-builder changelog bundle elasticsearch-with-lifecycle 9.2.0

  # Bundle changelogs for a beta release ({lifecycle} → "beta" inferred from "9.2.0-beta.1")
  docs-builder changelog bundle elasticsearch-with-lifecycle 9.2.0-beta.1
  ```

By default all changelogs that match the chosen source of truth are included in the bundle.

:::{tip}
It is strongly recommended to pull all of the content from each changelog into the bundle; otherwise you can't move or remove your changelogs. If your bundle contains only references to the files, add set [bundle.resolve](/contribute/configure-changelogs-ref.md#bundle-basic) to true and re-generate your bundle.
:::

To apply additional filtering by the changelog type, areas, or products, add [bundle rules](#rules).

1. Include all changelogs that have the `cloud-serverless` product identifier with target dates of either December 2 2025 (lifecycle `ga`) or December 6 2025 (lifecycle `beta`). For more information about product values, refer to [Product format](/cli/changelog/bundle.md).

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

## Filter by pull requests [changelog-bundle-pr]

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

## Filter by issues [changelog-bundle-issues]

You can use the `--issues` option to create a bundle of changelogs that relate to those GitHub issues.
Provide either a comma-separated list of issues (`--issues "https://github.com/owner/repo/issues/123,456"`) or a path to a newline-delimited file (`--issues /path/to/file.txt`).
Issues can be identified by a full URL (such as `https://github.com/owner/repo/issues/123`), a short format (such as `owner/repo#123`), or just a number (in which case `--owner` and `--repo` are required — or set via `bundle.owner` and `bundle.repo` in the configuration).

```sh
docs-builder changelog bundle --issues "12345,12346" \
  --repo elasticsearch \
  --owner elastic \
  --output-products "elasticsearch 9.2.2 ga"
```

## Filter by pull request or issue file [changelog-bundle-file]

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

## Filter by GitHub release notes [changelog-bundle-release-version]

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

## Hide features [changelog-bundle-hide-features]

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

## Hide private links

A changelog can reference multiple pull requests and issues in the `prs` and `issues` array fields.

To comment out links that are not in your allowlist in all changelogs in your bundles, refer to [changelog bundle](/cli/changelog/bundle.md).

If you are working in a private repo and do not want any pull request or issue links to appear (even if they target a public repo), you also have the option to configure link visibiblity in the [changelog directive](/syntax/changelog.md) and [changelog render](/cli/changelog/render.md) command.

:::{tip}
You must run the `docs-builder changelog bundle` command with the `--resolve` option or set `bundle.resolve` to `true` in the changelog configuration file (so that bundle files are self-contained) in order to hide the private links.
:::

## Amend bundles [changelog-bundle-amend]

When you need to add changelogs to an existing bundle, you can use the `docs-builder changelog bundle-amend` command, which creates _amend bundles_.
For example:

```sh
docs-builder changelog bundle-amend \
  ./docs/releases/9.3.0.yaml \
  --add "./docs/changelog/138723.yaml,./docs/changelog/1770424335.yaml"
```

Amend bundles follow a specific naming convention: `{parent-bundle-name}.amend-{N}.yaml` where `{N}` is a sequence number.

:::{note}
There is currently no command to **remove** changelogs from a bundle. You must edit the bundle file manually or else re-generate the bundle with an updated source of truth or a new rule that excludes the changelog.
:::

When bundles are turned into docs (either via the `changelog render` command or the `{changelog}` directive), amend files are **automatically merged** with their parent bundles.
The changelogs from all matching amend files are combined with the parent bundle's changelogs and the result is rendered as a single release.

:::{warning}
Don't explicitly list the amend bundles in the `--input` option of the `docs-builder changelog render` command--you'll get duplicate entries in the output files. List only the original/parent bundles.
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
If you created profiles, you can use them like this:

```sh
docs-builder changelog remove <profile> <version|report|url-list>
```

For example, if the source of truth for what was shipped in each release is:

- a list of GitHub pull requests or issues:

  ```sh
  docs-builder changelog remove elasticsearch-release ./prs.txt
  ```

- a buildkite promotion report:

  ```sh
  docs-builder changelog remove serverless-report ./promotion-report.html
  ```

- automated release notes for GitHub releases:

  ```sh
  docs-builder changelog remove agent-gh-release 1.34.1
  ```

  :::{note}
  This method requires a `GITHUB_TOKEN` or `GH_TOKEN` environment variable (or an active `gh` login) to fetch release details from the GitHub API.
  :::

- all changelog files that exist in a specific folder:

  ```sh
  docs-builder changelog remove release-all '*'
  ```

- all changelog files that match specific products, versions, and lifecycles:

  ```sh
  docs-builder changelog remove serverless-monthly 2026-02
  ```

Before deleting, the command automatically scans for bundles that still hold unresolved (`file:`) references to the matching changelog files.
If any are found, the command reports an error for each dependency.
This check prevents the `{changelog}` directive from failing at build time with missing file errors.
To proceed with removal even when unresolved bundle dependencies exist, use `--force`.

To preview what would be removed without deleting anything, use `--dry-run`.

For full option details, go to [](/cli/changelog/remove.md).

## Examples

The following sections provide more details about optional and advanced steps.

### Apply bundle rules [rules]

:::{important}
Not everything that was shipped in a release and has a changelog necessarily belongs in the release bundle.
:::

If you want to automatically include or exclude changelogs from bundles based on their areas, types, or products, you can accomplish this with rules in your changelog configuration file.
Bundle rules run as a secondary stage after the candidate changelogs are collected (for example, based on a PR list, promotion report, or other valid source of truth).

For example, you might choose to omit `other` or `docs` types of changelogs.
Or you might choose to omit all changelogs related to specific features (`areas`) from a product's release bundles.

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
For details, refer to [](/contribute/configure-changelogs-ref.md#rules-bundle) and [](/contribute/configure-changelogs-ref.md#advanced-rule-examples).

### Hide features [changelog-bundle-hide-features]

Changelogs have an optional `feature-id` field that you can use to associate the change with a specific feature or project.
If there are features or projects that are not yet ready for public documentation, you can list those IDs in the [`hide_features`](/contribute/configure-changelogs-ref.md#bundle-profiles) setting:

```yaml
bundle:
  directory: docs/changelog
  output_directory: docs/releases
  repo: elasticsearch 
  owner: elastic
  resolve: true
  profiles:
    serverless-report:
      output: "serverless/{version}.yaml"
      output_products: "cloud-serverless {version}"
      hide_features: <1>
        - feature-flag-1
        - feature-flag-2
```

1. The feature identifiers to hide.

When you use this profile to create a bundle, the list is carried forward into its metadata.
Any changelogs with matching `feature-id` values are commented out when you publish the bundle.

### Hide private links

A changelog can reference multiple pull requests and issues in its `prs` and `issues` fields.
You can allowlist links to certain repos with the `link_allow_repos` setting:

```yaml
bundle:
  directory: docs/changelog
  output_directory: docs/releases
  repo: elasticsearch 
  owner: elastic
  resolve: true
  link_allow_repos: <1>
    - elastic/elasticsearch
    - elastic/kibana
    - elastic/roadmap
```

1. Only links to these owner/repo pairs are shown in the release docs. Others are rewritten to `# PRIVATE:` sentinels.

There are no implicit values for this setting. You must list every repo whose links should appear, including the current repo.
When this setting is omitted entirely, no link filtering is applied.
For more details, refer to [PR and issue link allowlist](/cli/changelog/bundle.md#link-allowlist).

:::{tip}
You must set `bundle.resolve` to `true` in the changelog configuration file (so that bundle files are self-contained) in order to hide the private links. The bundle's changelog entries are sanitized but the individual changelog files are unchanged.
:::

If you are working in a private repo and do not want any pull request or issue links to appear (even if they target a public repo), you can also configure link visibility in the [changelog directive](/syntax/changelog.md#hide-links) and [changelog render](/cli/changelog/render.md) command.

## Next steps

After you've created release bundles, you can use them to generate [release docs](/contribute/publish-changelogs.md).
