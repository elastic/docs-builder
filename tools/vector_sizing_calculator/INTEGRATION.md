# Vector Sizing Calculator — Integration Guide

This directory contains **staging / alternate** artifacts for the vector sizing calculator:

| Path | Format | Use case |
|------|--------|----------|
| `index.html` | Standalone HTML | Local use, GitHub Pages, internal sharing |
| `embed.html` | Scoped HTML fragment | Embedding in existing pages (Asciidoctor passthrough) |
| `sizing-section.md` | MyST Markdown | Static (non-interactive) docs-content PR |
| `web-component/` | React + EUI (Vite) | Standalone build (`dist/*.iife.js`) or reference while porting |

The **canonical interactive implementation** for docs-builder lives in:

- `src/Elastic.Documentation.Site/Assets/web-components/VectorSizingCalculator/`
- `src/Elastic.Markdown/Myst/Directives/VectorSizing/` (`{vector-sizing-calculator}` directive)

## Architecture

```
docs-content repo                   docs-builder (source of truth)
─────────────────                   ───────────────────────────────
approximate-knn-search.md           Myst/Directives/VectorSizing/*.cs + *.cshtml
  │                                 Site/Assets/.../VectorSizingCalculator/*.tsx
  │  :::{vector-sizing-calculator}
  │  :::
  ▼
docs.elastic.co  ◄── main asset bundle (Parcel) registers <vector-sizing-calculator>
```

## Option A: Full interactive widget (recommended)

### In docs-builder (already present on this branch)

1. Directive: `src/Elastic.Markdown/Myst/Directives/VectorSizing/`
2. Web component: `src/Elastic.Documentation.Site/Assets/web-components/VectorSizingCalculator/`
3. Entry: `src/Elastic.Documentation.Site/Assets/main.ts` imports the component chunk.

### In docs-content

In `approximate-knn-search.md`, add:

```markdown
### Interactive sizing calculator [_vector_sizing_calculator]

:::{vector-sizing-calculator}
:::
```

## Option B: Static MyST Markdown (no interactivity)

Use `sizing-section.md` as in the original guide: copy into docs-content as a static section.

## Standalone `web-component/` (optional)

```bash
cd tools/vector_sizing_calculator/web-component
npm install
npm run dev
# http://localhost:5173 — uses local `index.html`, not the docs site bundle.
```

For the assembled Elastic docs site, prefer the Site `web-components` path above; this folder is mainly for isolated iteration or producing an IIFE for non-Parcel hosts.
