# Elastic.Documentation.Site

Shared Razor layout, the frontend bundles, and the design system. The full guide is `docs/development/design-system.md`; the living style guide runs at `/style-guide` under `docs-builder serve`.

## Design system rules

- **Colour never appears in component CSS.** Read a role (`--color-*`) or a scale (`--space-*`, `--radius-*`, `--text-*`). Raw values live only in `Assets/ds/tokens/` and `Assets/ds/themes/`. A test in `tests/Navigation.Tests/DesignSystem/` enforces this, and it also rejects `!important`.
- **Layers are namespaced** (`ds.tokens` … `ds.utilities`, declared in `Assets/ds.css`). Put new rules in the layer that matches their job; don't reorder layers.
- **JavaScript binds to `data-*` attributes and ARIA**, never to styling classes, and pages must be complete HTML without it.
- **Reduced motion means reduced.** Keep small colour, focus and fade transitions; put large movement in `@media (prefers-reduced-motion: no-preference)`.
- **New tokens need a style-guide entry.** Add it to `StyleGuide/StyleGuideTokens.cs`; a test checks the CSS defines every token the guide lists.
- **Components are folder slices, grouped by role.** `Components/<Group>/<Name>/` (Primitives, Forms, Navigation, Chrome, Overlays, Cards, Pages) holds `_<Name>.cshtml`, `<Name>Model.cs`, `<name>.css` and `<Name>Examples.cs`. The namespace mirrors the folders (`Elastic.Documentation.Site.Components.<Group>.<Name>`), `@inherits RazorSlice<...>` uses the fully qualified model type, and sibling components are imported in `Components/_ViewImports.cshtml`. Import the stylesheet from `Assets/ds.css`, register the examples in `StyleGuide/StyleGuide.cs`, and the tests pick them up.
- **Components that render Markdown live in `Elastic.Markdown`.** Their slices, models and examples are in `src/Elastic.Markdown/Components/<Name>/` (namespace `Elastic.Markdown.Components.<Name>`); only their CSS is here, in `Components/Markdown/<Name>/`, because one Vite bundle serves both. That project depends on this one, so it adds its style-guide sections through `StyleGuideCatalog.Add`, called by `MarkdownComponentSections.Register()`. A component that Site's own chrome renders, such as `DiagnosticsPanel` in the footer, stays here.
- **A directive that moves to the design system is handled in `src/Elastic.Markdown/Myst/Directives/DesignSystemDirectives.cs`.** Add a branch there and a flag-on test in `tests/Elastic.Authoring.Tests/DesignSystem/`; the legacy view stays untouched so flag-off output cannot change.
- **Everything is behind `NAVIGATION_PREVIEW`.** Flag off must match the previous output; use `/feature-flag` when touching the flag. With the flag on, the page loads only `ds.css` and `ds.js`, so a feature that needs script or style must be ported into the design-system bundle.

## Layout

| Path                            | Purpose                                                                                                                                      |
| ------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------- |
| `Assets/ds.css`, `Assets/ds.ts` | Design-system bundle entry points (Vite inputs `ds-css`, `ds`)                                                                               |
| `Assets/ds/`                    | Tokens, themes, reset, base, fonts, and enhancement modules                                                                                  |
| `Assets/ds/patterns/layout.css` | The named page layouts (`layout--single`, `--two`, `--three`, `--hub`) and the content `grid`; new pages start from one                      |
| `Assets/ds-guide.css`           | Style-guide-only scaffolding; never loaded on product pages                                                                                  |
| `StyleGuide/`                   | Style-guide pages (Razor slices), the page catalog, and the token metadata                                                                   |
| `visual/`                       | Playwright screenshot checks of the flag-off site across `serve` and `assembler serve`; baselines are seeded from `main` and never committed |

## Commands

```bash
npm run build            # Vite bundles, including ds.css / ds.js
npm run visual:seed      # build origin/main in a temp worktree and save its output as the baselines (local, git-ignored)
npm run visual:check     # build the assembler site from this branch and compare it with those baselines
dotnet test tests/Navigation.Tests -- --treenode-filter "/*/Elastic.Documentation.Navigation.Tests.DesignSystem/*/*"
```
