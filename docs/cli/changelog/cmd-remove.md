## Description

Remove changelog YAML files from a directory.

Two mutually exclusive modes are available:

- **Profile-based**: `docs-builder changelog remove <profile> <version|promotion-report>` — uses the same `bundle.profiles` configuration as [`changelog bundle`](/cli/changelog/bundle.md) to determine which changelogs to remove.
- **Option-based**: `docs-builder changelog remove --products "..."` (or `--prs`, `--issues`, `--all`, `--release-version`, `--report`) — specify the filter directly.

Before deleting anything, the command checks whether any matching files are referenced by unresolved bundles, to prevent silently breaking the `{changelog}` directive.

For more context, go to [](/contribute/bundle-changelogs.md#changelog-remove).

## Bundle dependency check

The dependency scan considers the **effective** entry list for each parent bundle: parent `entries` merged with amend sidecars (`{bundle}.amend-N.yaml`) in numeric order. Within each amend file, `exclude-entries` are applied before additions, matching how bundles are loaded for [`changelog render`](/cli/changelog/render.md) and the `{changelog}` directive.

A changelog file blocks deletion only when it still appears in that effective list as an **unresolved** entry (the bundle references the file by name and checksum rather than embedding inline title and type). Resolved entries do not require the source file on disk.

If you removed an entry from a bundle with [`changelog bundle-amend --remove`](/cli/changelog/bundle-amend.md), the corresponding `exclude-entries` record drops it from the effective list, so `changelog remove` can delete the source file even when the parent bundle still lists it.

Amend sidecar files are not scanned as parent bundles themselves—only the parent bundle file plus its amend chain is evaluated. If an amend file cannot be parsed, the command logs a warning and uses the parent bundle entries only for that dependency check.

## Directory resolution

Both modes use the same ordered fallback to locate changelog YAML files and existing bundles.

**Changelog files directory:**

| Priority | Profile-based | Option-based |
|----------|---------------|--------------|
| 1 | `bundle.directory` in `changelog.yml` | `--directory` CLI option |
| 2 | Current working directory | `bundle.directory` in `changelog.yml` |
| 3 | — | Current working directory |

**Bundles directory** (scanned during the dependency check):

| Priority | Both modes |
|----------|------------|
| 1 | `--bundles-dir` CLI option (option-based only) |
| 2 | `bundle.output_directory` in `changelog.yml` |
| 3 | `{changelog-dir}/bundles` |
| 4 | `{changelog-dir}/../bundles` |

Setting `bundle.directory` and `bundle.output_directory` in `changelog.yml` is recommended so you don't need to rely on running the command from a specific directory.

## Option-based examples

Exactly one filter must be specified: `--all`, `--products`, `--prs`, `--issues`, `--release-version`, or `--report`.

```sh
# Preview what would be removed (dry run)
docs-builder changelog remove --products "elasticsearch 9.3.0 *" --dry-run

# Remove by GitHub release tag
docs-builder changelog remove \
  --release-version v1.34.0 \
  --repo apm-agent-dotnet --owner elastic

# Preview using the latest release
docs-builder changelog remove --release-version latest --dry-run
```

:::{note}
`--release-version` requires a `GITHUB_TOKEN` or `GH_TOKEN` environment variable (or an active `gh` login) to fetch release details from the GitHub API.
:::

The `--products` filter supports wildcards:

- `"elasticsearch 9.3.0 ga"` — exact match
- `"elasticsearch * *"` — all elasticsearch changelogs
- `"* 9.3.* *"` — any product with a target starting with `9.3.`
- `"* * *"` — all changelogs (equivalent to `--all`)

## Profile-based examples

When `changelog.yml` defines `bundle.profiles`, use those same profiles with `changelog remove` to remove exactly the changelogs that would be included in a matching bundle.

Profile-based commands discover the changelog configuration automatically: they look for `changelog.yml` in the current directory, then `docs/changelog.yml`.

Refer to [](/contribute/bundle-changelogs.md#changelog-remove) for examples.
