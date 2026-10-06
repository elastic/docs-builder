# Explore

The browse-everything section of a [hub page](hub-pages.md). It is a titled band holding a stack of collapsible accordions, and it wraps one or more [`{card-group}`](card-group.md) directives.

A hub's full link list can run to nine or ten sections. Without grouping that is a very long page, so `{explore}` collapses it into a scannable stack.

See the [docs-builder documentation hub](../examples/products/docs-builder.md) for a rendered stack.

## Basic

```markdown
:::::{explore}
:id: explore
:title: Explore the docs toolchain
:intro: Find what you need, organized by task.

::::{card-group}
:title: Quick links
:id: quick-links

:::{link-card}
title: Releases and APIs
links:
  - label: Exporters
    url: /data/exporters/index.md
  - label: API reference
    url: /data/api.md
:::
::::

::::{card-group}
:title: Authoring
:id: authoring

:::{link-card}
title: Syntax
links:
  - label: Directives
    url: /syntax/directives.md
:::
::::
:::::
```

## Options

| Option | Notes |
|---|---|
| `:title:` | **Required.** H2 heading, for example "Explore Elasticsearch". |
| `:intro:` | Intro paragraph below the heading. |
| `:id:` | Section anchor. Use `explore` so `{hero}`'s tertiary action can jump to it. |
| `:collapsed:` | Collapses every accordion, including the first. Add it with no value or set it to `true`. Off by default. |

## What nesting changes

`{explore}` carries no options for individual accordions. Nesting drives everything:

- Each [`{card-group}`](card-group.md) inside becomes one accordion. Its `:title:` is the accordion header.
- The first accordion is expanded. The rest are collapsed. Set `:collapsed:` on the `{explore}` to collapse the first one too.
- A reader can expand as many accordions as they want. Expanding one does not collapse the others.
- Each [`{link-card}`](link-card.md) inside renders as a link column rather than a bordered card.

Toggling uses native `<details>` and `<summary>`, so it works without JavaScript.

## Several Explore sections on one page

Every `{explore}` expands its first accordion by default. On a page with more than one `{explore}`, that opens one accordion per section. To keep a single accordion open on load, leave the first `{explore}` as it is and set `:collapsed:` on the others:

```markdown
:::::{explore}
:id: explore
:title: Explore the docs toolchain

::::{card-group}
:title: Quick links
...
::::
:::::

:::::{explore}
:id: explore-advanced
:title: Go further
:collapsed:

::::{card-group}
:title: Extending
...
::::
:::::
```

## Fence depth

Nesting three directives needs three fence widths. The outer fence always needs one more colon than its deepest child:

| Directive | Fence |
|---|---|
| `{explore}` | `:::::` |
| `{card-group}` | `::::` |
| `{link-card}` | `:::` |
