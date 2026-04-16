---
navigation_title: API directives
applies_to:
  stack: ga
  serverless: ga
---

# API directives

API directives provide dynamic content from OpenAPI specifications configured in your docset.

## api-summary directive

Display operations or descriptions from API specifications.

```markdown
:::{api-summary}
:product: kibana
:type: operations
:tag: search
:::
```

### Options

`:product:`
: The product key from your docset's `api:` configuration. If not specified, infers from the current file name (removes `-api-overview.md` suffix).

`:type:`
: Content type to render:
  - `operations` (default) — Table of API operations grouped by tag
  - `description` — Rendered description from OpenAPI info section

`:tag:`
: For operations type, filter to show only operations with this tag. Shows all operations if not specified.

### Operations output

When `:type: operations` (or no type specified), renders a table with:

- Operations grouped by OpenAPI tags
- HTTP method badges (GET, POST, PUT, DELETE, etc.)
- API endpoint paths
- Links to individual operation pages

Each operation links to `/api/{product}/{operationId}` where `{operationId}` comes from the OpenAPI specification.

### Description output

When `:type: description`, renders the OpenAPI `info.description` field as Markdown. Uses a restricted Markdown pipeline that excludes docset-specific features like substitutions and directives to prevent recursion.

## Examples

### Show all operations for an API

```markdown
:::{api-summary}
:product: kibana
:::
```

### Show operations for a specific tag

```markdown
:::{api-summary}
:product: elasticsearch
:type: operations
:tag: search
:::
```

### Show API description

```markdown
:::{api-summary}
:product: kibana
:type: description
:::
```

## Requirements

- API must be configured in your docset's `api:` section
- OpenAPI specification file must be valid and accessible
- Product key must match a configured API product

## Error handling

If the directive encounters problems, it renders HTML comments with error messages:

- `<!-- No API configurations found -->` — No APIs configured in the docset
- `<!-- API configuration for 'product' not found -->` — Product key not found
- `<!-- api-summary: unknown :type: 'invalid' (use 'operations' or 'description') -->` — Invalid type option