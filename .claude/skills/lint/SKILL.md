---
name: lint
description: Auto-fix all formatting issues in the docs-builder repo — runs dotnet format for C# and npm fmt:write for TypeScript/JS. Use when the user asks to fix formatting, lint the code, or when a pre-commit hook fails due to formatting.
---

# Lint Skill

Fixes all formatting issues across C# and TypeScript in the docs-builder repo.

## Steps

### 1. Fix C# formatting

```bash
dotnet format
```

This applies the project's editorconfig and code style rules to all C# files.

### 2. Fix TypeScript/JS formatting

```bash
cd src/Elastic.Documentation.Site && npm run fmt:write
```

This runs Prettier on all frontend source files.

### 3. Report results

After both commands complete:
- If files were changed: list which files were reformatted
- If nothing changed: confirm "No formatting issues found"

## Notes

- This skill fixes files in place but does **not** commit
- Run this before `/commit` if you see formatting errors in build output or pre-commit hook failures
- Formatting rules are enforced by Husky.Net pre-commit hooks (prettier + eslint for TS, dotnet-lint on pre-push for C#)
- `dotnet format` is also available as `./build.sh lint` for the full lint pipeline
