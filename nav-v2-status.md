# nav-v2 prototype — status & plan

Branch: `nav-v2`
PR: https://github.com/elastic/docs-builder/pull/2927 (draft)
Last updated: 2026-03-24

---

## Goal

Prototype a new information architecture sidebar without touching production nav.
Gated behind the `nav-v2` feature flag (auto-enabled in `dev` and `preview` environments).

The V2 nav:
- Shows label sections (non-clickable headings) instead of bare TOC roots
- Renders the **full tree on every page** (not per-section like V1)
- Has accordion collapse (open one top-level section → others collapse)
- Auto-expands to the current page on direct URL load
- Disabled placeholder links styled with `cursor-not-allowed`
- Content built at **the same URL paths as V1** — only sidebar layout changes

---

## Current state ✓

Build passes. Preview at https://docs-v3-preview.elastic.dev/elastic/docs-builder/docs/2927/

### New nav item types

| Type | YAML key | C# type | Renders as |
|------|----------|---------|------------|
| Section heading | `label:` | `LabelNavigationNode` | Non-clickable uppercase header |
| Placeholder leaf | `title:` only | `PlaceholderNavigationLeaf` | Greyed-out disabled link |
| Placeholder folder | `group:` (no `page:`) | `PlaceholderNavigationNode` | Greyed-out folder with chevron |
| Page-folder | `group:` + `page:` | `PageFolderNavigationNode` | Clickable folder header with chevron |
| Cross-link leaf | `page:` | `PageCrossLinkLeaf` | Normal clickable link |
| TOC section | `toc:` | existing `IRootNavigationItem` | Full TOC tree (as V1) |

### URL scheme

V2 cannot control output file paths — `GlobalNavigationPathProvider` reads `NavigationTocMappings` from `navigation.yml` before `SiteNavigationV2` is constructed. So V2 reuses V1 URLs. Documented in PR description.

Production migration path: wire `NavigationTocMappings` from the V2 file, with optional `path:` overrides on `toc:` entries to pin stable V1 URLs and avoid redirects.

---

## Files changed

### New files

| File | Purpose |
|------|---------|
| `config/navigation-v2.yml` | Full IA structure — 6 top-level labels, nested labels, real `toc:`/`page:` wiring |
| `src/.../Toc/NavigationV2File.cs` | YAML model + `NavV2FileYamlConverter` |
| `src/.../V2/LabelNavigationNode.cs` | Non-clickable section heading |
| `src/.../V2/PlaceholderNavigationLeaf.cs` | Disabled placeholder link |
| `src/.../V2/PlaceholderNavigationNode.cs` | Disabled placeholder folder |
| `src/.../V2/PageCrossLinkLeaf.cs` | Real cross-link leaf (from `page:`) |
| `src/.../V2/PageFolderNavigationNode.cs` | Clickable folder with real URL (from `group:` + `page:`) |
| `src/.../V2/SiteNavigationV2.cs` | Extends `SiteNavigation`; builds V2 label tree |
| `src/.../Navigation/_TocTreeNavV2.cshtml` | V2 Razor partial |
| `src/.../Assets/pages-nav-v2.ts` | Accordion + current-page marking + auto-expand on load |

### Modified files

| File | Change |
|------|--------|
| `config/assembler.yml` | `NAV_V2: true` under `dev:` and `preview:` |
| `src/.../FeatureFlags.cs` | `NavV2Enabled` property |
| `src/.../ConfigurationFileProvider.cs` | `NavV2Deserializer`; `NavigationV2File` property |
| `src/.../NavigationViewModel.cs` | `IsNavV2` bool |
| `src/.../Navigation/_TocTree.cshtml` | Branches on `IsNavV2` |
| `src/.../AssemblerBuildService.cs` | Loads nav-v2 file; instantiates `SiteNavigationV2` |
| `src/.../GlobalNavigationHtmlWriter.cs` | Full V2 nav HTML cached once as `"nav-v2"` |
| `src/.../main.ts` | `initNavV2(nav)` when `[data-nav-v2]` present |

---

## navigation-v2.yml wiring status

| Section | Status |
|---------|--------|
| Elasticsearch fundamentals | Placeholders |
| Install, deploy, and administer | Placeholders |
| The Elasticsearch Platform → Ingest and manage data | Partially wired — "Ingest or migrate" group real pages + tocs; rest placeholders |
| The Elasticsearch Platform → Search, visualize and analyze | Placeholders |
| The Elasticsearch Platform → AI and machine learning | Placeholders |
| Solutions and project types | Placeholders |
| Reference | Commented out (18 real `toc:` entries ready, hidden while team reviews IA) |
| Troubleshooting | Commented out (real `page:` entries ready, hidden while team reviews IA) |

### "Ingest or migrate" detail

- Explicit `page:` entries for all current pages in `manage-data/ingest` and `manage-data/migrate`
- "Migrating your Elasticsearch data" uses `group:` + `page:` (page-folder): parent links to `manage-data/migrate`, four child pages beneath
- "Ingest tools (from Reference)" group: wired with 7 `toc:` entries (fleet, EDOT, integration-docs, search-connectors, logstash, apm, beats) + "Other ingest tools" sub-group (elasticsearch-hadoop, elastic-serverless-forwarder)
- `integration-docs` is `private: true` — absent in PR preview, present in production and localhost

---

## Known limitations

- **Private repos in PR preview**: `integration-docs` (Elastic integrations) is absent from preview builds; appears correctly in production and localhost. Same limitation affects V1.
- **URL paths**: V2 cannot assign new URL prefixes without wiring `NavigationTocMappings` from the V2 file (architectural blocker, documented in PR).

---

## Open items

1. **Remaining placeholders** — most sections still use `title:` placeholders pending IA decisions.
2. **Production migration** — wire `NavigationTocMappings` from V2 file; optional `path:` key on `toc:` entries for zero-redirect URL pinning.
3. **Re-enable Reference / Troubleshooting** — currently commented out; uncomment once team approves structure.

---

## How to test locally

```bash
dotnet run --no-restore --project src/tooling/docs-builder -- assembler build
dotnet run --project src/tooling/docs-builder -- assembler serve
# open http://localhost:4000
```
