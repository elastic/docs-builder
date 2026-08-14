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

## Current state: working ✓

`./build.sh build` passes (lint + compile + all unit tests).
Accordion expand/collapse verified in-browser with Playwright.

### Bugs fixed

| # | Bug | Fix |
|---|-----|-----|
| 1 | Feature flag never fired — `assembler.yml` sets `NAV_V2` (underscore) but `FeatureFlags(dict)` stores keys as-is; `IsEnabled("nav-v2")` never matched | Use `featureFlags.Set(key, value)` in `AssemblerBuildService` to normalise keys before lookup |
| 2 | Content built at `/docs/l2/get-started/` instead of `/docs/get-started/` — `SiteNavigationV2` was synthesising a new nav file with `l{depth+1}/` prefixes | Pass the original `SiteNavigationFile` to the base constructor; V2 only changes sidebar rendering |
| 3 | All label checkboxes shared the same ID (`v2-label-1ACA80E8`) — `ShortId.Create("label")` is a deterministic SHA256 hash; every `<label for="...">` targeted the same first checkbox, so only "Get Started" could ever open | `LabelNavigationNode`: `ShortId.Create("label", label)` — include the label text in the hash |

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

## Verified behaviours ✓

- Label sections visible as non-clickable headings, always expanded, no toggle
- Nested labels (e.g. "Ingest and manage data" inside "The Elasticsearch Platform") also always expanded, no toggle
- `title:` placeholder items render as disabled grey links
- TOC folder nodes within labels still have their own expand/collapse toggle
- Accordion: opening one TOC folder collapses its siblings at the same level

### Label typography (visual hierarchy)

| Level | Style | Example |
|-------|-------|---------|
| Level-1 (top-level) | `text-xs font-semibold uppercase tracking-widest text-ink` + `border-t border-grey-20` separator above | `ELASTICSEARCH FUNDAMENTALS` |
| Level-2 (nested) | `text-xs font-semibold uppercase tracking-widest text-ink/65` | `INGEST AND MANAGE DATA` |
| TOC folder/link | sentence-case, normal weight, clickable | `Deploy and manage` |

Both label levels use the same small-caps treatment; level-1 is distinguished by full ink colour and a thin horizontal rule above each group. Level-2 is 65% opacity to read as subordinate without being illegible.

---

## Proposed new information architecture

The team provided a JSON-defined IA to translate into `navigation-v2.yml`. Below is the structure and how it maps to existing V1 content paths.

### Top-level labels

| Label | Status | V1 content source |
|-------|--------|-------------------|
| Elasticsearch fundamentals | Partial — needs decomposition | `get-started` + ES core concepts (possibly from `elasticsearch://reference`) |
| Install, deploy, and administer | Mostly maps cleanly | `deploy-manage` (deploy, security, users-roles, monitor, upgrade, etc.) |
| The Elasticsearch Platform | Container label — has nested labels (see below) | — |
| Solutions and project types | Maps cleanly | `solutions` (search, observability, security, elasticsearch-solution-project) |
| Reference and resources | Maps cleanly | `elasticsearch://reference/elasticsearch` + `kibana://reference` |
| Troubleshooting | Maps cleanly | `troubleshoot` |

**ISLAND items (omit from nav):**
- Extension points
- Account & preferences

### Nested labels inside "The Elasticsearch Platform"

These are sub-labels grouping level-1 sections, not clickable headings:

```
The Elasticsearch Platform
  ├── [nested label] Ingest and manage data
  │     ├── Ingest or migrate: bring your data into Elasticsearch  → manage-data/ingest + manage-data/migrate
  │     ├── Store and manage data                                  → manage-data/data-store
  │     ├── Manage data lifecycle                                  → manage-data/lifecycle
  │     └── (time series use case)                                 → manage-data/use-case-timeseries
  │
  ├── [nested label] Search, visualize, and do stuff
  │     ├── Query and filter                                        → explore-analyze/query-filter
  │     ├── Discover                                               → explore-analyze/discover
  │     ├── Dashboards                                             → explore-analyze/dashboards
  │     ├── Visualize                                              → explore-analyze/visualize
  │     ├── Alerting                                               → explore-analyze/alerting
  │     ├── Cases                                                  → explore-analyze/cases
  │     ├── Transforms                                             → explore-analyze/transforms
  │     ├── Cross-cluster search                                   → explore-analyze/cross-cluster-search
  │     └── Report and share                                       → explore-analyze/report-and-share
  │
  └── [nested label] AI and machine learning
        ├── AI features (Agent Builder, AI Assistant)              → explore-analyze/ai-features
        ├── Elastic Inference                                      → explore-analyze/elastic-inference
        ├── Machine learning (anomaly detection, NLP, DFA)         → explore-analyze/machine-learning
        └── Scripting                                              → explore-analyze/scripting
```

