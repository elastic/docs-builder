---
navigation_title: Browser journey tests
---

# Browser journey tests

Journeys are short scripts of browser commands that open a page, interact with it, and check what the page shows. They run in Chrome through [Nullean.Cdp](https://cdp.nullean.net), and `Nullean.Snapshots` compares what they read against a verified transcript. They live in `tests/Elastic.Documentation.Journeys.Tests/`.

## Four ways to run journeys

| Command | Runs | Against | Compares transcripts | Use it to |
|---|---|---|---|---|
| `mise run journeys` | TUnit tests (`Nullean.Snapshots`) | `docs/` served by this task, as an isolated site | yes | change journeys and the site together, and review transcript changes with `dotnet snap review` |
| `mise run journeys:live [site]` | `docs-journeys` | a deployed site (`prod`, `edge`, `demo`), read-only | no, only the `expect` lines | check a deployment, or a pull request's journeys against prod |
| `dotnet snap journey run <glob> --base-url <url>` | `snap` | any site you started | yes | run some journeys outside the test project, with the same output as `docs-journeys` |
| `cdp run <file> --base-url <url>` | `Nullean.Cdp.Cli` | any site | no | debug one journey file; its output is fenced for agents |

TUnit prints its own progress and result blocks. The other three print results as each journey finishes, with skipped
journeys grouped by reason and a failure shown with the journey's lines around the failing one.

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

The tasks `journeys:assembler` and `journeys:codex` serve a built assembler (`.artifacts/assembly`) or Codex (`.artifacts/codex/docs`) site, but every journey in this project checks the isolated site, so on those targets they all skip with a reason and a green run is not coverage of either site. To check a deployed assembler site, run the live journeys with `docs-journeys` below.

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
| `feedback` | The "Was this page helpful?" buttons render with their accessible names (needs the `page-feedback` flag, which the isolated site only turns on in Debug builds) |
| `a11y` | The accessibility check on the home page, a content page and the 404 page. The transcript has a `feedback` and a `plain` variant, because the feedback buttons fail the contrast rule |
| `markdown-alternates` | The `<page>.md` alternate of a page (`.http` files; isolated target only) |
| `mcp` | The remote MCP server: health, `initialize`, `tools/list` (`.http` files, in memory) |
| `docs-api` | The docs API: health and liveness (`.http` files, in memory) |

## HTTP areas

Areas that need no browser use `.http` files in `http/<area>/`, the format Visual Studio, Rider and VS Code run. Each file is one test, and its responses are in `.snapshots/http/<area>/<name>.verified.responses`.

- `markdown-alternates` sends its requests to the site the target serves. It uses pages of this repository's `docs/` folder, so the assembler and Codex targets skip it with a reason.
- `mcp` and `docs-api` start `Elastic.Documentation.Mcp.Remote` and `Elastic.Documentation.Api` in memory with `WebApplicationFactory`, whatever the target is. They only cover requests that need no credentials: a tool call that searches Elasticsearch has no file yet.
- Page bodies are cut to their first line (`BodyScrubbers.FirstQuotedLine`) and MCP descriptions to an ellipsis (`BodyScrubbers.WithoutProse`), so a snapshot changes when behaviour changes and not when the wording does. `Mcp.Remote.Tests` still snapshots the full handshake.

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

## Run the journeys against a deployed site

`docs-journeys` is a native command-line tool that runs journey files against any URL. It uses the Chrome on the machine it runs on. It checks the site with the `expect` lines and compares no transcripts.

```bash
docs-journeys list
docs-journeys run --url https://docs-demo.elastic.dev --area landing,content --junit junit.xml
```

### What a run knows about the site

Journeys say what they need in header comments (`# @requires build-type:isolated`, `# @requires navigation-preview`, `# @requires !env:prod`, `# @tags smoke`, `# @mutates`). A run decides which journeys apply from three facts, and a journey that does not apply is listed as skipped with its reason, in the console, the JUnit file and the step summary:

- **The environment.** `--env <name>`, or the name of the environment whose host the URL belongs to (`www.elastic.co` is `prod`, `docs-edge.elastic.dev` and its CloudFront origin are `edge`, `docs-demo.elastic.dev` is `demo`, `localhost` is `local`). Any other host is unknown.
- **The build type and the feature flags.** The tool reads the `docs:build-type` and `docs:features` tags from the page `--probe` names (`/docs` by default). `--build-type` and `--feature` (comma separated) override what the page says. A page that answers with neither `docs:build-type` nor a `build-type-*` body class (isolated and Codex builds always render the class) is an assembler site that predates both, so the build type is `assembler` and no feature is on. A page that does not answer leaves both unknown.
- **Unknown is not matched.** A plain condition on something unknown fails and a negated one holds, so `# @requires !env:prod` still runs on a site whose environment is unknown.

The run prints a `context:` line with what it settled on and where each part came from. When every selected journey is skipped, nothing was checked and the command exits with 1.

### Options

| Option | Meaning |
|---|---|
| `--url` | The site to run against. For an assembler site given as an origin, journeys open relative paths under `/docs/` (`open get-started` is `https://host/docs/get-started`) and absolute ones from the origin; a URL that names a path is used as given. |
| `--journeys` | A folder of journeys to run instead of the ones compiled into the binary. An empty folder is an error. Needed for `--changed-only`. |
| `--area` | Only these areas, comma separated. An area with no journeys is an error. |
| `--only` | Only these journeys by id (`area/name`), comma separated. An id that does not exist is an error. |
| `--verbose` | List every skipped journey instead of collapsing a reason that more than three share. |
| `--tag` | Only journeys with one of these `# @tags`, comma separated. |
| `--changed-only <ref>` | Only journeys that differ from the git ref (`origin/main`) or are new, in the folder `--journeys` names. No change exits with 0. |
| `--env`, `--build-type`, `--feature`, `--probe` | The context, as above. |
| `--parallel` | How many journeys run at once, in one shared browser. The default is 4. |
| `--allow` | Extra hosts a journey may reach, on top of `*.elastic.co`, `*.elastic.dev`, `*.cloudfront.net` and the third-party hosts the docs pages load. |
| `--real-user-agent` | Do not identify as a monitor. The site's telemetry then runs, and its exports are allowed as writes. |
| `--allow-writes` | Do not refuse requests that are not GET, HEAD or OPTIONS. |
| `--writable` | URL globs a read-only run may write to anyway, comma separated. |
| `--allow-mutations` | Run journeys marked `# @mutates`. |
| `--allow-all-skipped` | Exit with 0 when every selected journey was skipped. The prod check of a pull request uses it, because a change to isolated-site journeys has nothing to run on prod. A failed site probe still exits 1. |
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

Every journey under `tests/Elastic.Documentation.Journeys.Tests/journeys` is compiled into the binary, so one file carries the runner and the journeys that match its build, and `list` and `run` need no folder. The tool decides which of them apply to the site from their `# @requires` lines: the isolated-site journeys (`#isolated-header`, the `/syntax/*` pages) say `build-type:isolated` and a deployed assembler site skips them, and the live journeys (`journeys/live`, one of them a `@monitor`) say `build-type:assembler`. A run where every journey is skipped still exits with 1, so it cannot pass without checking anything.

A release also attaches `docs-journeys-<os>-<arch>.zip`, with the same attestation as the `docs-builder` zip.

## Live journeys

The `live/` folders next to the area folders hold journeys for a published site: prod, edge or demo. They were ported from the Elastic Synthetics journeys in `src/Elastic.Documentation.Site/synthetics/` and keep their assertions. They cannot run against the isolated site, so the test project claims them (`Areas/LiveJourneys.cs`) and shows them as skipped.

A live journey opens paths relative to a base URL that ends in `/docs/`, so `open get-started` opens `/docs/get-started`. Its header says where it applies and whether it is a Synthetics monitor:

```text
# @tags navigation smoke
# @requires build-type:assembler
# @monitor schedule=15
```

The layout changes with the `navigation-preview` flag, so a journey puts the steps that differ in `feature` blocks. `docs-journeys` reads the flags from the site, so a live run needs only the site's name:

```bash
mise run journeys:live demo                                   # every journey that applies to demo
mise run journeys:live prod --area navigation --tag smoke  # a selection
mise run journeys:live demo --only navigation/main-paths   # one journey, by id
```

Results appear as each journey finishes. A reason that more than three journeys share (every isolated-site journey on a deployed site) is shown once with a count, and `--verbose` lists them. A failure is repeated at the end with the journey's path in the checkout, the line, the error and the last page it checked.

`--url` takes `prod`, `edge`, `demo` and `local` (`http://localhost:3000`) as well as an origin.

To debug a single file with Nullean.Cdp's own runner, `cdp run` from `Nullean.Cdp.Cli` takes the same context as flags. The allow list must include the hosts the docs pages load from (`RunCommand.DefaultAllow`), or those requests are refused:

```bash
cdp run tests/Elastic.Documentation.Journeys.Tests/journeys/navigation/live/main-paths.journey \
  --base-url https://docs-demo.elastic.dev/docs/ \
  --build-type assembler --env demo --feature navigation-preview \
  --read-only --viewport 1280x800 \
  --user-agent "Mozilla/5.0 (X11; Linux x86_64) Chrome/130.0.0.0 Safari/537.36 Elastic/Synthetics" \
  --header "X-Docs-Synthetic-Monitor: true" \
  --allow "*.elastic.co" --allow "*.elastic.dev" --allow "*.cloudfront.net" \
  --allow "*.googletagmanager.com" --allow "elastic-global-nav-assets.s3.us-west-2.amazonaws.com"
```

- `--read-only` refuses every request that is not a read, so a live run cannot change the site.
- The `Elastic/Synthetics` user agent switches the page's telemetry off. Without it, a read-only run fails on the telemetry `POST`.
- Pass `--feature assembler-api-explorer` for the API journey, which only applies where that flag is on.

| Site | Flags | Layout |
|---|---|---|
| `https://www.elastic.co/docs/` | none | legacy |
| `https://docs-edge.elastic.dev/docs/` | `assembler-api-explorer` | legacy |
| `https://docs-demo.elastic.dev/docs/` | `assembler-api-explorer`, `navigation-preview` | design system |

## Export the monitors to Elastic Synthetics

A journey marked `# @monitor` can also run as an Elastic Synthetics monitor. Two commands turn the journeys into monitors and send them to Kibana:

```bash
docs-journeys export synthetics --url https://docs-demo.elastic.dev --out synthetics
docs-journeys push --out synthetics --env demo
```

`export synthetics` reads the site the way `run` does (its `docs:*` tags, `--env`, `--build-type`, `--feature`), so a monitor is written only for a journey that applies to that site. It then runs `@nullean/journeys` through `npx`, which writes one file per monitor into `<out>/journeys`. It also writes the project config (`synthetics.config.ts`) and a `package.json`, and installs the two packages the generated files import, because `push` bundles them.

| Option | Meaning |
|---|---|
| `--url`, `--out` | The site and where the monitors go. Both are required. `--journeys` names a folder instead of the embedded journeys. |
| `--env` | The environment's name. Defaults to the one the URL's host belongs to. The export fails when it is unknown, because the name picks the project. |
| `--build-type`, `--feature`, `--probe` | The same as for `run`. |
| `--paging` | Turn `# @alert pagerduty` into alert configuration. Only `--env prod` accepts it: edge, demo and staging monitors never page. |
| `--project` | The Synthetics project id. Default: `elastic-co-docs-<env>`. |
| `--kibana-url`, `--space` | Where the project lives. The defaults are the docs observability cluster and the `default` space. |
| `--allow-empty` | Do not fail when no journey is exported. |
| `--dry-run` | Print the context, the project config and the commands, and write and run nothing. |

An export that produces no monitor fails, and so does a `push` of an output folder that holds none: `push` replaces the project's whole monitor set, so pushing nothing deletes every monitor in it.

`push` runs `@elastic/synthetics push` from the output folder. It reads the API key from the `SYNTHETICS_API_KEY` environment variable and nowhere else, so the key is never an argument and never printed, and it stops before running anything when the variable is missing. `--dry-run` prints the command and whether the key is set, and needs no key.

Each environment is its own project, which is what keeps their monitors apart. The command does not pass `--tags` to `push`: for `push` it is a filter that selects monitors by their own tags, so `--tags env:demo` would push nothing for monitors that do not carry that tag.

The versions of `@nullean/journeys` and `@elastic/synthetics` are pinned in `SyntheticsPlan`, so a push never changes with a release of either tool. Node.js and access to the npm registry are required.

## In CI

`journeys.yml` builds `docs-builder` and the test project once on `ubuntu-24.04`, then runs the journeys in shards with the runner's own Chrome. It runs when the site, its markup, the serving and configuration code, `docs/` or the journey tests change, and on every push to `main`.

`tests/Elastic.Documentation.Journeys.Tests/shards.json` maps each shard to test classes. A new test class must be listed in a shard, or the `plan` job fails; a new folder under `journeys/` runs only through a class that claims it. To split a slow shard, move classes into a new entry in that file.

A failed run uploads a `journeys-results-<shard>` artifact. Extract it at the repository root, where it restores `.artifacts/.snapshots/received/`, and run `dotnet snap review`.

## Checks on pull requests

Two jobs in `journeys-live.yml` need no site build.

- **Journeys stay valid against prod.** When a pull request changes a journey or an `.http` file, the changed ones run read-only against `https://www.elastic.co`. A journey whose `@requires` does not hold on prod is skipped with its reason. The job fails when a journey fails. It passes when every changed journey was skipped (`--allow-all-skipped`), because a change to isolated-site journeys has nothing to run on prod, but not when the site probe failed, since the skips would then say nothing about the site. A journey for a feature that ships with the pull request and is not yet on prod says so with `@requires !env:prod`.
- **The `docs-journeys` image.** When the tool changes, the image builds without being pushed, the binary is copied out of it the way another repository's workflow would, and `docs-journeys list` runs from it. This is also the Linux native build check.

## Accessibility findings

The `a11y` journeys run Nullean.Cdp's own accessibility rules and record the result in the verified transcript, violations included. The check is not relaxed: a new violation, or a fixed one, changes the transcript and fails the journey until someone reads the diff and accepts it. A transcript that lists violations is the list of known issues, not a statement that they are fine. The transcript names generated class names such as `span.css-16juuyo`, so a change to the header's styles can change it without a real regression.
