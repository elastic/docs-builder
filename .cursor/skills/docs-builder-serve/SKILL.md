---
name: docs-builder-serve
description: >-
  Builds and runs the docs-builder isolated preview server (Kestrel on localhost, default port 3000).
  Use when the user wants local preview, localhost docs, serve mode, layout/CSS validation in isolated
  mode, or to check if the dev server is up and whether log warnings block startup.
---

# docs-builder-serve

## Purpose

Apply this skill when the user wants to:

- Run docs-builder locally for preview
- Open or verify `http://localhost:3000/` (or another `--port`)
- Know if a log line is blocking startup or safe to ignore
- Validate layout or static asset behavior in **isolated** mode (not assembler)

## Context

- **Mode:** `serve` = isolated preview (isolated header, **not** elastic-nav from assembler).
- **Tooling project:** `src/tooling/docs-builder/docs-builder.csproj`
- **Default docset:** `docs/` relative to the **repository root** (current working directory should be the repo root when running commands). Override with `-p|--path` if needed.
- **Default URL:** `http://localhost:3000/` (use `--port <n>` to change)

## Steps

1. **Build the CLI** (required after C#, Razor, or embedded view changes):

   ```bash
   dotnet build src/tooling/docs-builder/docs-builder.csproj
   ```

2. **Start the server** without compiling again in the same invocation:

   ```bash
   dotnet run --project src/tooling/docs-builder/docs-builder.csproj --no-build -- serve
   ```

   Optional: `serve --port 3001` (or another port) if 3000 is busy.

3. **Inspect terminal output**

   - Success: look for `Now listening on: http://localhost:3000` (or the chosen port).
   - **Non-blocking (typical):** cross-link / navigation children warnings during docset load often still allow startup.
   - **Blocking:** process exits, or no “Now listening” line after a clear fatal error.

4. **Report clearly**

   Use the response template below unless the user only asked a narrow question.

5. **How to stop**

   - `Ctrl+C` in the terminal running the server.
   - If the user must free the port without that terminal: `lsof -ti :3000 | xargs kill` (adjust port); only suggest killing PIDs the user owns.

## Response template

- **Build:** success / failure (short reason if failure)
- **Server:** running / not running
- **URL:** `http://localhost:3000/` (or actual port)
- **Mode:** isolated `serve` (not assembler static output)
- **Logs:** brief note on any warnings and whether they block preview
- **Stop:** `Ctrl+C` (and port-kill only if relevant)

## Guardrails

- Do **not** describe elastic-nav as available in isolated `serve`; assembler is a separate flow (`assembler build` / `assemble` + `assembler serve`).
- Do **not** equate isolated sidebar Nav V2 **markup** with the **global** `navigation-v2.yml` tree from an assembler build.
- Treat cross-link / navigation-structure warnings as **non-blocking** unless startup fails or logs show an unhandled exception before listening.
- Run commands from the **docs-builder repository root** so `docs/` and project paths resolve correctly.
