# docs-builder

Elastic's documentation build toolchain. Processes Markdown from multiple repos into a unified documentation site, validates cross-repo references, serves with live reload, and ships as native AOT binaries for CI.

## Architecture

| Project | Purpose |
|---|---|
| `src/Elastic.Markdown/` | Core Markdown parser and Myst directive/role engine |
| `src/tooling/docs-builder/` | CLI — entry point `Program.cs`, commands in `Commands/` |
| `src/Elastic.Documentation.Site/` | Frontend (TypeScript · React · Parcel) |
| `src/Elastic.Documentation.Configuration/` | YAML config schema and loading |
| `src/Elastic.Documentation.Navigation/` | Nav tree assembly and validation |
| `src/Elastic.Documentation.Links/` | Cross-repo link index and resolution |
| `src/Elastic.ApiExplorer/` | API reference rendering |
| `src/Elastic.Codex/` | Content management and assembly |
| `src/services/` | Background microservices |
| `src/infra/` | AWS Lambda functions (link index updater, changelog scrubber) |

## Essential Commands

```bash
dotnet build                                    # verify compilation
dotnet run --project src/tooling/docs-builder   # run CLI locally
./build.sh unit-test                            # all unit tests (fast, ~1 min)
./build.sh integrate                            # integration tests (slow — needs cloned repos)
./build.sh lint                                 # C# format check (read-only)
./build.sh clean                                # remove .artifacts/
dotnet format                                   # auto-fix C# formatting
dotnet test tests/Elastic.Markdown.Tests/       # single test project
```

### TypeScript frontend

```bash
cd src/Elastic.Documentation.Site
npm ci
npm run build           # production build
npm run watch           # dev with live reload
npm run test            # Jest
npm run lint            # ESLint
npm run fmt:write       # Prettier auto-fix
npm run compile:check   # TypeScript type check only
```

## Testing

Tests live in `tests/` (unit) and `tests-integration/` (integration).

- **C#**: xUnit v3 · TUnit · FakeItEasy
- **TypeScript**: Jest
- **F# authoring**: `tests/authoring/`
- **Integration**: clones real repos, runs full assembler — only run when integration files change

Use the `/test` skill to pick the right test project automatically.

## Key Locations

| What | Where |
|---|---|
| Myst extensions (directives, roles) | `src/Elastic.Markdown/Myst/` |
| CLI commands | `src/tooling/docs-builder/Commands/` |
| Config schema | `src/Elastic.Documentation.Configuration/` |
| Frontend assets | `src/Elastic.Documentation.Site/Assets/` |
| Test helpers | `tests/Elastic.Markdown.Tests/TestHelpers.cs` |
| Repo / nav config | `config/` |
| Docs site | `/docs/` |

## Code Style

Mechanical formatting is fully enforced by `.editorconfig` and the Husky.Net pre-commit/pre-push hooks — run `dotnet format` or `/lint` to fix before committing. Never use `--no-verify`.

Beyond what `.editorconfig` can check:

- **Async**: public async methods → `PascalCaseAsync`; private → `PascalCase`. Never `.Result`/`.Wait()`. Always accept `CancellationToken`. Use `ConfigureAwait(false)` in library code.
- **Class member order**: fields → constructors (prefer primary) → properties → methods (grouped by function, not visibility).
- **Complexity**: max 5–7 branches per method. Extract named helpers rather than nesting.
- **Early returns**: guard clauses first, happy path last.
- **Parameters**: max 4 — use a record/options object beyond that. Boolean params must be named at call sites.
- **Collections**: never return `null` — return `[]`. Use the TryGet pattern for lookups.
- **Testing**: TUnit for new test projects; AwesomeAssertions fluent style. Method naming: `Method_Scenario_Expected`.
- **Comments**: only when *why* is non-obvious. No `#region`. No multi-paragraph docstrings.

Use `/style-review` to check a diff against these rules.

## Documentation

Update `/docs/` whenever Markdown syntax or rendering behaviour changes.
