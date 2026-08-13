# Link card

A card with a title, a description, and a list of links. It is designed to sit inside a [`{card-group}`](card-group.md), and it renders standalone too.

See the [docs-builder documentation hub](../examples/products/docs-builder.md) for both rendering modes on one page.

## Basic

```markdown
:::{link-card}
title: Writing content
link: /getting-started/writing-content.md
description: Author a page, add links, and preview it locally.
links:
  - label: Pages and links
    url: /getting-started/pages-and-links.md
  - label: Syntax guide
    url: /syntax/index.md
:::
```

The body is **YAML, not markdown**. The directive expects a fixed schema and renders it, so an author fills in fields rather than writing markup. A missing `title` or invalid YAML fails the build.

## Schema

```yaml
title: Writing content             # required, the card heading
link: /getting-started/serve.md    # optional, makes the title clickable
description: One short blurb.      # optional
icon: elasticsearch                # optional, product-keyed inline SVG
variant: es                        # optional accent: es, obs, or sec
links:                             # optional, the card's links
  - label: Pages and links
    url: /getting-started/pages-and-links.md
```

A card holds one group of links. To present a second group, add a second card. There is no
sub-list, so every group of links reads the same way wherever it appears.

## Variants

`variant: es`, `obs`, or `sec` adds a left border in the matching solution colour. Use it for solution cards.

`icon` takes the same product keys as [`{hero}`](hero.md): `elasticsearch`, `kibana`, `observability`, `security`.

## Inside `{explore}`

Nested in an [`{explore}`](explore.md) section, through a `{card-group}` ancestor, the same YAML renders as a titled link column instead of a bordered card. One thing changes: `description` is dropped, because a column is a pure link index.

## Links

Every `link` and every entry in `links` validates at build time. Use one of these forms:

| Form | Example | Behavior |
|---|---|---|
| Site-absolute path | `/syntax/index.md` | The markdown extension is stripped. The link preloads on hover. |
| Cross-link scheme | `elasticsearch://reference/index.md` | Resolves through the link index. |
| External URL | `https://www.elastic.co/docs/api/doc/elasticsearch` | Opens in a new tab, with `rel="noopener noreferrer"`. |

A relative path such as `foo.md` is rejected. Prefer a cross-link scheme for any page outside the current repository. A site-absolute path that points into another repository's documentation set is validated nowhere.
