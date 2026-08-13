---
name: docs-refresh-3000-4000
description: Refreshes local docs previews on localhost:3000 (isolated serve with FEATURE_NAV_V2=true) and localhost:4000 (assembler snapshot). Run proactively after changes to CSS, Razor/cshtml, navigation, or site assets that affect the sidebar or layout—do not wait for the user to ask. Also use when the user reports stale preview, connection refused, or asks to restart rebuilds.
---

# Docs Refresh 3000/4000

## Purpose

Run the full refresh loop so UI changes are visible in both local previews:

- `http://localhost:3000` → isolated `serve` with `FEATURE_NAV_V2=true`
- `http://localhost:4000` → `assembler serve` from `.artifacts/assembly`

## When to Use (automatic)

**After completing edits** that affect what you see in the browser previews, run this workflow **without being asked**, including:

- `src/Elastic.Documentation.Site/Assets/styles.css` or other CSS
- `*.cshtml` (layout, nav, `_TocTreeNavV2`, etc.)
- `Assets/**/*.tsx` / web components bundled into the site
- navigation-related TypeScript (e.g. `pages-nav-v2.ts`)

Skip only for pure C# backend changes with no UI impact, or when the user explicitly declines a rebuild.

## When to Use (on request)

- User says changes are not reflected on `3000` or `4000`
- Stale CSS or blank/old layout on either port
- User asks to restart both servers or rebuild the snapshot

## Commands (from repo root)

1. **Build** (regenerates static assets consumed by the assembler):

```bash
dotnet build
```

2. **Rebuild assembled snapshot** (required for `:4000`; use skip flag if private clones fail):

```bash
dotnet run --project src/tooling/docs-builder -- assembler build --skip-private-repositories
```

3. **Stop** existing listeners on 3000/4000 (if any):

```bash
pids=$(lsof -t -nP -iTCP:3000 -sTCP:LISTEN 2>/dev/null); [ -n "$pids" ] && kill $pids
pids=$(lsof -t -nP -iTCP:4000 -sTCP:LISTEN 2>/dev/null); [ -n "$pids" ] && kill $pids
```

4. **Start isolated serve** (3000):

```bash
FEATURE_NAV_V2=true dotnet run --project src/tooling/docs-builder -- serve --port 3000
```

5. **Start assembler serve** (4000):

```bash
dotnet run --project src/tooling/docs-builder -- assembler serve --port 4000
```

Background the last two if appropriate; confirm startup with logs or `lsof`.

## Verification Checklist

- Logs show `Application started` / listening on `localhost:3000` and `localhost:4000`
- `lsof -nP -iTCP:3000 -sTCP:LISTEN` returns a process
- `lsof -nP -iTCP:4000 -sTCP:LISTEN` returns a process

If the user still does not see updates, suggest a hard refresh:

- macOS: `Cmd + Shift + R`
- or a private window

## Notes

- `assembler serve` only serves what is in `.artifacts/assembly`; new CSS/Razor needs **`dotnet build` + `assembler build`** before restarting `:4000`.
- Isolated `serve` on `:3000` picks up rebuilt site assets after `dotnet build`; restart if something looks cached.
- `--skip-private-repositories` avoids clone failures (e.g. SAML/private repos) during local preview.
