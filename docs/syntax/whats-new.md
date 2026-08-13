# What's new

A panel with a "New" badge, section title, optional release-notes link list on the right, and rows of recent highlights. Each row has a title, description, and right-aligned meta pill (e.g. `9.4 preview`, `Mar 2026`).

## Centralized lookup (recommended)

Edit content in one place — [`config/whats-new.yml`](../configure/site/products.md) — and any page can render a product's panel with a one-line directive:

```markdown
:::{whats-new}
:product: kibana
:::
```

`config/whats-new.yml` schema:

```yaml
products:
  kibana:
    title: What's new in Kibana
    id: whats-new                     # used as the section anchor
    badge: New                        # optional, default 'New'
    release-links:
      - { label: '9.4', url: /release-notes/kibana }
      - { label: Serverless, url: /release-notes/serverless }
    items:
      - title: Dashboards APIs
        description: Programmatically create and manage dashboards
        link: /api/dashboards
        meta: 9.4 preview
      - title: AI agent skills
        description: Teach AI coding agents to work with the Elastic stack
        link: /ai/agent-skills
        meta: Mar 2026
```

## Inline override

If `:product:` is omitted, the directive expects the same schema as a YAML body. Useful when the page needs a one-off feed that doesn't belong in the central file:

```markdown
:::{whats-new}
title: What's new in this release
items:
  - title: Custom feature A
    description: Details
    link: /feature-a
    meta: 9.4
:::
```

## Options

| Option | Notes |
|---|---|
| `:product:` | Product key from `config/whats-new.yml`. When set, the body is ignored. |

## Anchor

`{whats-new}` registers its `id` as a section anchor — `{on-this-page}` automatically picks it up alongside `{card-group}` sections.
