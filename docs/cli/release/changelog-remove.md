# changelog remove

Remove changelog YAML files from a directory.

You can use either profile-based removal or raw filter flags:

- **Profile-based**: `docs-builder changelog remove <profile> <version|promotion-report>` — uses the same `bundle.profiles` configuration as [`changelog bundle`](/cli/release/changelog-bundle.md) to determine which changelogs to remove.
- **Option-based**: `docs-builder changelog remove --products "..." ` (or `--prs`, `--issues`, `--all`) — specify the filter directly.

These modes are mutually exclusive. You can't combine a profile argument with option-based flags.

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
:   Mutually exclusive with `--all`, `--products`, `--prs`, `--issues`, `--report`, `--owner`, `--repo`, `--config`, `--directory`, and `--bundles-dir`.

`[1] <string?>`
:   Version number, promotion report URL/path, or URL list file.
:   For example, `9.2.0`, `https://buildkite.../promotion-report.html`, or `/path/to/prs.txt`.
:   See [Profile argument types](/cli/release/changelog-bundle.md#profile-argument-types) for details on accepted formats.

`[2] <string?>`
:   Optional: Promotion report URL/path or URL list file when the second argument is a version string.
:   When provided, `[1]` must be a version string and `[2]` is the PR/issue filter source.
:   For example, `docs-builder changelog remove serverless-release 2026-02 ./promotion-report.html`.

## Options

`--all`
:   Remove all changelog files in the directory.
:   Exactly one filter option must be specified: `--all`, `--products`, `--prs`, or `--issues`.
:   Not allowed with a profile argument.

`--bundles-dir <string?>`
:   Optional: Override the directory scanned for bundles during the dependency check.
:   When not specified, the directory is discovered automatically from config or fallback paths.
:   Not allowed with a profile argument. In profile mode, the bundles directory is derived from `bundle.output_directory` in the changelog configuration.

`--config <string?>`
:   Optional: Path to the changelog configuration file.
:   Defaults to `docs/changelog.yml`.
:   Not allowed with a profile argument. In profile mode, the configuration is discovered automatically.

`--directory <string?>`
:   Optional: The directory that contains the changelog YAML files.
:   When not specified, uses `bundle.directory` from the changelog configuration if set, otherwise the current directory.
:   Not allowed with a profile argument. In profile mode, the directory is derived from `bundle.directory` in the changelog configuration.

`--dry-run`
:   Print the files that would be removed and any bundle dependency conflicts, without deleting anything.
:   Valid in both profile and option-based mode.

`--force`
:   Proceed with removal even when files are referenced by unresolved bundles.
:   Emits a warning per dependency instead of blocking.
:   Valid in both profile and option-based mode.

`--issues <string[]?>`
:   Filter by issue URLs (comma-separated), or a path to a newline-delimited file.
:   Can be specified multiple times.
:   Exactly one filter option must be specified: `--all`, `--products`, `--prs`, `--issues`, or `--report`.
:   When using a file, every line must be a fully-qualified GitHub issue URL. Bare numbers and short forms are not allowed in files.
:   Not allowed with a profile argument.

`--owner <string?>`
:   The GitHub repository owner, which is required when pull requests or issues are specified as numbers.
:   Not allowed with a profile argument.

`--products <List<ProductInfo>?>`
:   Filter by products in format `"product target lifecycle, ..."`
:   Exactly one filter option must be specified: `--all`, `--products`, `--prs`, or `--issues`.
:   Not allowed with a profile argument.
:   All three parts (product, target, lifecycle) are required but can be wildcards (`*`). Multiple comma-separated values are combined with OR: a changelog is removed if it matches any of the specified product/target/lifecycle combinations. For example:

- `"elasticsearch 9.3.0 ga"` — exact match
- `"cloud-serverless 2025-12-02 ga, cloud-serverless 2025-12-06 beta"` — remove changelogs for either cloud-serverless 2025-12-02 ga or cloud-serverless 2025-12-06 beta
- `"elasticsearch * *"` — all elasticsearch changelogs
- `"* 9.3.* *"` — any product with a target starting with `9.3.`
- `"* * *"` — all changelogs (equivalent to `--all`)

`--prs <string[]?>`
:   Filter by pull request URLs (comma-separated), or a path to a newline-delimited file.
:   Can be specified multiple times.
:   Exactly one filter option must be specified: `--all`, `--products`, `--prs`, `--issues`, or `--report`.
:   When using a file, every line must be a fully-qualified GitHub PR URL. Bare numbers and short forms are not allowed in files.
:   Not allowed with a profile argument.

`--repo <string?>`
:   The GitHub repository name, which is required when pull requests or issues are specified as numbers.
:   Not allowed with a profile argument.

`--report <string?>`
:   Filter by pull requests extracted from a promotion report. Accepts a URL or a local file path.
:   Exactly one filter option must be specified: `--all`, `--products`, `--prs`, `--issues`, or `--report`.
:   Not allowed with a profile argument.

## Profile-based removal [changelog-remove-profile]

When a `changelog.yml` configuration file defines `bundle.profiles`, you can use those same profiles with `changelog remove` to remove exactly the changelogs that would be included in a matching bundle.

Profile-based commands discover the changelog configuration automatically (no `--config` flag): they look for `changelog.yml` in the current directory, then `docs/changelog.yml`. If neither file is found, the command returns an error with instructions to run `docs-builder changelog init` or to re-run from the folder where the file exists.

Only the `products` field from a profile is used for removal. The `output`, `output_products`, `repo`, `owner`, and `hide_features` fields are bundle-specific and are ignored.

For example, if your configuration file defines:

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

This removes changelogs for `elasticsearch 9.2.0 ga` — the same set that `docs-builder changelog bundle elasticsearch-release 9.2.0` would include.

You can also pass a promotion report URL or file path as the second argument, in which case the command removes changelogs whose PR URLs appear in the report:

```sh
docs-builder changelog remove elasticsearch-release https://buildkite.../promotion-report.html
```

When using a profile with `{version}` in the `output` or `output_products` pattern, pass the version as the second argument and the report as the third:

```sh
docs-builder changelog remove serverless-release 2026-02 ./promotion-report.html
```

Or with a URL list file:

```sh
docs-builder changelog remove serverless-release 2026-02 ./prs.txt
```

For option-based removal with a promotion report:

```sh
docs-builder changelog remove \
  --report https://buildkite.../promotion-report.html \
  --directory ./docs/changelog
```
