---
name: docs-assembler-nav-v2-preview
description: >-
  Rebuilds the assembled documentation snapshot and serves static HTML with the full global sidebar
  from navigation-v2.yml (Nav V2 IA), elastic-nav shell, and dev feature flags. Use when the user needs
  the complete assembler navigation tree—not isolated TOC—localhost:4000, NAV_V2, assembler build/serve,
  stale layout/CSS on port 4000, blank page or connection refused on 4000, missing .artifacts/assembly,
  or "Can not serve empty directory" when starting assembler serve.
---

# docs-assembler-nav-v2-preview

## Purpose

Use when the user wants:

- The **full Nav V2 tree** driven by `config/navigation-v2.yml` (labels, `toc:` sections, placeholders, assembler wiring)
- Preview on **`assembler serve`** (default **port 4000**), not isolated `serve` on 3000
- To refresh `.artifacts/assembly` after layout, Razor, or `Assets/` CSS/JS changes (static HTML lags until rebuild)
- To recover from an empty or failed assembly output before serving

## Context

- **Isolated `serve` (3000)** reuses Nav V2 **components** but feeds them the **local docset TOC** only. It does **not** load `navigation-v2.yml` as the global IA.
- **Assembler build** produces `.artifacts/assembly` with **BuildType.Assembler**, **elastic-nav**, and—when the environment enables it—**`NAV_V2: true`** (see `config/assembler.yml` under `dev` and `preview`).
- Default assembler environment is **`dev`** unless overridden; `dev` includes `NAV_V2: true`.
- **`assembler serve`** only reads **pre-generated** files from `.artifacts/assembly` (plus Debug behavior for `_static`—see `StaticWebHost`). It does not re-run Razor per request.

## Prerequisites

- Commands from the **docs-builder repository root**.
- **Git checkouts** for assembler repos must be valid (e.g. `git rev-parse HEAD` works). If clones are missing or corrupt, run **`assembler clone`** before **`assembler build`**.
- **Network** may be required for cross-link indices during build.
- **GitHub access:** `assembler clone` pulls private `elastic/*` repositories. The machine needs **SSH keys or HTTPS + PAT** authorized for the org (including **SAML SSO** approval for Elastic org). Without that, clone/build will fail with `git fetch` exit **128** or SAML messages—**the agent cannot fix auth**; the user must sign in / authorize keys.

### Without access to private repos (local Nav V2 smoke test)

Use the **global** CLI flag **`--skip-private-repositories`** (stripped by `GlobalCli` before subcommands) on **clone** and **build** so private entries (`cloud`, `integration-docs`, etc.) are omitted. The local `docs/` tree is injected as **docs-builder** content per `AssemblyConfiguration`.

```bash
dotnet run --project src/tooling/docs-builder/docs-builder.csproj --no-build -- --skip-private-repositories assembler clone
dotnet run --project src/tooling/docs-builder/docs-builder.csproj --no-build -- --skip-private-repositories assembler build --environment dev
dotnet run --project src/tooling/docs-builder/docs-builder.csproj --no-build -- assembler serve --port 4000
```

**Caveat:** the assembled site is **incomplete** vs production (missing private docsets). **Nav V2** and **navigation-v2.yml** still render for what remains—enough for UI/IA checks. Documented similarly in `CONTRIBUTING.md` (Aspire / integration tests).

## When the user asks to "run the build" or fix an empty 4000

Execute in order (from repo root), and **report each outcome**:

1. `dotnet build src/tooling/docs-builder/docs-builder.csproj`
2. `dotnet run --project src/tooling/docs-builder/docs-builder.csproj --no-build -- assembler clone`  
   — or with **`-- --skip-private-repositories`** before `assembler clone` if private repos fail (see above).
3. `dotnet run --project src/tooling/docs-builder/docs-builder.csproj --no-build -- assembler build --environment dev`  
   — same optional **`--skip-private-repositories`** prefix if step 2 used it.
4. Verify output exists, e.g. `test -f .artifacts/assembly/docs/index.html` (or `ls .artifacts/assembly/docs/`)
5. Only then: `dotnet run --project src/tooling/docs-builder/docs-builder.csproj --no-build -- assembler serve --port 4000`

If step 2 or 3 fails, **do not** expect step 5 to work; see **Troubleshooting**.

## Steps

