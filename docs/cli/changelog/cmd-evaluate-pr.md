## Description

:::{note}
This command is intended for CI automation. It is used internally by the changelog GitHub Actions and is not typically invoked directly by users.
:::

Evaluate a pull request for changelog generation eligibility. Performs pre-flight checks (body-only edit, bot loop detection, manual edit detection), loads the changelog configuration, checks label-based creation rules, resolves the PR title and type, and sets GitHub Actions outputs for downstream steps.

When running under GitHub Actions (the `GITHUB_ACTIONS` environment variable is set), the command writes a decision metadata file to `.artifacts/changelog-decision/metadata.json`. This file is picked up by the downstream `changelog github-comment` command to post or update the sticky PR comment. Pass `--is-fork`, `--can-commit`, and `--maintainer-can-modify` so the comment command can select the correct comment body for fork and comment-only PR strategies.

## GitHub Actions outputs

| Output | Description |
|--------|-------------|
| `status` | Evaluation result: `skipped`, `manually-edited`, `no-title`, `no-label`, `missing-entry`, `missing-impact-action`, `conflicting-impact-action`, or `proceed` |
| `should-generate` | `true` if `changelog add` should run |
| `title` | Resolved PR title |
| `description` | Release note extracted from the PR body (when `extract.release_notes` is enabled and a release note is found). Long or multi-line release notes (over 120 characters) are placed here. Passed downstream as `CHANGELOG_DESCRIPTION` for `changelog add`. |
| `impact` | Content of the `## Impact` section (or `Impact:` label) in the PR body. Passed downstream as `CHANGELOG_IMPACT` for `changelog add`. |
| `action` | Content of the `## Action` section (or `Action:` label) in the PR body. Passed downstream as `CHANGELOG_ACTION` for `changelog add`. |
| `type` | Resolved changelog type |
| `products` | Comma-separated product specs resolved from PR labels via `pivot.products` mappings |
| `label-table` | Markdown table of configured label-to-type mappings |
| `product-label-table` | Markdown table of configured label-to-product mappings |
| `changelog-dir` | Resolved changelog directory (from `bundle.directory` or default `docs/changelog`) |
| `existing-changelog-filename` | Filename of a previously committed changelog for this PR (if any) |
| `skip-labels` | Comma-separated list of configured skip labels (from `rules.create` exclude rules) |

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
  --strip-title-prefix \
  --require-changelog-file
```

Pass `--require-changelog-file` to fail the PR (`missing-entry`) when no changelog entry file exists for the PR number. The entry file is looked up in `bundle.directory` (default `docs/changelog`). This flag is designed to be passed as a workflow input rather than hardcoded in `changelog.yml`.

### Require Impact and Action for breaking changes [require-impact-action]

Impact and Action come from one source, in this order of precedence:

1. The changelog entry file for the PR, when there is one. Its `impact:` and `action:` fields win.
2. The PR description, when there is no entry file.

A breaking-change PR that has an entry file and also has `## Impact` or `## Action` in its description fails with `conflicting-impact-action` when a section does not match the file's field exactly, whether or not `--require-impact-action` is set. Leading and trailing whitespace and line-ending style are ignored. The comment asks the author to keep them in one place or make them match.

Pass `--require-impact-action` to fail the PR (`missing-impact-action`) when its resolved type is `breaking-change` and its source lacks an Impact and/or Action. With an entry file that means a missing `impact:` or `action:` field. Without one it means a missing `Impact` or `Action` section in the PR description. Recognized forms are a markdown heading of any level (`## Impact`, `### Action`) or a label (`Impact: ...`). Sections that contain only HTML comments, such as an unfilled PR template, count as missing. Like `--require-changelog-file`, this flag is designed to be passed as a workflow input.

The sticky PR comment lists the missing sections with a template to paste into the description. Once the sections are present, `evaluate-pr` sets the `impact` and `action` outputs. When Impact or Action is scraped from the PR description, the comment stays on the PR and echoes the scraped text in a collapsible block, so you can check it before bundling. Nothing is echoed when the values come from an entry file, since the file is already part of the PR. `evaluate-pr` records the captured sections in the decision metadata, so the echo works even when no entry file is generated in the same run.
