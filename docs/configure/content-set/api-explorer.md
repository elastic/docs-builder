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

### Advanced configuration with intro and outro pages

You can add custom Markdown content before and after the auto-generated API documentation using a sequence format:

```yaml
api:
  kibana:
    - file: kibana-intro.md
    - spec: kibana-openapi.json
    - file: kibana-additional-notes.md
```

This configuration creates a navigation structure where:

1. **Intro pages** (before the first `spec`) appear at the top of the sidebar
2. **Generated API content** (operations, tags, types) appears in the middle
3. **Outro pages** (after the spec) appear at the bottom

#### Intro and outro page features:

- **Full Myst support**: Intro/outro pages support the full range of Myst Markdown features including cross-links, substitutions, and directives
- **Automatic exclusion**: No need to add intro/outro files to the `exclude:` list - they're automatically excluded from normal HTML generation
- **URL collision detection**: Build fails if intro/outro page names conflict with reserved API Explorer segments (`types/`, `tags/`) or operation names

#### Multiple intro/outro pages

You can include multiple intro and outro pages:

```yaml
api:
  kibana:
    - file: introduction.md
    - file: getting-started.md
    - spec: kibana-openapi.json
    - file: examples.md
    - file: troubleshooting.md
```

#### Sample intro page

Here's a sample intro page (`kibana-intro.md`):

```markdown
# Kibana APIs

Welcome to the Kibana API documentation. These APIs allow you to manage Kibana programmatically.

## Before you begin

Make sure you have:

- A running Kibana instance
- Valid authentication credentials
- Understanding of RESTful API principles
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

Use the `x-displayName` extension (from [Redocly](https://redocly.com/docs-legacy/api-reference-docs/specification-extensions/x-display-name)) on tag objects to provide user-friendly display names in navigation and landing pages while maintaining stable URLs based on the canonical tag name.

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

### `x-tagGroups` for sidebar grouping

Use the document-level `x-tagGroups` extension (from [Redocly](https://redocly.com/docs-legacy/api-reference-docs/specification-extensions/x-tag-groups)) to define how tags are grouped in the API Explorer sidebar. Each group has a display `name` and a list of tag `name` values that belong to it. Group order in the array is the order of top-level sections in the navigation.

```json
{
  "openapi": "3.0.3",
  "info": { "title": "Example", "version": "1.0.0" },
  "paths": {},
  "x-tagGroups": [
    {
      "name": "Search & Document APIs",
      "tags": ["search", "document", "eql", "esql", "sql"]
    },
    {
      "name": "Cluster Management",
      "tags": ["indices", "cluster", "snapshot"]
    }
  ]
}
```

**Behavior:**

- When `x-tagGroups` is present and valid, the API Explorer uses it as an additional level of grouping in the sidebar.
- When `x-tagGroups` is absent, tags are listed directly under the API root in a single flat layer.
- Any operation tag that is not listed under any group is still included: it appears under a fallback section named `unknown`, and the build logs a warning so you can fix the spec.
