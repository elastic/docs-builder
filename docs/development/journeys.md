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

`docs-journeys` is a native command-line tool that runs journey files against any URL. It uses the Chrome on the machine it runs on. It checks the site with the `expect` lines and compares no transcripts.

```bash
docs-journeys list --journeys tests/Elastic.Documentation.Journeys.Tests/journeys
docs-journeys run --url https://docs-demo.elastic.dev --journeys journeys --area landing,content --junit junit.xml
```

### What a run knows about the site

Journeys say what they need in header comments (`# @requires build-type:isolated`, `# @requires navigation-preview`, `# @requires !env:prod`, `# @tags smoke`, `# @mutates`). A run decides which journeys apply from three facts, and a journey that does not apply is listed as skipped with its reason, in the console, the JUnit file and the step summary:

- **The environment.** `--env <name>`, or the name of the environment whose host the URL belongs to (`www.elastic.co` is `prod`, `docs-edge.elastic.dev` and its CloudFront origin are `edge`, `docs-demo.elastic.dev` is `demo`, `localhost` is `local`). Any other host is unknown.
- **The build type and the feature flags.** The tool reads the `docs:build-type` and `docs:features` tags from the page `--probe` names (`/docs` by default). `--build-type` and `--feature` (comma separated) override what the page says. A site that predates the tags, or a page that does not answer, leaves them unknown.
- **Unknown is not matched.** A plain condition on something unknown fails and a negated one holds, so `# @requires !env:prod` still runs on a site whose environment is unknown.

The run prints a `context:` line with what it settled on and where each part came from. When every selected journey is skipped, nothing was checked and the command exits with 1.

### Options

| Option | Meaning |
|---|---|
| `--url` | The origin to run against. A journey that opens `/docs` opens it on this origin. |
| `--journeys` | Required. The folder of journeys to run. An empty folder is an error. |
| `--area` | Only these areas, comma separated. An area with no journeys is an error. |
| `--tag` | Only journeys with one of these `# @tags`, comma separated. |
| `--changed-only <ref>` | Only journeys that differ from the git ref (`origin/main`) or are new. No change exits with 0. |
| `--env`, `--build-type`, `--feature`, `--probe` | The context, as above. |
| `--parallel` | How many journeys run at once, in one shared browser. The default is 4. |
| `--allow` | Extra hosts a journey may reach, on top of `*.elastic.co`, `*.elastic.dev`, `*.cloudfront.net` and the third-party hosts the docs pages load. |
| `--real-user-agent` | Do not identify as a monitor. The site's telemetry then runs, and its exports are allowed as writes. |
| `--allow-writes` | Do not refuse requests that are not GET, HEAD or OPTIONS. |
| `--writable` | URL globs a read-only run may write to anyway, comma separated. |
| `--allow-mutations` | Run journeys marked `# @mutates`. |
| `--junit` | Write a JUnit XML report. |
| `--artifacts` | Write screenshots and recordings here, one folder per journey. |

### Identity and read-only by default

A run is safe against a live site unless told otherwise:

- It sends the user agent `... Elastic/Synthetics docs-journeys` and the header `X-Docs-Synthetic-Monitor: true`, and starts at 1280 by 800. The docs frontend switches its telemetry SDK off for that user agent, so monitor traffic stays out of real telemetry. `--real-user-agent` turns this off.
- It refuses every request that is not GET, HEAD or OPTIONS. A page that tries one fails the step with `Blocked POST <url>`. `--allow-writes` turns this off, and `--writable` allows single URLs.
- It skips journeys marked `# @mutates`, which `--allow-mutations` (or `--allow-writes`) runs.

### Listing

`docs-journeys list` prints each journey with its tags, requirements, monitor, alerts and `mutates` flag, and `--json` prints the same as JSON. A journey that does not parse is shown as an error and the command exits with 1.

### Exit codes

The command exits with 1 when any journey fails, when one cannot be parsed (the others still run), or when every selected journey was skipped. It writes a table to `GITHUB_STEP_SUMMARY` when that variable is set.

The binary ships no journeys. The journeys under `tests/Elastic.Documentation.Journeys.Tests/journeys` check the isolated docs-builder test site (`#isolated-header`, the `/syntax/*` pages), so they say `# @requires build-type:isolated` and a run against a deployed assembler site skips them. Pass `--journeys` a folder written for the site you run against.

A release also attaches `docs-journeys-<os>-<arch>.zip`, with the same attestation as the `docs-builder` zip.
