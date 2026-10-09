---
navigation_title: Design system
---

# Design system

The design system gives Razor views, directive renderers, and client-side enhancements one set of tokens, components, and themes.
It is in progress and ships behind the `NAVIGATION_PREVIEW` feature flag.
With the flag off, pages render exactly as before.

## Style guide

`docs-builder serve` hosts a living style guide at `http://localhost:3000/style-guide`.
It renders real design-system markup and reads every swatch, type size, and spacing value from the tokens, so it can't drift from the product.
The route doesn't depend on the feature flag.

The style guide is only served by `docs-builder serve`.

## Enable the design system on site pages

Set the flag through the environment when you run the server:

```shell
FEATURE_NAVIGATION_PREVIEW=true dotnet run --project src/tooling/docs-builder -- serve
```

When the flag is on, the page head loads `ds.css` and `ds.js` in addition to the existing bundle.
The existing bundle stays until the page chrome moves to design-system components.

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

## Layers

The design system declares its cascade layers under a `ds` namespace so they never merge with other stylesheets' layers:

```css
@layer ds.tokens, ds.theme, ds.reset, ds.base, ds.components, ds.patterns, ds.pages, ds.utilities;
```

Later layers win over earlier ones, so a theme in `ds.theme` overrides tokens without `!important`.

## Themes

The default theme, Bold Minimal, writes each colour role once as `light-dark(light, dark)`.
The mode follows the system.
Setting `data-theme="light"` or `data-theme="dark"` on any element, usually `<html>`, pins the mode for that element and its descendants.
The theme switcher in the style guide persists the choice in `localStorage` under `docs-theme`.
Without JavaScript the page follows the system setting.

To write your own theme, load a stylesheet after `ds.css` that overrides tokens:

```css
@layer ds.theme {
    :root {
        --color-accent: oklch(55% 0.2 300);
        --radius-md: 0;
        --shadow-slab: none;
    }
}
```

The design-system stylesheets target Chrome and Edge 123, Firefox 120, and Safari 17.5 or later, which support `light-dark()` natively.
The bundle sets this floor in the `ds-css` and `ds-guide-css` targets in `package.json`.

## Motion

Motion confirms a change and never decorates.
Under `prefers-reduced-motion: reduce`, small colour, focus, and fade feedback stays.
Large movement, springs, staggered entrances, shimmer, and flashing colour go away.
Components declare their large movement inside `@media (prefers-reduced-motion: no-preference)`.

## JavaScript

Pages are complete HTML without JavaScript.
Enhancements live in `Assets/ds.ts` and its modules, and they bind to `data-*` attributes and ARIA state, never to styling classes.
A theme can therefore change class-based CSS freely without breaking behaviour.
