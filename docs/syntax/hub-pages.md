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
| [`{intro}`](intro.md) | Small "getting started" callout panel with a teal accent bar. |
| [`{whats-new}`](whats-new.md) | "What's new" panel populated from `config/whats-new.yml`. |
| [`{card-group}`](card-group.md) | Section heading + card grid container. |
| [`{link-card}`](link-card.md) | Rich card with title, description, primary link list, and optional aside. |

## Page skeleton

```markdown
---
layout: hub
---

:::{hero}
:icon: kibana
:version: v9 / Serverless (current)
:versions: v8,v7
:quick-links: Install=/install,Tutorial=/tutorial
:releases: Latest&#58; [Stack 9.4.1](/rn) (Mar 28, 2026)

# Kibana

The UI for the Elasticsearch platform.
:::

:::{on-this-page}
:::

:::{intro}
**New to Kibana?** [Take the tutorial](/tutorial) — 30 minutes, hands-on.
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
