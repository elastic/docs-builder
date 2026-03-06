---
navigation_title: "changelog evaluate-artifact"
---

# changelog evaluate-artifact

:::{note}
This command is intended for CI automation. It is used internally by the changelog GitHub Actions and is not typically invoked directly by users.
:::

Evaluate a downloaded changelog artifact in the submit workflow.
Reads `metadata.json`, fetches the current PR state from the GitHub API, validates the head SHA, re-checks PR labels against the creation rules embedded in the metadata, and sets GitHub Actions outputs for downstream steps (commit, comment).

## Usage

```sh
docs-builder changelog evaluate-artifact [options...] [-h|--help]
```

## Options

`--metadata <string>`
:   Path to the downloaded `metadata.json` file.

`--owner <string>`
:   GitHub repository owner.

`--repo <string>`
:   GitHub repository name.

`--comment-only`
:   Post changelog as a PR comment instead of committing.
:   Default: `false`

## GitHub Actions outputs

| Output | Description |
|--------|-------------|
| `pr-number` | Pull request number (from metadata) |
| `head-ref` | PR head branch ref |
| `head-sha` | PR head commit SHA |
| `status` | Artifact status |
| `config-file` | Path to `changelog.yml` (from metadata) |
| `changelog-dir` | Changelog directory path |
| `label-table` | Markdown label-to-type table (for failure comments) |
| `should-commit` | `true` if the changelog should be committed to the PR branch |
| `should-comment-success` | `true` if a success comment should be posted (fork or comment-only mode) |
| `should-comment-failure` | `true` if a failure comment should be posted (e.g., missing type label) |

## Environment variables

| Variable | Purpose |
|----------|---------|
| `GITHUB_TOKEN` | GitHub API authentication for fetching current PR state |

## Examples

Evaluate a downloaded artifact:

```sh
docs-builder changelog evaluate-artifact \
  --metadata /tmp/changelog-result/metadata.json \
  --owner elastic \
  --repo elasticsearch
```

Evaluate in comment-only mode (for fork PRs or dry-run):

```sh
docs-builder changelog evaluate-artifact \
  --metadata /tmp/changelog-result/metadata.json \
  --owner elastic \
  --repo elasticsearch \
  --comment-only
```
