## Description

:::{note}
This command is intended for CI automation. It is used internally by the changelog GitHub Actions and is not typically invoked directly by users.
:::

Validate that a pull request's labels contain a recognised changelog type label, and optionally a product label. Unlike `changelog evaluate-pr`, this command performs no GitHub API access, no title resolution, no bot-loop detection, and no manual-edit detection — it only resolves labels against the configured `pivot.types`, `pivot.products`, and `rules.create` settings. This makes it safe to run on `pull_request` events from forks without write permissions.

Exits non-zero when `status` is `no-label`. All other statuses (`ok`, `skipped`) exit zero.

## GitHub Actions outputs

| Output | Description |
|--------|-------------|
| `status` | Validation result: `ok`, `no-label`, or `skipped` |
| `type` | Resolved changelog type (when `ok`) |
| `products` | Comma-separated product specs resolved from PR labels (when resolved) |
| `label-table` | Markdown table of configured label-to-type mappings (when `no-label`) |
| `product-label-table` | Markdown table of configured label-to-product mappings (when `no-label` due to missing product) |
| `skip-labels` | Comma-separated list of configured skip labels (from `rules.create` exclude rules) |

## Examples

```sh
docs-builder changelog validate-labels \
  --config docs/changelog.yml \
  --pr-labels "enhancement,Team:Core"
```
