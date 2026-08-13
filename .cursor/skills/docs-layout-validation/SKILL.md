---
name: docs-layout-validation
description: >-
  Guides validation of documentation page layout: sidebar, main column width, TOC column, and
  `_Layout.cshtml` changes via isolated `serve`. Use when the user works on sidebar/main spacing,
  full-bleed nav, `doc-page-main`, Nav V2 chrome, or asks if a layout tweak shows in local preview
  versus assembler-only behavior.
---

# docs-layout-validation

## Purpose

Use when the task involves:

- Sidebar / pages nav layout or width
- Main content width, max-width, centering, or horizontal spacing
- Changes to `src/Elastic.Markdown/_Layout.cshtml` or related layout CSS
- Judging whether a layout tweak is visible in **local isolated preview** or needs **assembler**

## Relevant files

- **Primary Razor layout:** `src/Elastic.Markdown/_Layout.cshtml`
- **Sidebar partial:** `src/Elastic.Documentation.Site/Layout/_PagesNav.cshtml`
- **Nav tree / V2 markup:** `src/Elastic.Documentation.Site/Navigation/_TocTree.cshtml`, `_TocTreeNavV2.cshtml`
- **Shared layout CSS (tokens, sidebar chrome, `doc-page-main`):** `src/Elastic.Documentation.Site/Assets/styles.css`
- **CLI for preview:** `src/tooling/docs-builder/docs-builder.csproj`

After changing `Assets/*.css`, ensure `Elastic.Documentation.Site` (or full docs-builder build) has run so `_static/styles.css` is regenerated; it is not committed and isolated `serve` uses the built static bundle.

## What isolated `serve` validates

Good source of truth for:

- Grid/flex structure of sidebar + main + TOC column (as rendered by `Elastic.Markdown` default layout)
- Spacing, padding, max-width rules applied in that layout path
- Responsive behavior at **md+** vs stacked mobile layout where applicable
- Whether Razor/CSS changes compile and render in **BuildType.Isolated**

## What isolated `serve` does not replace

Requires **assembler build** + **`assembler serve`** (or deployed assembly) to validate:

- **elastic-nav** and header chrome from assembler
- **Global** navigation from `navigation-v2.yml` / assembler navigation pipeline
- Final HTML snapshot behavior under `.artifacts/assembly`

## Validation checklist

1. **Identify touched files** (Razor vs CSS vs both) and whether behavior is layout-only or navigation-data-driven.
2. **Rebuild** what embeds views or generates CSS:
   - `dotnet build src/tooling/docs-builder/docs-builder.csproj` (and Site project if only CSS in `Assets/` changed without going through full build).
3. **Run isolated preview** (see project skill `docs-builder-serve` or rule `docs-builder`): `serve` on `http://localhost:3000/`.
4. **Open a normal Markdown doc page** (not only landing / full-search / special layouts) if the change targets the default doc layout.
5. **Check in the browser**
   - Sidebar position and width (including full-bleed vs centered shell)
   - Main + “On this page” column alignment
   - Regressions at **md** and **lg** if the change targets breakpoints
6. **Report**
   - What files drive the visible change
   - Confirmed or not in isolated `serve`
   - Whether **assembler** validation is still recommended (elastic-nav / global nav / static output freshness)

## Response style

Be explicit about:

- What **can** be validated with isolated `serve`
- What **still** needs assembler (or a fresh `assembler build` before `assembler serve`)
- Whether symptoms point to **layout/CSS** vs **navigation YAML / cross-links / toc data**

## Guardrails

- Never claim isolated `serve` **fully** validates elastic-nav or the assembled site.
- If symptoms match **assembled** navigation or stale **port 4000** static output, recommend `dotnet build` + `assembler build` (or `assemble`) then `assembler serve`, and a hard refresh.
- Separate **visual/layout** issues from **navigation content** issues (toc, docset, cross-links).
- Do not assume changes under `src/Elastic.Documentation.Site/Assets/` are live without a build that regenerates `_static/`.
