# Hero

A full-bleed hero band with a product icon, page title, description, and up to three call-to-action buttons. Designed for the [hub layout](hub-pages.md) but reusable on any page.

All hero content is supplied via options. The directive body is not used.

## Basic

```markdown
:::{hero}
:icon: kibana
:title: Kibana documentation hub
:description: The UI for the Elasticsearch platform.
:primary-action: [Get started](#get-started)
:secondary-action: [What's new](#whats-new)
:tertiary-action: [Explore Kibana docs](#explore)
:::
```

The `:title:` option doubles as the page title (no body H1 needed). `:description:` supports inline markdown, including links, bold, and emphasis.

On the hub pages the three actions are in-page jumps to the main sections: **Get started** (`#get-started`), **What's new** (`#whats-new`), and **Explore {product} docs** (`#explore`). An action whose URL starts with `#` renders with a downward chevron to signal an in-page jump. Actions are optional, so omit them for a pure identity hero.

## Options

| Option | Type | Notes |
|---|---|---|
| `:title:` | string | **Required.** Page heading shown next to the icon. Also picked up as the document's page title. |
| `:description:` | inline markdown | One-line summary shown below the title. Supports bold, italics, and links. |
| `:icon:` | string | Product key. Resolves to an inline SVG via the product-icon lookup. Known keys: `elasticsearch`, `kibana`, `observability`, `security`. Unknown keys fall back to a single-letter chip. |
| `:primary-action:` | markdown link | First call-to-action button, given slightly more emphasis. Format: `[Label](/url)` or `[Label](#anchor)`. |
| `:secondary-action:` | markdown link | Second call-to-action button (outline). Format: `[Label](/url)` or `[Label](#anchor)`. |
| `:tertiary-action:` | markdown link | Third call-to-action button (outline). Format: `[Label](/url)` or `[Label](#anchor)`. |

Release cadence (latest version, serverless deployment date, release-notes links) lives in the [`{whats-new}`](whats-new.md) section, not the hero.

## Actions

```markdown
:::{hero}
:icon: elasticsearch
:title: Elasticsearch documentation hub
:description: The distributed search and analytics engine.
:primary-action: [Get started](#get-started)
:secondary-action: [What's new](#whats-new)
:tertiary-action: [Explore Elasticsearch docs](#explore)
:::
```

Each action is a single markdown link. Actions render left to right in the order primary, secondary, tertiary. Anchor links (`#section`) get a downward chevron; other links render as plain buttons.
