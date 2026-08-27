---
navigation_title: API Explorer
---

# API Explorer

The API Explorer renders OpenAPI specifications as API documentation. If you configure it in your content set, `docs-builder` generates pages from the spec. The generated pages include:

- API operations
- request and response schemas
- shared type definitions
- inline examples

:::{warning}
This feature is still under development and the functionality described on this page might change.
:::

## Get started

This repository includes a working example. Follow the steps with that example. To use your own product, replace the key and spec in the steps.

:::::{stepper}

::::{step} Add an `api:` entry to `docset.yml`

The `api` key is only valid in `docset.yml`. Do not use it in `toc.yml`. Each product key takes a sequence with exactly one entry. That entry has:

- required `spec:`
- required `product:`
- optional `repository:`
- optional `children:`

```yaml
api:
  elasticsearch:
    - spec: elasticsearch-openapi.json
      product: elasticsearch
      repository: elastic/elasticsearch-specification   # optional; see Reference
      children:                                         # optional
        - file: getting-started.md
```

This repository uses the key `docs-builder-elasticsearch`. Assembler preview then does not collide with docs-content. See `docs/_docset.yml`.

The product key is the URL suffix. `elasticsearch` produces pages under `/api/doc/elasticsearch/`.

::::

::::{step} Preview the generated pages

If `--watch` is on, {{dbuild}} skips API generation. Run {{dbuild}} without `--watch`:

```bash
docs-builder serve
```

