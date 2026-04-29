---
name: commit
description: Stage relevant files and create a well-formed git commit for the docs-builder repo. Use this when the user asks to commit changes, save work, or create a commit.
---

# Commit Skill

Creates a clean, well-formed commit following the docs-builder project conventions.

## Steps

### 1. Understand what changed

Run these in parallel:
```bash
git status
git diff
git diff --staged
git log --oneline -5
```

### 2. Stage files

Stage specific files by name — never `git add -A` or `git add .` blindly. Exclude:
- `.env` files or anything with secrets/credentials
- Large binaries not already tracked
- Unrelated changes to the task at hand

### 3. Write the commit message

Rules:
- **First line**: Imperative mood, ≤72 chars, no trailing period (e.g. `Add async timeout handling to assembler`)
- **Body** (optional): One short paragraph explaining *why*, not what. Skip if the title is self-explanatory.
- **Trailer**: Always append `Co-Authored-By: Claude Sonnet 4.6 (1M context) <noreply@anthropic.com>`

Always pass the message via HEREDOC to avoid shell escaping issues:
```bash
git commit -m "$(cat <<'EOF'
Title here

Optional body explaining why.

Co-Authored-By: Claude Sonnet 4.6 (1M context) <noreply@anthropic.com>
EOF
)"
```

### 4. Handle hook failures

This project uses **Husky.Net** git hooks:
- **pre-commit**: runs prettier, eslint, typescript-check
- **pre-push**: runs dotnet-lint

If a hook fails:
1. Read the error output carefully
2. Fix the underlying issue (run `/lint` if it's a formatting problem)
3. Re-stage the affected files
4. Create a **new commit** — never `git commit --amend` for a failed commit, and never use `--no-verify`

### 5. Verify success

Run `git status` after the commit to confirm a clean working tree.
