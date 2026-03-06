---
navigation_title: "changelog prepare-artifact"
---

# changelog prepare-artifact

:::{note}
This command is intended for CI automation. It is used internally by the changelog GitHub Actions and is not typically invoked directly by users.
:::

Package the changelog artifact for cross-workflow transfer.
Resolves the final status from the `evaluate-pr` and `changelog add` outcomes, copies the generated YAML file (if any), writes `metadata.json`, and sets GitHub Actions outputs.

This command always exits with code 0 so the subsequent artifact upload step runs regardless of the internal status.

## Usage

```sh
docs-builder changelog prepare-artifact [options...] [-h|--help]
```

## Options

`--staging-dir <string>`
:   Directory where `changelog add` wrote the generated YAML.

`--output-dir <string>`
:   Directory to write the artifact (`metadata.json` + YAML).

`--evaluate-status <string>`
:   Status output from the `evaluate-pr` step.

`--generate-outcome <string>`
:   Outcome of the `changelog add` step (`success`, `failure`, or `skipped`).

`--pr-number <int>`
:   Pull request number.

`--head-ref <string>`
:   PR head branch ref.

`--head-sha <string>`
:   PR head commit SHA.

`--label-table <string?>`
:   Optional: markdown label table from `evaluate-pr`.

`--config <string?>`
:   Optional: path to `changelog.yml` (used to extract creation rules for metadata).

## GitHub Actions outputs

| Output | Description |
|--------|-------------|
| `status` | Final artifact status: `success`, `no-label`, `no-title`, `error`, `skipped`, or `manually-edited` |

## File outputs

| File | Condition |
|------|-----------|
| `{output-dir}/metadata.json` | Always written |
| `{output-dir}/{pr_number}.yaml` | Only when the changelog was generated successfully |

## Examples

Package a successful changelog generation:

```sh
docs-builder changelog prepare-artifact \
  --staging-dir /tmp/changelog-staging \
  --output-dir /tmp/changelog-result \
  --evaluate-status proceed \
  --generate-outcome success \
  --pr-number 42 \
  --head-ref feature-branch \
  --head-sha abc123 \
  --config docs/changelog.yml
```
