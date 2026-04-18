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
