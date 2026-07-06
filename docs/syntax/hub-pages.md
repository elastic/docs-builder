---
products:
  - id: elasticsearch
  - id: kibana
---

# Hub pages

Hub pages are product-scoped landing pages — a 360° overview of one product across versions, deployment types, and surfaces. They're enabled by setting `layout: hub` in the page's frontmatter and composed from a small set of dedicated directives.

## Enable the layout

```yaml
---
layout: hub
---
```

The `hub` layout drops the right-rail "On this page" TOC and the prev/next nav. The body owns the full width of the content column so directives can render full-bleed sections.

## Directives

| Directive | Purpose |
|-----------|---------|
| [`{hero}`](hero.md) | Full-bleed page hero with product icon, title, search, version dropdown, and quick-action pills. |
| [`{on-this-page}`](on-this-page.md) | Auto-generated inline TOC linking each section anchor on the page. |
| [`{get-started}`](get-started.md) | Onboarding funnel: intro line, install snippet, tutorial link, and numbered steps. |
| [`{whats-new}`](whats-new.md) | "What's new" panel populated from `config/whats-new.yml`. |
| [`{card-group}`](card-group.md) | Section heading + card grid container. Renders as an accordion group inside `{explore}`. |
| [`{link-card}`](link-card.md) | Rich card with title, description, primary link list, and optional aside. Renders as a link column inside `{explore}`. |
| [`{explore}`](explore.md) | "Explore {product}" section: a stack of collapsible accordion groups wrapping several `{card-group}`s. |

## Page skeleton

```markdown
---
layout: hub
---

:::{hero}
:icon: kibana
:title: Kibana
:description: The UI for the Elasticsearch platform.
:releases: Latest&#58; [Stack 9.4.1](/rn) (Mar 28, 2026)
:::

:::{on-this-page}
:::

:::{get-started}
title: Get started in 3 steps
intro: Spin up Kibana, connect your data, and start exploring in minutes.
tutorial:
  label: Tutorial
  url: /tutorial
steps:
  - icon: launch
    title: Run Kibana
    description: Start locally, run in Docker, or open a free Cloud trial.
:::

:::{whats-new}
:product: kibana
:::

::::{card-group}
:title: Install and administer
:id: install

:::{link-card}
title: Self-managed
link: /deploy-manage/deploy/self-managed
description: Run Kibana on your own infrastructure.
links:
  - { label: Docker, url: /deploy/docker }
  - { label: Configure, url: /deploy/configure }
:::
::::
```

Group the remaining card-groups under an [`{explore}`](explore.md) section so they render
as a stack of collapsible accordions:

```markdown
:::::{explore}
:title: Explore Kibana
:intro: Explore the apps and capabilities that help you act on your data.

::::{card-group}
:title: Install and administer
:id: install

:::{link-card}
title: Self-managed
link: /deploy-manage/deploy/self-managed
links:
  - { label: Docker, url: /deploy/docker }
:::
::::
:::::
```

## Product badges

Every regular (non-hub) page that declares one or more `products:` in its frontmatter automatically gets a clickable badge above its H1, linking to that product's hub page. The badge → hub URL mapping is set per product in [`config/products.yml`](../configure/site/products.md):

```yaml
products:
  kibana:
    display: 'Kibana'
    versioning: 'stack'
    hub: 'products/kibana/9.0'   # path slug -- relative to site root
```

When `hub:` is null, no badge renders for that product.
