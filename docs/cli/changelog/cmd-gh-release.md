## Description

Create changelog files and a bundle from a GitHub release by parsing pull request references from the release notes.

:::{important}
Only automated GitHub release notes (the default format or [Release Drafter](https://github.com/release-drafter/release-drafter) format) are supported at this time.
:::

For general information about changelogs, go to [](/data/release-notes/overview.md).

## Output

The command creates two types of output in the directory specified by `--output`:

- One YAML changelog file per pull request found in the release notes.
- A bundle file at `{output}/bundles/{version}-{product}-bundle.yml` that references all created changelog files.

The product, target version, and lifecycle are inferred automatically from the release tag and the repository name (via [products.yml](https://github.com/elastic/docs-builder/blob/main/config/products.yml)). For example, a tag of `v9.2.0` on `elastic/elasticsearch` creates changelogs with `product: elasticsearch`, `target: 9.2.0`, and `lifecycle: ga`.

## Entry sourcing precedence

For each pull request found in the release notes, the command follows the same fidelity ladder as commit-range bundling:

1. **A checked-in changelog entry wins.** If an entry for the PR already exists in the repository's entry pool (uploaded via `changelog-upload`), it is used verbatim — matched by file-name-derived PR numbers (file names survive scrubbing) or by its `prs` references.
2. **Otherwise an entry is synthesized from PR metadata**: release-note text from the PR body becomes the description (the same extraction path `changelog add` uses, controlled by `extract.release_notes`), and linked issues are carried over (`extract.issues`).
3. **Title/link-only** is the last resort when the PR body carries no release-note text.

When the entry pool cannot be reached, the command warns and falls back to synthesis, so repositories that never upload individual entries keep working.

## Configuration

The `rules.bundle` section of your `changelog.yml` applies to bundles created by this command.
For details, refer to [](/data/release-notes/configure-ref.md#rules-bundle).

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
