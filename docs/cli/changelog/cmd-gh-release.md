## Description

Create changelog files and a bundle from a GitHub release by parsing pull request references from the release notes.

:::{important}
Only automated GitHub release notes (the default format or [Release Drafter](https://github.com/release-drafter/release-drafter) format) are supported at this time.
:::

For general information about changelogs, go to [](/contribute/changelog.md).

## Output

The command creates two types of output in the directory specified by `--output`:

- One YAML changelog file per pull request found in the release notes.
- A bundle file at `{output}/bundles/{version}-{product}-bundle.yml` that references all created changelog files.

The product, target version, and lifecycle are inferred automatically from the release tag and the repository name (via [products.yml](https://github.com/elastic/docs-builder/blob/main/config/products.yml)). For example, a tag of `v9.2.0` on `elastic/elasticsearch` creates changelogs with `product: elasticsearch`, `target: 9.2.0`, and `lifecycle: ga`.

## Configuration

The `rules.bundle` section of your `changelog.yml` applies to bundles created by this command.
For details, refer to [](/contribute/configure-changelogs-ref.md#rules-bundle).

## Examples

```sh
# Latest release
docs-builder changelog gh-release elastic/elasticsearch

# Specific version tag
docs-builder changelog gh-release elastic/elasticsearch v9.2.0

# Short repository name (defaults to elastic/ owner)
docs-builder changelog gh-release elasticsearch v9.2.0

# Custom output directory
docs-builder changelog gh-release elasticsearch v9.2.0 \
  --output ./docs/changelog \
  --config ./docs/changelog.yml

# Description with placeholders
docs-builder changelog gh-release elasticsearch v9.2.0 \
  --description "Elasticsearch {version} release. Download: https://github.com/{owner}/{repo}/releases/tag/v{version}"
```
