# Hero

A full-bleed hero band with a product icon, page title, description, fake search box, version dropdown, quick-action pills, and an optional release-status line. Designed for the [hub layout](hub-pages.md) but reusable on any page.

## Basic hero

```markdown
:::{hero}
:icon: kibana
:version: v9 / Serverless (current)

# Kibana

The UI for the Elasticsearch platform.
:::
```

The first H1 inside the body is the page title. Subsequent paragraphs render as the description below the title.

## Options

| Option | Type | Notes |
|---|---|---|
| `:icon:` | string | Product key. Resolves to an inline SVG via the product-icon lookup. Known keys: `elasticsearch`, `kibana`, `observability`, `security`. Unknown keys fall back to a single-letter chip. |
| `:version:` | string | Label inside the version chip (e.g. `v9 / Serverless (current)`). |
| `:versions:` | comma list | When set, the chip becomes a `<details>` dropdown listing other versions. Items use `Label` (greyed, "soon") or `Label=URL` (clickable). |
| `:quick-links:` | comma list, `Label=URL` | Pill bar below the version chip. |
| `:releases:` | inline markdown | Small status line under the pills, supports inline links and bold/italic. |
| `:search:` | bool, default `true` | Show the placeholder search box. |

## Version dropdown

```markdown
:::{hero}
:icon: kibana
:version: v9 / Serverless (current)
:versions: v8=https://www.elastic.co/docs/products/kibana/8.x,v7

# Kibana
:::
```

`v8=...` is a clickable link. `v7` (no `=URL`) renders greyed out with a "soon" badge.

## Quick links and releases

```markdown
:::{hero}
:icon: elasticsearch
:version: v9 / Serverless (current)
:quick-links: Install=/install,API reference=/api,Release notes=/release-notes
:releases: Latest&#58; [Stack 9.4.1](/rn) (Mar 28, 2026) · [Serverless deployed](/srn) Apr 1, 2026

# Elasticsearch
The distributed search and analytics engine.
:::
```

The `releases` option is inline markdown — it supports the same syntax as a single line of body markdown. Use `&#58;` for a literal colon when you don't want YAML to parse the value as a key/value pair.
