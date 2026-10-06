---
navigation_title: API Explorer
---

# API Explorer

The API Explorer renders OpenAPI specifications as interactive API documentation. When you configure it in your content set, `docs-builder` automatically generates a product landing page, an `/authentication` page when the spec declares security schemes, a `/servers` page when the spec declares servers, tag and operation pages, request and response schemas, shared type definitions, and inline examples.

The assembler also writes a combined **API catalog** at `/docs/api/`: a grid of product cards on its own layout (no API sidebar). Each card opens the HTML landing page and includes REST and category badges plus JSON and YAML downloads. Markdown, JSON, and YAML stay on the product landing page and in the catalog Markdown export. The card shows `info.description`, clamped to three lines.

The catalog reuses listing filter chips. A click selects one category. Cmd or Ctrl click adds or removes categories. Categories are discovery labels only. They do not claim versioned availability. An API with no `catalog.categories` appears only when **All** is selected. The filter bar shows only categories that at least one API uses. Filter state is not stored in the URL.

:::{warning}
This feature is still under development and the functionality described on this page might change.
:::

## Configure the API Explorer

Add the `api` key to your `docset.yml` file to enable the API Explorer. Each product key takes a
single-entry sequence with a required `spec:` and `product:`, and optional `repository:`,
`children:`, and `catalog:`:

```yaml
api:
  elasticsearch:
    - spec: elasticsearch-openapi.json
      product: elasticsearch
  kibana:
    - spec: kibana-openapi.json
      product: kibana
```

Each product key produces its own section of API documentation. For example, `elasticsearch` generates pages under `/api/elasticsearch/` and `kibana` generates pages under `/api/kibana/`.

The `api` key is only valid in `docset.yml`. You can't use it in `toc.yml` files.

### `spec:` (required)

A path to an OpenAPI spec file, relative to the folder that contains `docset.yml`. `spec:` serves
two purposes at once:

- If a file exists at that path, {{dbuild}} renders it directly. This is the common setup for a
  docset that carries its own spec file.
