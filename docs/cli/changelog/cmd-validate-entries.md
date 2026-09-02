## Description

:::{note}
This command is intended for CI automation. It is used internally by the changelog GitHub Actions and is not typically invoked directly by users.
:::

Validate the content of changelog entry files that a pull request added or modified. Unlike `changelog evaluate-pr`, this command focuses exclusively on the YAML content of changelog entry files — not on which labels the PR carries or whether a changelog file exists at all. It runs four groups of checks:

1. **Schema and required fields** — YAML parses, required fields (`title`, `type`, `products`) are present.
2. **Config-value membership** — `type`, `subtype`, `areas`, and `lifecycle` values are recognised by the changelog configuration.
3. **PR existence** — Own-repo PR references point to pull requests that actually exist (checked via GraphQL).
4. **Entry hygiene** — `note-*` prefix files are excluded, `source-redirect` cannot be authored, `versions` cannot appear in entry files.

Exits non-zero when any finding has `Error` severity. Warnings are logged but do not block.

When running under GitHub Actions (the `GITHUB_ACTIONS` environment variable is set) and `--pr-number` is provided, the command writes a decision metadata file to `.artifacts/changelog-decision/metadata.json`. This file is picked up by the downstream `changelog github-comment` command to post or update the sticky PR comment.

## File discovery

When `--files` is not supplied, the command discovers changed files by calling the GitHub API (`GET /repos/{owner}/{repo}/pulls/{pr-number}/files`). Only files that are directly inside the configured changelog directory (no subdirectories), end in `.yaml` or `.yml`, and do not start with `note-` are validated.

Supply `--files` to bypass API discovery and validate a known set of files — useful for local runs or custom CI setups.

## Decision metadata

When `--pr-number` is supplied and the command runs under GitHub Actions, it writes `.artifacts/changelog-decision/metadata.json` relative to the checkout root. The metadata carries the gate (`entries`), PR number, head ref/SHA, and a list of findings (file, severity, message). A consumer workflow uploads this file as the `changelog-decision` artifact and a `workflow_run` job picks it up to call `changelog github-comment`.

## Examples

```sh
# CI mode: discover changed files from the GitHub API
docs-builder changelog validate-entries \
  --config docs/changelog.yml \
  --owner elastic \
  --repo my-repo \
  --pr-number 42 \
  --pr-labels "enhancement,Team:Core" \
  --head-ref feature-branch \
  --head-sha abc123

# Local mode: validate specific files without GitHub API access
docs-builder changelog validate-entries \
  --config docs/changelog.yml \
  --owner elastic \
  --repo my-repo \
  --pr-number 0 \
  --pr-labels "" \
  --files docs/changelog/42.yaml
```
