---
navigation_title: OpenAPI
---

# OpenAPI Support

{{dbuild}} can generate documentation from OpenAPI specifications. The API Explorer renders the spec as API reference pages on your documentation site.

## API Explorer

Configure a spec in `docset.yml`. Then preview the generated pages. See [API Explorer](./api-explorer.md) for setup, page URLs, multi-version trees, and OpenAPI extensions.

To override a description or a parameter, add an `op-*.md` or `tag-*.md` file next to the spec. To add extra sections, use the same files. See [Writing supplemental content](./supplemental.md).

```yaml
api:
  elasticsearch:
    - spec: elasticsearch-openapi.json
      product: elasticsearch
  kibana:
    - spec: kibana-openapi.json
      product: kibana
```

Each product key produces its own API documentation section under `/api/doc/<key>/`. The section includes tag grouping, code samples, and schema type pages.
