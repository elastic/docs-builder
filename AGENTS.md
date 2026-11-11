# docs-builder

Elastic's documentation build toolchain. Processes Markdown from multiple repos into a unified documentation site, validates cross-repo references, serves with live reload, and ships as native AOT binaries for CI.

## Architecture

| Project | Purpose |
|---|---|
| `src/Elastic.Markdown/` | Core Markdown parser and Myst directive/role engine |
| `src/tooling/docs-builder/` | CLI — entry point `Program.cs`, commands in `Commands/` |
| `src/tooling/essc/` | AOT CLI indexing elastic.co content (Contentstack, Labs) into Elasticsearch; ships as `ghcr.io/elastic/website-search-essc` |
| `src/services/search/Elastic.Documentation.Search.Contract/` | Shared search document/mapping/API contract used by docs search and essc |
| `src/Elastic.Documentation.Site/` | Frontend (TypeScript · React · Vite) |
| `src/Elastic.Documentation.Configuration/` | YAML config schema and loading |
| `src/Elastic.Documentation.Navigation/` | Nav tree assembly and validation |
| `src/Elastic.Documentation.Links/` | Cross-repo link index and resolution |
| `src/Elastic.ApiExplorer/` | API reference rendering |
| `src/Elastic.Codex/` | Content management and assembly |
| `src/services/` | Background microservices |
| `src/infra/` | AWS Lambda functions (link index updater, changelog scrubber) |

## Essential Commands

