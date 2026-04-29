---
name: style-review
description: Review changed or staged C# code against the docs-builder coding standards and flag violations. Use when the user asks to review code style, check for standards violations, or audit a diff before committing.
---

# Style Review Skill

Reviews C# changes for violations that `dotnet format` and `.editorconfig` cannot catch — behavioral patterns, naming conventions, and architectural rules.

## What this skill does NOT check

`.editorconfig` + `dotnet format` already enforce (mechanically):
- `var` everywhere, no explicit types
- No `this.` qualifier
- Language keywords (`string` not `String`)
- Expression bodies
- Allman braces, newlines before `{`
- Null propagation (`?.`, `??`)
- Pattern matching over `is`/`as` casts
- Inlined variable declarations (`out var x`)
- Throw expressions, conditional delegate calls
- Modifier order
- File-scoped namespaces
- Object/collection initializers
- `[]` instead of `new List<T>()` / `Array.Empty<T>()`

Run `dotnet format` (or `/lint`) to fix those. Only continue with this skill for the behavioral checks below.

## Steps

### 1. Get the diff

```bash
git diff          # unstaged changes
git diff --staged # staged changes
git diff main...HEAD  # full branch
```

### 2. Check behavioral rules

**Async correctness** (error-level):
- Public async methods must end in `Async`; private async methods must NOT
- Never `.Result` or `.Wait()` on an async call
- Library code must use `.ConfigureAwait(false)` on awaits
- All async methods should accept `CancellationToken ct = default`

**Method design** (error-level):
- Max 4 parameters — flag anything over that without a record/options object
- Boolean parameters must use named argument syntax at every call site
- Cyclomatic complexity > 7 branches in one method — flag and suggest extraction

**Structural** (error-level):
- No `#region` directives
- Class member order violated: fields → constructors → properties → methods (grouped by function, not visibility)

**Return values** (error-level):
- Methods returning collections must never return `null` — must return `[]`
- Lookups should use the TryGet pattern, not null returns

**Testing** (style-level, in `tests/` and `tests-integration/`):
- New test projects should use TUnit + AwesomeAssertions, not xUnit directly
- Test method naming: `MethodName_Scenario_Expected`
- No assertions without fluent style (`Should().Be(...)` not `Assert.Equal(...)`)

**Comments** (style-level):
- Comments that describe *what* the code does (not *why*) — flag for removal
- Multi-paragraph docstrings on non-public members
- `#region` (already error-level above)

### 3. Report

Format violations as a numbered list with severity:

```
ERROR:
1. src/Elastic.Markdown/Foo.cs:42 — public async method `GetData` missing `Async` suffix
2. src/Elastic.Markdown/Foo.cs:67 — `.Result` blocks on async call; use `await`

STYLE:
3. tests/Elastic.Markdown.Tests/Bar.cs:15 — test name `TestGetUser` should follow `GetUser_ValidId_ReturnsUser` pattern
```

If no violations: "No behavioral style violations found."

### 4. Do NOT auto-fix

Report only. For formatting issues, run `/lint`. For logic violations (blocking async, null collections), the developer must fix manually.
