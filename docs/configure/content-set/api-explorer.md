---
navigation_title: API Explorer
---

# API Explorer

The API Explorer renders OpenAPI specifications as interactive API documentation. When you configure it in your content set, `docs-builder` automatically generates pages for each API operation, request and response schemas, shared type definitions, and inline examples.

:::{warning}
This feature is still under development and the functionality described on this page might change.
:::

## Requirements

OpenAPI specification files must be in JSON format and located in the same folder as your `docset.yml` (or in a subfolder of it).

## Configure the API Explorer

Add the `api` key to your `docset.yml` file to enable the API Explorer.
The key maps product names to OpenAPI files.
Paths are relative to the folder that contains `docset.yml`.

For example, if your content set is structured like this:

```
docs/
  docset.yml
  elasticsearch-openapi.json
  kibana-openapi.json
  index.md
  ...
```

Your `docset.yml` can reference the files as follows:

```yaml
api:
  elasticsearch: elasticsearch-openapi.json
  kibana: kibana-openapi.json
```

Each product key produces its own section of API documentation.
For example, `elasticsearch` generates pages under `/api/elasticsearch/` and `kibana` generates pages under `/api/kibana/`.

:::{note}
The `api` key is only valid in `docset.yml`.
You can't use it in `toc.yml` files.
:::

## Link to API pages in navigation

You can reference API pages in your `toc.yml` or `docset.yml` navigation using cross-link syntax:

```yaml
toc:
  - file: index.md
  - title: Elasticsearch API Reference
    crosslink: elasticsearch://api/elasticsearch/
```

## Run the API Explorer

The API Explorer generates documentation in two scenarios:

- [docs-builder build](/cli/docset/build.md): API docs are generated as part of the standard build. Use `--skip-api` to skip generation for faster iteration on content.
- [docs-builder serve](/cli/docset/serve.md): API docs are generated on startup and regenerated automatically when spec files change.

:::{note}
API generation is skipped when running `docs-builder serve --watch`. This is a performance optimization for `dotnet watch` workflows. Run `serve` without `--watch` to include API docs in your local preview.
:::

The API Explorer generates the following types of pages from your OpenAPI file:

- **Landing page**: An overview of the API grouped by tag
- **Operation pages**: One page per API operation, with the HTTP method, path, parameters, request body, response schemas, and examples
- **Schema type pages**: Dedicated pages for complex shared types such as `QueryContainer` and `AggregationContainer`

## Customize the landing page

By default, the API Explorer generates a landing page.
To override that behavior, specify a `template`:

```yaml
api:
  kibana:
    spec: kibana-openapi.json
    template: kibana-api-overview.md
```

The template file:

- Must be Markdown files with `.md` extension
- Can use standard Markdown, substitutions, and directives

:::{note}
Template files must be explicitly excluded if they are not used in your table of contents.
Otherwise `docs-builder` treats them like normal pages and navigation can fail at build or serve time.
Add a glob (or explicit paths) under `exclude:` in `docset.yml` that matches your template filenames.
For example, exclude `*-api-overview.md`.
:::

### Example [custom-template-example]

Here's a sample template file (`kibana-api-overview.md`):

```markdown
# {{api.kibana.title}}

Welcome to the {{api.kibana.title}} documentation (version {{api.kibana.version}}).
This page provides an overview of the available Kibana APIs.

:::{api-summary}
:product: kibana
:type: description
:::
```

This example includes an [API directive](/syntax/api-directives.md) and [substitutions](/syntax/substitutions.md#api-info).