- Its basename (for example `elasticsearch-openapi.json`) is always used to look up this API's
  entry in the remote version index, whether or not the file exists locally. See
  [Remote spec resolution](#remote-spec-resolution).

### `product:` (required)

A product id defined in `products.yml`. This binds the API to that product's versioning system
and display name. The build fails with a suggestion if `product:` doesn't match a known product id.

### `repository:` (optional)

An `org/repo` override (for example `elastic/elasticsearch-specification`) used to look up this
API in the remote version index, instead of the current checkout's own GitHub remote. Set this
whenever the repository that publishes the OpenAPI spec differs from the repository the docset
itself builds from:

```yaml
api:
  elasticsearch:
    - spec: elasticsearch-openapi.json
      product: elasticsearch
      repository: elastic/elasticsearch-specification
```

Most docsets omit `repository:` — it's only needed for this cross-repo case. When omitted,
{{dbuild}} derives the repository from the current checkout's GitHub remote.

### `children:` (optional)

Explicit hand-written pages rendered under `api/<key>/`, in the declared order:

```yaml
api:
  kibana:
    - spec: kibana-openapi.json
      product: kibana
      children:
        - file: kibana-api-overview.md
```

`children:` is the only way to inject hand-written content into an API reference section:

- Child pages are fully rendered Markdown with access to all MyST directives, substitutions, and cross-links.
- Child files are automatically excluded from normal HTML generation — you do not need to add them to the `exclude:` list.

**What you cannot do today:** there is no way to override or augment an individual operation,
tag, schema, or parameter description using a local Markdown file. Every description for generated
operations, tags, and schema types comes verbatim from the OpenAPI JSON. For per-operation and
per-parameter enrichment see the [CLI reference](../cli-schema/index.md), which provides a
fine-grained supplemental mechanism as a reference model for what future API augmentation could
look like.

#### Child file naming and validation

A file's URL slug is derived from its filename: lowercase, with spaces and underscores replaced by
hyphens, and the `.md` extension removed. For example, `Getting-Started.md` becomes the slug
`getting-started`.

The following slugs are reserved and cannot be used as child file names:

| Reserved slug | Reason |
|---|---|
| `types` | API Explorer uses this path for schema type pages |
| `tags` | API Explorer uses this path for tag landing pages |
| `group` | Tag landing pages use `/group/` |
| `operation` | Operation pages use `/operation/` |
| `authentication` | Reserved for API Explorer and cannot be used as a child file slug |
| `servers` | Reserved for API Explorer and cannot be used as a child file slug |

Additionally, the slug must not match any operation moniker already generated by the spec. The
build fails with a descriptive error if either collision occurs, naming the conflicting file and
the reserved or operation segment.

If the same slug is produced by two different child files in the same product, the build
also fails with a duplicate-slug error.

### `catalog:` (optional)

Use this when an API should appear under one or more catalog chips on the API catalog page. An
API may list several categories. The same identifiers are used in `applies_to`, but a category
here does not mean the API is generally available for every version of that deployment.

```yaml
api:
  elasticsearch:
    - spec: elasticsearch-openapi.json
      product: elasticsearch
      catalog:
        categories:
          - self
          - ece
          - ess
```

`catalog.categories:` accepts:

- `self` → Self-managed
- `ece` → Elastic Cloud Enterprise
- `ess` or `ech` → Elastic Cloud Hosted
- `serverless` → Serverless

Unknown values fail the build. Omit `catalog:` to keep the API visible only under **All**.

### One spec per product

Each product key in the `api:` block must have **exactly one** entry, with **exactly one**
`spec:`. The build fails if a product sequence is empty or has more than one entry. Multiple
specs per product are not currently supported.

Product pages show an API product switcher on the far right of the grey secondary
top bar, immediately before the version picker. Isolated builds keep a product
`<select>` at the top of the left navigation. The list includes every API whose spec
loaded, and a Back to hub option. Each row uses that spec's title (`info.title`), the
same text as the landing page heading.

Assembler API pages also show a Jump to API box at the top of that sidebar. The box searches API operations only. Isolated and air-gapped builds omit the box. Markdown docs pages do not get it back.

## Remote spec resolution

When `spec:` does not resolve to a file on disk, {{dbuild}} resolves the current (`main`) version
of that spec remotely through a CloudFront-backed version index shared by every Elastic repository
that publishes OpenAPI specs.

### How specs are published

Each repository publishes its OpenAPI spec under a stable object key in a shared bucket:

```
<org>/<repo>/<branch>/<spec-name>.<ext>
```

For example, Elasticsearch's spec is published from a separate specification repository, at keys
like `elastic/elasticsearch-specification/main/elasticsearch.json` and
`elastic/elasticsearch-specification/8.19/elasticsearch.json`.

### The version index

A single root `index.json` manifest maps every published spec to its highest-minor branch per
major. It is keyed by `org/repo`, then by spec basename (matching `spec:`'s basename), then by
version moniker (`main`, `9`, `8`, ...):

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

{{dbuild}} fetches this manifest once per build from
`https://d29hkgsdo66d1n.cloudfront.net/index.json`, then looks up the `org/repo` (from
`repository:`, falling back to the current checkout's GitHub remote) and the `spec:` basename to
find this API's versions. Spec objects are fetched at
`{base}/{org}/{repo}/{version}/{spec-basename}`.

If the API has no local spec file and the `org/repo` or spec basename does not have a matching
entry in the index, the build fails with an error naming the API and what was missing. If a local
spec file is also configured, that error becomes a warning instead, and the build falls back to
rendering the local file.

For versioned products, {{dbuild}} renders every resolved version from the index:

| Index moniker | URL path | Role |
|---|---|---|
| `main` | `/api/doc/<key>/` | Canonical current-major tree |
| `9`, `8`, … | `/api/doc/<key>/v9/`, `/api/doc/<key>/v8/`, … | Released major snapshots |

The numeric `9` entry is a frozen v9 snapshot. It is distinct from the moving `main` entry.
When a local spec file exists, it overrides only the `main` moniker. Older majors still resolve
remotely through the index.

Versionless products (`versioning: serverless` and similar) render only the unversioned
`/api/doc/<key>/` path even when the index lists historical monikers. When more than one
version is rendered, assembler API pages show the same `version-dropdown` as Docs on the
far right of the grey secondary top bar. The current tree is labeled `latest`, and each
frozen major is `v9`, `v8`. Isolated builds keep a left-nav switcher with the same labels.

### Smoke-test every CloudFront spec locally

The docs-builder dev docset ships six API keys that mirror every spec currently listed
in the live version index. They have no local spec files, so `docs-builder serve` fetches each one
from CloudFront:

| URL path | Index entry |
|---|---|
| `/api/elasticsearch/` | `elastic/elasticsearch-specification` → `elasticsearch.json` |
| `/api/elasticsearch-serverless/` | `elastic/elasticsearch-specification` → `elasticsearch-serverless.json` |
| `/api/kibana/` | `elastic/kibana` → `kibana.yaml` |
| `/api/kibana-serverless/` | `elastic/kibana` → `kibana-serverless.yaml` |
| `/api/cloud-connect/` | `elastic/cloud-connected-api` → `cloud-connect.yml` |
| `/api/cloud-serverless/` | `elastic/serverless-api-specification` → `elastic-cloud-serverless.yml` |

Run `docs-builder serve` (without `--watch`) and open any path above.

## Place your spec files

To carry a spec locally, place the OpenAPI specification file in the same folder as your
`docset.yml` (or in a subfolder of it). The path you specify in `spec:` is resolved relative to
the `docset.yml` location.

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
  elasticsearch:
    - spec: elasticsearch-openapi.json
      product: elasticsearch
  kibana:
    - spec: kibana-openapi.json
      product: kibana
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
- **Tag landing pages**: One page per tag that lists operations in that tag, with the tag's display name, optional OpenAPI `description` (CommonMark), and optional `externalDocs` link
- **Operation pages**: One page per API operation, with the HTTP method, path, parameters, request body, response schemas, and examples
- **Schema type pages**: Dedicated pages for complex shared types such as `QueryContainer` and `AggregationContainer`. On operation pages, those properties link to that page.

## OpenAPI extensions

The API Explorer supports some OpenAPI specification extensions to enhance navigation and display:

- [x-codeSamples](#x-codesamples)
- [x-displayName](#x-displayname)
- [x-req-auth](#x-req-auth)
- [x-tagGroups](#x-taggroups)

For background on OpenAPI vendor extensions, refer to [OpenAPI Specification](https://spec.openapis.org/oas/latest.html#specification-extensions).

### Multi-language code examples [x-codesamples]

When an OpenAPI operation includes the `x-codeSamples` extension, the API Explorer renders the code samples as a carousel with one card per language, such as Console, cURL, Python, JavaScript, Ruby, PHP, Java, and C#.

The `x-codeSamples` extension is a JSON array of objects, each with a `lang` and `source` field:

```json
"x-codeSamples": [
  { "lang": "Console", "source": "GET /_search" },
  { "lang": "curl", "source": "curl -X GET ..." },
  { "lang": "Python", "source": "resp = client.search()" }
]
```

Code samples appear in the right-hand **Examples** rail on every operation page that has the extension, regardless of HTTP method. Below 1024px the rail is not hidden: it stacks as a single column under the operation reference. Below 768px the API sidebar uses the same hamburger checkbox as docs pages (`#pages-nav-hamburger`).

How the rail is laid out:

- **Example chips.** When an operation declares several named request `examples`, each one becomes a chip above the carousel. Response examples are matched to request examples by title or summary. Response examples that match no request (typical error payloads) are shared across the examples as extra **status-code tabs**, without replacing an example's own body for the same status. When there are no request examples, named response-only examples collapse into a single example so the status tabs stay primary. Examples without a `summary` get a readable title from their key: `executeBuiltinEsqlToolRequest` becomes "Execute builtin ES|QL tool".
- **Language carousel.** Each example shows a scroll-snap strip with one card per language. A card header shows the language and the client that runs it (for example `elasticsearch-java`), not the path. Readers switch language with the dots, the arrows, the arrow keys, or by swiping. No language card is hidden, so the browser's find-in-page reaches every sample, and inactive examples use `hidden="until-found"` so a match inside one opens it.
- **Size and expand.** Each snippet is as tall as its own code, up to 16 lines, and the card area follows the snippet you are looking at. Longer code scrolls inside the card, and an expand button (its tooltip gives the line count) appears next to the language arrows. Expanding gives the card as much of the rail as it can by folding the response down to its header; press it again to restore. The response card keeps its own size, about a quarter of the window height, however large the request is, so it does not change when you switch language or example; the request card gives way first when space is tight. The rail never scrolls as a whole: only the code inside each card does.
- **Headings and descriptions.** A small **Examples** heading labels the example chips. An example's description appears as a muted note under the chips, cut to three lines with a **Show more** link when it is longer. The description is shown exactly as the spec writes it.
- **Full screen preview.** Every code card, request and response alike, has a button next to **Copy** that opens its code in a dialog 100 characters wide and nearly the full window height; longer lines scroll sideways. The dialog shows the language or status you were looking at, has its own **Copy** and **Close** buttons, and closes with `Esc` or a click outside it.
- **Language order.** Console comes first. The other languages follow GitHub's [Innovation Graph](https://innovationgraph.github.com/global-metrics/programming-languages) global ranking of programming languages by developers using them: JavaScript, Python, curl (as Shell), Java, C#, PHP, Ruby, Go, Rust. Languages not in that list keep their order from the spec, after the ranked ones.
- **Language names.** The `lang` of an `x-codeSamples` entry is matched without regard to case (`cURL`, `Curl` and `curl` are the same language). A suffix after a known language name, such as `cURL_tag_names`, makes that sample part of a separate example named after the suffix ("Tag names") instead of a language of its own.
- **Where samples attach.** `x-codeSamples` attach to the example whose request body matches the Console sample. When the samples' body matches no named example, they become their own first example, titled **Example**.
- **Generated samples.** An example that only has a JSON body gets a generated **Console** request, and a **curl** request when the operation has a curl sample to copy the host and headers from. Hovering one of these cards shows that it was built from the example's request body. The request line comes from an example description that starts with ``Run `METHOD path` ``, or else from the operation's dominant method and shortest path.
- **Missing languages.** Languages that only exist for another example show as dimmed dots and a tile that jumps to that example.

Request and response code boxes show a non-selectable line-number gutter (selection and copy omit the numbers). Response bodies that are JSON objects/arrays use the Figma Card/Code token colors (black structure, green strings, blue booleans, maroon numbers); other payloads such as SSE streams stay plaintext so highlighting does not invent misleading colors. Single-line `curl` samples are reformatted for display (method and URL on the first line, one flag per line, with `\` continuations).

When an operation has **no** `x-codeSamples`, the API Explorer synthesizes a minimal **Console** and **curl** sample from the HTTP method, path, required query parameters, required headers (for example `kbn-xsrf`), and the document `servers` URL so the examples rail is never empty. Author-provided `x-codeSamples` always win over these synthetic samples. When the rail has samples (or request examples) but the operation declares response status codes without example bodies, the rail still shows status-code tabs: responses with no content render **No body**, and responses that declare a content type/schema but no example render **No example**.

When there is only a single example, the rail skips the example chips. **Console is the default language**, because you can paste it straight into Kibana Dev Tools. Once a reader picks another language, that choice is remembered in the browser and applies on every operation page. Links can open a specific example and language with `#example=<example>&lang=<language>`, for example `#example=search-slicing&lang=curl`. The link is always written in lowercase and read without regard to case, so `lang=Curl` works too.

### Paths and methods [operation-paths]

An operation page lists every path of the operation, one row per path, longest path first. Each row shows the dominant HTTP method (POST over PUT over PATCH over GET over DELETE over HEAD). Other methods on the same path appear as an **also GET** hint when they accept the same parameters, request body, and responses. Path segments that a shorter path leaves out are shown dashed, and their path parameters are marked **optional** with a hint naming the path to use without them. For example, the search operation shows `POST` (also `GET`) `/{index}/_search` and `POST` (also `GET`) `/_search`, with `index` optional.

The paths come from either of two sources, and both produce the same rows:

- The `**All methods and paths for this operation:**` (or `**Spaces method and path for this operation:**`) HTML block that bump.sh-flavoured specs put in the operation description. The block is removed from the rendered description.
- Separate spec operations that share a grouping key, when `api-nav-grouping` is on (see below).

### Prerequisites [x-req-auth]

Add the operation-level `x-req-auth` extension to list authentication or privilege requirements that users must satisfy before calling the API.
The API Explorer renders these lines in a **Prerequisites** section on the operation page.

`x-req-auth` is a JSON array of strings.
Each non-empty string becomes one item in the prerequisites list (leading and trailing whitespace is trimmed).

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



When prerequisites are present, **Prerequisites** also appears in the on-page table of contents (after **Paths**).
When the section has more than one item, it renders collapsed with a one-line name summary (same accordion as Query Parameters).
When the extension is missing, empty, or not a JSON array, the section is omitted.
Malformed values are skipped and the build may log a warning.

`x-req-auth` is independent of OpenAPI `security` / `securitySchemes`. Privilege lines go in **Prerequisites**; HTTP schemes go in **Authorization**.

### Authorization

The API Explorer reads OpenAPI `security` on the operation, or the document-level `security` when the operation omits the field.
An empty operation `security: []` is an explicit override to none and hides the section.

Each listed scheme is resolved against `components.securitySchemes`. Only these labels are shown, in first-seen order and de-duplicated:

| OpenAPI scheme | Label |
|---|---|
| `type: apiKey` | `Api key` |
| `type: http`, `scheme: basic` | `Basic` |
| `type: http`, `scheme: bearer` | `Bearer` |

Other scheme types (`oauth2`, `openIdConnect`, `mutualTLS`, HTTP digest, unknown HTTP schemes) are omitted.
OpenAPI treats items in the `security` array as OR and keys inside one object as AND; the page flattens those groups into a unique label list.

When schemes are present, **Authorization** appears as a section heading (same visual weight as **Prerequisites**) and in the on-page table of contents.
When the section has more than one scheme, it renders collapsed with a one-line name summary.

The same accordion applies to **Path Parameters**, **Query Parameters**, and **Request** when those sections have more than one item. A single-item section stays expanded as a plain heading.

### Tag labels [x-displayname]

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

- When `x-displayName` is present, it's used for navigation titles, tag landing page titles, and section headings on the main API overview
- When `x-displayName` is absent, the canonical tag `name` is used as a fallback
- Tag landing page URLs and tag URL segments are derived from the canonical tag `name`

:::{note}
If two different canonical tag names normalize to the same tag landing page URL, the build fails with an error that names both tags and the colliding segment so the spec can be fixed.
:::

### Tag groups [x-taggroups]

The document-level `x-tagGroups` extension (from [Redocly](https://redocly.com/docs-legacy/api-reference-docs/specification-extensions/x-tag-groups)) names groups of tags. Each group has a display `name` and a list of tag `name` values.

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

The default sidebar matches bump.sh. The product overview, Authentication, Servers, and any `children:` markdown pages sit in one group. A divider separates that group from tag folders. Each OpenAPI operation is a visible child of its tag.

Classification folders, collapsing operations that share a grouping key into one page, and Types pages require `FEATURE_API_NAV_GROUPING` (the `api-nav-grouping` feature flag). That flag is off by default.

With the flag on, all operations that share a grouping key render as **one page**, built around the dominant method on the longest path. The page URL uses the operation id without its numeric suffix: `search`, `search-1`, `search-2` and `search-3` become `operation-search`. The former per-operation URLs, such as `operation-search-2`, become redirect pages to the merged page, and they keep any `#fragment`.

When the flag is on:

- When `x-tagGroups` is present and valid, the API Explorer uses it as an additional level of grouping in the sidebar.
- A group's section title links to the main API overview for that product. It is not a separate page and does not point at the first tag in the group. Tag landings stay under `/group/`.
- When `x-tagGroups` is absent, tags are listed directly under the API root.
- Any operation tag that is not listed under any group is still included. It appears under a fallback section named `unknown`, and the build logs a warning so you can fix the spec.
