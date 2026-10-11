---
navigation_title: Design system
---

# Design system

The design system gives Razor views, directive renderers, and client-side enhancements one set of tokens, components, and themes.
It ships behind the `NAVIGATION_PREVIEW` feature flag.
With the flag off, pages render exactly as before.

## Style guide

`docs-builder serve` hosts a living style guide at `http://localhost:3000/style-guide`.
It renders real design-system markup and reads every swatch, type size, and spacing value from the tokens, so it can't drift from the product.
The route doesn't depend on the feature flag.

The style guide is only served by `docs-builder serve`.

Besides the components, the guide lists the heroes and landing sections in every variant, and has page previews: whole pages of the isolated, assembler, Codex and API builds.
The previews are written by the real pipeline, not copied from it. A small sample documentation set, including a hub page and a page that uses every directive and role, is built by the real generator with the new design on. The Codex landing and group pages come from the real Codex navigation and views over a few sample sets, and the API pages from the real API Explorer over a made-up spec. The previews need no repository.
Each is also served on its own at `/style-guide/preview/<id>`.

The **Download** button in the guide header downloads a zip with every guide page and preview as static HTML, next to the design-system assets in `_static`.
The pages link to each other and to the assets with relative paths, so the folder opens from disk or can be shared.

## Enable the design system on site pages

Set the flag through the environment when you run the server:

```shell
FEATURE_NAVIGATION_PREVIEW=true dotnet run --project src/tooling/docs-builder -- serve
```

When the flag is on, the page head loads `ds-layers.css`, `ds.css` and `ds.js` and nothing else: not `styles.css`, `main.js` or the build-type stylesheets.
`ds.js` carries its own runtime in `Assets/ds/runtime.ts`, the framework-free parts of `main.ts`: htmx navigation, telemetry, code highlighting, math, diagrams and tables.
`Assets/ds/bundle.test.ts` fails when anything in the `ds.ts` import graph pulls in React, EUI or Emotion.

## Tokens

All design-system CSS lives in `src/Elastic.Documentation.Site/Assets/ds/` and is bundled from `Assets/ds.css`.
Tokens come in three tiers:

| Tier | File | Contents |
|---|---|---|
| Palette | `ds/tokens/primitives.css` | Raw brand colours, named for what they are, such as `--palette-elastic-blue` |
| Scales | `ds/tokens/scales.css` | Type, spacing, radius, z-index, motion, and font stacks |
| Roles | `ds/themes/bold-minimal.css` | Colour roles, such as `--color-text` and `--color-accent`, and the slab shadows |

Components read roles and scales and never raw colour values.
A test fails the build when component CSS contains a hex colour, a colour function, or `!important`.

## Components

Each component is a Razor slice in its own folder under `src/Elastic.Documentation.Site/Components/<Name>/`:

| File | Purpose |
|---|---|
| `_<Name>.cshtml` | The markup. Views only print the model; decisions such as class names live in the model |
| `<Name>Model.cs` | A record with typed parameters, using enums instead of strings for variants |
| `<name>.css` | Styles in the `ds.components` layer, imported from `Assets/ds.css` |
| `<Name>Examples.cs` | The canonical examples, shared by the style guide and the tests |

Call a component from another view with `@await RenderPartialAsync(_Button.Create(new ButtonModel { ... }))`.
The style guide renders every example live and shows the model call and the exact HTML next to it.

Tests render every example and check that:

- no icon name is unknown,
- every class in the markup is defined in the design-system CSS,
- every link and button has an accessible name and every input has a label,
- ids are unique on a page, and
- every component stylesheet is imported by the bundle.

## Directives

With the flag on, these directives render with design-system components instead of their legacy views: admonitions, dropdowns, tabs, steppers, buttons, button groups and fenced code blocks.
`DirectiveHtmlRenderer` asks `DesignSystemDirectives` first; when it handles the block, the legacy view is skipped, and otherwise rendering continues as before.
Parsing, nested Markdown and cross-references are unchanged, so only the markup differs.
Tabs that share a `sync` group stay in step through `Assets/ds/tabs.ts`, which binds to `data-tab-sync-*` attributes and not to the legacy tab classes.
Code blocks keep `pre code`, so the existing client-side highlighter still finds them, and `Assets/ds/copy.ts` reveals the copy button, which is hidden in the HTML because it cannot work without JavaScript.

