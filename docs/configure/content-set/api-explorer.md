---
navigation_title: API Explorer
---

# API Explorer

The API Explorer renders OpenAPI specifications as interactive API documentation. When you configure it in your content set, `docs-builder` automatically generates pages for each API operation, request and response schemas, shared type definitions, and inline examples.

:::{warning}
This feature is still under development and the functionality described on this page might change.
:::

## Configure the API Explorer

Add the `api` key to your `docset.yml` file to enable the API Explorer. The key maps product names to OpenAPI JSON specification files. Paths are relative to the folder that contains `docset.yml`.

```yaml
api:
  elasticsearch: elasticsearch-openapi.json
  kibana: kibana-openapi.json
```

Each product key produces its own section of API documentation. For example, `elasticsearch` generates pages under `/api/elasticsearch/` and `kibana` generates pages under `/api/kibana/`.

The `api` key is only valid in `docset.yml`. You can't use it in `toc.yml` files.

## Place your spec files

OpenAPI specification files must be in JSON format and located in the same folder as your `docset.yml` (or in a subfolder of it). The path you specify in `api` is resolved relative to the `docset.yml` location.

For example, if your content set is structured like this:

```
docs/
  docset.yml
  elasticsearch-openapi.json
  kibana-openapi.json
  index.md
  ...
```

Your `docset.yml` references the specs as follows:

```yaml
api:
  elasticsearch: elasticsearch-openapi.json
  kibana: kibana-openapi.json
```

## When the API Explorer runs

The API Explorer generates documentation in two scenarios:

- **`docs-builder build`**: API docs are generated as part of the standard build. Use `--skip-api` to skip generation for faster iteration on content.
- **`docs-builder serve`**: API docs are generated on startup and regenerated automatically when spec files change.

:::{note}
API generation is skipped when running `docs-builder serve --watch`. This is a performance optimization for `dotnet watch` workflows. Run `serve` without `--watch` to include API docs in your local preview.
:::

## Link to API pages in navigation

You can reference API pages in your `toc.yml` or `docset.yml` navigation using cross-link syntax:

```yaml
toc:
  - file: index.md
  - title: Elasticsearch API Reference
    crosslink: elasticsearch://api/elasticsearch/
```

## What the API Explorer renders

The API Explorer generates the following types of pages from your OpenAPI spec:

- **Landing page**: An overview of the API grouped by tag
- **Operation pages**: One page per API operation, with the HTTP method, path, parameters, request body, response schemas, and examples
- **Schema type pages**: Dedicated pages for complex shared types such as `QueryContainer` and `AggregationContainer`

## Multi-language code examples

When an OpenAPI operation includes the `x-codeSamples` extension, the API Explorer renders the code samples with a language selector tab. This lets users switch between available languages such as Console, cURL, Python, JavaScript, Ruby, PHP, and Java.

The `x-codeSamples` extension is a JSON array of objects, each with a `lang` and `source` field:

```json
"x-codeSamples": [
  { "lang": "Console", "source": "GET /_search" },
  { "lang": "curl", "source": "curl -X GET ..." },
  { "lang": "Python", "source": "resp = client.search()" }
]
```

The code samples appear in a standalone "Code Examples" section on every operation page that has the extension, regardless of HTTP method. This means GET, DELETE, and other operations without a request body also display language tabs when `x-codeSamples` are present. When multiple languages are available, they appear as tabs. The selected language persists across operations and page navigations. When only one language is available, the example renders without a tab selector.

Console is treated as the default language and appears first in the tab order when present.
