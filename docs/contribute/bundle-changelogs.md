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
- an explicit list of changelog file paths

Deriving the source of truth from the contents of a folder or from the metadata in changelogs are the least accurate options (unless you have additional processes to confirm the validity of that information).
An explicit path list is appropriate for ad hoc releases where a small set of known changelog files must be bundled and those entries may not have `prs` or `issues` fields.
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

### Bundle by report, URL list, or path list [profile-url]

If the source of truth for what was shipped in each release is:

- a list of GitHub pull requests
- a list of GitHub issues
- a buildkite promotion report (which contains a list of PRs)
- an explicit list of changelog YAML paths (one `.yaml`/`.yml` path per line)

... your profile does not have any mandatory settings.
However it's a good idea to define the [basic bundle settings](/contribute/configure-changelogs-ref.md#bundle-basic) and the [profile settings](/contribute/configure-changelogs-ref.md#bundle-profiles) for the output filename and output products.
For example:

```yaml
bundle:
  directory: docs/changelog <1>
  output_directory: docs/releases <2>
  repo: elasticsearch 
  owner: elastic
  profiles:
    serverless-report:
      output: "serverless/{version}.yaml" <3>
      output_products: "cloud-serverless {version}" <4>
    elasticsearch-release:
      output: "elasticsearch/{version}.yaml"
      output_products: "elasticsearch {version} {lifecycle}"
```

1. The directory that contains changelog files.
2. The directory that contains changelog bundles.
3. If `output` is omitted, the default path and file names are used. This example shows how you can use a `{version}` variable to customize the bundle's filename.
4. The bundle's product metadata, which affects the rules that are applied and the product and version titles that ultimately appear in the documentation. If omitted, it's derived from all the changelogs in the bundle.

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
docs-builder changelog bundle <profile> <version|report|url-list|path-list>
```

The second argument accepts a version string, a promotion report URL or path, a URL list file (a plain-text file with one fully-qualified GitHub URL per line), or path list file (one changelog file path per line).

If you are using a `{version}` placeholder in the `output_products` or `output` fields, you must provide that value as well as your report or list argument.

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

- a buildkite promotion report:

  ```sh
  # Bundle changelogs from a buildkite report ({version} → "2026-02-13")
  docs-builder changelog bundle serverless-report 2026-02-13 ./promotion-report.html
  ```

- a list of changelog files:

  ```sh
  # Bundle changelogs from a file list ({version} → "2026-07-17")
  docs-builder changelog bundle serverless-release 2026-07-17 ./changelogs.txt
  ```

  ... where `changelogs.txt` is a newline delimited file with paths like this this:

  ```txt
  docs/changelog/1770424335-adhoc-security-fix.yaml
  docs/changelog/1770424401-adhoc-feature.yaml
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
Bundles are self-contained: the full content of each changelog is embedded in the bundle, so you can freely move or remove the changelog files afterward.

To apply additional filtering by the changelog type, areas, or products, add [bundle rules](#rules).

## Amend bundles [changelog-bundle-amend]

When you need to add changelogs to an existing bundle, you can use the `docs-builder changelog bundle-amend` command, which creates _amend bundles_.
For example:

```sh
docs-builder changelog bundle-amend \
  ./docs/releases/9.3.0.yaml \
  --add "./docs/changelog/138723.yaml,./docs/changelog/1770424335.yaml"
```

Amend bundles follow a specific naming convention: `{parent-bundle-name}.amend-{N}.yaml` where `{N}` is a sequence number.

To remove entries from an existing bundle without editing the parent file, use `--remove` on the same command:

```sh
docs-builder changelog bundle-amend \
  ./docs/releases/9.3.0.yaml \
  --remove "./docs/changelog/138723.yaml"
```

This creates an amend file with `exclude-entries` that is merged when the bundle is rendered.

When bundles are turned into docs (either via the `changelog render` command or the `{changelog}` directive), amend files are **automatically merged** with their parent bundles.
The changelogs from all matching amend files are combined with the parent bundle's changelogs and the result is rendered as a single release.

:::{warning}
Don't explicitly list the amend bundles in the `--input` option of the `docs-builder changelog render` command--you'll get duplicate entries in the output files. List only the original/parent bundles.
:::

For more details and examples, go to [](/cli/changelog/bundle-amend.md).

## Remove changelog files [changelog-remove]

A single changelog file might be applicable to multiple releases (for example, it might be delivered in both Stack and {{serverless-short}} releases or {{ech}} and Enterprise releases on different timelines).
After it has been included in all of the relevant bundles, it is reasonable to delete the changelog to keep your repository clean.
Because bundles are self-contained, deleting changelog files never affects existing bundles or the docs built from them.

You can use the `docs-builder changelog remove` command to remove changelogs.
If you created profiles, you can use them like this:

```sh
docs-builder changelog remove <profile> <version|report|url-list|path-list>
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

- a list of changelog files:

  ```sh
  docs-builder changelog remove serverless-release ./changelogs.txt
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
You can allowlist links to certain repos with the [`link_allow_repos` setting](/contribute/configure-changelogs-ref.md#bundle-basic).
For example:

```yaml
bundle:
  directory: docs/changelog
  output_directory: docs/releases
  repo: elasticsearch 
  owner: elastic
  link_allow_repos: <1>
    - elastic/elasticsearch
    - elastic/kibana
    - elastic/roadmap
```

1. Only links to these owner/repo pairs are shown in the release docs. Others are rewritten to `# PRIVATE:` sentinels.

There are no implicit values for this setting.
You must list every repo whose links should appear, including the current repo.
When this setting is omitted entirely, no link filtering is applied.

:::{tip}
The bundle's changelog entries are sanitized but the individual changelog files are unchanged.
:::

If you are working in a private repo and do not want any pull request or issue links to appear (even if they target a public repo), you can also configure link visibility in the [changelog directive](/syntax/changelog.md#hide-links) and [changelog render](/cli/changelog/render.md) command.

## Next steps

After you've created release bundles, you can use them to generate [release docs](/contribute/publish-changelogs.md).
