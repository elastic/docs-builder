## Description

Validate changelog entry files that a PR added or modified. Checks YAML validity, required fields, config-value membership, PR-number existence, and entry hygiene. Exits non-zero on any error-level finding; warnings do not block.

```sh
# Simplest local usage — owner/repo inferred from git remote, config from docs/changelog.yml
docs-builder changelog validate 4009
```

Pass `--require` to also fail when no entry file references this PR number, combining file-presence and content validation in a single command.

When running under GitHub Actions and `--head-sha` is provided, writes decision metadata to `.artifacts/changelog-decision/metadata.json` for the downstream `changelog github-comment` step.

## Owner, repo, and config resolution

| Value | Precedence |
|---|---|
| `--owner` | CLI flag → git remote origin |
| `--repo` | CLI flag → git remote origin |
| `--config` | CLI flag → `docs/changelog.yml` in the git root |

For local use you rarely need to supply any of these. For CI the runner's checkout has no git remote, so pass them explicitly.

## File discovery

Without `--files`, the command calls the GitHub API (`GET /repos/{owner}/{repo}/pulls/{pr}/files`) to list changed files and filters to changelog entry files (top-level `.yaml`/`.yml` under the changelog directory, not `note-*`). A `GITHUB_TOKEN` is needed for this. Supply `--files` to bypass API discovery entirely.

## Examples

```sh
# Local: everything inferred
docs-builder changelog validate 4009

# Local: also assert an entry file references this PR
docs-builder changelog validate 4009 --require

# Local: validate a specific file without a GitHub token
docs-builder changelog validate 4009 --files docs/changelog/4009.yaml

# CI: explicit owner/repo/labels (git remote not available in the runner)
docs-builder changelog validate "$PR_NUMBER" \
  --config "$CONFIG" \
  --owner "$REPO_OWNER" \
  --repo "$REPO_NAME" \
  --pr-labels "$PR_LABELS"
```
