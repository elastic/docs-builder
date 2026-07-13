# docs-builder

Elastic's documentation build toolchain. Processes Markdown from multiple repos into a unified documentation site, validates cross-repo references, serves with live reload, and ships as native AOT binaries for CI.

## Architecture

| Project | Purpose |
|---|---|
| `src/Elastic.Markdown/` | Core Markdown parser and Myst directive/role engine |
| `src/tooling/docs-builder/` | CLI — entry point `Program.cs`, commands in `Commands/` |
| `src/tooling/essc/` | AOT CLI indexing elastic.co content (Contentstack, Labs) into Elasticsearch; ships as `ghcr.io/elastic/website-search-essc` |
| `src/services/search/Elastic.Documentation.Search.Contract/` | Shared search document/mapping/API contract used by docs search and essc |
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

**CLI changes:** after editing `src/tooling/docs-builder/Commands/`, regenerate `docs/cli-schema.json`:

```bash
dotnet run --project src/tooling/docs-builder -- __schema > docs/cli-schema.json
```

See [CONTRIBUTING.md](CONTRIBUTING.md#cli-reference-maintenance) for supplemental CLI docs under `docs/cli/`.

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

- **C#**: xUnit v3 · AwesomeAssertions · FakeItEasy is the current stack across every test project. TUnit is the elastic/dotnet org's target standard and the intended destination for a future migration, but no project has moved yet — don't write new tests against TUnit APIs until that migration actually happens.
- **TypeScript**: Jest
- **F# authoring**: `tests/authoring/`
- **Integration**: clones real repos, runs full assembler — only run when integration files change

Use the `/test` skill to pick the right test project automatically. A change to product code should land with a test that exercises it — the mapping from changed source to test project (mirrored by `/test`):

| Changed path | Test project / command |
|---|---|
| `src/Elastic.Markdown/` | `dotnet test tests/Elastic.Markdown.Tests/` |
| `src/Elastic.Documentation.Configuration/` | `dotnet test tests/Elastic.Documentation.Configuration.Tests/` |
| `src/Elastic.Documentation.Navigation/` | `dotnet test tests/Navigation.Tests/` (prefix dropped) |
| `src/Elastic.Documentation.Indexing/` | `dotnet test tests/Elastic.Documentation.Indexing.Tests/` |
| `src/tooling/essc/` | `dotnet test tests/Elastic.SiteSearch.Tests/` (essc's root namespace is `Elastic.SiteSearch.Cli`) |
| `src/Elastic.ApiExplorer/` | `dotnet test tests/Elastic.ApiExplorer.Tests/` |
| `src/Elastic.Documentation.Site/` | `cd src/Elastic.Documentation.Site && npm run test` |
| `tests-integration/` | `./build.sh integrate` |
| Multiple / uncertain | `./build.sh unit-test` |

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

## Cross-cutting decisions

- **AOT + source-generated JSON everywhere.** Both CLIs (`docs-builder`, `essc`), the Lambda functions in `src/infra/`, and most shared libraries are Native AOT (`<PublishAot>true</PublishAot>`) or AOT-compatible (`<IsAotCompatible>true</IsAotCompatible>`). The load-bearing rule this implies: **any new serialized type must be registered with `[JsonSerializable]` on the right `JsonSerializerContext`** (canonical example: `src/Elastic.Documentation/Serialization/SourceGenerationContext.cs`) or it fails at runtime once published/trimmed, not at compile time. Code in AOT/AOT-compatible projects must stay reflection-free — this is why `SYSLIB1100`/`SYSLIB1101`/`IL3050` are suppressed in those `.csproj` files rather than fixed; they can't be fixed, only worked around.
- **The shared search contract.** `src/services/search/Elastic.Documentation.Search.Contract/` exists as a separate, dependency-light project specifically so both the docs indexing pipeline (`Elastic.Markdown/Exporters/Elasticsearch/`) and `essc` reference the same document schema, mappings, and analysis config — one content-source's fields can't drift from another's. See `docs/development/essc.md` for the source-by-source detail (Contentstack, Labs).

## Boundaries: never touch / human-gated

- **Destructive `essc indices` commands target production Elasticsearch.** `unify`, `copy --force`, `cleanup`, `sync-remote`, `unify-incremental-sync` (`src/tooling/essc/Commands/IndicesCommands.cs`) delete indices and repoint aliases against real clusters. Never run these against a real cluster without an explicit human decision; prefer `--dry-run` first. `cleanup`'s planning step (`IndicesCleanupPlanner.Plan`, same folder) is a pure function that's idempotent by design — retrying a cleanup run after a partial apply re-plans against current state and deletes nothing further (see `Applying_the_plan_then_replanning_yields_no_further_deletions` in `tests/Elastic.SiteSearch.Tests/IndicesCleanupPlannerTests.cs`); the mutating steps around it still touch a real cluster, so the caution above still applies.
- **`docs-builder` commands tagged `[CommandIntent(Intent.Destructive)]` / `[MutationScope(MutationScope.Global)]` / `[RequiresAuth]`** — Assembler `Apply`, Codex `Apply`, `ChangelogCommand`'s publish path, `Move`/`mv` (`src/tooling/docs-builder/Commands/`). These do mass S3 deletes or repo-wide link rewrites; the attributes on a command are the ground truth for its blast radius — read them before running anything unfamiliar.
- **Prod Lambda deploys are GitHub-Environment-gated.** `deploy-link-index-updater-lambda-prod` and `deploy-changelog-scrubber-lambda-prod` in `.github/workflows/release.yml` require the `link-index-updater-prod` / `changelog-scrubber-prod` environments respectively. Don't try to route around environment protection.
- **`config/assembler.yml` is baked into the changelog-scrubber Lambda as its public-bucket allowlist.** Editing it changes what private-repo content can be published to the public bucket — treat changes here as high blast-radius, not a routine config edit.
- **Credentials load from dotnet user-secrets (the `docs-builder` store) or environment variables** (`src/tooling/essc/Elasticsearch/SourcingConfiguration.cs`, `ContentStackConfiguration.cs`) — never hardcode a key/URL or commit one to config.
- Never skip hooks (`--no-verify`) — see Code Style.

## Code Style

Mechanical formatting is fully enforced by `.editorconfig` and the Husky.Net pre-commit/pre-push hooks — run `dotnet format` or `/lint` to fix before committing. Never use `--no-verify`.

Beyond what `.editorconfig` can check:

- **Async**: public async methods → `PascalCaseAsync`; private → `PascalCase`. Never `.Result`/`.Wait()`. Always accept `CancellationToken`. Use `ConfigureAwait(false)` in library code.
- **Class member order**: fields → constructors (prefer primary) → properties → methods (grouped by function, not visibility).
- **Complexity**: max 5–7 branches per method. Extract named helpers rather than nesting.
- **Early returns**: guard clauses first, happy path last.
- **Parameters**: max 4 — use a record/options object beyond that. Boolean params must be named at call sites.
- **Collections**: never return `null` — return `[]`. Use the TryGet pattern for lookups.
- **Testing**: xUnit v3 with AwesomeAssertions fluent style is the current standard (see Testing section — TUnit is a future migration target, not yet used). Method naming: `Method_Scenario_Expected`.
- **Comments**: only when *why* is non-obvious. No `#region`. No multi-paragraph docstrings.

Use `/style-review` to check a diff against these rules.

## Documentation

Update `/docs/` whenever Markdown syntax or rendering behaviour changes.
