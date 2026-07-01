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

## Layout

The grid auto-fills 1, 2, or 3 columns based on viewport width using `grid-template-columns: repeat(auto-fill, minmax(310px, 1fr))`. Card heights match within a row.
