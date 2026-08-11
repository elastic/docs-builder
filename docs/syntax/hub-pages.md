# Hub pages

A hub page is a product-scoped landing page. It gives a reader one 360° view of a product across versions, deployment types, and surfaces.

Hub pages are composed entirely from directives. There is no free-form body content. That constraint is deliberate. It lets every link validate at build time, and it keeps every hub structurally consistent whoever authors it.

See the [Elasticsearch documentation hub](../examples/products/docs-builder.md) for a complete page.

## Enable the layout

Set `layout: hub` in the page frontmatter:

```yaml
---
layout: hub
---
```

## What the layout changes

The hub layout differs from the default page layout in three ways:

- The right-rail table of contents is removed. The version dropdown lives in that rail, so a hub page does not show it.
- The previous and next page navigation is removed.
- The body owns the full width of the content column, so directives can render full-bleed sections.

The left sidebar stays. A reader can move between sibling hubs from there.

## Page title

A hub page has no authored H1. The page title comes from the first `{hero}` directive's `:title:` option.

Title detection tries three sources in order:

1. A top-level H1 in the body.
2. An H1 nested inside a directive.
3. The `:title:` option of the first `{hero}`.

One field therefore drives both the on-page heading and the browser tab title.

## Search

A hub page exists to answer generic queries such as "Elasticsearch docs". Two fields carry that:

- The `{hero}` `:title:` option, which becomes the indexed page title.
- The frontmatter `description`, which becomes the indexed description.

Write both deliberately. The search body indexes the hero title and description only. Section and card titles stay out, so a hub does not compete with the pages it links to on specific queries.

## Directives

| Directive | Purpose |
|---|---|
| [`{hero}`](hero.md) | Identity band. Carries the product icon, the page title, a description, and up to three actions. |

## Page skeleton

```markdown
---
layout: hub
---

:::{hero}
:icon: elasticsearch
:title: Elasticsearch documentation hub
:description: The distributed search and analytics engine at the heart of the Elastic platform.
:::
```