1. **Build the CLI** (pick up Razor, C#, and embedded site changes):

   ```bash
   dotnet build src/tooling/docs-builder/docs-builder.csproj
   ```

2. **Ensure sources exist** (if the previous build failed with git errors):

   ```bash
   dotnet run --project src/tooling/docs-builder/docs-builder.csproj --no-build -- assembler clone
   ```

3. **Regenerate assembly output** (this cleans and repopulates `.artifacts/assembly` on success):

   ```bash
   dotnet run --project src/tooling/docs-builder/docs-builder.csproj --no-build -- assembler build
   ```

   Optional: set environment explicitly to match `assembler.yml` (defaults to `dev`):

   ```bash
   dotnet run --project src/tooling/docs-builder/docs-builder.csproj --no-build -- assembler build --environment dev
   ```

4. **Serve the snapshot**:

   ```bash
   dotnet run --project src/tooling/docs-builder/docs-builder.csproj --no-build -- assembler serve --port 4000
   ```

5. **Open the site** — root often redirects to `docs`; dev URI in config is `http://localhost:4000` with `path_prefix: docs`, so start from:

   - `http://localhost:4000/` (redirect), or  
   - `http://localhost:4000/docs/` as needed.

6. **Verify Nav V2** — in the sidebar HTML, look for `data-nav-v2` / `docs-sidebar-nav-v2` on a normal doc page inside the assembled site.

7. **Confirm the server is listening** — logs must include `Now listening on: http://localhost:4000`. Optionally check `lsof -i :4000 -sTCP:LISTEN` or `curl -sI http://localhost:4000/`.

## Troubleshooting: nothing on port 4000 / blank / connection refused

| Symptom | Likely cause | What to do |
|--------|----------------|------------|
| Browser cannot connect; **nothing listening** on 4000 | `assembler serve` never started or crashed on startup | Run serve after a **successful** build; read the console error. |
| **`Can not serve empty directory: …/.artifacts/assembly`** | `.artifacts/assembly` **does not exist** (or tooling treats it as missing) | Run **`assembler build`** successfully first. `StaticWebHost` throws if the directory is absent (`StaticWebHost.cs` checks `dir.Exists`). |
| **`assembler build`** fails with **`git rev-parse HEAD`** / exit **128** | Checkout dir exists but is **not a valid git repo** or has no commits | Re-clone or fix that checkout under the assembler checkouts path (see clone logs). |
| **`assembler clone`** / **`git fetch`** fails with **SAML SSO** / access rights | Host is **not authenticated** to Elastic org repos | User must use PAT + SSO or SSH key **authorized for the org**; retry `assembler clone`. |
| Build succeeded earlier but UI looks old | Serving **stale** snapshot | Re-run **`assembler build`** after code/CSS changes, then serve again. |

**Pre-flight before claiming 4000 works:**

```bash
test -d .artifacts/assembly && test -f .artifacts/assembly/docs/index.html && echo "assembly OK" || echo "assembly missing or incomplete"
```

## One-shot alternative

Equivalent to clone + build + serve in one flow (long-running):

```bash
dotnet build src/tooling/docs-builder/docs-builder.csproj
dotnet run --project src/tooling/docs-builder/docs-builder.csproj --no-build -- assemble --serve
```

(`assemble` may clone and build; `--serve` uses port **4000** by default per product docs.)

## Response template

- **CLI build:** success / failure
- **Assembler build:** success / failure (note if `assembly` was cleaned mid-failure)
- **Serve:** URL and port
- **Nav:** confirm full tree requires successful **assembler** build with **`NAV_V2`** for the chosen environment
- **If stale UI:** remind that **`assembler build`** must complete after code changes before **`assembler serve`**
- **If 4000 is empty:** state whether **port is listening**, whether **`.artifacts/assembly`** exists, and the **last error** from clone/build/serve (auth vs git vs missing directory)

## Guardrails

- Do **not** tell the user that isolated **`serve` on 3000** shows the **`navigation-v2.yml`** global tree.
- Do **not** assume **`assembler serve`** reflects the latest Razor/CSS without a successful **`assembler build`** after those changes.
- If **`assembler build`** fails after cleaning output, **`assembler serve`** may serve nothing or old content—fix clones/build first.
- **`--assume-build`** skips regeneration when output exists; avoid it when the user explicitly needs **fresh** HTML/CSS.

## Related

- Project rule: `.cursor/rules/docs-builder.md`
- Isolated preview skill: `.cursor/skills/docs-builder-serve/SKILL.md`
- Layout validation skill: `.cursor/skills/docs-layout-validation/SKILL.md`
- IA reference: `nav-v2-status.md`, `config/navigation-v2.yml`, `config/assembler.yml` (`feature_flags.NAV_V2`)
