---
navigation_title: "changelog gh-release"
---

# changelog gh-release

Create changelog files and a bundle from a GitHub release by parsing pull request references from the release notes.

:::{important}
Only automated GitHub release notes (the default format or [Release Drafter](https://github.com/release-drafter/release-drafter) format) are supported at this time.
:::

For general information about changelogs, go to [](/contribute/changelog.md).

## Usage

```sh
docs-builder changelog gh-release <repo> [version] [options...] [-h|--help]
```

## Arguments

`repo`
:   Required: GitHub repository in `owner/repo` format (for example, `elastic/elasticsearch`) or just the repository name (for example, `elasticsearch`), which defaults to `elastic` as the owner.

`version`
:   Optional: The release tag to fetch (for example, `v9.2.0` or `9.2.0`). Defaults to `latest`.

## Options

`--config <string?>`
:   Optional: Path to the changelog.yml configuration file. Defaults to `docs/changelog.yml`.

`--output <string?>`
:   Optional: Output directory for the generated changelog files. Falls back to `bundle.directory` in `changelog.yml` when not specified. Defaults to `./changelogs`.

`--strip-title-prefix`
:   Optional: Remove square brackets and the text within them from the beginning of pull request titles, and also remove a colon if it follows the closing bracket.
:   For example, `"[Inference API] New embedding model support"` becomes `"New embedding model support"`.
:   Multiple bracket prefixes are also supported (for example, `"[Discover][ESQL] Fix filtering"` becomes `"Fix filtering"`).

`--warn-on-type-mismatch`
:   Optional: Warn when the type inferred from Release Drafter section headers (for example, "Bug Fixes") doesn't match the type derived from the pull request's labels. Defaults to `true`.

## Output

The command creates two types of output in the directory specified by `--output`:

- One YAML changelog file per pull request found in the release notes.
- A bundle file at `{output}/bundles/{version}-{product}-bundle.yml` that references all created changelog files.

The product, target version, and lifecycle are inferred automatically from the release tag and the repository name (via [products.yml](https://github.com/elastic/docs-builder/blob/main/config/products.yml)). For example, a tag of `v9.2.0` on `elastic/elasticsearch` creates changelogs with `product: elasticsearch`, `target: 9.2.0`, and `lifecycle: ga`.

## Configuration

The `rules.bundle` section of your `changelog.yml` applies to bundles created by this command.
Type, area, and product filtering all apply.
For details, refer to [Rules for filtered bundles](/cli/release/changelog-bundle.md#changelog-bundle-rules).
If you use per-product rule overrides and changelogs can belong to multiple products, refer to [Per-product rule resolution for multi-product entries](/contribute/changelog.md#changelog-bundle-multi-product-rules).

## Examples

### Create changelogs from the latest release

```sh
docs-builder changelog gh-release elastic/elasticsearch
```

### Create changelogs from a specific version tag

```sh
docs-builder changelog gh-release elastic/elasticsearch v9.2.0
```

### Use a short repository name

```sh
docs-builder changelog gh-release elasticsearch v9.2.0
```

### Specify a custom output directory

```sh
docs-builder changelog gh-release elasticsearch v9.2.0 \
  --output ./docs/changelog \
  --config ./docs/changelog.yml
```

### Strip component prefixes from titles

```sh
docs-builder changelog gh-release elasticsearch v9.2.0 --strip-title-prefix
```
