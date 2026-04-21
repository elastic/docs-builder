---
navigation_title: "changelog evaluate-pr"
---

# changelog evaluate-pr

:::{note}
This command is intended for CI automation. It is used internally by the changelog GitHub Actions and is not typically invoked directly by users.
:::

Evaluate a pull request for changelog generation eligibility.
Performs pre-flight checks (body-only edit, bot loop detection, manual edit detection), loads the changelog configuration, checks label-based creation rules, resolves the PR title and type, and sets GitHub Actions outputs for downstream steps.

## Usage

```sh
docs-builder changelog evaluate-pr [options...] [-h|--help]
```

## Options

`--config <string>`
:   Path to the `changelog.yml` configuration file.

`--owner <string>`
:   GitHub repository owner.

`--repo <string>`
:   GitHub repository name.

`--pr-number <int>`
:   Pull request number.

`--pr-title <string>`
:   Pull request title.

`--pr-labels <string>`
:   Comma-separated PR labels.

`--head-ref <string>`
:   PR head branch ref.

`--head-sha <string>`
:   PR head commit SHA.

`--event-action <string>`
:   GitHub event action (e.g., `opened`, `synchronize`, `edited`).

`--title-changed`
:   Whether the PR title changed (for `edited` events).
:   Default: `false`

`--strip-title-prefix`
:   Remove square-bracket prefixes from the PR title (e.g., `[Inference API] Title` becomes `Title`).
:   Default: `false`

`--bot-name <string>`
:   Bot login name for loop detection.
:   Default: `github-actions[bot]`

## GitHub Actions outputs

| Output | Description |
|--------|-------------|
| `status` | Evaluation result: `skipped`, `manually-edited`, `no-title`, `no-label`, or `proceed` |
| `should-generate` | `true` if `changelog add` should run |
| `should-upload` | `true` if the artifact should be uploaded |
| `title` | Resolved PR title |
| `description` | Release note extracted from the PR body (when `extract.release_notes` is enabled and a release note is found). Long or multi-line release notes (>120 characters) are placed here. Passed downstream as `CHANGELOG_DESCRIPTION` for `changelog add`. |
| `type` | Resolved changelog type |
| `products` | Comma-separated product specs resolved from PR labels via `pivot.products` mappings (e.g., `cloud-hosted, cloud-serverless`) |
| `label-table` | Markdown table of configured label-to-type mappings |
| `product-label-table` | Markdown table of configured label-to-product mappings |
| `existing-changelog-filename` | Filename of a previously committed changelog for this PR (if any) |

## Environment variables

| Variable | Purpose |
|----------|---------|
| `GITHUB_TOKEN` | GitHub API authentication for bot-commit and manual-edit detection |

## Examples

Evaluate PR #42 in the `elastic/elasticsearch` repository:

```sh
        docs-builder changelog evaluate-pr \
  --config docs/changelog.yml \
  --owner elastic \
  --repo elasticsearch \
  --pr-number 42 \
  --pr-title "Add new feature" \
  --pr-labels "enhancement,Team:Core" \
  --head-ref feature-branch \
  --head-sha abc123 \
  --event-action opened \
  --strip-title-prefix
```
