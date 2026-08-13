# Docs builder project context

## Project

- This repo contains the docs builder tooling and local doc rendering workflows.
- Main local preview command (after a successful build):

  ```bash
  dotnet run --project src/tooling/docs-builder/docs-builder.csproj --no-build -- serve
  ```

- Default local URL: `http://localhost:3000/`
- `serve` uses the docset at **`docs/`** relative to the current working directory when run from the repo root. Use `-p|--path` to point at another folder.

## Important paths

- Tooling project: `src/tooling/docs-builder/docs-builder.csproj`
- Local docset used for quick validation (default for `serve`): `docs/`
- Layout file commonly touched for sidebar + main structure: `src/Elastic.Markdown/_Layout.cshtml`

## Workflow guidance

- Prefer `serve` with the local `docs/` folder when validating layout or CSS changes quickly.
- `serve` runs in **isolated** mode: isolated header, not the assembler elastic-nav. Sidebar may still use **Nav V2 markup** for styling; that is not the same as the **global** `navigation-v2.yml` tree produced by an assembler build.
- If the task explicitly requires elastic-nav or fully assembled site behavior, use the **assembler** flow (`assembler build` / `assemble`, then `assembler serve`), not `serve` alone.

## Validation expectations

1. Build the CLI (required after C#, Razor, or embedded view changes):

   ```bash
   dotnet build src/tooling/docs-builder/docs-builder.csproj
   ```

2. Run `serve` without rebuilding the project in the same invocation:

   ```bash
   dotnet run --project src/tooling/docs-builder/docs-builder.csproj --no-build -- serve
   ```

## Notes

- Warnings about navigation children or cross-links may appear in logs and do not necessarily block startup.
- Confirm the server is running by checking for: `Now listening on: http://localhost:3000`
- Static CSS/JS under `src/Elastic.Documentation.Site/_static/` is generated (e.g. when building `Elastic.Documentation.Site`); do not assume it is the only source of truth without rebuilding when changing `Assets/`.
