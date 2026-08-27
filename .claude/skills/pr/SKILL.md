---
name: pr
description: Create a GitHub pull request for the current branch with a focused why/what body and exactly one release-drafter label. Use when the user asks to open a PR, create a pull request, or ship a branch.
---

# PR Skill

Read [`.claude/skills/writing-style.md`](../writing-style.md) and [`.claude/skills/surfaces.md`](../surfaces.md) before writing anything.

Creates a GitHub PR body that a newcomer can orient from in under a minute: front-loaded outcome, grounded Why, behaviour-led What, verifiable by the reviewer.

## Steps

### 1. Understand the branch

```bash
git status
git log main..HEAD --oneline
git diff main...HEAD --stat
```

### 2. Commit uncommitted work if needed

If the working tree has changes that belong in this PR, read and follow [commit](../commit/SKILL.md) first. Do not commit inline. Do not skip hooks.

### 3. Push (if needed)

```bash
git push -u origin HEAD
```

### 4. Write the PR title

- ≤70 characters, imperative mood, no trailing period
- States what changed at a human level — not a file list, not a symbol name
- No `[bug]` / `[feature]` / `[chore]` prefixes — the label carries the type

### 5. Write the PR body

Required structure:

```
<One or two sentences, no heading. What this changes and the effect.
 A newcomer reads only this and knows whether the PR concerns them.>

**Affects:** <one to three surfaces from surfaces.md, most affected first>

## Why
<Two to four sentences. The concrete failure or gap. Active voice, present tense
 for current behaviour. Do not open with the history of a prior PR.>

## What

#### <name or short label>
<Prose paragraph. Lead with behaviour, not a symbol or path. Name a symbol only
 when the reviewer needs it to find the code. Two to four sentences max.
 Three to five sections total.>

## Verify
<How a reviewer confirms this locally. Use real commands they would run:
 `./build.sh`, `dotnet test`, `npm run test`, `dotnet run --project …`.
 If there is no clear local verification step, omit this section entirely.
 Do not list CI checks, YAML linting, or bash scripts an agent would run
 to prove their own work — those are not reviewer steps.>
```

Conditional add-ons — each is one or two sentences with a bold lead-in, no heading:

- **Breaking** — what a consumer must change and when it bites them. Pairs with the `breaking` label.
- **Out of scope** — a gap this PR deliberately leaves, so a reviewer does not raise it as a finding.
- **Risk** — shared or production state this touches. Required when the change reaches anything in `CLAUDE.md`'s "Boundaries: never touch / human-gated" list, or leaves state that a code revert will not undo.
- **Stack** — position and links: `3 of 5, on top of #3855`. A bare `Stack: 3/5` with no links is not enough.

**Do not** include bullet lists of changed files. Do not summarize what the diff already states plainly.

### 6. Choose exactly ONE label

`.github/workflows/required-labels.yml` enforces exactly one release-drafter label at `mode: exactly, count: 1` — two labels or zero fails CI. Pick the single best fit.

| Label | Use when |
|---|---|
| `breaking` | An existing config, invocation, or documented behaviour stops working |
| `feature` | A capability that did not exist before |
| `enhancement` | An existing capability got better |
| `bug` | A defect in existing behaviour is fixed |
| `documentation` | Docs-only change (`docs/` pages, not incidental doc updates in the same PR) |
| `chore` | Cleanup, refactor, internal restructure — no user-visible change |
| `dependencies` | Dependency version bumps |
| `automation` | CI/CD, GitHub Actions, build tooling |
| `redesign` | Frontend visual or structural redesign |
| `changelog:skip` | Nothing worth a changelog line |

Never use `fix` (use `bug`) or `ci` (use `automation`) — both are release-drafter aliases that split the same changelog category.

**`feature` vs `enhancement`:** `feature` = didn't exist; `enhancement` = existed but got better.

**`breaking` guidance:** use it when an older `docs-builder` invocation or an existing repo config stops working. The canonical trigger is `Configuration` in the `**Affects:**` line plus a rename or removal — [#3856](https://github.com/elastic/docs-builder/pull/3856) removed `output:` from `changelog.yml` profiles and shipped as `feature`; it should have been `breaking`. Internal C# type renames and private method signature changes are not breaking.

### 7. Create the PR

One call — title, label, and body together. No follow-up `gh pr edit`:

```bash
gh pr create --title "<title>" --label "<label>" --body "$(cat <<'EOF'
<lead sentence(s)>

**Affects:** <surfaces>

## Why

...

## What

- ...
- ...
- ...

## Verify

```bash
<command>
```
EOF
)"
```

### 8. Return the PR URL

Always print the URL so the user can open it directly.
