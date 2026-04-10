# Vector Sizing Calculator — Integration Guide

This directory contains three versions of the vector sizing calculator, each suited
for a different deployment path:

| File | Format | Use case |
|------|--------|----------|
| `index.html` | Standalone HTML | Local use, GitHub Pages, internal sharing |
| `embed.html` | Scoped HTML fragment | Embedding in existing pages (Asciidoctor passthrough) |
| `sizing-section.md` | MyST Markdown | Static (non-interactive) docs-content PR |
| `web-component/` | React + EUI web component | Interactive widget for docs.elastic.co |
| `docs-builder-directive/` | C# + Razor | Custom `{vector-sizing-calculator}` directive for docs-builder |

## Architecture

```
docs-content repo                   docs-builder repo              web-component (this dir)
─────────────────                   ──────────────────             ─────────────────────────
                                                                   src/
approximate-knn-search.md           VectorSizingBlock.cs              main.tsx  (custom element)
  │                                 VectorSizingView.cshtml           Calculator.tsx  (React+EUI)
  │ uses directive:                 VectorSizingViewModel.cs          calculations.ts (math)
  │                                     │                             components/
  │  :::{vector-sizing-calculator}      │                                BreakdownChart.tsx
  │  :::                                │
  │                                     ▼                          npm run build
  │                              Outputs HTML:                          │
  │                              <vector-sizing-calculator>             ▼
  │                              </vector-sizing-calculator>     dist/vector-sizing-calculator.iife.js
  │                                                                     │
  ▼                                                                     ▼
docs.elastic.co  ◄──── assembled site loads ────────────────── /_static/*.js
```

## Option A: Full interactive widget (recommended)

This path gives docs.elastic.co an interactive calculator using the exact same EUI
components as the rest of the site.

### Step 1: Build the web component

```bash
cd tools/vector_sizing_calculator/web-component
npm install
npm run build
# Output: dist/vector-sizing-calculator.iife.js
```

### Step 2: PR to elastic/docs-builder

Add the custom directive so that `:::{vector-sizing-calculator}` is a recognized block.

Files to add under `src/Elastic.Markdown/Myst/Directives/VectorSizing/`:
- `VectorSizingBlock.cs`
- `VectorSizingView.cshtml`
- `VectorSizingViewModel.cs`

Patches to existing files:
- `DirectiveBlockParser.cs` — register the directive name
- `DirectiveHtmlRenderer.cs` — wire up the renderer

See `docs-builder-directive/DirectiveBlockParser.patch.md` for exact changes.

### Step 3: Add the JS bundle to the docs site

The built `vector-sizing-calculator.iife.js` needs to be served as a static asset
on docs.elastic.co. The exact mechanism depends on how the docs site handles assets:

- If static assets go in a `_static/` directory, copy the bundle there.
- If there's a CDN/asset pipeline, publish the bundle and reference it.
- Add `<script src="/_static/vector-sizing-calculator.iife.js" defer></script>`
  to the site's base HTML template (similar to how KaTeX is loaded for `{math}` blocks).

### Step 4: PR to elastic/docs-content

In `approximate-knn-search.md`, add the directive inside the
"Ensure data nodes have enough memory" section:

```markdown
### Interactive sizing calculator [_vector_sizing_calculator]

Use the calculator below to estimate disk and off-heap RAM requirements for your
`dense_vector` fields.

:::{vector-sizing-calculator}
:::
```

## Option B: Static MyST Markdown (no interactivity)

If the docs-builder team prefers not to add a new directive, the `sizing-section.md`
file contains a static version using only existing directives (`{tab-set}`, `{math}`,
`{dropdown}`, `{note}`, `{tip}`). This adds comprehensive formulas, tables, and worked
examples but loses the interactive calculator experience.

### PR to elastic/docs-content

Replace the "Ensure data nodes have enough memory" section in `approximate-knn-search.md`
with the contents of `sizing-section.md`.

## Development

```bash
cd tools/vector_sizing_calculator/web-component
npm install
npm run dev
# Opens http://localhost:5173 with live reload
```

The dev server renders `index.html` which loads the `<vector-sizing-calculator>` custom
element. Edit `src/Calculator.tsx` and changes appear immediately.
