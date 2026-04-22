---
navigation_title: API Explorer
---

# API Explorer

The API Explorer renders OpenAPI specifications as interactive API documentation. When you configure it in your content set, `docs-builder` automatically generates pages for each API operation, request and response schemas, shared type definitions, and inline examples.

:::{warning}
This feature is still under development and the functionality described on this page might change.
:::

## Configure the API Explorer

Add the `api` key to your `docset.yml` file to enable the API Explorer. The key maps product names to OpenAPI JSON specification files.
Paths are relative to the folder that contains `docset.yml`.

### Basic Configuration

```yaml
api:
  elasticsearch: elasticsearch-openapi.json
  kibana: kibana-openapi.json
```

Each product key produces its own section of API documentation. For example, `elasticsearch` generates pages under `/api/elasticsearch/` and `kibana` generates pages under `/api/kibana/`.

The `api` key is only valid in `docset.yml`. You can't use it in `toc.yml` files.

### Advanced configuration with templates

When you specify a `template` file, `docs-builder` uses your custom Markdown file as the API landing page instead of generating an automatic overview:

```yaml
api:
  kibana:
    spec: kibana-openapi.json
    template: kibana-api-overview.md
```

Template files:

- Must be Markdown files with `.md` extension
- Can use standard Markdown, substitutions

:::{note}
They must be explicitly excluded if they are not used in your table of contents.
Otherwise `docs-builder` treats them like normal pages and navigation can fail at build or serve time.
Add a glob (or explicit paths) under `exclude:` in `docset.yml` that matches your template filenames.
For example, exclude `*-api-overview.md`.
:::

#### Template example

Here's a sample template file (`kibana-api-overview.md`):

```markdown
# Kibana APIs

Welcome to the Kibana API documentation.
```

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

## OpenAPI extensions

The API Explorer supports the following OpenAPI specification extensions to enhance navigation and display:

### `x-displayName` for tags

Use the `x-displayName` extension on tag objects to provide user-friendly display names in navigation and landing pages while maintaining stable URLs based on the canonical tag name.

```json
{
  "tags": [
    {
      "name": "tasks",
      "description": "The task management APIs enable you to get information about tasks currently running.",
      "x-displayName": "Task management"
    },
    {
      "name": "ml_anomaly", 
      "description": "Machine learning anomaly detection APIs.",
      "x-displayName": "Machine Learning Anomaly Detection"
    }
  ]
}
```

**Behavior:**
- When `x-displayName` is present, it's used for navigation titles and section headings in the API Explorer
- When `x-displayName` is absent, the canonical tag `name` is used as a fallback
- Navigation URLs and internal references always use the canonical tag `name` for stability
- This extension follows the [Redocly specification extension pattern](https://redocly.com/docs-legacy/api-reference-docs/specification-extensions/x-display-name)
