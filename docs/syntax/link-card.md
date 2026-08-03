# Link card

A rich card with a title, description, primary link list, and an optional aside.  Designed to live inside a [`{card-group}`](card-group.md), but renders standalone too.

## Basic

```markdown
:::{link-card}
title: Discover
link: /discover
description: Browse documents, filter, and query your indices in real time.
links:
  - label: Get started with Discover
    url: /discover/get-started
  - label: Use ES|QL in Kibana
    url: /esql
:::
```

The card body is **YAML, not markdown** — the directive expects a fixed schema and renders it. Authors don't write HTML, fences, or any directive options; they fill in fields.

## Schema

```yaml
title: Discover                    # required -- card heading
link: /discover/                   # optional -- makes the title clickable
description: One-paragraph blurb.  # optional
icon: elasticsearch                # optional -- product-keyed inline SVG
variant: es                        # optional accent: es | obs | sec
links:                             # optional -- primary link list
  - label: Get started
    url: /discover/get-started
  - label: Use ES|QL
    url: /esql
aside:                             # optional bottom rail
  label: Panel types
  links:
    - { label: Visualizations, url: /viz }
    - { label: Maps, url: /maps }
    - { label: Text, url: /text }
```

## Variants

`variant: es | obs | sec` adds a left-border accent in the corresponding solution color (yellow / pink / teal). Use for solution cards.

```markdown
:::{link-card}
title: Elasticsearch
link: /solutions/elasticsearch
icon: elasticsearch
variant: es
description: Build powerful search and RAG applications.
links:
  - { label: Solution overview, url: /solutions/elasticsearch }
  - { label: Get started, url: /solutions/elasticsearch/get-started }
aside:
  label: Key features
  links:
    - { label: Vector search, url: /solutions/search/vector }
    - { label: Semantic search, url: /solutions/search/semantic }
:::
```

## Aside

The optional `aside` adds a separator line and a small label + dot-separated inline link list. Use it for supplementary references that don't fit the primary link list (e.g. "Panel types", "Chart types", "Key features").

## Errors

The directive emits build errors when:

- The body isn't valid YAML.
- `title` is missing or empty.