Flag-on tests live in `tests/Elastic.Authoring.Tests/DesignSystem/`.
Set `NavigationPreview = true` in the `SetupOptions` of an authoring test to render a document with the flag on.

## Content directives

Directives whose legacy markup already used semantic classes keep that markup and gain design-system CSS: agent skills, contributors, related learning, CLI modifiers, applies-to switches, hub pages, tables and mermaid diagrams.
Directives that depended on utility classes or inline handlers are rebuilt as components:

| Directive | Component | Notes |
|---|---|---|
| Image, figure | `Image` | A link to the image that opens a native `dialog` lightbox; without script the link opens the image |
| Image carousel | `Image` | A scroll-snap row; script reveals the previous, next and dot controls |
| Page card | `PageCard` | One whole-row link with one arrow |
| Listing | `Listing` | Page cards with a filter bar that script reveals |
| Diagnostics console | `DiagnosticsPanel` | The developer console on `docs-builder serve`; script polls the build state |

## Pages and layouts

With the flag on, these layouts also use design-system markup:

| Layout | Notes |
|---|---|
| Full-page search | The `SearchPage` component: a form, facets and results drawn by script from the same search API as the dialog. Questions also get a streamed AI answer above the results, from the same endpoint as Ask AI |
| Codex | The same shell as documentation pages, with the search dialog in the header and landing pages built from `codex-card` markup |
| API reference | The documentation shell with the API switchers above the tree as link menus, a jump field that filters the tree, and `api-reference.css`, the previous API styles ported onto tokens |
| Hub | A full-bleed hero band on the gradient with the cluster art, a pastel page wash behind the sections, no sidebar, and a Get started slab built from `GetStartedDesignView.cshtml`. Compare against the Hub board in the design zip |
| Landing, archive, 404, Codex landing, search | Built to the Landing, Archive, NotFound, Codex and Search boards. The footer is light by default and dark on archive and Codex pages, and the Codex header is a dark band |

`Elastic.Markdown/Layout/_DesignSystemShellStart.cshtml` and `_DesignSystemShellEnd.cshtml` are the one place that composes the shell, so documentation and Codex pages share it. They are two partials because a layout writes its body in place, so the body is rendered between them.

## Layouts and grid

Every page is one of a few named layouts, defined in `Assets/ds/patterns/layout.css`. A new page picks one and fills its slots; it does not set its own widths, gutters or breakpoints.

| Layout | Columns | Used by |
|---|---|---|
| `layout layout--single` | One column inside the page box | Archive, search, 404, Codex sections |
| `layout layout--bands` (+ `layout--wash`) | A stack of full-width `band`s, each with its content in `band__content`, which takes the same box as the header's content; `band--hero` for a hero | Hub pages and the landing page |
| `layout layout--two` | Sidebar, content | API pages |
| `layout layout--three` | Sidebar, content, right-hand rail | Documentation articles, Codex documentation |

There are two width modes. **Contained** is the default: the page box is `--layout-max` (84.5rem, 1352px) wide, centred, with a `--layout-gutter` (1rem) on each side, and it is the width of the elastic.co header, which follows the token; the assembler has to be contained. **Full width** is a mode of the page, not of one layout: `FullWidth` on the layout model adds `layout-full` to the body. The header bar is always the whole window, and its content shares `--layout-max` with the page, so in full width both widen together and the sidebar stays under the header's left edge. The sidebar anchors there and the content and rail are centred in the rest, up to `--layout-max-wide` (120rem), so a 4K window is not sparse.

The content is optimised for `--layout-measure` (90ch; `--layout-measure-wide`, 110ch, in full width). The sidebar and rail take what is left of the box after the content and `--layout-gap`: 55% to the sidebar and 45% to the rail, each clamped between its `--layout-nav-min`/`--layout-nav-max` or `--layout-rail-min`/`--layout-rail-max`. Beyond the maxima the content grows (contained) or the group is centred (full width). The parts are `layout__nav`, `layout__main`, `layout__content` and `layout__rail`.

