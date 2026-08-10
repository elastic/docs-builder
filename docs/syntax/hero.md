# Hero

A full-bleed identity band with a product icon, page title, description, and up to three actions. It is designed for the [hub layout](hub-pages.md), and it works on any page.

All hero content comes from options. The directive body is not used.

See the [Elasticsearch documentation hub](../examples/products/elasticsearch.md) for a rendered hero.

## Basic

```markdown
:::{hero}
:icon: elasticsearch
:title: Elasticsearch documentation hub
:description: The distributed search and analytics engine at the heart of the Elastic platform.
:::
```

The `:title:` option doubles as the page title, so a hub page needs no body H1. See [Page title](hub-pages.md#page-title).

## Options

| Option | Type | Notes |
|---|---|---|
| `:title:` | string | **Required.** Renders as the page `<h1>` next to the icon. Also used as the document title. |
| `:description:` | inline markdown | One-line summary below the title. Supports bold, italics, and links. |
| `:icon:` | string | Product key. Resolves to an inline SVG. Known keys: `elasticsearch`, `kibana`, `observability`, `security`. An unknown key falls back to a single-letter chip. |
| `:primary-action:` | markdown link | First action. Format: `[Label](/url)` or `[Label](#anchor)`. |
| `:secondary-action:` | markdown link | Second action. |
| `:tertiary-action:` | markdown link | Third action. |

## Actions

Each action is a single markdown link. Actions render left to right, in the order primary, secondary, tertiary. Actions are optional. Omit them for a pure identity hero.

```markdown
:::{hero}
:icon: kibana
:title: Kibana documentation hub
:description: The UI for the Elasticsearch platform.
:primary-action: [Get started](#get-started)
:secondary-action: [Browse the docs](/explore-analyze.md)
:::
```

An action whose URL starts with `#` renders with a chevron, to signal an in-page jump.

Action URLs validate at build time. Use one of these forms:

- An in-page anchor, for example `#get-started`.
- A site-absolute path that starts with `/`, for a page in the same repository.
- A cross-link scheme such as `elasticsearch://`, for a page in another repository.
- An external URL.

A relative path such as `foo.md` is rejected. This differs from an inline markdown link, where a relative path resolves against the source file's directory.

## Description markup

`:description:` is a directive option, not a body block, so it never reaches the document pipeline. It renders with the default Markdown pipeline. Basic inline markup works. Substitutions, roles, and link validation do not apply inside it. Keep the description to plain prose.
