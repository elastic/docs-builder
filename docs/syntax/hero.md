# Hero

A full-bleed identity band with a product icon, page title, description, and up to three actions. It is designed for the [hub layout](hub-pages.md), and it works on any page.

All hero content comes from options. The directive body is not used.

See the [Elasticsearch documentation hub](../examples/products/docs-builder.md) for a rendered hero.

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

The option names set the order, not the weight. All three render as secondary buttons, with the same styling as the [button](/syntax/buttons.md) directive.

```markdown
:::{hero}
:icon: elasticsearch
:title: Elasticsearch documentation hub
:description: The distributed search and analytics engine.
:primary-action: [Install Elasticsearch](https://www.elastic.co/downloads/elasticsearch)
:secondary-action: [Get started](#get-started)
:tertiary-action: [Syntax reference](/syntax/hero.md)
:::
```

Actions render as buttons, and no button on the site carries an arrow. The arrow belongs to the eyebrow link, which sends the reader onward to the docs home.

Action URLs validate at build time. Use one of these forms:

| Form | Example | Behavior |
|---|---|---|
| In-page anchor | `#get-started` | Jumps to a section on the same page. Does not preload. |
| Site-absolute path | `/syntax/hero.md` | The markdown extension is stripped. The link preloads on hover. |
| Cross-link scheme | `docs-content://get-started/index.md` | Resolves through the link index. Not treated as external, so it does not open in a new tab. |
| External URL | `https://www.elastic.co/downloads/elasticsearch` | Opens in a new tab, with `rel="noopener noreferrer"`. Does not preload. |

A relative path such as `foo.md` is rejected. This differs from an inline markdown link, where a relative path resolves against the source file's directory.

## Description markup

`:description:` is a directive option, not a body block, so it never reaches the document pipeline. It renders with the default Markdown pipeline. Basic inline markup works. Substitutions, roles, and link validation do not apply inside it. Keep the description to plain prose.
