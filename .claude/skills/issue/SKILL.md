---
name: issue
description: File a well-formed bug report or feature request. Use when the user asks to open an issue, report a bug, or request a feature in the docs-builder repo.
---

# Issue Skill

Read [`.claude/skills/writing-style.md`](../writing-style.md) and [`.claude/skills/surfaces.md`](../surfaces.md) before writing anything.

Files a GitHub issue that matches the repo's templates, applies correct labels, and checks for duplicates first.

## Steps

### 1. Check for duplicates

Search for near-duplicates before opening anything. Link any you find in the issue body rather than filing a second.

```bash
gh issue list --search "<key terms>" --limit 10
```

### 2. Determine the issue type

- **Bug** — something that used to work stopped, or produces wrong output. Use `bug-report` structure.
- **Feature / enhancement** — something that does not exist yet, or needs to be better. Use `enhancement` structure.

### 3. Write the title

- ≤70 characters, no trailing period
- States the observable problem or the wanted capability — not the internal cause or the implementation

### 4. Write the body

**Bug report:**

```
<One sentence: what went wrong and, briefly, under what condition. Be specific.>

### What happened

<What you saw. Include the command you ran, the input, and the exact output or
 error. Commands and error messages go in fenced blocks.>

### How to reproduce

<Minimal steps. A command and the file it ran against is enough if that covers it.
 Skip this section if the "What happened" section already makes it reproducible.>

### Version or commit

<Output of `docs-builder --version`, or the commit SHA if building from source.
 This is the single most useful piece of triage data.>
```

**Feature request:**

```
<One sentence: the outcome you want, not the implementation.>

### What is getting in your way

<The concrete limitation. What are you trying to do, and what stops you?
 One to three sentences.>

### What would you like instead

<Your proposed change or outcome. If you have a specific implementation in mind,
 describe it — but a clear outcome is enough.>

### Anything else

<Examples from other tools, links, screenshots, or context that did not fit above.
 Skip this section if there is nothing to add.>
```

Formatting rules:
- Same plain-language rules as PR bodies — active voice, short sentences, no mechanical noun clusters.
- Commands and error messages in fenced blocks.
- Backticks on all identifiers: flags, config keys, file paths, method names.
- Skip any section that has nothing to say — a blank section adds noise, not structure.

### 5. Choose labels

File with **two or three labels**:

1. **Type** (required, from the template): `bug` or `enhancement`
2. **Area** (one, if it fits): derived from the surface map in `surfaces.md`. Pick from the repo's existing area labels — do not invent new ones:
   `authoring` · `links` · `tables` · `attributes` · `versioning` · `build` · `automation` · `migration` · `SEO` · `user-experience` · `tech-debt` · `design`
3. **`needs triage`** (always)

These are issue labels, not release-drafter PR labels. Do not use `feature`, `chore`, `redesign`, `changelog:skip`, etc. here.

### 6. Create the issue

One call — title, labels, and body together:

```bash
gh issue create \
  --title "<title>" \
  --label "bug,<area-label>,needs triage" \
  --body "$(cat <<'EOF'
<lead sentence>

### What happened

...

### Version or commit

...
EOF
)"
```

Replace `bug` with `enhancement` for feature requests. Omit the area label if none fits — do not force one.

### 7. Return the issue URL

Always print the URL so the user can open it directly.
