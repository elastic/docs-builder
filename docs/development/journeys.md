---
navigation_title: Browser journey tests
---

# Browser journey tests

Journeys are short scripts of browser commands that open a page, interact with it, and check what the page shows. They run in Chrome through [Nullean.Cdp](https://cdp.nullean.net), and `Nullean.Snapshots` compares what they read against a verified transcript. They live in `tests/Elastic.Documentation.Journeys.Tests/`.

## Run them

```bash
mise run journeys
```

This builds `docs-builder`, serves this repository's `docs/` folder on a free port, and runs every journey against it. Chrome must be installed. Cdp finds it, or you can set `CDP_CHROME`.

To run one area or one journey, pass a TUnit filter:

```bash
mise run journeys -- --treenode-filter "/*/*/NavigationJourneys/*"
mise run journeys -- --treenode-filter "/*/*/*/Run(sidebar)"
```

To run against a built assembler or Codex site, build it first and use the matching task:

```bash
mise run journeys:assembler   # needs .artifacts/assembly
mise run journeys:codex       # needs .artifacts/codex/docs
```

The test host removes every `FEATURE_*` variable from the environment of the site it starts, so a flag exported in your shell does not change what a journey sees.

## Layout

Each folder under `journeys/` is an area, and each area has one test class under `Areas/`. A journey is a `.journey` file, and its transcript lives in `.snapshots/journeys/<area>/<name>.verified.journey`.

| Area | Covers |
|---|---|
| `landing` | The home page and the 404 page |
| `navigation` | The sidebar and the table of contents |
| `content` | Tabs, dropdowns, admonitions and the code copy button |
| `markdown-alternates` | The `<page>.md` alternate of a page (`.http` files; isolated target only) |
| `mcp` | The remote MCP server: health, `initialize`, `tools/list` (`.http` files, in memory) |
| `docs-api` | The docs API: health and liveness (`.http` files, in memory) |

## HTTP areas

Areas that need no browser use `.http` files in `http/<area>/`, the format Visual Studio, Rider and VS Code run. Each file is one test, and its responses are in `.snapshots/http/<area>/<name>.verified.responses`.

- `markdown-alternates` sends its requests to the site the target serves. It uses pages of this repository's `docs/` folder, so the assembler and Codex targets skip it with a reason.
- `mcp` and `docs-api` start `Elastic.Documentation.Mcp.Remote` and `Elastic.Documentation.Api` in memory with `WebApplicationFactory`, whatever the target is. They only cover requests that need no credentials: a tool call that searches Elasticsearch has no file yet.
- Page bodies are cut to their first line (`BodyScrubbers.FirstQuotedLine`) and MCP descriptions to an ellipsis (`BodyScrubbers.WithoutProse`), so a snapshot changes when behaviour changes and not when the wording does. `Mcp.Remote.Tests` still snapshots the full handshake.

## Write a journey

```text
set viewport 1280 800
open /syntax/tabs
expect checked find role radio --name "Tab #1 title"
find nth 1 .tabs-label click
expect checked find role radio --name "Tab #2 title"
```

- Start with `set viewport 1280 800`. At the default size the site uses its mobile layout, where a click on a sidebar link does nothing and reports no error.
- After a click on a link, wait for the new address with `wait --url <glob>`. The site navigates with htmx and not a full page load, so the next step would otherwise still see the old page.
- Find controls by role and accessible name first (`find role link click --name Admonitions --exact`). Use a CSS selector, quoted, when no role fits. `find nth` counts from zero.
- A journey that only uses `expect` produces an empty transcript. Use `get text` or `snapshot -c -s <selector>` where you want the page content in the snapshot.
- Accept new or changed transcripts with `SNAPSHOT_ACCEPT=1 mise run journeys`, then read the diff of the `.verified.journey` files.
