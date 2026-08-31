# Surface map

Answers "what does this change affect?" from the paths in a diff.
Used by the `pr` skill to write the `**Affects:**` line, and by the `issue` skill to pick an area label.

---

## How to use this map

**Prefer an existing entry when one fits.** Consistent terms are the whole point — a reviewer who sees `Release notes` on ten PRs knows immediately what moved.

**Coin your own term when the map is stale or a plainer word fits better.** When you do, say so in the PR body (`Affects: Contributor workflow — not in the map; proposed as an addition`) and add it here in the same PR. A coined term that never lands back in the map is a one-off, not a convention.

---

## Output rules

- **One to three surfaces**, most affected first.
- More than three means the change is either genuinely cross-cutting or should be split.
- A change with no reader-visible surface: `**Affects:** Internals only`. Do not stretch for an entry.
- `Authoring`, `Configuration`, and `CLI` are the surfaces most often *missed*, because a change to shared code reaches them indirectly. Check them before settling.
- Do **not** list `Documentation` when the `docs/` change only documents the same PR's code change. Reserve it for PRs whose point is the documentation.
- `Configuration` plus a rename or removal is the trigger to reconsider the `breaking` label.
- `Deploys & previews`, `Release notes` publishing, and the Lambdas are where the **Risk** line usually applies. Cross-reference `CLAUDE.md`'s "Boundaries: never touch / human-gated" list.
- For the test-project mapping (`Elastic.Markdown/` → `dotnet test tests/Elastic.Markdown.Tests/`, etc.), see `CLAUDE.md`. Do not duplicate it here.

---

## The map

Named for what a reader *loses* when it breaks, not for the project that implements it.

| Surface | Primary paths | What breaks for whom |
|---|---|---|
| `Authoring` | `src/Elastic.Markdown/`, `tests/authoring/` | Markdown syntax and rendering — every doc author |
| `Navigation` | `src/Elastic.Documentation.Navigation/`, `config/navigation*.yml` | Nav trees, TOC, sidebar structure |
| `Site UI` | `src/Elastic.Documentation.Site/` | Page chrome, layout, styling, client-side behaviour |
| `API reference` | `src/Elastic.ApiExplorer/`, `src/Elastic.Documentation.OpenApiIndex/`, `src/infra/docs-lambda-openapi-index/` | OpenAPI-driven reference pages |
| `Release notes` | `src/services/Elastic.Changelog/`, `src/infra/docs-lambda-changelog-scrubber/`, `docs/cli/changelog/` | Changelog entries, bundling, publishing |
| `Search` | `src/Elastic.Documentation.Indexing/`, `src/services/search/`, `src/tooling/essc/`, `config/search.yml` | Docs search and elastic.co website search, indexing, ranking |
| `Links & redirects` | `src/Elastic.Documentation.Links/`, `src/Elastic.Documentation.LinkIndex/`, `src/infra/docs-lambda-index-publisher/`, `docs/_redirects.yml`, `config/legacy-url-mappings.yml` | Cross-repo links, link validation, redirects |
| `Assembler builds` | `src/services/Elastic.Documentation.Assembler/`, `src/tooling/docs-builder/Commands/Assembler/` | The multi-repo assembled site |
| `Isolated builds` | `src/services/Elastic.Documentation.Isolated/`, `IsolatedBuildCommand.cs` | Single-docset builds — what a repo runs on its own content |
| `Codex builds` | `src/Elastic.Codex/`, `src/tooling/docs-builder/Commands/Codex/` | Codex content assembly |
| `Docs API` | `src/api/Elastic.Documentation.Api/` | The runtime service: Ask AI, search endpoints |
| `MCP` | `src/api/Elastic.Documentation.Mcp.Remote/` | The remote MCP server and its consumers |
| `Deploys & previews` | `src/services/Elastic.Documentation.Deploying/`, `DeployCommands.cs`, `assembler-preview*.yml`, `docs-preview*-local.yml` | PR previews, S3 and CloudFront state, the redirect store |
| `Configuration` | `src/Elastic.Documentation.Configuration/`, `config/*.yml` | Config schema — a rename here breaks older `docs-builder` versions |
| `CLI` | `src/tooling/docs-builder/Commands/`, `docs/cli-schema.json` | Commands and flags — anyone scripting `docs-builder` |
| `Automation` | `.github/workflows/`, `build.sh`, `Directory.Packages.props` | CI, release plumbing, the build itself |
| `Documentation` | `docs/` | This repo's own documentation |
| `Migration` | `src/authoring/Elastic.LegacyDocs.Migration/`, `src/Elastic.Documentation.LegacyDocs/`, `src/tooling/docs-migrate/`, `src/tooling/adoc-compare/` | Legacy AsciiDoc migration tooling |
| `Agentic Skills` | `.claude/skills/`, `.github/ISSUE_TEMPLATE/`, `.github/*.md` | The AI-assisted workflows for commits, PRs, issues, and reviews |

---

## Issue area labels

When the `issue` skill files a bug or feature request, it picks a type label (`bug` or `enhancement`, from the template) plus at most one area label from this set. Use the surface map to derive the area.

These are the repo's existing issue area labels — do not invent new ones:

`authoring` · `links` · `tables` · `attributes` · `versioning` · `build` · `automation` · `migration` · `SEO` · `user-experience` · `tech-debt` · `design`

Plus `needs triage` on every new issue.

Surface → area label guidance:
- `Authoring` → `authoring`
- `Navigation` → (no direct match — omit or use `user-experience`)
- `Site UI` → `design` or `user-experience`
- `Links & redirects` → `links`
- `Configuration`, `CLI` → `build`
- `Automation` → `automation`
- `Migration` → `migration`
- `Search` → (no direct match — omit)
- Other surfaces → omit the area label; `needs triage` is enough
