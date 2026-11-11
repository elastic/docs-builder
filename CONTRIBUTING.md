# Contributing

## Prerequisites

### Native with mise (recommended for active development)

Install [mise](https://mise.jdx.dev/getting-started.html) and, from the repository root:

```bash
mise trust        # once per clone: mise.toml runs shell (tasks, the postinstall hook), so mise asks you to trust it
mise install      # .NET SDK and Node at the versions in global.json and .nvmrc, the dotnet tools, the git hooks
mise run setup    # also installs the frontend dependencies (npm ci)
mise tasks        # list everything you can run
```

Trust the root git folder (the main checkout) **once**: every git worktree of that clone is trusted too, so a new worktree needs no `mise trust`. A separate clone is a separate trust decision.

mise reads the SDK version from `global.json` and the Node version from `.nvmrc`, so those files stay the single source of each version. `mise install` also restores the local dotnet tools in `dotnet-tools.json` (husky, curb, minver — no workload install required) and installs the git hooks.

The everyday tasks:

| Task | Does |
|---|---|
| `mise run serve` | Serve `docs/` with live reload |
| `mise run build` | Build the solution |
| `mise run test` | Unit tests (extra args pass through: `mise run test -- --test-parallelism 4`) |
| `mise run integrate` | Integration tests (slow, clones repositories) |
| `mise run lint` / `mise run fmt` | Check / fix C# formatting |
| `mise run watch` | The watch loop described [below](#continuously-build-during-development) |
| `mise run site:build`, `site:test`, `site:lint`, `site:fmt`, `site:check` | The frontend scripts in `src/Elastic.Documentation.Site` |
| `mise run schema` | Regenerate `docs/cli-schema.json` |

`./build.sh` and `./dev.sh` still work. The mise tasks call the same build project and scripts.

### Docker, without local installs (`./dev.sh`)

If you only need to validate rendering or authoring changes, the Docker setup removes the .NET/Node prerequisite. It is not the primary development interface: use it when you don't want to install anything. Run `./dev.sh <command>` directly: `mise run` would install the native .NET and Node toolchain first, which this route avoids.

```bash
./dev.sh build            # build images (once, or after Dockerfile changes)
./dev.sh isolated-serve   # serve docs with hot reload at http://localhost:3000
./dev.sh test             # run the full unit-test suite
```

The same setup runs the assembled site and the codex. All three modes use the same verbs
(`<mode>-serve`, `<mode>-build`, `<mode>-watch`, and `<mode>-clone` where the mode clones repositories):

| Mode | Serves | URL | Commands |
|---|---|---|---|
| `isolated` | One docset (`docs/`) | http://localhost:3000 | `isolated-serve`, `isolated-build` |
| `assembler` | Every repository under one navigation | http://localhost:4000 | `assembler-clone`, `assembler-build`, `assembler-serve`, `assembler-watch` |
| `codex` | The codex portal | http://localhost:4001 | `codex-clone`, `codex-build`, `codex-serve`, `codex-watch` |

```bash
./dev.sh assembler-clone    # clone the repositories (first run, needs network)
./dev.sh assembler-watch    # rebuild and reload on every source change
```

Notes for the assembler and codex modes:

- They run from source in the tooling image, so local code changes apply without a publish.
- Builds are static and the site is down while one runs. `-watch` rebuilds on each change to `src/`, `config/`, or `docs/`, then reloads open pages.
- Clones and build output live in named volumes. `./dev.sh clean` removes them.
- The image has no `ssh`, so `dev.sh` clones over HTTPS. Private repositories need `GITHUB_TOKEN` or a logged-in `gh`.
- The serve commands publish a fixed port and reject `--port`.
- `codex` reads its config from `CODEX_CONFIG` (default `config/codex.example.yml`).
- The assembler builds the API Explorer pages. Set `FEATURE_ASSEMBLER_API_EXPLORER=false` to skip them and build faster. After changing it, run `./dev.sh assembler-build --no-assume-build` once, because the build stamp ignores environment variables.

Run `./dev.sh help` for the full list of commands. Docker files live under `build/dev/`.

## Validate the fully assembled documentation

```bash
./dev.sh assembler          # clone, build and serve every repository under one navigation
./dev.sh assembler-watch    # rebuild and serve on every source change
```

The site is served at http://localhost:4000. See [Docker, without local installs](#docker-without-local-installs-devsh) for the other modes and the notes on private repositories.

## Continuously build during development

### Single-docset watch (authoring, templates, frontend)

```shell
mise run watch
```

Monitors code, Razor template files, and frontend assets for a **single documentation set**.
Markdown files refresh via live reload without a recompile. Code or layout changes relaunch
the server automatically. Web assets are rebuilt by `vite build --watch` in the background.

To watch the assembled site (assembler, navigation, multi-repo), use `./dev.sh assembler-watch`.

## CLI reference maintenance

When you add or change CLI options in `src/tooling/docs-builder/Commands/`, regenerate the checked-in schema so CI and the published CLI reference stay in sync:

```bash
mise run schema
```

Commit the updated `docs/cli-schema.json` with your code changes. CI compares the schema (ignoring the `version` field) and fails if it has drifted.

The schema drives auto-generated parameter tables and usage synopses on CLI reference pages. For behavior, workflows, and examples that the schema cannot express, also update supplemental files under `docs/cli/` (see [Writing supplemental content](docs/schema-support/cli-schema/supplemental.md)).

## Working on non-trivial changes

Before starting implementation on anything non-trivial — a new directive, a CLI command, a change that touches more than one project — write a short plan first: a few paragraphs (in the PR description, a linked issue, or handed to an agent as a prompt) stating what's changing and an explicit definition of done. This isn't bureaucracy for its own sake: it's what lets a change be scoped to a single sitting/session instead of sprawling as the "actual" requirements get discovered mid-implementation. Trivial fixes (typos, one-line bug fixes, dependency bumps) don't need this.

# Release Process

This section outlines the process for releasing a new version of this project.

## Versioning

This project uses [Semantic Versioning](https://semver.org/) and its version is
automatically determined by [release-drafter](https://github.com/release-drafter/release-drafter)
based on the labels of the pull requests merged into the `main` branch.

See the [release-drafter configuration](./.github/release-drafter.yml) for more details.

## Git Hooks with Husky.Net

This repository uses [Husky.Net](https://alirezanet.github.io/Husky.Net/) to automatically format and validate code before commits.

### What Gets Checked

**Pre-commit hooks** (run on staged files):
- **dotnet-lint** - Checks C# formatting with [curb](https://curb.nullean.net) (`dotnet curb check .`)
- **prettier** - Formats TypeScript, JavaScript, CSS, and JSON files in `src/Elastic.Documentation.Site/`
- **typescript-check** - Type checks TypeScript files with `tsc --noEmit` (only if TS files are staged)
- **eslint** - Lints and fixes JavaScript/TypeScript files

### Installation

Husky.Net is included as a dotnet tool. `mise install` restores the tools and installs the git hooks, so after cloning the repository (or creating a worktree) run the following. `mise trust` is only needed once per clone, in the root git folder, and covers its worktrees:

```bash
mise trust    # first run in a clone only
mise install
```

That's it! The hooks will now run automatically on every commit.

The hooks run through `mise exec` when mise is installed, so they get the pinned SDK and Node even where mise isn't activated, such as GUI git clients. Without mise they fall back to the `dotnet` and `npx` on your `PATH`.

The pre-commit hook runs `dotnet tool restore` before curb. A new worktree still needs `mise install` once (or `dotnet tool restore && dotnet husky install` without mise) so `.husky/_/husky.sh` exists.

### Usage

Once installed, hooks run automatically when you commit:

```bash
git add .
git commit -m "your message"  # Pre-commit hooks run here
```

**Note:** If hooks modify files (prettier, eslint), the commit will fail so you can review the changes. Simply stage the changes and commit again:

```bash
git add -u
git commit -m "your message"
```

If the **dotnet-lint** hook fails, run `mise run fmt` to fix the formatting and commit the fixes.

### Manual Execution

You can test hooks without committing:

```bash
# Run all pre-commit tasks
dotnet husky run --group pre-commit

# Test individual tasks
dotnet husky run --name dotnet-lint
dotnet husky run --name prettier
dotnet husky run --name typescript-check
dotnet husky run --name eslint
```

### Configuration

Hook configuration is defined in `.husky/task-runner.json`. See the [Husky.Net documentation](https://alirezanet.github.io/Husky.Net/guide/task-runner.html) for more details.

## Creating a New Release

To create a new release trigger the [release](https://github.com/elastic/docs-builder/actions/workflows/release.yml) workflow on the `main` branch.

Every time a pull request is merged into the `main` branch, release-drafter will
create a draft release or update the existing draft release in the [Releases](https://github.com/elastic/docs-builder/releases) page.

To create a new release you need to publish the existing draft release created by release-drafter.

> [!IMPORTANT]
> Make sure the [release-drafter workflow](https://github.com/elastic/docs-builder/actions/workflows/release-drafter.yml) is finished before publishing the release.

> [!NOTE]
> When a release is published, the [create-major-tag workflow](./.github/workflows/create-major-tag.yml)
> will force push a new major tag in the format `vX` where `X` is the major version of the release.
> For example, if the release is `1.2.3` was published, the workflow will force push a new tag `v1` on the same commit.