Open [http://localhost:3000/api/doc/docs-builder-elasticsearch/](http://localhost:3000/api/doc/docs-builder-elasticsearch/). That landing page comes from `docs/elasticsearch.json`.

If you only edit Markdown outside the API tree, use `--skip-api` with `docs-builder build`.

::::

::::{step} Enrich an operation

Put a Markdown file named after the spec `operationId` into `api/<key>/`. Do not add a toc entry. The build finds the file automatically.

This repository includes `docs/api/docs-builder-elasticsearch/op-async-search-get.md`. After you run `serve`, open this page:

[http://localhost:3000/api/doc/docs-builder-elasticsearch/operation/operation-async-search-get/](http://localhost:3000/api/doc/docs-builder-elasticsearch/operation/operation-async-search-get/)

The file does this:

- It replaces the spec description.
- It overrides the `keep_alive` parameter text.
- It appends a **When to poll** section after the generated reference.

See [Writing supplemental content](./supplemental.md) for file naming, heading rules, tag files, and `children:` pages.

::::

::::{step} Override one major version

If a description or parameter must differ for one major, add a version-suffixed file next to the base file:

```text
api/elasticsearch/
  op-search.md        # every version that has this operation
  op-search.v8.md     # merged on top of the base file for 8.x only
```

The unversioned `/api/doc/<key>/` tree uses the overlay of the current major. Then that tree matches `/vN/` for that major. Full merge rules are in [Writing supplemental content](./supplemental.md#version-specific-files).

::::

::::{step} Read the build error, then fix the file

Validation is strict. The build fails if any of these occur:

- a misspelled `operationId`
- an unknown parameter key
- a reserved child slug

Typical messages:

```text
API supplemental file 'op-nope.md' does not match any operationId in the latest spec
API supplemental: Parameter 'typo' not found in operation 'async-search-get' in the latest spec
```

Fix the file. Then rebuild. The full list is in [Writing supplemental content](./supplemental.md#validation-errors).

::::

:::::

## Reference

| `docset.yml` key | Required | Description |
|---|---|---|
| `spec:` | yes | Path to an OpenAPI file, relative to `docset.yml`. If the file exists, {{dbuild}} renders it. The basename always looks up the remote version index. |
| `product:` | yes | A product id from `products.yml`. Binds the API to that product's versioning system and display name. |
| `repository:` | no | `org/repo` used to look up the version index instead of this checkout's GitHub remote. Set this when the spec is published from a different repository. |
| `children:` | no | Hand-written Markdown pages under `api/<key>/`, in declared order. See [children:](./supplemental.md#children-pages). |

Each product key must have exactly one sequence entry. That entry must have exactly one `spec:`. If the sequence is empty, the build fails. If the sequence has more than one entry, the build also fails.

### `spec:`

- If a file exists at that path, {{dbuild}} renders it. This is the usual setup when a docset includes its own spec.
- The basename (for example `elasticsearch-openapi.json`) always looks up this API in the remote version index. This is true if the file exists locally. It is also true if the file is missing. See [Remote spec resolution](#remote-spec-resolution).

### `product:`

If `product:` does not match a known product id, the build fails. The error includes a suggestion.

### `repository:`

If the spec is published from a different repository than the docset, set `repository:`:

```yaml
api:
  elasticsearch:
    - spec: elasticsearch-openapi.json
      product: elasticsearch
      repository: elastic/elasticsearch-specification
```

Most docsets omit `repository:`. If you omit it, {{dbuild}} uses the GitHub remote of the current checkout.

### `children:`

`children:` is the only way to add a full Markdown page to an API reference section. {{dbuild}} excludes child files from normal HTML generation. Do not add them to `exclude:`.

A `*.vN.md` suffix limits that child to major `N` only. Naming, reserved slugs, and collisions are in [Writing supplemental content](./supplemental.md#children-pages).

## Page URLs

{{dbuild}} uses the bump.sh URL scheme. `{key}` is the `api:` map key. It is not the `product:` id.

| Page | Path |
|---|---|
| Product root (`main`) | `/api/doc/{key}/` |
| Released major | `/api/doc/{key}/v9/`, `/api/doc/{key}/v8/`, … |
| Operation | `/api/doc/{key}/operation/operation-{operationId}/` |
| Tag landing | `/api/doc/{key}/group/endpoint-{tagSlug}/` |
| Schema type | `/api/doc/{key}/types/{schemaMoniker}/` |
| Child Markdown | `/api/doc/{key}/{slug}/` |

{{dbuild}} lowercases operation ids in the URL. For tag slugs, it replaces spaces with hyphens. It also lowercases the tag name. Underscores stay.

## Multi-version behavior

For versioned products, {{dbuild}} renders every resolved version from the index:

| Index moniker | URL path | Role |
|---|---|---|
| `main` | `/api/doc/{key}/` | Canonical current-major tree |
| `9`, `8`, … | `/api/doc/{key}/v9/`, `/v8/`, … | Released major snapshots |

The numeric `9` entry is a frozen v9 snapshot. It is distinct from the moving `main` entry. The unversioned tree uses the overlay of the current major. Then `/api/doc/{key}/` matches `/vN/` for that major.

If a local spec file exists, it overrides only the `main` moniker. Older majors still resolve from the index.

Versionless products (`versioning: serverless` and similar) render only `/api/doc/{key}/`. This is true even if the index lists older monikers. If more than one version is rendered, API pages show a version dropdown at the top of the left navigation rail.

## Remote spec resolution

If `spec:` does not resolve to a file on disk, {{dbuild}} resolves the current (`main`) version of that spec from a remote version index. The index is on CloudFront. Every Elastic repository that publishes OpenAPI specs shares this index.

### How specs are published

Each repository publishes its OpenAPI spec under a stable object key in a shared bucket:

```
<org>/<repo>/<branch>/<spec-name>.<ext>
```

Elasticsearch publishes its spec from a separate specification repository. Example keys are `elastic/elasticsearch-specification/main/elasticsearch.json` and `elastic/elasticsearch-specification/8.19/elasticsearch.json`.

### The version index

A single root `index.json` manifest maps every published spec to its highest-minor branch per major. The keys are:

1. `org/repo`
2. spec basename (the basename of `spec:`)
3. version moniker (`main`, `9`, `8`, ...)

```json
{
  "elastic/elasticsearch-specification": {
    "elasticsearch.json": {
      "main": { "version": "main" },
      "9": { "version": "9.5" },
      "8": { "version": "8.19" }
    }
  }
}
```

{{dbuild}} fetches this manifest once per build from `https://d29hkgsdo66d1n.cloudfront.net/index.json`. It looks up the `org/repo` from `repository:`. If `repository:` is missing, it uses the GitHub remote of the current checkout. It then looks up the basename of `spec:` to find the versions of this API. Spec objects are fetched at `{base}/{org}/{repo}/{version}/{spec-basename}`.

If the API has no local spec file, and the `org/repo` or spec basename is missing from the index, the build fails. The error names the API and what was missing. If a local spec file is also configured, that error becomes a warning. Then the build renders the local file.

## Place your spec files

If you carry a spec locally, put the OpenAPI file in the same folder as `docset.yml`. You can also put it in a subfolder. {{dbuild}} resolves the `spec:` path from the `docset.yml` location.

Example layout:

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
  elasticsearch:
    - spec: elasticsearch-openapi.json
      product: elasticsearch
  kibana:
    - spec: kibana-openapi.json
      product: kibana
```

## When the API Explorer runs

- **`docs-builder build`.** {{dbuild}} generates API docs as part of the standard build. Use `--skip-api` to skip generation when you edit other content.
- **`docs-builder serve`.** {{dbuild}} generates API docs on startup. It regenerates them when spec files change.
- **Assembler builds.** {{dbuild}} generates API docs when the `ASSEMBLER_API_EXPLORER` feature flag is on. That flag is on for the `staging` and `preview` environments. Production stays off until cutover.

:::{note}
If you run `docs-builder serve --watch`, {{dbuild}} skips API generation. This is a performance optimization for `dotnet watch` workflows. Run `serve` without `--watch` to include API docs in your local preview.
:::

This repository declares a local `docs-builder-elasticsearch` API in `_docset.yml`. That entry reads `elasticsearch.json`. It sets `repository: elastic/elasticsearch-specification`. Use that entry to preview ApiExplorer and supplemental files during isolated `docs-builder serve`. Open `/api/doc/docs-builder-elasticsearch/`.

## Link to API pages in navigation

Reference API pages in `toc.yml` or `docset.yml` with cross-link syntax:

```yaml
toc:
  - file: index.md
  - title: Elasticsearch API Reference
    crosslink: elasticsearch://api/doc/elasticsearch/
```

## What the API Explorer renders

The API Explorer generates these page types from your OpenAPI spec:

- **Landing page.** An overview of the API grouped by tag.
- **Tag landing pages.** One page per tag that lists operations in that tag. The page includes the tag display name, an optional OpenAPI `description` (CommonMark), and an optional `externalDocs` link.
- **Operation pages.** One page per API operation. The page includes the HTTP method, path, parameters, request body, response schemas, and examples.
- **Schema type pages.** Dedicated pages for complex shared types such as `QueryContainer` and `AggregationContainer`.

## OpenAPI extensions

The API Explorer supports some OpenAPI specification extensions. These extensions improve navigation and display:

- [x-codeSamples](#x-codesamples)
- [x-displayName](#x-displayname)
- [x-req-auth](#x-req-auth)
- [x-tagGroups](#x-taggroups)

For background on OpenAPI vendor extensions, refer to [OpenAPI Specification](https://spec.openapis.org/oas/latest.html#specification-extensions).

### Multi-language code examples [x-codesamples]

If an OpenAPI operation includes the `x-codeSamples` extension, the API Explorer renders the samples with a language selector tab. Users can switch among Console, cURL, Python, JavaScript, Ruby, PHP, and Java.

The `x-codeSamples` extension is a JSON array of objects. Each object has a `lang` field and a `source` field:

```json
"x-codeSamples": [
  { "lang": "Console", "source": "GET /_search" },
  { "lang": "curl", "source": "curl -X GET ..." },
  { "lang": "Python", "source": "resp = client.search()" }
]
```

The code samples appear in a standalone "Code Examples" section on every operation page that has the extension. This is true for every HTTP method. GET, DELETE, and other operations without a request body also show language tabs when `x-codeSamples` is present. If multiple languages are available, they appear as tabs. The selected language persists across operations and page navigations. If only one language is available, the example renders without a tab selector.

Console is the default language. If Console is present, it appears first in the tab order.

### Prerequisites [x-req-auth]

Add the operation-level `x-req-auth` extension to list authentication or privilege requirements. Users must satisfy these requirements before they call the API.
The API Explorer renders these lines in a **Prerequisites** section on the operation page.

`x-req-auth` is a JSON array of strings.
Each non-empty string becomes one item in the prerequisites list. {{dbuild}} trims leading and trailing whitespace.

```json
{
  "get": {
    "operationId": "get-snapshot",
    "responses": { "200": { "description": "ok" } },
    "x-req-auth": [
      "Cluster privilege: `cluster:admin/snapshot`"
    ]
  }
}
```

If prerequisites are present, **Prerequisites** also appears in the on-page table of contents (after **Paths**).
If the extension is missing, empty, or not a JSON array, the API Explorer omits the section.
{{dbuild}} skips malformed values. The build may log a warning.

### Tag labels [x-displayname]

Use the `x-displayName` extension (from [Redocly](https://redocly.com/docs-legacy/api-reference-docs/specification-extensions/x-display-name)) on tag objects. This sets a display name for navigation and landing pages. URLs stay based on the canonical tag name.

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

- If `x-displayName` is present, the API Explorer uses it for navigation titles, tag landing page titles, and section headings on the main API overview.
- If `x-displayName` is absent, the API Explorer uses the canonical tag `name`.
- Tag landing page URLs and tag URL segments come from the canonical tag `name`.

:::{note}
If two different canonical tag names normalize to the same tag landing page URL, the build fails. The error names both tags and the colliding segment so you can fix the spec.
:::

### Tag groups [x-taggroups]

Use the document-level `x-tagGroups` extension (from [Redocly](https://redocly.com/docs-legacy/api-reference-docs/specification-extensions/x-tag-groups)) to group tags in the API Explorer sidebar. Each group has a display `name` and a list of tag `name` values. Group order in the array is the order of top-level sections in the navigation.

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

- If `x-tagGroups` is present and valid, the API Explorer uses it as an extra grouping level in the sidebar.
- In the navigation tree, a group's section title links to the **main API overview** for that product. It is not a separate page. It does not point at the first tag in the group. Tag landings stay under `.../group/...`.
- If `x-tagGroups` is absent, the API Explorer lists tags directly under the API root in a single flat layer.
- If an operation tag is not listed under any group, it still appears. The API Explorer shows it under a fallback section named `unknown`. The build logs a warning so you can fix the spec.