[mise](https://mise.jdx.dev) is the entry point. `mise install` provisions the .NET SDK (from `global.json`) and Node (from `.nvmrc`), restores the local dotnet tools and installs the git hooks. `mise tasks` lists every task. `build.sh` and `dev.sh` still work; the tasks call them and the F# build project.

```bash
mise trust                                      # once per clone, in the root git folder; its worktrees are trusted too
mise install                                    # SDK, Node, dotnet tools, git hooks
mise run build                                  # verify compilation
mise run serve                                  # run the CLI locally: serve docs/ with live reload
mise run test                                   # all unit tests (fast, ~1 min)
mise run integrate                              # integration tests (slow — needs cloned repos)
mise run lint                                   # C# format check (read-only)
mise run fmt                                    # auto-fix C# formatting
mise run clean                                  # remove .artifacts/
dotnet test tests/Elastic.Markdown.Tests/       # single test project
./dev.sh <command>                                  # Docker route, no local installs (see CONTRIBUTING.md)
```

**CLI changes:** after editing `src/tooling/docs-builder/Commands/`, regenerate `docs/cli-schema.json`:

```bash
mise run schema
```

See [CONTRIBUTING.md](CONTRIBUTING.md#cli-reference-maintenance) for supplemental CLI docs under `docs/schema-support/cli-schema/`.

### TypeScript frontend

```bash
mise run site:install   # npm ci (skipped when package-lock.json is unchanged; the other site: tasks run it first)
mise run site:build     # production build
mise run site:watch     # dev with live reload
mise run site:test      # Vitest
mise run site:lint      # ESLint
mise run site:fmt       # Prettier auto-fix
mise run site:check     # TypeScript type check only
```

The same scripts run with `npm run <script>` from `src/Elastic.Documentation.Site`.

## Testing

Tests live in `tests/` (unit) and `tests-integration/` (integration).

- **C#**: TUnit · AwesomeAssertions · FakeItEasy across all test projects. All test projects (unit and integration) use TUnit (wired centrally via `tests/Directory.Build.props` and `tests-integration/Directory.Build.props`; no per-project flag needed). Write new tests using TUnit APIs: `[Test]`, `[Arguments(...)]`, `[MethodDataSource(nameof(X))]`.
- **TypeScript**: Vitest
- **Snapshots** (`Nullean.Snapshots`, wired for every test project in `tests/Directory.Build.props`; helpers in `tests-shared/Elastic.Documentation.Testing`): expected output lives in snapshots, not in chains of `Contain` assertions.
  - Small values go inline: `x.Should().MatchInlineSnapshot("""...""")`, `MatchInlineHtmlSnapshot`, `MatchInlineJsonSnapshot`. Leave the argument out to write a new one. Large values go to a verified file: `MatchSnapshot()`, `MatchHtmlSnapshot()`, `MatchJsonSnapshot()`, `MatchYamlSnapshot()`.
  - Diagnostics: `collector.Diagnostics.ToSnapshotText()` (`DiagnosticsSnapshot`). A clean run is the empty snapshot.
  - Navigation trees and generated files: `NavigationDump.ToText(...)`, `MockFileSystemDump.ToText(fs, dir)`. Rendered docs HTML: `DocsHtml.Section()` (the page content, no fixture H1, no htmx attributes) or `DocsHtml.Body()`.
  - Accept with the `snap` tool (`mise run snap:status`, `mise run snap:review`, `mise run snap:accept <key>`; `mise run snap:diff`, `snap:reject` and `snap:orphans` take the same arguments), or `SNAPSHOT_ACCEPT=1`. Read the diff of the changed `.verified.*` files and inline literals before committing. Nothing is accepted in CI. When a CI test run fails, download the `received-snapshots-*` artifact into `.artifacts/.snapshots/received/` and run `dotnet snap review`.
  - Changing a snapshot test's output is a behaviour change. Mutate the code once and confirm the test fails before you commit.
- **HTML snapshots** (`Nullean.Snapshots.Html`, used in `Elastic.ApiExplorer.Tests`): when a test would check one rendered block with many `Contain` assertions, compare it to a verified snapshot instead and keep one assertion that states the intent.
  - Write `html.Should().MatchHtmlSnapshot()`. Narrow it with `new HtmlSnapshotOptions().Select("#some-id")` or `.RemoveAttributes(...)` so unrelated markup and volatile values (bundle names, CI git info) stay out.
  - Snapshot a partial or one section, not a whole page. Whole-page snapshots break on unrelated changes.
  - Verified files live in `{TestProject}/.snapshots/{Class}/{Method}.verified.html`. Use one named test per snapshot rather than `[Arguments]`, which puts a hash in the file name.
  - Create or update them with `SNAPSHOT_ACCEPT=1 dotnet test --project tests/Elastic.ApiExplorer.Tests --treenode-filter "/*/*/MyTests/*"`, then review the diff of the `.verified.html` files. The variable is ignored when `CI=true`.
  - Before committing, change the markup in the partial and confirm the test fails, then revert.
- **Integration** (`tests-integration/`): needs real Elasticsearch credentials in the `docs-builder` user-secrets store, or Docker. Not run in CI; run with `mise run integrate` when you change search, MCP or content-date enrichment code.

Use the `/test` skill to pick the right test project automatically. A change to product code should land with a test that exercises it — the mapping from changed source to test project (mirrored by `/test`):

| Changed path | Test project / command |
|---|---|
| `src/Elastic.Markdown/` | `mise run test` (targets `Elastic.Markdown.Tests` and `Elastic.Authoring.Tests`) |
| `src/Elastic.Documentation.Configuration/` | `mise run test` (targets `Elastic.Documentation.Configuration.Tests`) |
| `src/Elastic.Documentation.Navigation/` | `mise run test` (targets `Navigation.Tests`) |
| `src/Elastic.Documentation.Indexing/` | `mise run test` (targets `Elastic.Documentation.Indexing.Tests`) |
| `src/authoring/Elastic.LegacyDocs.Migration/` | `mise run test` (targets `Elastic.LegacyDocs.Migration.Tests`) |
| `src/tooling/essc/` | `mise run test` (targets `Elastic.SiteSearch.Tests`) |
| `src/Elastic.ApiExplorer/` | `mise run test` (targets `Elastic.ApiExplorer.Tests`) |
| `src/services/Elastic.Documentation.Deploying/` | `mise run test` (targets `Elastic.Documentation.Deploying.Tests`) |
| `src/Elastic.Documentation.Site/` | `mise run site:test`; design-system, style-guide and `_Head` changes also run `mise run test` (`Navigation.Tests`, `DesignSystem` folder); changes to shared views or layout CSS also run `npm run visual:seed` once and `npm run visual:check` to prove flag-off output is unchanged. Markup or behaviour changes to page chrome, navigation or content components also run `mise run journeys:test`. See [`src/Elastic.Documentation.Site/AGENTS.md`](src/Elastic.Documentation.Site/AGENTS.md) |
| `tests/Elastic.Documentation.Journeys.Tests/` | `mise run journeys:test` (browser journeys; needs Chrome, and is not part of `mise run test`) |
| `tests-integration/` | `mise run integrate` |
| Multiple / uncertain | `mise run test` |

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

Mechanical formatting is fully enforced by `.editorconfig` and [curb](https://curb.nullean.net) via the MSBuild integration (auto-rewrites on `dotnet build` in Debug mode) and the Husky.Net pre-commit hook (`curb check`). To fix manually: `dotnet curb format .`. Never use `--no-verify`.

**After a build that produces only errors curb natively handles** (IDE0005, IDE0007, IDE0034, IDE0040, IDE0044, IDE0071, IDE0090, IDE0240, IDE0250, IDE0251), run:
```bash
curb cleanup
```
If the build also has style errors/warnings outside that set, add `--forward` to hand remaining diagnostics to `dotnet format style`:
```bash
curb cleanup --forward
```

Beyond what `.editorconfig` can check:

- **Async**: public async methods → `PascalCaseAsync`; private → `PascalCase`. Never `.Result`/`.Wait()`. Always accept `CancellationToken`. Use `ConfigureAwait(false)` in library code.
- **Class member order**: fields → constructors (prefer primary) → properties → methods (grouped by function, not visibility).
- **Complexity**: max 5–7 branches per method. Extract named helpers rather than nesting.
- **Early returns**: guard clauses first, happy path last.
- **Parameters**: max 4 — use a record/options object beyond that. Boolean params must be named at call sites.
- **Collections**: never return `null` — return `[]`. Use the TryGet pattern for lookups.
- **Results**: a method with more than one outcome returns a `union` — not `bool` + `out`, not a `bool Success` + a nullable payload, not a `(bool, T?)` tuple. Case types are top-level; prefix with the union name only when the bare name would be too generic at a match site (e.g. `LinkResolved` not `Resolved`). The one documented exception is a genuine keyed lookup, where `bool TryGetX(key, out value)` with `false` meaning "absent" is idiomatic and sufficient. If the `out` parameter carries an error message or an exception, it is not a lookup — use a union. Never assign a meaningful value to an `out` parameter on the failure path. Exhaustive switches over unions are enforced by CS8509, which this repo already treats as an error (`TreatWarningsAsErrors=true`).
- **Sealed hierarchies**: annotate an `abstract` type as `closed` instead when all its subtypes live in the same assembly and the set is intentionally finite. `closed` is implicitly abstract — remove the `abstract` keyword. Wire types that use `[JsonPolymorphic]` use `closed`, not `union` — `closed` does not change the serialization representation; `union` does.
- **Testing**: TUnit with AwesomeAssertions fluent style. Method naming: `Method_Scenario_Expected`.
- **Comments**: only when *why* is non-obvious. No `#region`. No multi-paragraph docstrings.

Use `/style-review` to check a diff against these rules.

## Agentic skills

Always invoke the skill for the following tasks — never substitute your own format:

- **Commits** — use `/commit` before creating any git commit.
- **Pull requests** — use `/pr` before running `gh pr create`. Never write your own PR body or title without going through the skill first.
- **Issues** — use `/issue` before running `gh issue create`. Never file an issue without going through the skill first.
- **Feature flags** — use `/feature-flag` when you add or extend a flag. Flag-off must match the previous default.

## Documentation

Update `/docs/` whenever Markdown syntax or rendering behaviour changes.
