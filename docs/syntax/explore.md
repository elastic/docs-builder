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
| `:mode:` | Which accordions are open on load: `collapsed`, `first`, or `expanded`. Defaults to `collapsed`. See [Modes](#modes). |

## What nesting changes

`{explore}` carries no options for individual accordions. Nesting drives everything:

- Each [`{card-group}`](card-group.md) inside becomes one accordion. Its `:title:` is the accordion header.
- All accordions start collapsed. Set [`:mode:`](#modes) on the `{explore}` to open the first one or all of them.
- A reader can expand as many accordions as they want. Expanding one does not collapse the others.
- Each [`{link-card}`](link-card.md) inside renders as a link column rather than a bordered card.

Toggling uses native `<details>` and `<summary>`, so it works without JavaScript.

## Modes

The `:mode:` option sets which accordions are open when the page loads. A reader can still open and close any accordion.

| Mode | Behavior |
|---|---|
| `collapsed` | Every accordion is closed. This is the default. |
| `first` | The first accordion is open. The rest are closed. |
| `expanded` | Every accordion is open. |

An unknown value produces a warning, and the section renders as `collapsed`.

## Several Explore sections on one page

Every `{explore}` section applies its own mode. To keep a single accordion open on a page with more than one `{explore}`, set `:mode: first` on one section and leave the others at the default:

```markdown
:::::{explore}
:id: explore
:title: Explore the docs toolchain
:mode: first

::::{card-group}
:title: Quick links
...
::::
:::::

:::::{explore}
:id: explore-advanced
:title: Go further

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
