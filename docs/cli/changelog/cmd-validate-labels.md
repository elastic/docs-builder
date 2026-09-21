## Description

:::{note}
This command is intended for CI automation. It is used internally by the changelog GitHub Actions and is not typically invoked directly by users.
:::

Validate that a pull request's labels contain a recognised changelog type label, and optionally a product label. Unlike `changelog evaluate-pr`, this command performs no GitHub API access, no title resolution, no bot-loop detection, and no manual-edit detection — it only resolves labels against the configured `pivot.types`, `pivot.products`, and `rules.create` settings. This makes it safe to run on `pull_request` events from forks without write permissions.

Exits non-zero when `status` is `no-label`. All other statuses (`ok`, `skipped`) exit zero.

When running under GitHub Actions (the `GITHUB_ACTIONS` environment variable is set) and `--pr-number` is provided, the command writes a decision metadata file to `.artifacts/changelog-decision/metadata.json`. This file is picked up by the downstream `changelog github-comment` command to post or update the sticky PR comment.

## GitHub Actions outputs

| Output | Description |
|--------|-------------|
| `status` | Validation result: `ok`, `no-label`, or `skipped` |
| `type` | Resolved changelog type (when `ok`) |
| `products` | Comma-separated product specs resolved from PR labels (when resolved) |
| `label-table` | Markdown table of configured label-to-type mappings (when `no-label`) |
| `product-label-table` | Markdown table of configured label-to-product mappings (when `no-label` due to missing product) |
| `skip-labels` | Comma-separated list of configured skip labels (from `rules.create` exclude rules) |

## Decision metadata

When `--pr-number` is supplied and the command runs under GitHub Actions, it writes `.artifacts/changelog-decision/metadata.json` relative to the checkout root. The file contains the PR number, head ref/SHA, validation status, and label tables. A consumer workflow uploads this file as the `changelog-decision` artifact and a `workflow_run` job picks it up to call `changelog github-comment`.

## Examples

```sh
docs-builder changelog validate-labels \
  --config docs/changelog.yml \
  --pr-labels "enhancement,Team:Core" \
  --pr-number 42 \
  --head-ref feature-branch \
  --head-sha abc123
```
