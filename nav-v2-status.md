# nav-v2 prototype — status & plan

Branch: `nav-v2`
Date: 2026-03-20

---

## Goal

Prototype a new information architecture sidebar without touching production nav.
Gated behind the `nav-v2` feature flag (enabled automatically in `dev` and `preview` environments).

The V2 nav:
- Shows label sections (non-clickable headings) instead of bare TOC roots
- Renders the **full tree on every page** (not per-section like V1)
- Has accordion collapse (open one top-level section → others collapse)
- No auto-expand to current page (progressive disclosure)
- Disabled placeholder links styled with `cursor-not-allowed`

Content is built at **the same URL paths as V1** — the V2 flag changes only the sidebar layout.

---

## Current state: build green, awaiting assembler re-build

`./build.sh build` passes (lint + compile + all unit tests).
The assembler serve at `localhost:4000` still shows the **old nav** because it was built before two bugs were fixed. User needs to re-run `assembler build` to get the fixed output.

### Bugs fixed in this session

| # | Bug | Fix |
|---|-----|-----|
| 1 | Feature flag never fired — `assembler.yml` sets `NAV_V2` (underscore) but `FeatureFlags(dict)` stores keys as-is; `IsEnabled("nav-v2")` never matched | Use `featureFlags.Set(key, value)` in `AssemblerBuildService` to normalise keys before lookup |
| 2 | Content built at `/docs/l2/get-started/` instead of `/docs/get-started/` — `SiteNavigationV2` was synthesising a new nav file with `l{depth+1}/` prefixes | Pass the original `SiteNavigationFile` to the base constructor; V2 only changes sidebar rendering |

---

## Files changed

### New files
| File | Purpose |
|------|---------|
| `config/navigation-v2.yml` | Skeleton: 8 label sections matching current top-level nav |
| `src/Elastic.Documentation.Configuration/Toc/NavigationV2File.cs` | YAML model (`LabelNavV2Item`, `TocNavV2Item`, `PageNavV2Item`) + `NavV2FileYamlConverter` |
| `src/Elastic.Documentation.Navigation/V2/LabelNavigationNode.cs` | Non-clickable section heading; implements `INodeNavigationItem` |
| `src/Elastic.Documentation.Navigation/V2/PlaceholderNavigationLeaf.cs` | Disabled placeholder link; implements `ILeafNavigationItem` |
| `src/Elastic.Documentation.Navigation/V2/SiteNavigationV2.cs` | Extends `SiteNavigation`; passes original nav file to base; builds V2 label tree from `navigation-v2.yml` |
| `src/Elastic.Documentation.Site/Navigation/_TocTreeNavV2.cshtml` | V2 Razor partial: labels as `<span>`, placeholders as `aria-disabled`, folders/leaves same as V1 |
| `src/Elastic.Documentation.Site/Assets/pages-nav-v2.ts` | Accordion collapse + current-page marking (no auto-expand) |

### Modified files
| File | Change |
|------|--------|
| `config/assembler.yml` | `NAV_V2: true` under `dev:` and `preview:` feature_flags |
| `src/.../Builder/FeatureFlags.cs` | Added `NavV2Enabled` property |
| `src/.../ConfigurationFileProvider.cs` | `NavV2Deserializer` (reflection-based); optional `NavigationV2File` property; `TryCreateTemporaryConfigurationFile` helper |
| `src/.../NavigationViewModel.cs` | Added `IsNavV2` bool (default `false`) |
| `src/.../Navigation/_TocTree.cshtml` | Branches on `IsNavV2`; wraps V2 tree in `<nav data-nav-v2>` |
| `src/.../AssemblerBuildService.cs` | Normalises feature flag keys; loads `navigation-v2.yml` + instantiates `SiteNavigationV2` when flag on; skips path-prefix duplicate validation for V2 |
| `src/.../GlobalNavigationHtmlWriter.cs` | Detects `SiteNavigationV2`; always returns full V2 nav HTML (cached once as `"nav-v2"`); `SiteNavigationV2Wrapper` exposes `V2NavigationItems` as the tree root |
| `src/.../main.ts` | On `htmx:load`, calls `initNavV2(nav)` when `[data-nav-v2]` is present; otherwise falls back to `initNav()` |

---

## How the V2 nav renders on every page

```
HtmlWriter.RenderLayout (called per page)
  → NavigationHtmlWriter.RenderNavigation(root, navigationItem)
    → GlobalNavigationHtmlWriter detects `globalNavigation is SiteNavigationV2`
      → RenderV2Navigation: renders full V2 tree once, cached as "nav-v2"
      → returns same HTML for every page
```

`_TocTree.cshtml` checks `Model.IsNavV2`:
- **true** → `<nav data-nav-v2><ul id="nav-tree"> ... _TocTreeNavV2 ... </ul></nav>`
- **false** → existing V1 `<ul id="nav-tree"> ... _TocTreeNav ... </ul>`

`main.ts` on `htmx:load`:
- `[data-nav-v2]` present → `initNavV2(nav)` (accordion + current-page marker, no auto-expand)
- absent → `initNav()` (V1 behaviour unchanged)

---

## Next steps / open items

1. **Re-run assembler build** to see V2 nav in the browser.

2. **Verify accordion works** — click a top-level label to expand; click another to confirm the first collapses.

3. **Verify `expanded: true`** — "Reference" section in `navigation-v2.yml` has `expanded: true`; confirm it starts open.

4. **Current-page highlighting** — `pages-nav-v2.ts` marks the active link; confirm it highlights the right item without auto-expanding parents.

5. **`navigation-v2.yml` content** — currently mirrors V1 structure exactly. The whole point of V2 is to let you freely rearrange this file. The skeleton has these top-level labels:
   - Get Started, Solutions, Manage Data, Explore & Analyze, Deploy & Manage, Cloud Account, Troubleshoot, Reference

6. **Placeholder / page crosslinks** — `page:` items in `navigation-v2.yml` currently render as disabled placeholders (prototype shortcut). Wire up real cross-link resolution if needed.

7. **`l{depth+1}/` parallel paths** — dropped for now (content stays at V1 paths). Could be re-added later if the team wants to preview a new IA at separate URLs while keeping V1 live.

8. **Uncommitted** — nothing is committed yet. All changes are working-tree only.

---

## How to test locally

```bash
# from docs-builder root
assembler clone          # if repos not yet cloned
assembler build          # re-build with nav-v2 fixes
assembler serve          # serve at localhost:4000
```

Open `http://localhost:4000/docs/get-started/` — sidebar should show label sections
with accordion behaviour instead of the flat V1 nav.

To disable V2 nav temporarily without changing code:
```bash
FEATURE_NAV_V2=false assembler build
```