### Content mapping details

**`manage-data` sub-trees** (single TOC root, decomposed into level-1 sections):
- `manage-data/ingest` — ingest pipelines, agentless, tools, sample data, transform/enrich
- `manage-data/migrate` — migration guides
- `manage-data/data-store` — aliases, data streams, index basics, mapping, templates, text analysis
- `manage-data/lifecycle` — ILM, data tiers, rollup, curator
- `manage-data/use-case-use-elasticsearch-to-manage-time-series-data` — time series use case

**`explore-analyze` sub-trees** (single TOC root, split across two nested labels):
- AI & ML group: `ai-features`, `elastic-inference`, `machine-learning`, `scripting`
- Search/Visualize group: `query-filter`, `discover`, `dashboards`, `visualize`, `alerting`, `cases`, `transforms`, `cross-cluster-search`, `report-and-share`, `find-and-organize`, `workflows`
- Note: `geospatial-analysis` and `numeral-formatting` need placement decision

**`deploy-manage` sub-trees** (single TOC root, stays under "Install, deploy, and administer"):
- `deploy` (Elastic Cloud, ECE, ECK, self-managed)
- `security`, `users-roles`, `manage-spaces`
- `monitor`, `autoscaling`, `production-guidance`
- `upgrade`, `uninstall`
- `distributed-architecture`, `remote-clusters`, `tools`

**`solutions` sub-trees** (stays together under "Solutions and project types"):
- `search` (full-text, AI/semantic, hybrid, RAG, ranking)
- `observability`
- `security`
- `elasticsearch-solution-project`

### Key challenges for the YAML translation

1. **`manage-data` is one TOC root** — can't reference `manage-data/ingest` as a `toc:` directly; only `manage-data` is a valid TOC root. The new IA wants to surface sub-sections as top-level nav items. Approaches:
   - Use `manage-data` as a single `toc:` under the label, accepting the flattened tree — simple but doesn't match the IA
   - Create new separate TOC roots for each sub-tree — requires changes to `docs-content` repo
   - Use `title:` placeholders for sections not yet wired up

2. **`explore-analyze` is one TOC root** — same issue; splitting into "AI/ML" vs "Search/Visualize" groups requires either new TOC roots or navigating the tree via sub-path `toc:` references (if the assembler supports it)

3. **`get-started` scope** — "Elasticsearch fundamentals" is broader than the current `get-started` TOC; it likely needs content from `elasticsearch://reference` (concepts, architecture, etc.)

### Pragmatic V2 YAML approach

Given the above constraints, the most viable near-term approach:

- Keep **single `toc:` references** pointing at existing roots
- Use **nested labels** to visually group them as intended by the IA
- Add **`title:` placeholders** for sections that don't yet have a dedicated TOC root
- This lets the prototype render the intended IA structure immediately, with real links where content exists

---

## Open items / next steps

1. ~~**Rewrite `navigation-v2.yml`**~~ ✓ Done — 6 top-level labels, nested labels inside "The Elasticsearch Platform", `title:` placeholders for the AI/ML sub-sections that don't yet have their own toc roots.

2. ~~**Nested label support**~~ ✓ Done — labels nest at arbitrary depth; YAML parser, builder, and Razor partial all recurse. `LabelNavigationNode.ExpandedByDefault` is now unused (labels are unconditionally expanded); can be removed in a cleanup pass.

3. **Current-page highlighting** — `pages-nav-v2.ts` marks the active link; verify it highlights the right item without auto-expanding parents.

4. **Placeholder / page crosslinks** — `page:` items in `navigation-v2.yml` currently render as disabled placeholders (prototype shortcut). Wire up real cross-link resolution if needed.

5. **`l{depth+1}/` parallel paths** — dropped for now (content stays at V1 paths). Could be re-added later if the team wants to preview a new IA at separate URLs while keeping V1 live.

---

## PR and commit workflow

**PR:** https://github.com/elastic/docs-builder/pull/2927 (draft)

After every commit:
1. `git push` — keep remote branch in sync
2. Update the PR description if the commit meaningfully changes scope

## How to test locally

```bash
# from docs-builder root
dotnet run --no-restore --project src/tooling/docs-builder -- assembler build
dotnet run --project src/tooling/docs-builder -- assembler serve
```

Open `http://localhost:4000/docs/get-started/` — sidebar should show label sections
always expanded, with nested labels visible immediately.

To disable V2 nav temporarily without changing code:
```bash
FEATURE_NAV_V2=false dotnet run --no-restore --project src/tooling/docs-builder -- assembler build
```
