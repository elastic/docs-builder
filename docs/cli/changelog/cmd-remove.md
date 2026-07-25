## Description

Remove changelog YAML files from a directory.

Two mutually exclusive modes are available:

- **Profile-based**: `docs-builder changelog remove <profile> <version|promotion-report>` — uses the same `bundle.profiles` configuration as [`changelog bundle`](/cli/changelog/bundle.md) to determine which changelogs to remove.
- **Option-based**: `docs-builder changelog remove --products "..."` (or `--prs`, `--issues`, `--all`, `--release-version`, `--report`, `--files`) — specify the filter directly.

Bundles are self-contained (every entry is embedded inline), so removing changelog source files never affects existing bundles or the `{changelog}` directive. The command deletes matching files without any bundle dependency check.

For more context, go to [](/contribute/bundle-changelogs.md#changelog-remove).

## Directory resolution

Both modes use the same ordered fallback to locate changelog YAML files.

| Priority | Profile-based | Option-based |
|----------|---------------|--------------|
| 1 | `bundle.directory` in `changelog.yml` | `--directory` CLI option |
| 2 | Current working directory | `bundle.directory` in `changelog.yml` |
| 3 | — | Current working directory |

Setting `bundle.directory` in `changelog.yml` is recommended so you don't need to rely on running the command from a specific directory.

## Option-based examples

Exactly one filter must be specified: `--all`, `--products`, `--prs`, `--issues`, `--release-version`, `--report`, or `--files`.

```sh
# Preview what would be removed (dry run)
docs-builder changelog remove --products "elasticsearch 9.3.0 *" --dry-run

# Remove by GitHub release tag
docs-builder changelog remove \
  --release-version v1.34.0 \
  --repo apm-agent-dotnet --owner elastic

# Preview using the latest release
docs-builder changelog remove --release-version latest --dry-run

# Remove an explicit list of changelog files
docs-builder changelog remove --files "./docs/changelog/a.yaml,./docs/changelog/b.yaml" --dry-run
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
