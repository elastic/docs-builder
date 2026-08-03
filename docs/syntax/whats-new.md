# What's new

A section with a heading, subtitle, and a "View release notes" link on the right, followed by a grid of highlight cards. Each card has an optional badge, a date and category tag, a title, a description, and a "Read more" link. One card can be marked `featured` to span two columns.

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
    release-links:                        # rendered as "Latest release notes: <label> · <label>"
      - { label: Stack 9.4.1, url: /release-notes/kibana }
      - { label: Serverless · Apr 1, 2026, url: /release-notes/cloud-serverless }
    items:
      - title: Dashboards and visualizations APIs
        description: Programmatically create and manage dashboards
        link: /api/dashboards
        badge: New                    # optional per-card badge
        date: 9.4 preview             # left meta (date or version)
        tag: Dashboards               # right meta (category)
        featured: true                # spans two columns; use for one card
      - title: AI agent skills
        description: Teach AI coding agents to work with the Elastic stack
        link: /ai/agent-skills
        date: Mar 2026
        tag: AI Assistant
```

## Inline override

If `:product:` is omitted, the directive expects the same schema as a YAML body. Useful when the page needs a one-off feed that doesn't belong in the central file:

```markdown
:::{whats-new}
title: What's new in this release
intro: Stay up to date with the latest features.
items:
  - title: Custom feature A
    description: Details
    link: /feature-a
    date: 9.4
    tag: Search
    featured: true
:::
```

## Options

| Option | Notes |
|---|---|
| `:product:` | Product key from `config/whats-new.yml`. When set, the body is ignored. |

## Anchor

`{whats-new}` registers its `id` as a section anchor so it can be linked to directly.
