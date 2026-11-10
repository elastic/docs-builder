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

## Run the journeys against a deployed site

`docs-journeys` is a native command-line tool that runs the same journey files against any URL. It is read-only: it runs the `expect` lines and reads nothing into a transcript. It uses the Chrome on the machine it runs on.

```bash
docs-journeys list
docs-journeys run --url https://d3fvim9nkr2bzu.cloudfront.net --area landing,content --junit junit.xml
```

| Option | Meaning |
|---|---|
| `--url` | The origin to run against. A journey that opens `/docs` opens it on this origin. |
| `--area` | Only these areas, comma separated. An area with no journeys is an error. |
| `--journeys` | Run the journeys in this folder instead of the ones next to the binary. |
| `--parallel` | How many journeys run at once, in one shared browser. The default is 4. |
| `--allow` | Extra hosts a journey may reach, on top of `*.elastic.co` and the third-party hosts the docs pages load. |
| `--junit` | Write a JUnit XML report. |
| `--artifacts` | Write screenshots and recordings here, one folder per journey. |

The command exits with 1 when any journey fails, and writes a table to `GITHUB_STEP_SUMMARY` when that variable is set.

The binary ships with the journeys that were in the repository when it was built, so the journeys always match the `docs-builder` version they were released with. Take it from the `edge` image the way workflows take `docs-builder`:

```bash
docker cp "$(docker create --name tc ghcr.io/elastic/docs-builder-journeys:edge)":/app/. ./docs-journeys && docker rm tc
./docs-journeys/docs-journeys run --url https://d3fvim9nkr2bzu.cloudfront.net
```

A release also attaches `docs-journeys-<os>-<arch>.zip`, with the same attestation as the `docs-builder` zip.
