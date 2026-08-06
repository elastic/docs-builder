---
navigation_title: OpenAPI
---

# OpenAPI Support

{{dbuild}} can generate documentation from OpenAPI specifications. The API Explorer renders specs as interactive, navigable reference documentation directly within your documentation site.

## API Explorer

The [API Explorer](./api-explorer.md) generates pages for every operation, tag, and shared schema type from an OpenAPI JSON specification. Configure it in your `docset.yml`:

```yaml
api:
  elasticsearch: elasticsearch-openapi.json
  kibana: kibana-openapi.json
```

Each product key produces its own section of API documentation with tag grouping, code samples, and schema type pages.
