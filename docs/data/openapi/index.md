---
navigation_title: OpenAPI
---

# OpenAPI Support

{{dbuild}} can generate API reference pages from an OpenAPI spec.

## API Explorer

Add an `api:` entry in `docset.yml`. Then preview the pages. See [API Explorer](./api-explorer.md) for configuration, URL paths, and OpenAPI extensions.

To change an operation or tag page, put an `op-*.md` or `tag-*.md` file in `api/<key>/`. See [Writing supplemental content](./supplemental.md).

```yaml
api:
  elasticsearch:
    - spec: elasticsearch-openapi.json
      product: elasticsearch
```

The map key is the URL suffix. `elasticsearch` produces pages under `/api/doc/elasticsearch/`.
