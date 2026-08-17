# What's new

A recency panel for a [hub page](hub-pages.md). A reader who bookmarks a hub wants a quick answer to "what changed recently" without hunting through release notes.

See the [docs-builder documentation hub](../examples/products/docs-builder.md) for a rendered panel.

## Basic

The common case is one line:

```markdown
:::{whats-new}
:product: docs-builder
:::
```

`:product:` looks the key up in `hub-whats-new.yml` at the root of the documentation set. The content is authored once there and every page that names the same product renders the same panel. One edit updates them all.

## Where the content lives

`hub-whats-new.yml` sits beside `changelog.yml` and `redirects.yml`, at the root of the content repository rather than in the build tool. A writer edits the panel without opening docs-builder, and without waiting for a docs-builder release.

```yaml
products:
  docs-builder:
    title: What's new in docs-builder
    id: whats-new
    intro: Recent additions to the toolchain.
    release-links:
      - label: View release notes
        url: /data/release-notes/index.md
    items:
      - title: Hub pages
        description: A product-scoped landing page composed entirely from directives.
        link: /syntax/hub-pages.md
        date: Aug 2026
        tag: Syntax
        featured: true
```

| Field | Notes |
|---|---|
| `title` | H2 heading. |
| `id` | Section anchor. Use `whats-new` so `{hero}`'s secondary action can jump to it. |
| `intro` | One-line lead. |
| `release-links` | Links to the full release notes, shown beside the heading. List more than one when a product has several release streams. |
| `upgrade-link` | An upgrade prompt below the grid. Takes `label` and `url`. |
| `items` | The highlight cards. |

Each item takes a `title`, a `description`, a `link`, a `date` and a `tag`. Mark one item `featured: true` to span two columns.

The `date` renders as you write it. Use sentence case, for example `Aug 2026`.

Every field except `title` is optional. The example file uses each one once, so you can start from it and delete what you do not need.

## Inline body

Omit `:product:` and give the directive the same schema as a YAML body, for a one-off panel that does not belong in the shared file:

```markdown
:::{whats-new}
title: What's new
items:
  - title: Hub pages
    description: A product-scoped landing page.
    link: /syntax/hub-pages.md
    date: Aug 2026
:::
```

## Scope limit

The directive reads the file in the current documentation set. It cannot render another repository's panel.

That is not a syntax gap. Cross-link resolution maps pages through the link index, and a YAML data file is not a page. In an isolated build the other repository is not checked out, so there is no file to read at all.

Every hub page lives in the same repository as its content file, so this costs nothing today.

## Links

Every `release-links[].url`, `upgrade-link.url`, and `items[].link` validates at build time, using the same forms as [`{link-card}`](link-card.md#links).
