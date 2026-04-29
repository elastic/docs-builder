---
name: pr
description: Create a GitHub pull request for the current branch with a focused why/what body and exactly one release-drafter label. Use when the user asks to open a PR, create a pull request, or ship a branch.
---

# PR Skill

Creates a GitHub PR with a body focused on *why* and *what*, labeled for the release changelog.

## Steps

### 1. Understand the branch

Run these to see what the PR contains:
```bash
git log main..HEAD --oneline
git diff main...HEAD --stat
```

### 2. Push the branch (if needed)

```bash
git push -u origin HEAD
```

### 3. Write the PR title

- ≤70 characters
- Imperative mood, no trailing period
- Describes the change at a human level (not a file list)

### 4. Write the PR body

Use this structure:

```
## Why

<One or two sentences: the problem, gap, or need this addresses. What would go wrong without this change?>

## What

<What changed, at a meaningful level — not a file list. Think: what does a reviewer need to understand to evaluate this?>

## How (optional)

<Only include this section if a completely new architectural mechanism was introduced that future contributors need to understand at a big-picture level. Skip for normal feature additions, fixes, or refactors — those belong in code comments, not here.>
```

**Do not** include bullet lists of changed files. Do not summarize what's already obvious from the diff.

### 5. Choose exactly ONE label

Pick the single best-fit label. Apply it with `--label <label>`.

| Label | Use when |
|-------|----------|
| `breaking` | Existing behavior or public API breaks |
| `feature` | New capability that did not exist before |
| `enhancement` | Improves or extends an existing feature |
| `bug` | Fixes a defect in existing behavior |
| `fix` | Alias for bug fix (use `bug` by preference) |
| `documentation` | Docs-only change (markdown, /docs/ pages) |
| `chore` | Cleanup, refactor, internal restructure — no user-visible change |
| `dependencies` | Dependency version bumps |
| `automation` | CI/CD, GitHub Actions, scripts, build tooling |
| `ci` | Alias for automation (use `automation` by preference) |
| `redesign` | Frontend visual/structural redesign work |
| `changelog:skip` | Housekeeping with no changelog entry (e.g. typo fixes, config tweaks) |

When in doubt between `feature` and `enhancement`: `feature` = didn't exist, `enhancement` = existed but got better.

### 6. Create the PR

```bash
gh pr create --title "<title>" --label "<label>" --body "$(cat <<'EOF'
## Why

...

## What

...
EOF
)"
```

### 7. Return the PR URL

Always print the PR URL so the user can open it directly.
