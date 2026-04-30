Aggregates changelog YAML files matching a filter into a single bundle file. The bundle is the artifact used by the `{changelog}` directive and `docs-builder changelog render` to produce release notes.

The command has **two mutually exclusive modes**. You cannot mix them: supplying a profile name on the command line disables all filter and output flags.

## Profile-based mode

Define reusable profiles in `changelog.yml` and invoke by name. This is the recommended approach for release workflows because the filter, output path, and product metadata are all captured in configuration and don't need to be specified on the command line.

```sh
# Bundle using a named profile (version inferred for {lifecycle} placeholder)
docs-builder changelog bundle elasticsearch-release 9.2.0

# Bundle using a profile with a promotion report as the filter source
docs-builder changelog bundle elasticsearch-release 9.2.0 ./promotion-report.html
```

The second positional argument accepts:
- A version string (e.g. `9.2.0`, `9.2.0-beta.1`) — lifecycle is inferred automatically (`ga`, `beta`, `rc`)
- A promotion report URL or file path
- A plain-text URL list file (one fully-qualified GitHub URL per line)

When your profile uses `{version}` in its output pattern and you also want to filter by a report, pass both arguments.

Example profile in `changelog.yml`:

```yaml
bundle:
  repo: elasticsearch
  owner: elastic
  directory: docs/changelog
  output_directory: docs/releases
  profiles:
    elasticsearch-release:
      products: "elasticsearch {version} {lifecycle}"
      output: "elasticsearch/{version}.yaml"
      output_products: "elasticsearch {version}"
```

## Option-based mode

Supply filter flags directly when you don't have a profile configured or need a one-off bundle.

Exactly one of the following filter flags is required:

- `--all` — include every changelog in the directory
- `--input-products` — match by product, target date, and lifecycle (e.g. `"elasticsearch * *"`)
- `--prs` — filter by PR URLs or a newline-delimited file of PR URLs
- `--issues` — filter by issue URLs or a newline-delimited file of issue URLs
- `--release-version` — fetch PR references from a GitHub release tag (e.g. `v9.2.0` or `latest`)
- `--report` — filter by PRs referenced in a promotion report (URL or local file)

```sh
# Bundle all changelogs in docs/changelog/
docs-builder changelog bundle --all --directory docs/changelog

# Bundle changelogs for a specific product release
docs-builder changelog bundle \
  --input-products "elasticsearch 9.2.0 ga" \
  --output docs/releases/9.2.0.yaml

# Bundle from a GitHub release
docs-builder changelog bundle \
  --release-version v9.2.0 \
  --repo elasticsearch \
  --owner elastic
```

## Resolved vs. reference bundles

By default the bundle contains only file names and checksums — the original changelog files must remain on disk for rendering. Add `--resolve` (or set `bundle.resolve: true` in `changelog.yml`) to embed the full entry content inside the bundle. A resolved bundle is:

- Required when using the `{changelog}` directive after deleting the source changelog files
- Required when `link_allow_repos` is configured (private-link scrubbing only runs during resolve)
- Necessary to regenerate rendered Markdown or AsciiDoc after the source files are removed

:::{tip}
For most release workflows, use `--resolve`. It makes the bundle self-contained and allows you to clean up the changelog files with `docs-builder changelog remove` immediately after bundling.
:::

## CI usage

Pass `--plan` to emit GitHub Actions step outputs (`needs_network`, `needs_github_token`, `output_path`) without generating the bundle. Use this in a planning step to decide whether subsequent steps require a GitHub token or network access.

For full configuration reference, see [Bundle changelogs](/contribute/bundle-changelogs.md).
