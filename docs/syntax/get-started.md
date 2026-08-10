# Get started

The onboarding section of a [hub page](hub-pages.md). It gives a new reader one opinionated path to a first success, before they face the full link list.

The section is optional. Skip it when the hero already says what to do next.

See the [docs-builder documentation hub](../examples/products/docs-builder.md) for a rendered section.

## Basic

A hub's onboarding section is three steps. The first offers two equally weighted ways to start, and the rest are single links.

```markdown
:::{get-started}
title: Get started in 3 steps
intro: Install docs-builder, write your first page, then preview and publish it.
steps:
  - title: Install docs-builder
    options:
      - label: Install locally
        description: Install the CLI on your machine.
        code: curl -sSL https://ela.st/docs-builder-install | sh
        language: sh
      - label: Run in a container
        description: No local install needed.
        url: /getting-started/installation.md
        url-label: Container setup
  - title: Write your first page
    description: Author Markdown, add links, and use the directive set.
    link: /getting-started/writing-content.md
    link-label: Start writing
  - title: Preview and publish
    description: Serve the site locally with live reload, then publish it.
    link: /getting-started/serve.md
    link-label: Serve locally
:::
```

The body is YAML, not markdown, like [`{link-card}`](link-card.md).

## Schema

| Field | Notes |
|---|---|
| `title` | **Required.** H2 heading. |
| `intro` | One-line lead below the heading. |
| `steps` | The numbered steps. |

Everything the section offers lives inside a step. An install command belongs in `steps[0].options[]`, which keeps the whole path inside the numbered sequence.

A command in a step option goes through the standard code block, so it gets syntax highlighting and a copy button like every other code block on the site.

Every field except `title` is optional. The [example hub](../examples/products/docs-builder.md) uses each one once, and shows all three step shapes, so you can start from it and delete what you do not need.


## Step shapes

A step takes one of three shapes.

**Plain.** A `title` and a `description`. Nothing is clickable.

```yaml
- title: Preview and publish
  description: Serve the site locally, then publish it.
```

**Link.** Add `link` and `link-label`, and the whole step card becomes clickable.

```yaml
- title: Write your first page
  description: Author markdown, add links, and use the directive set.
  link: /getting-started/writing-content.md
  link-label: Start writing
```

**Options.** Add `options` for two or more equally weighted paths, shown side by side. Each option takes a `label`, a `description`, and either a copyable `code` snippet with its `language`, or a `url` with a `url-label`.

```yaml
- title: Preview and publish
  options:
    - label: Preview locally
      description: Serve the site with live reload while you write.
      code: docs-builder serve
      language: sh
    - label: Publish
      description: Build the site and publish it.
      url: /getting-started/publish.md
      url-label: How to publish
```

Steps are numbered automatically, in source order. The number sits before the title, because the section describes a sequence and the number is what carries that.

## Links

Every `steps[].link` and `options[].url` validates at build time, using the same forms as [`{link-card}`](link-card.md#links).
