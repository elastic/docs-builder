## Description

:::{note}
This command is intended for CI automation. It is used internally by the changelog GitHub Actions and is not typically invoked directly by users.
:::

Evaluate a pull request for changelog generation eligibility. Performs pre-flight checks (body-only edit, bot loop detection, manual edit detection), loads the changelog configuration, checks label-based creation rules, resolves the PR title and type, and sets GitHub Actions outputs for downstream steps.

## GitHub Actions outputs

| Output | Description |
|--------|-------------|
| `status` | Evaluation result: `skipped`, `manually-edited`, `no-title`, `no-label`, or `proceed` |
| `should-generate` | `true` if `changelog add` should run |
| `should-upload` | `true` if the artifact should be uploaded |
| `title` | Resolved PR title |
| `description` | Release note extracted from the PR body (when `extract.release_notes` is enabled and a release note is found). Long or multi-line release notes (over 120 characters) are placed here. Passed downstream as `CHANGELOG_DESCRIPTION` for `changelog add`. |
| `type` | Resolved changelog type |
| `products` | Comma-separated product specs resolved from PR labels via `pivot.products` mappings |
| `label-table` | Markdown table of configured label-to-type mappings |
| `product-label-table` | Markdown table of configured label-to-product mappings |
| `existing-changelog-filename` | Filename of a previously committed changelog for this PR (if any) |

## Environment variables

| Variable | Purpose |
|----------|---------|
| `GITHUB_TOKEN` | GitHub API authentication for bot-commit and manual-edit detection |

## Examples

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