Sizes are in `rem`, so they follow the reader's font size, and the content measure is in `ch`; `px` is only for borders and the three breakpoints. The breakpoints are 1100px (the rail drops above the content), 860px (the sidebar becomes a drawer) and 760px (grids become one column); media queries cannot read custom properties, so they are written out in the CSS.

Gaps between things come from six rhythm steps named for what they separate: `--rhythm-tight` (8px, inside a component), `--rhythm-stack` (16px, running content), `--rhythm-block` (24px, a heading and its content, and between components), `--rhythm-group` (32px), `--rhythm-section` (48px, between sections and the padding of a band) and `--rhythm-band` (72px, the top of a hero and the foot of the last band). `stack` spaces its children with them and `section` and `section--band` pad a band. Pages read these tokens, not raw `--space` steps.

All stylesheets use three breakpoints only, 760, 860 and 1100 pixels; a test fails on any other `@media` width (container queries are exempt).

Repeating blocks inside the content use the content grid: `grid grid--2`, `grid--3` or `grid--4` step down by themselves, and `grid--auto` fits as many columns as `--grid-min` allows. The style guide shows the layouts, drawn to scale in both width modes, at `/style-guide/layouts`, and the grid and rhythm at `/style-guide/grid`.

## Page layout

With the flag on, pages use a design-system shell: header, sidebar, article, right-hand rail and footer.
The article uses the `prose` class, so plain Markdown output is styled by tag from the design-system tokens, and dark mode applies to prose as well.
On narrow screens the sidebar becomes a drawer that opens from a checkbox, so it needs no JavaScript.

The sidebar tree is rendered once per section and shared by every page in it, so `NavigationCurrentMarker` stamps the current page into it for each page, adding `aria-current="page"` and opening the folders above it.
Folders expand with native checkboxes, and `Assets/ds/nav.ts` only restores what the reader opened.
The outline in the rail marks the heading in view through `Assets/ds/toc.ts`.

## Interactive pieces

These replace React components built on EUI. Each renders complete markup on the server and uses a small script only to add behaviour.

| Piece | Replaces | How it works |
|---|---|---|
| Applies-to badge | `applies-to-popover` | A native `details` element holds the panel. Script moves the open panel into the browser's top layer, next to its trigger, so no ancestor can clip it |
| Search | The modal search in the header | A native `dialog`, opened by the header field or `Ctrl+K`. Isolated builds query the Pagefind index and assembled builds query the search API. Results are drawn from a template, and highlights are built from text nodes |
| Page feedback | `page-feedback` | The server renders the whole widget, hidden. Script reveals it, keeps a draft in `sessionStorage` and sends the same payload as before |
| Version and section pickers | `version-dropdown` | A native `details` menu of plain links |
| Vector sizing calculator | `vector-sizing-calculator` | The server renders the whole form, hidden, with native inputs, `datalist` presets and a `details` for advanced settings. `Assets/ds/vector-sizing.ts` reveals it and reuses the existing calculation modules for the arithmetic |
| Ask AI | The React assistant | A button and a native `dialog` side panel. `Assets/ds/ask-ai.ts` streams the answer from the same endpoint as before and renders it as sanitized Markdown. Shown on assembler and Codex pages. Suggestions come from the existing assistant configuration, the conversation is kept in session storage, rate limits are explained, answers list their references, and thumbs feedback is sent to the existing endpoint |
| Overlays | EUI tooltips, toasts and flyouts | `Tooltip` needs no script. `Toast` and `Flyout` reveal their dismiss and open buttons from script. `Banner`, `Skeleton` and `EmptyState` are plain markup |
| Section bar | The secondary navigation | A server-rendered docs bar. The elastic.co header on assembled sites is still loaded by script from elastic.co |

The server renders each of these without the legacy custom elements, so a flag-on page loads no EUI components.

## Layers

The design system declares its cascade layers under a `ds` namespace so they never merge with other stylesheets' layers. The order lives in `Assets/ds-layers.css`, which must load before `ds.css`:

