# Hero

A full-bleed hero band with a product icon, page title, description, and an optional release-status line. Designed for the [hub layout](hub-pages.md) but reusable on any page.

All hero content is supplied via options. The directive body is not used.

## Basic

```markdown
:::{hero}
:icon: kibana
:title: Kibana
:description: The UI for the Elasticsearch platform.
:::
```

The `:title:` option doubles as the page title (no body H1 needed). `:description:` supports inline markdown — links, bold, emphasis.

## Options

| Option | Type | Notes |
|---|---|---|
| `:title:` | string | **Required.** Page heading shown next to the icon. Also picked up as the document's page title. |
| `:description:` | inline markdown | One-line summary shown below the title. Supports bold, italics, and links. |
| `:icon:` | string | Product key. Resolves to an inline SVG via the product-icon lookup. Known keys: `elasticsearch`, `kibana`, `observability`, `security`. Unknown keys fall back to a single-letter chip. |
| `:releases:` | inline markdown | Small status line under the title, supports inline links and bold/italic. |

## Releases

```markdown
:::{hero}
:icon: elasticsearch
:title: Elasticsearch
:description: The distributed search and analytics engine.
:releases: Latest&#58; [Stack 9.4.1](/rn) (Mar 28, 2026) · [Serverless deployed](/srn) Apr 1, 2026
:::
```

The `:releases:` option is inline markdown — same syntax as a single line of body markdown. Use `&#58;` for a literal colon when you don't want YAML to parse the value as a key/value pair.
