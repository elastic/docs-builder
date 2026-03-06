# changelog remove

Remove changelog YAML files from a directory.

You can use either profile-based or command-option-based removal:

- **Profile-based**: `docs-builder changelog remove <profile> <version|promotion-report>` — uses the same `bundle.profiles` configuration as [`changelog bundle`](/cli/release/changelog-bundle.md) to determine which changelogs to remove.
- **Option-based**: `docs-builder changelog remove --products "..." ` (or `--prs`, `--issues`, `--all`, `--release-version`, `--report`) — specify the filter directly.

These modes are mutually exclusive. You can't combine a profile argument with the command filter options.

Before deleting anything, the command checks whether any of the matching files are referenced by unresolved bundles, to prevent silently breaking the `{changelog}` directive.

For more context, go to [](/contribute/changelog.md#changelog-remove).

## Usage

```sh
docs-builder  changelog remove [arguments...] [options...] [-h|--help]
```

## Arguments

These arguments apply to profile-based removal:

`[0] <string?>`
:   Profile name from `bundle.profiles` in the changelog configuration file.
:   For example, "elasticsearch-release".
:   When specified, the second argument is the version, promotion report URL, or URL list file.

`[1] <string?>`
:   Version number, promotion report URL/path, or URL list file.
:   For example, `9.2.0`, `https://buildkite.../promotion-report.html`, or `/path/to/prs.txt`.
:   See [Profile argument types](/cli/release/changelog-bundle.md#profile-argument-types) for details on accepted formats.

`[2] <string?>`
:   Optional: Promotion report URL/path or URL list file when the second argument is a version string.
:   When provided, `[1]` must be a version string and `[2]` is the PR/issue filter source.
:   For example, `docs-builder changelog remove serverless-release 2026-02 ./promotion-report.html`.

## Options

For command-option-based removal, only one filter option can be specified: `--all`, `--products`, `--prs`, `--issues`, `--release-version`, or `--report`.

`--all`
:   Remove all changelog files in the directory.
:   Cannot be combined with a profile argument.

`--bundles-dir <string?>`
:   Optional: Override the directory scanned for bundles during the dependency check.
:   When not specified, the directory is resolved in order: `bundle.output_directory` from the changelog configuration, then `{changelog-dir}/bundles`, then `{changelog-dir}/../bundles`.
:   Not allowed with a profile argument. In profile mode, the same automatic discovery applies.

`--config <string?>`
:   Optional: Path to the changelog configuration file.
:   Defaults to `docs/changelog.yml`.
:   Not allowed with a profile argument. In profile mode, the configuration is discovered automatically.

`--directory <string?>`
:   Optional: The directory that contains the changelog YAML files.
:   When not specified, falls back to `bundle.directory` from the changelog configuration, then the current working directory.
:   Not allowed with a profile argument. In profile mode, the same fallback applies (starting from `bundle.directory`).

`--dry-run`
:   Print the files that would be removed and any bundle dependency conflicts, without deleting anything.
:   Valid in both profile and command-option-based mode.

`--force`
:   Proceed with removal even when files are referenced by unresolved bundles.
:   Emits a warning per dependency instead of blocking.
:   Valid in both profile and command-option-based mode.

`--issues <string[]?>`
:   Filter by issue URLs (comma-separated), or a path to a newline-delimited file.
:   Can be specified multiple times.
:   When using a file, every line must be a fully-qualified GitHub issue URL. Bare numbers and short forms are not allowed in files.
:   Cannot be combined with a profile argument.

`--owner <string?>`
:   Optional: The GitHub repository owner, which is used when pull requests or issues are specified as numbers.
:   Precedence: `--owner` flag > `bundle.owner` in `changelog.yml` > `elastic`.
:   Cannot be combined with a profile argument.

`--products <List<ProductInfo>?>`
:   Filter by products in format `"product target lifecycle, ..."`
:   Cannot be combined with a profile argument.
:   All three parts (product, target, lifecycle) are required but can be wildcards (`*`). Multiple comma-separated values are combined with OR: a changelog is removed if it matches any of the specified product/target/lifecycle combinations. For example:

- `"elasticsearch 9.3.0 ga"` — exact match
- `"cloud-serverless 2025-12-02 ga, cloud-serverless 2025-12-06 beta"` — remove changelogs for either cloud-serverless 2025-12-02 ga or cloud-serverless 2025-12-06 beta
- `"elasticsearch * *"` — all elasticsearch changelogs
- `"* 9.3.* *"` — any product with a target starting with `9.3.`
- `"* * *"` — all changelogs (equivalent to `--all`)

`--prs <string[]?>`
:   Filter by pull request URLs (comma-separated), or a path to a newline-delimited file.
:   Can be specified multiple times.
:   When using a file, every line must be a fully-qualified GitHub PR URL. Bare numbers and short forms are not allowed in files.
:   Cannot be combined with a profile argument.

`--release-version <string?>`
:   GitHub release tag to use as a source of pull requests (for example, `"v9.2.0"` or `"latest"`).
:   When specified, the command fetches the release from GitHub, parses PR references from the release notes, and use it as the removal filter. Only automated GitHub release notes (the default format or [Release Drafter](https://github.com/release-drafter/release-drafter) format) are supported at this time.
:   Requires repo (`--repo` or `bundle.repo` in `changelog.yml`) and owner (`--owner` flag > `bundle.owner` in `changelog.yml` > `elastic`) details.
:   Requires a `GITHUB_TOKEN` or `GH_TOKEN` environment variable (or an active `gh` login).

`--repo <string?>`
:   The GitHub repository name, which is required when pull requests or issues are specified as numbers or when using `--release-version`.
:   Precedence: `--repo` flag > `bundle.repo` in `changelog.yml`.
:   Cannot be combined with a profile argument.

`--report <string?>`
:   Filter by pull requests extracted from a promotion report. Accepts a URL or a local file path.
:   Exactly one filter option must be specified: `--all`, `--products`, `--prs`, `--issues`, or `--report`.
:   Not allowed with a profile argument.

## Directory resolution [changelog-remove-dirs]

Both modes use the same ordered fallback to locate changelog YAML files and existing bundles.

**Changelog files directory** (where changelog YAML files are read from):

| Priority | Profile-based | Option-based |
|----------|---------------|--------------|
| 1 | `bundle.directory` in `changelog.yml` | `--directory` CLI option |
| 2 | Current working directory | `bundle.directory` in `changelog.yml` |
| 3 | — | Current working directory |

**Bundles directory** (scanned for existing bundles during the dependency check):

| Priority | Both modes |
|----------|------------|
| 1 | `--bundles-dir` CLI option (command-option-based only) |
| 2 | `bundle.output_directory` in `changelog.yml` |
| 3 | `{changelog-dir}/bundles` |
| 4 | `{changelog-dir}/../bundles` |

:::{note}
"Current working directory" means the directory you are in when you run the command (`pwd`).
Setting `bundle.directory` and `bundle.output_directory` in `changelog.yml` is recommended so you don't need to rely on running the command from a specific directory.
:::

## Option-based examples

### Remove by GitHub release [changelog-remove-release-version]

You can use `--release-version` to fetch pull request references directly from GitHub release notes and use them as the removal filter.

:::{important}
Only automated GitHub release notes (the default format or [Release Drafter](https://github.com/release-drafter/release-drafter) format) are supported at this time.
:::

This mirrors the equivalent [`--release-version` option on `changelog bundle`](/cli/release/changelog-bundle.md#changelog-bundle-release-version) and is useful when cleaning up after a release-based bundle.
For example:

```sh
docs-builder changelog remove \
  --release-version v1.34.0 \
  --repo apm-agent-dotnet --owner elastic 
```

The repo and owner used to fetch the release follow the same precedence as `changelog bundle`:

- Repo: `--repo` flag > `bundle.repo` in `changelog.yml` (one source is required)
- Owner: `--owner` flag > `bundle.owner` in `changelog.yml` > `elastic`

Use `--dry-run` to preview which files would be deleted before committing:

```sh
docs-builder changelog remove \
  --release-version v1.34.0  \
  --dry-run
```

Pass `latest` to target the most recent release:

```sh
docs-builder changelog remove \
  --release-version latest \
  --dry-run
```

:::{note}
`--release-version` requires a `GITHUB_TOKEN` or `GH_TOKEN` environment variable (or an active `gh` login) to fetch release details from the GitHub API.
:::

## Profile-based examples [changelog-remove-profile]

When a `changelog.yml` configuration file defines `bundle.profiles`, you can use those same profiles with `changelog remove` to remove exactly the changelogs that would be included in a matching bundle.

Profile-based commands discover the changelog configuration automatically (no `--config` flag): they look for `changelog.yml` in the current directory, then `docs/changelog.yml`. If neither file is found, the command returns an error with instructions to run `docs-builder changelog init` or to re-run from the folder where the file exists.

### Profile fields

The `changelog remove` command reads the same `bundle.profiles` configuration as `changelog bundle`, but only a subset of fields are relevant to removal:

| Field | Used by `changelog remove`? | Notes |
|---|---|---|
| `products` | Yes, when filtering by product | Required when the profile argument is a version string and no `source: github_release` is set. Not needed when the filter comes from a promotion report, URL list file, or `source: github_release`. |
| `source` | Yes | `source: github_release` fetches the PR list from the GitHub release to use as the removal filter. |
| `repo` | Yes, with `source: github_release` | Identifies the GitHub repository to fetch the release from. |
| `owner` | Yes, with `source: github_release` | Identifies the GitHub repository owner. |
| `output` | No | Ignored — removal does not write any output files. |
| `output_products` | No | Ignored. |
| `hide_features` | No | Ignored. |

### Remove by product

You can create profiles that are equivalent to the `--products` filter option, that is to say the removal will affect only changelogs with matching `products`.

```yaml
bundle:
  profiles:
    elasticsearch-release:
      products: "elasticsearch {version} {lifecycle}"
      output: "elasticsearch-{version}.yaml"
```

You can remove the matching changelogs with:

```sh
docs-builder changelog remove elasticsearch-release 9.2.0 --dry-run
```

This removes changelogs for `elasticsearch 9.2.0 ga` — the same set that `docs-builder changelog bundle elasticsearch-release 9.2.0` would include. The lifecycle is inferred from the version string: `9.2.0` → `ga`, `9.2.0-beta.1` → `beta`. Refer to [Lifecycle inference for standard profiles](/cli/release/changelog-bundle.md#changelog-bundle-standard-profile-lifecycle) for details.

### Remove by report or URL list

You can also create profiles that are equivalent to the `--prs`, `--issues`, or `--report` filter options.
That is to say the removal will affect only changelogs with matching `prs` or `issues`.

For these profile-based commands, you can pass a promotion report URL, a local `.html` file, or a URL list file as the second argument. The command removes changelogs whose `prs` field matches the PR URLs extracted from the report or file. The following commands perform the same task with and without a profile:

```sh
docs-builder changelog remove serverless-report ./promotion-report.html

docs-builder changelog remove \
  --report ./promotion-report.html
```

Alternatively, use a newline-delimited text file that lists pull request or issue URLs:

```sh
docs-builder changelog remove serverless-report ./prs.txt
```

When you want to use both a version (for `{version}` substitution in the output filename) and a report as the filter, pass both as separate arguments:

```sh
docs-builder changelog remove serverless-report 2026-02-13 ./promotion-report.html
```

### Remove by GitHub release profiles [changelog-remove-github-release-profile]

To make removal by GitHub release more easily repeatable, create a profile with `source: github_release` in your changelog configuration file.
For example:

```yaml
bundle:
  profiles:
    agent-gh-release:
      source: github_release
      repo: apm-agent-dotnet
      owner: elastic 
      output: "agent-{version}.yaml"
```

You can remove the matching changelogs with:

```sh
docs-builder changelog remove agent-gh-release 1.34.0
```

Use `--dry-run` to preview the files that would be deleted before committing:

```sh
docs-builder changelog remove agent-gh-release 1.34.0 --dry-run
```

:::{note}
`source: github_release` profiles require a `GITHUB_TOKEN` or `GH_TOKEN` environment variable (or an active `gh` login) to fetch release details from the GitHub API.
The `repo` and `owner` used to identify the release follow the same precedence as bundling: profile-level `repo`/`owner` override `bundle.repo`/`bundle.owner`, which in turn override the default owner `elastic`.
:::

For the full list of profile configuration fields, go to [Profile configuration fields](/cli/release/changelog-bundle.md#changelog-bundle-profile-config).
