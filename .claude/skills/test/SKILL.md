---
name: test
description: Run the relevant tests for what changed — detects whether to run C# unit tests, specific test projects, integration tests, or JS/TS tests based on changed files. Use when the user asks to run tests, verify changes, or check if something is broken.
---

# Test Skill

Runs the right tests for what changed — fast feedback without running everything.

## Steps

### 1. Detect changed files

```bash
git diff --name-only HEAD
```

Or, if checking staged changes:
```bash
git diff --name-only --staged
```

### 2. Pick the right test command

Use this decision table based on which directories have changed files:

| Changed path | Test command |
|---|---|
| `src/Elastic.Documentation.Site/` | `cd src/Elastic.Documentation.Site && npm run test` |
| `src/Elastic.Markdown/` | `dotnet test tests/Elastic.Markdown.Tests/` |
| `src/Elastic.Documentation.Configuration/` | `dotnet test tests/Elastic.Documentation.Configuration.Tests/` |
| `src/Elastic.Documentation.Navigation/` or `Navigation.Tests/` | `dotnet test tests/Navigation.Tests/` |
| `src/Elastic.ApiExplorer/` | `dotnet test tests/Elastic.ApiExplorer.Tests/` |
| `tests-integration/` | `./build.sh integrate` |
| `src/tooling/docs-builder/` | `./build.sh unit-test` |
| Multiple areas or uncertain | `./build.sh unit-test` |

If files span more than two source areas, prefer `./build.sh unit-test` to run all unit tests at once.

### 3. Run and report

- Show the command being run
- On **pass**: summarize pass count, confirm all green
- On **fail**: show the specific failing test(s), the error message, and suggest a fix approach based on the failure

## Notes

- Integration tests (`./build.sh integrate`) require cloned repos and take significantly longer — only run when integration test files changed or the user explicitly asks
- When in doubt, `./build.sh unit-test` is the safe default (completes in under a minute)
- TypeScript type checking (not tests) is `npm run compile:check` from `src/Elastic.Documentation.Site/`
