# On this page

A row of links to the sections of a [hub page](hub-pages.md), placed under the [hero](hero.md). It gives a reader a short way to jump to a section without scrolling the whole page.

The directive takes no options and no body. It collects its links from the page, so they stay in sync with the sections.

See the [docs-builder documentation hub](../examples/products/docs-builder.md) for a rendered row.

## Basic

Place the directive directly after the `{hero}`:

```markdown
:::{hero}
:icon: kibana
:title: Kibana documentation hub
:description: The UI for the Elasticsearch platform.
:::

:::{on-this-page}
:::
```

## Which sections are listed

The row lists the hub sections that render as an H2, in page order. Each link uses the section title and points to the section anchor.

| Directive | Listed | Anchor |
|---|---|---|
| [`{get-started}`](get-started.md) | Yes | `get-started` |
| [`{whats-new}`](whats-new.md) | Yes, when it has an `id` and a `title` | The `id` field |
| [`{card-group}`](card-group.md) outside `{explore}` | Yes, when it has an `:id:` and a `:title:` | The `:id:` option |
| [`{explore}`](explore.md) at the default level | Yes, when it has an `:id:` | The `:id:` option |
| [`{explore}`](explore.md) with `:level: 3` | No. It renders as an H3. | |
| [`{card-group}`](card-group.md) inside `{explore}` | No. It renders as an accordion. | |

A section without an anchor cannot be linked, so it is skipped. Headings written in plain markdown are not listed.

If the page has no listed sections, the directive renders nothing.

To keep the row short, group sections under one `{explore}` heading and set `:level: 3` on the others. See [Nested sections](explore.md#nested-sections).

## Appearance

The row sits below the hero rule. Each link carries a grey rule on its left edge, as in the right-hand table of contents on other pages. The rule and the text darken on hover and keyboard focus.

The row wraps on narrow screens.
