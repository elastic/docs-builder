## Description

Validate changelog files that a pull request added or modified. Currently validates changelog entry files (`.yaml`/`.yml` files under the changelog directory), running four groups of checks:

1. **Schema and required fields** — YAML parses, required fields (`title`, `type`, `products`) are present.
2. **Config-value membership** — `type`, `subtype`, `areas`, and `lifecycle` values are recognised by the changelog configuration.
3. **PR existence** — Own-repo PR references point to pull requests that actually exist (checked via GraphQL).
4. **Entry hygiene** — `source-redirect` cannot be authored, `versions` cannot appear in entry files.

Exits non-zero when any finding has `Error` severity. Warnings are logged but do not block.

Pass `--require-changelog-file` to also fail when no changelog entry file references this PR — combining file-presence and content validation in a single step.

When running under GitHub Actions (the `GITHUB_ACTIONS` environment variable is set) and `--pr-number` is provided, the command writes a decision metadata file to `.artifacts/changelog-decision/metadata.json`. This file is picked up by the downstream `changelog github-comment` command to post or update the sticky PR comment.

## File discovery

When `--files` is not supplied, the command discovers changed files by calling the GitHub API (`GET /repos/{owner}/{repo}/pulls/{pr-number}/files`). Only files directly inside the configured changelog directory (no subdirectories), ending in `.yaml` or `.yml`, and not starting with `note-` are validated.

Supply `--files` to bypass API discovery and validate a known set of files — useful for local runs or custom CI setups.

## Examples

```sh
# CI mode: discover changed files from the GitHub API
docs-builder changelog validate \
  --config docs/changelog.yml \
  --owner elastic \
  --repo my-repo \
  --pr-number 42 \
  --pr-labels "enhancement,Team:Core" \
  --head-ref feature-branch \
  --head-sha abc123

# With file-presence enforcement
docs-builder changelog validate \
  --config docs/changelog.yml \
  --owner elastic \
  --repo my-repo \
  --pr-number 42 \
  --pr-labels "bug" \
  --require-changelog-file

# Local mode: validate specific files without GitHub API access
docs-builder changelog validate \
  --config docs/changelog.yml \
  --owner elastic \
  --repo my-repo \
  --pr-number 0 \
  --pr-labels "" \
  --files docs/changelog/42.yaml
```
