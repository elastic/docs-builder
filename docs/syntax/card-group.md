# Card group

A section heading + a card grid container. Wraps one or more [`{link-card}`](link-card.md) directives.

## Basic

```markdown
::::{card-group}
:title: Install and administer
:id: install

:::{link-card}
title: Self-managed
link: /deploy/self-managed
description: Run on your own infrastructure.
links:
  - { label: Docker, url: /deploy/docker }
:::

:::{link-card}
title: Elastic Cloud Hosted
link: /deploy/cloud
description: Managed deployments on AWS, GCP, or Azure.
:::
::::
```

The outer fence uses **four** colons (`::::`) so the inner three-colon `:::` fences for the cards aren't interpreted as a closing fence. Use as many extra colons on the outer fence as you need to nest cleanly.

## Options

| Option | Notes |
|---|---|
| `:title:` | H2 heading. Optional — without it, only the grid renders. |
| `:intro:` | Optional intro paragraph below the heading. |
| `:id:` | Section anchor. Picked up by [`{on-this-page}`](on-this-page.md). |
| `:variant:` | Optional layout variant. Set to `solutions` to lock a 3-up grid that steps down to 2 then 1 column and tolerates a fourth card without shrinking into a narrow fourth column. |

## Layout

By default the grid auto-fills 1, 2, or 3 columns based on viewport width using `grid-template-columns: repeat(auto-fill, minmax(310px, 1fr))`. Card heights match within a row.

With `:variant: solutions`, the grid is locked to three equal columns (two below 980px, one below 640px). A fourth card wraps to the next row rather than compressing the layout.

## Inside `{explore}`

When a card-group sits inside an [`{explore}`](explore.md) section, it renders as a collapsible **accordion group** instead of a titled grid: the `:title:` becomes the accordion header and the child [`{link-card}`](link-card.md)s render as link columns. No extra options are needed — the `{explore}` ancestor drives the change.
