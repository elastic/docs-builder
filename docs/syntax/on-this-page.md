# On this page

Inline TOC chip. Auto-collects every [`{card-group}`](card-group.md) and [`{whats-new}`](whats-new.md) on the same page that has both an `id` and a `title`, and renders them as middle-dot-separated anchor links inside a single rounded panel.

## Basic

```markdown
:::{on-this-page}
:::
```

That's the whole directive — no options, no body. Drop it once on a hub page (typically right after the [`{hero}`](hero.md)) and it stays in sync as you add or remove sections.

## How items are collected

For each section directive on the page, an item is added to the chip when:

- The directive declares an `id` (becomes the anchor — `#install`).
- The directive declares a `title` (becomes the link label).

`{card-group}`s with no `:title:` are skipped. `{whats-new}` panels populated from `config/whats-new.yml` get their `id` and `title` from that file; if the entry sets neither, the `{whats-new}` block is skipped.

## Order

Items appear in document order — top to bottom of the page.