```css
@layer theme, base, components, ds.tokens, ds.theme, ds.reset, ds.base, ds.components, ds.patterns, ds.pages, ds.utilities, utilities;
```

Later layers win over earlier ones, so a theme in `ds.theme` overrides tokens without `!important`.
The order also names the `theme`, `base`, `components` and `utilities` layers that Tailwind used, so a stylesheet written for them still sorts around the design system.

## Themes

The default theme, Bold Minimal, writes each colour role once, with the light and dark values side by side: `var(--ds-l, light) var(--ds-d, dark)`.
`--ds-l` and `--ds-d` are a toggle pair. The one for the active mode is `initial` and the other is empty, so only the active value survives. This is what the native `light-dark()` does, written out so it works in browsers that predate it.
The mode follows the system.
Setting `data-theme="light"` or `data-theme="dark"` on any element, usually `<html>`, pins the mode for that element and its descendants.
The theme declares the roles again on every `[data-theme]` element, so a dark section can sit on a light page.
There are three themes: `light`, `dark` and `black`, a true black for OLED screens. `data-contrast="more"` pushes text and borders further from their surfaces, and `data-motion="reduce"` calms large movement.
The theme switch, in the second navigation bar and in the style guide header, picks light or dark directly and keeps theme, contrast and motion, each with a "match system" choice, in a menu.
The choices persist in `localStorage` under `docs-theme`, `docs-contrast` and `docs-motion`, and a script in `<head>` applies them before first paint.
Without JavaScript the page follows the system settings.

To write your own theme, load a stylesheet after `ds.css` that overrides roles. A theme that doesn't need a dark mode can write plain values:

```css
@layer ds.theme {
    :root {
        --color-accent: oklch(55% 0.2 300);
        --radius-md: 0;
        --shadow-slab: none;
    }
}
```

The design-system stylesheets target Chrome and Edge 111, Firefox 113 and Safari 16.2 or later.
The bundle sets this floor in the `ds-css` and `ds-guide-css` targets in `package.json`.
The floor is set by `color-mix()`, which has no fallback.
`:has()`, container queries and popovers enhance the page where the browser has them and fall back to a simpler layout where it doesn't.

## Heroes, backgrounds and the call to action

A `hero-block` carries a variant class (`spotlight`, `mesh`, `grain`, `dusk`, `halftone`, `blueprint`, `paper`, `split`, `stripe` or `compact`) and its own dark colour scope, so what sits in it reads on it.
Each variant has a dimmed counterpart in dark and in true black. The style guide's Hero variants page shows all of them.

Sections get a background from a `band` modifier: `soft`, `tint`, `dots`, `grid`, `hatch`, `halftone`, `wash`, `wash-a`, `wash-b`, `wash-c`, `ink`, `blue`, `lagoon`, `dusk`, `aurora`, `plain` or `edge-diag`.
Landing pages alternate these instead of leaving sections white. The Backgrounds page lists them, and `cta-band` closes a page with a gradient, a title and one or two actions.

The teal `btn--cta` button is for heroes and bands only, once per hero. A second action in a hero is a secondary button.
The version picker, when a page has one, sits at the top of the left navigation.

## Code blocks

After the client-side highlighter runs, each line of a code block is wrapped in a `code-block__line` element.
Closing and reopening any token that spans lines keeps the text, and so the copy button, unchanged.
A block of six or more lines gets a numbered gutter, a one-line shell block gets a `$` prompt, and a `diff` block tints its added and removed lines.

## Motion

Motion confirms a change and never decorates.
Under `prefers-reduced-motion: reduce`, small colour, focus, and fade feedback stays.
Large movement, springs, staggered entrances, shimmer, and flashing colour go away.
Components declare their large movement inside `@media (prefers-reduced-motion: no-preference)`.

## JavaScript

Pages are complete HTML without JavaScript.
Enhancements live in `Assets/ds.ts` and its modules, and they bind to `data-*` attributes and ARIA state, never to styling classes.
A theme can therefore change class-based CSS freely without breaking behaviour.
