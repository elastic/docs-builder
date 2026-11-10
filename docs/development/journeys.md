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
| `landing` | The home page, the 404 page, a hub page and a group of page cards |
| `navigation` | The sidebar and the table of contents |
| `content` | Tabs, dropdowns, admonitions and the code copy button |
| `chrome` | The isolated header (logo, search button, GitHub link, deployment info) and the footer |
| `page-actions` | The "View as Markdown" and "Edit this page" links |
| `feedback` | The "Was this page helpful?" buttons render with their accessible names |
| `a11y` | The accessibility check on the home page, a content page and the 404 page |

The isolated site has no theme switch, no "report an issue" link and no search that works without the search API, so those have no journeys here. The feedback journey only checks that the buttons render: pressing one sends feedback to the API.

## Write a journey

```text
set viewport 1280 800
open /syntax/tabs
wait --load networkidle
expect checked find role radio --name "Tab #1 title"
find nth 1 .tabs-label click
expect checked find role radio --name "Tab #2 title"
```

- Start with `set viewport 1280 800`. At the default size the site uses its mobile layout, where a click on a sidebar link does nothing and reports no error.
- After `open`, and before the first click, run `wait --load networkidle`. The page attaches its click handlers when its scripts finish loading, and a click before that does nothing and reports no error. With four test runs at once, a sidebar journey without the wait failed in 6 of 16 runs, and none with it.
- After a click on a link, wait for the new address with `wait --url <glob>`. The site navigates with htmx and not a full page load, so the next step would otherwise still see the old page.
- Find controls by role and accessible name first (`find role link click --name Admonitions --exact`). Use a CSS selector, quoted, when no role fits. `find nth` counts from zero.
- A journey that only uses `expect` produces an empty transcript. Use `get text` or `snapshot -c -s <selector>` where you want the page content in the snapshot.
- Accept new or changed transcripts with `SNAPSHOT_ACCEPT=1 mise run journeys`, then read the diff of the `.verified.journey` files.

## Accessibility findings

The `a11y` journeys run Nullean.Cdp's own accessibility rules and record the result in the verified transcript, violations included. The check is not relaxed: a new violation, or a fixed one, changes the transcript and fails the journey until someone reads the diff and accepts it. A transcript that lists violations is the list of known issues, not a statement that they are fine. The transcript names generated class names such as `span.css-16juuyo`, so a change to the header's styles can change it without a real regression.
