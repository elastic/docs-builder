---
navigation_title: API Explorer
---

# API Explorer

The API Explorer renders OpenAPI specifications as interactive API documentation. When you configure it in your content set, `docs-builder` automatically generates pages for each API operation, request and response schemas, shared type definitions, and inline examples.

:::{warning}
This feature is still under development and the functionality described on this page might change.
:::

## Configure the API Explorer

Add the `api` key to your `docset.yml` file to enable the API Explorer. The key maps product names to OpenAPI JSON specification files and optional landing page templates. Paths are relative to the folder that contains `docset.yml`.

### Basic Configuration

```yaml
api:
  elasticsearch: elasticsearch-openapi.json
  kibana: kibana-openapi.json
```

### Advanced configuration with templates

```yaml
api:
  elasticsearch:
    spec: elasticsearch-openapi.json
    template: elasticsearch-api-overview.md
```
<!-- 
Coming soon, multi-document support:
  kibana:
    specs:
      - kibana-core-api.json
      - kibana-alerting-api.json
    template: kibana-api-overview.md
 -->

Each product key produces its own section of API documentation. For example, `elasticsearch` generates pages under `/api/elasticsearch/` and `kibana` generates pages under `/api/kibana/`.

The `api` key is only valid in `docset.yml`. You can't use it in `toc.yml` files.

#### Configuration options

- **`spec`**: Path to a single OpenAPI specification file (string format for backward compatibility)
- **`template`**: Path to a Docs V3 markdown file to use as the landing page template (optional)
<!--
Coming soon, multi-document support:
- **`specs`**: Array of paths to multiple OpenAPI specification files
-->

**Rules**:
<!-- Coming soon, multi-document support: - Use either `spec` (single file) or `specs` (multiple files), not both -->
- Template files must use [Docs V3 syntax](../../syntax/index.md)
- All paths are resolved relative to the `docset.yml` location

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

## Template directives

When using custom templates, you can include template directives to insert generated API navigation content.
Directives use the syntax `{{% directive-name attribute="value" %}}`.

### `api-operations-nav`

Generates navigation links to API operations, organized by tag.

#### Basic usage

```markdown
## All API Operations

{{% api-operations-nav %}}
```

#### Filtered by tag

```markdown
## Search APIs

{{% api-operations-nav tag="search" %}}

## Index Management APIs

{{% api-operations-nav tag="indices" %}}
```

<!--
Coming soon, multi-spec support
#### Filtered by spec (Multi-Spec Configuration)

```markdown
## Core APIs

{{% api-operations-nav from="kibana-core-api" %}}

## Alerting APIs

{{% api-operations-nav from="kibana-alerting-api" %}}
```
-->

#### Excluding tags

```markdown
## Public APIs

{{% api-operations-nav exclude="internal,experimental" %}}
```

#### Supported attributes

- **`tag`**: Filter operations by OpenAPI tag (for example, `tag="search"`)
- **`exclude`**: Comma-separated list of tags to exclude (for example, `exclude="internal,deprecated"`)
<!--
Coming soon, multi-document support:
- **`from`**: Filter operations from a specific spec file (multi-spec only, e.g., `from="kibana-core-api"`)
-->

## What the API Explorer renders

The API Explorer generates the following types of pages from your OpenAPI spec:

- **Landing page**: Auto-generated overview grouped by tag, or custom template-based content
- **Operation pages**: One page per API operation, with the HTTP method, path, parameters, request body, response schemas, and examples
- **Schema type pages**: Dedicated pages for complex shared types such as `QueryContainer` and `AggregationContainer`
