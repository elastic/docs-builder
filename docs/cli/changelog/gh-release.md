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

`--description <string?>`
:   Optional: Bundle description text with placeholder support.
:   Supports `{version}`, `{lifecycle}`, `{owner}`, and `{repo}` placeholders. Overrides `bundle.description` from config.

`--output <string?>`
:   Optional: Output directory for the generated changelog files. Falls back to `bundle.directory` in `changelog.yml` when not specified. Defaults to `./changelogs`.

`--strip-title-prefix`
:   Optional: Remove square brackets and the text within them from the beginning of pull request titles, and also remove a colon if it follows the closing bracket.
:   For example, `"[Inference API] New embedding model support"` becomes `"New embedding model support"`.
:   Multiple bracket prefixes are also supported (for example, `"[Discover][ESQL] Fix filtering"` becomes `"Fix filtering"`).
:   By default, the behavior is determined by the `extract.strip_title_prefix` changelog configuration setting (which defaults to `false`).
`--warn-on-type-mismatch`
:   Optional: Warn when the type inferred from Release Drafter section headers (for example, "Bug Fixes") doesn't match the type derived from the pull request's labels. Defaults to `true`.

## Output

The command creates two types of output in the directory specified by `--output`:

- One YAML changelog file per pull request found in the release notes.
- A bundle file at `{output}/bundles/{version}-{product}-bundle.yml` that references all created changelog files.

The product, target version, and lifecycle are inferred automatically from the release tag and the repository name (via [products.yml](https://github.com/elastic/docs-builder/blob/main/config/products.yml)). For example, a tag of `v9.2.0` on `elastic/elasticsearch` creates changelogs with `product: elasticsearch`, `target: 9.2.0`, and `lifecycle: ga`.

## Configuration

The `rules.bundle` section of your `changelog.yml` applies to bundles created by this command (after changelog files are gathered from the release).
Which fields take effect depends on [bundle rule modes](/contribute/configure-changelogs.md#bundle-rule-modes).
For details, refer to [Rules for filtered bundles](/cli/changelog/bundle.md#changelog-bundle-rules).
If you use per-product rule overrides, refer to [Product-specific bundle rules](/contribute/configure-changelogs.md#rules-bundle-products).

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

### Add description with placeholders

```sh
docs-builder changelog gh-release elasticsearch v9.2.0 \
  --description "Elasticsearch {version} includes new features and fixes. Download: https://github.com/{owner}/{repo}/releases/tag/v{version}"
```

### Strip component prefixes from titles

```sh
docs-builder changelog gh-release elasticsearch v9.2.0 --strip-title-prefix
```
