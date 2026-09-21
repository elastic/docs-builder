# Card group

A section heading and a card grid container. It wraps one or more [`{link-card}`](link-card.md) directives.

See the [docs-builder documentation hub](../examples/products/docs-builder.md) for both rendering modes on one page.

## Basic

```markdown
::::{card-group}
:title: Install and administer
:id: install

:::{link-card}
title: Self-managed
link: /getting-started/installation.md
description: Run on your own infrastructure.
:::

:::{link-card}
title: Serve locally
link: /getting-started/serve.md
description: Preview the site while you write.
:::
::::
```

The outer fence uses **four** colons, so the inner three-colon fences are not read as a closing fence. Add as many extra colons to the outer fence as the nesting needs.

## Options

| Option | Notes |
|---|---|
| `:title:` | H2 heading. Optional. Without it, only the grid renders. |
| `:intro:` | Intro paragraph below the heading. |
| `:id:` | Section anchor. |
| `:variant:` | Set to `solutions` to lock a 3-up grid. |

## Layout

By default the grid auto-fills 1, 2, or 3 columns based on the available width. Card heights match within a row.

With `:variant: solutions`, the grid locks to three equal columns, stepping to two and then one at narrower widths. A fourth card wraps to the next row instead of compressing the layout into a narrow fourth column.

## Inside `{explore}`

Nest a card group in an [`{explore}`](explore.md) section and it renders as a collapsible accordion instead of a titled grid. The `:title:` becomes the accordion header, and each child [`{link-card}`](link-card.md) renders as a link column.

No option controls this. The `{explore}` ancestor drives it. That keeps every card grid elsewhere on the site working unchanged, and it means an author wraps existing groups in `{explore}` rather than learning a second directive.
