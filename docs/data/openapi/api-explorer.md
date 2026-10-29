---
navigation_title: API Explorer
---

# API Explorer

The API Explorer renders OpenAPI specifications as interactive API documentation. When you configure it in your content set, `docs-builder` automatically generates a product landing page, an `/authentication` page when the spec declares security schemes, a `/servers` page when the spec declares servers, tag and operation pages, request and response schemas, shared type definitions, and inline examples.

Authored API Markdown pages can use [`meta_title`](../../syntax/frontmatter.md#meta-title) to set a different page title.

The assembler also writes a combined **Elastic APIs** page at `/docs/api/`, on its own layout with no API sidebar. The cards match the ones on the [docs landing page](https://www.elastic.co/docs): a rounded box with the product icon, a bold title, and a short description. The whole card is one link to the API. The title sits on a full-width band with one line of text under it that says which deployments the APIs cover. Two small labels group the APIs into two full-width rows. **Elastic Stack** shows Elasticsearch, Kibana and Logstash as three equal large cards, with the Serverless variants of Elasticsearch and Kibana as small compact cards directly under their parents. **Orchestration** shows the five Elastic Cloud APIs three across, in the same compact card as the Serverless variants but without the attachment. An API that is in neither group follows them, sorted by title. Each API appears once. If only one group has APIs, the label is dropped. If fewer than two of Elasticsearch, Kibana and Logstash are present, the group has no larger cards.

Under the line of text sits the same box as in the API sidebar, labelled **Type ❲/❳ to explore API**. It explores operations across every API, so you can jump to an endpoint without knowing which product it belongs to. Type <kbd>/</kbd> or <kbd>s</kbd> anywhere on the page, outside a text field, to jump to it. Like the sidebar box, it is omitted on isolated and air-gapped builds.

The cards for APIs that are in neither group show the API's deployments as small pills taken from `catalog.categories`. They use the same look as `applies_to` badges and the same names (**Self-managed**, **ECE**, **ECH**, **Serverless**), but show the name only: the catalog knows the deployment, not a lifecycle or version. The grouped cards leave them out, and an API with no categories shows no pills. The description is a short plain-text summary of `info.description` (the first paragraph with Markdown removed). JSON, YAML, and Markdown downloads stay on each API's landing page and in the `/docs/api.md` export.

The API title comes from `info.title` in the spec, so fix naming and description copy in the spec.

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

### `children:` (optional) [api-children]

Explicit hand-written pages rendered under `api/<key>/`, in the declared order:

```yaml
api:
  kibana:
    - spec: kibana-openapi.json
      product: kibana
      children:
        - file: kibana-api-overview.md
```

Child pages are listed under the **Topics** heading in the sidebar, in the order you declare them, after the Authentication page, which always comes first. The spec's [`x-topics`](#x-topics) follow, and a child page takes priority over a topic it covers. A child covers a topic when its first heading matches the topic title, or when its text matches the topic content. The API Explorer then builds no page for that topic. Redirects from bump.sh topic URLs, such as `/topic/topic-kibana-spaces`, to the child page live in the CloudFront configuration in `elastic/docs-infra`, not in the build.

Child pages also behave as follows:

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
| `topic` | Topic pages from `x-topics` use `/topic/` |
| `authentication` | Reserved for API Explorer and cannot be used as a child file slug |
| `servers` | Reserved for API Explorer and cannot be used as a child file slug |

Additionally, the slug must not match any operation moniker already generated by the spec. The
build fails with a descriptive error if either collision occurs, naming the conflicting file and
the reserved or operation segment.

If the same slug is produced by two different child files in the same product, the build
also fails with a duplicate-slug error.

### `catalog:` (optional)

This sets the deployment tags on the API's row on the Elastic APIs page. An API may list several categories. The same identifiers are used in `applies_to`, but a category here does not mean the API is generally available for every version of that deployment. An API with no `catalog:` shows no tags.

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

Unknown values fail the build.

### One spec per product

Each product key in the `api:` block must have **exactly one** entry, with **exactly one**
`spec:`. The build fails if a product sequence is empty or has more than one entry. Multiple
specs per product are not currently supported.

The left navigation starts with one selector that says which API you are reading and which version: `[logo] Elasticsearch API  main ⇅`. It opens a menu with the versions of this API, then every API whose spec loaded (a versioned API opens its latest numbered version, such as `/v9/`) (each with its logo), then an **All APIs** link back to the catalog. Each row uses that spec's title (`info.title`), the same text as the landing page heading. It is the same on assembler and isolated builds, and with or without `navigation-preview`. With a single version the menu lists only the APIs; with a single API and a single version the name is plain text. The **Copy page** menu at the top right of each page also holds **Download OpenAPI spec (JSON)** and **(YAML)**, which download the whole spec. The page itself starts with the full breadcrumb, which begins at Docs. The catalog page has no API, so it has no selector; its finder sits in the title band instead.

Below the selector, the left navigation has a box labelled **Type ❲/❳ to explore API**. It explores operations across every API, not only the one you are reading, and shows the matches in a list; it does not filter the navigation tree. It is not the site-wide search. Type <kbd>/</kbd> or <kbd>s</kbd> anywhere on the page, outside a text field, to jump to the box, as on GitHub. The shortcut is off on narrow screens, where the navigation sits in a drawer. Isolated and air-gapped builds omit it. Markdown docs pages do not get it back.

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
version is rendered, the bar shows a version picker beside the API name. The current tree is
labeled `main`, and each frozen major is `v9`, `v8`. The highest frozen major carries `(latest)`, as in `v9 (latest)`. This is a label only: the unversioned URL still serves `main`. Isolated builds put the same picker
under the API name in the left navigation.
Picking a version opens the same page in that version when it has one, for example the same
operation, and that version's landing page otherwise.

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
- **Schema type pages**: Dedicated pages for large, recursive types that many operations share: **Query** (Query DSL), **Aggregation** and **Aggregation result** (Aggregations), and **Processor** (Ingest). On operation pages, a property of one of these types links to its page instead of listing it again. Links match the schema id, such as `ingest._types.ProcessorContainer`, so another schema that shares the short name lists its own fields.

### How types read

Schema names under `components/schemas` are names for code, not for readers, so the API Explorer never shows them. A property's type reads as its shape instead: `object`, `string[]`, `enum`, `map string to object`, or `union` with its options. A type with its own page reads as that page's title, such as **Query**.

A map (`additionalProperties`) reads as `map string to X`, whether it's inline or a `$ref` to a named schema. When the map's values have fields, limits such as `maxLength` or `pattern`, or allowed values, a `<string>` row under the map lists them. A `default` alone doesn't add the row. The map's own `minProperties` and `maxProperties` show next to its type.

A request body leaves out `readOnly` fields, because only the server sets them, and a response leaves out `writeOnly` fields, because only the client sends them. Schema type pages list both.

A request or response body that declares more than one content type shows a **Content type** select at the top of its body, with the content types that have a schema first. Choosing one swaps the body below it. A response row shows the content type in view as a tag, and the select updates it. A content type with no schema, such as one the spec lists only to attach examples, shows "No schema is declared for this content type."

Each section is a collapsible heading, folded by default. Its summary punctuation says what the names are:

| Summary | Meaning |
|---|---|
| `{ a, b }` | The fields of a JSON request body |
| `A \| B` | Alternatives, such as the variants of a `oneOf` or the credentials of **Authentication** |
| `A + B` | Needed together, such as the privileges in **Required authorization** |
| `a, b` | Anything else, such as parameters or the fields of a form |

The names in a summary aren't searchable text, so find-in-page (<kbd>Ctrl</kbd>+<kbd>F</kbd>) skips them and finds the same field inside the folded section, which then opens. This relies on `hidden="until-found"`: in a browser without it, the folded sections can't be searched.

Path, query and header parameters list like request body properties, each in its own section. An `object` or map parameter, such as one with `style: deepObject`, shows its fields under the parameter. Header parameters named `Accept`, `Content-Type` or `Authorization` aren't listed, because OpenAPI ignores them: the media types and security schemes describe those headers.

A parameter row shows an **Example** line when its `example`, its `examples` or its schema's `example` says something the type doesn't. The line is left out when the example is a boolean (including `"true"` as text), a bare number, a value the **Values** row already lists, the default, or longer than 60 characters. The picked example is the first of `example`, `examples` and the schema's `example` that passes those checks.

The variants of a `oneOf` or `anyOf` are labelled by the first of these that applies:

1. The variant schema's `title`.
2. A field with a single allowed value, such as `type: eql`.
3. The fields no other variant has, such as `{ lives }`.
4. A shortened form of the schema name, as a last resort.

To control how a variant reads, set a `title` on its schema.

## OpenAPI extensions

The API Explorer supports some OpenAPI specification extensions to enhance navigation and display:

- [x-codeSamples](#x-codesamples)
- [x-displayName](#x-displayname)
- [x-req-auth](#x-req-auth)
- [x-tagGroups](#x-taggroups)
- [x-topics](#x-topics)

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
- **Language carousel.** Each example shows a scroll-snap strip with one card per language. A card header shows the language and the client that runs it (for example **Elasticsearch Java Client**), not the path. On Elasticsearch APIs the client name links to that client's docs under [Elasticsearch clients](https://www.elastic.co/docs/reference/elasticsearch/clients/); **Kibana Dev Tools** links to the Console docs on every API. Readers switch language with the dots, the arrows, the arrow keys, or by swiping. No language card is hidden, so the browser's find-in-page reaches every sample, and inactive examples use `hidden="until-found"` so a match inside one opens it.
- **Size.** Each snippet is as tall as its own code, up to 16 lines, and the card area follows the snippet you are looking at. Longer code scrolls inside the card; the full screen preview shows all of it. The response card keeps its own size, about a quarter of the window height, however large the request is, so it does not change when you switch language or example; the request card gives way first when space is tight. The rail never scrolls as a whole: only the code inside each card does.
- **Headings and descriptions.** A small **Examples** heading labels the example chips, and **Request** and **Response** labels sit above the request samples and the response card. An example's description appears as a muted note under the chips, cut to three lines with a **Show more** link when it is longer. The description is shown exactly as the spec writes it.
- **Full screen preview.** Every code card that has code to show, request and response alike, has a button next to **Copy** that opens its code in a dialog 100 characters wide and as tall as the code, up to the window height; longer code scrolls inside it and longer lines scroll sideways. The dialog shows the language or status you were looking at, has its own **Copy** and **Close** buttons, and closes with `Esc` or a click outside it.
- **Language order.** Console comes first. The other languages follow GitHub's [Innovation Graph](https://innovationgraph.github.com/global-metrics/programming-languages) global ranking of programming languages by developers using them: JavaScript, Python, curl (as Shell), Java, C#, PHP, Ruby, Go, Rust. Languages not in that list keep their order from the spec, after the ranked ones.
- **Language names.** The `lang` of an `x-codeSamples` entry is matched without regard to case (`cURL`, `Curl` and `curl` are the same language). A suffix after a known language name, such as `cURL_tag_names`, makes that sample part of a separate example named after the suffix ("Tag names") instead of a language of its own. When the operation already lists a request example with that title, the samples join it instead of adding another example: use the example's `summary` (or its key, when it has none) as the suffix.
- **Samples on an example.** A request example can carry its own `x-codeSamples`, next to its `summary` and `value`. Those samples belong to that example, whatever their language names. An operation can keep repeating the first example's samples in its own `x-codeSamples`, for tools that read only the operation; the page shows each of those once, and a sample on the example wins over an operation sample of the same example and language. The Markdown copy of the page lists the samples of each example under its title. The example is shown once, with its body and its samples. Examples are read from every request content type, and when two examples have the same title, only the first one's samples are used, because they show as one example.
- **Where samples attach.** `x-codeSamples` attach to the example whose request body matches the Console sample. When the samples' body matches no named example, they become their own first example, titled **Example**.
- **Sample methods.** On a path that lists several methods (`POST` also `GET`), the request method in Console and curl samples, and in a ``Run `METHOD path` `` example description, follows the path's dominant method when the sample's own method is one the path lists; those methods are interchangeable, so the call is the same. A sample whose method or path matches none of the operation's paths, and every client-library sample, is shown as written.
- **Generated samples.** An example that only has a JSON body gets a generated **Console** request, and a **curl** request when the operation has a curl sample to copy the host and headers from. The request line comes from an example description that starts with ``Run `METHOD path` `` (with the method aligned to its path), or else from the operation's dominant method and shortest path.
- **Only available languages.** The dots and the arrows cover the languages that example has. When you switch to an example that lacks your chosen language, it opens on its first language and your choice applies again on the next example that has it.

Request and response code boxes show a non-selectable line-number gutter (selection and copy omit the numbers). Response bodies that are JSON objects/arrays use the Figma Card/Code token colors (black structure, green strings, blue booleans, maroon numbers); other payloads such as SSE streams stay plaintext so highlighting does not invent misleading colors. Single-line `curl` samples are reformatted for display (method and URL on the first line, one flag per line, with `\` continuations).

When an operation has **no** `x-codeSamples`, the API Explorer synthesizes a minimal **Console** and **curl** sample from the HTTP method, path, required query parameters, required headers (for example `kbn-xsrf`), and the document `servers` URL so the examples rail is never empty. Every value comes from the spec, and the samples are marked as generated:

- **Parameter values.** A path or required query parameter with an example (on the parameter, then on its schema, in OpenAPI 3.0 `example` or 3.1 `examples`) shows that value, else its schema's `default` or first `enum` value; without any, the parameter's name stands in as an uppercase placeholder: `ORGANIZATION_ID` for `{organization_id}` in Console, and the shell variable `${ORGANIZATION_ID}` in the curl, beside `${KIBANA_URL}` and `${API_KEY}`. Bare braces are never kept, because curl reads them as a glob and would silently send the bare name. An array example follows the parameter's `style` and `explode`: `ids=a&ids=b` for the default query style, one delimited value otherwise. An object example follows them too: `a=1&b=2` for the default query style, `ids[a]=1` for `deepObject`, `key,value` pairs otherwise; an object that holds another object keeps the placeholder.
- **Authentication.** The curl sample authenticates with the operation's security schemes (the first alternative whose schemes curl can all send; cookie API keys can't be sent): an API key goes in its header or query parameter as `${API_KEY}` (as `Authorization: ApiKey ${API_KEY}` when the header is `Authorization`, the form Elastic APIs expect), basic auth is `-u "${USERNAME}:${PASSWORD}"`, and bearer, OAuth 2.0 and OpenID Connect send `Authorization: Bearer ${TOKEN}`. A required header with an example keeps it; one without is replaced by the credential.
- **Request body.** When the spec has no request example and the request is JSON, the samples carry a body built from the request schema. It holds the required properties only, without the `readOnly` ones. Each value is the first one the schema states (`example`, `default`, `const`, the first `enum` value), or a placeholder for its type: `"string"`, `0` or `false`. A `oneOf` or `anyOf` takes its first variant that is not `null`, a property that only wraps a `$ref` in `allOf` takes the wrapped type, a literal `null` example, default or enum member is skipped, and a required property with nothing to show is left out rather than written as `null`. Nesting stops after a few levels. The curl sample sends `Content-Type` together with a body.
- **Host.** A spec that names no `servers` gets the product's host variable before the path: `${ELASTICSEARCH_URL}`, `${KIBANA_URL}`, `${LOGSTASH_URL}`, or `${API_URL}` for other products. A server URL that is one variable, such as Kibana's `https://{kibana_url}`, becomes that variable as a shell variable, `${KIBANA_URL}`, which the product's own samples use.
- **Generated note.** A sample docs-builder built ends its code block with a muted line, **Generated from the API spec**, whose tooltip says the sample was generated from the spec, not written by hand, and asks the reader to check placeholder values such as `string` or `INDEX` before running it. Samples written in the spec carry no note.
- **Console only where it runs.** Console calls Elasticsearch, and Kibana with a `kbn:` prefix, so the **Console** sample appears only for the `elasticsearch`, `serverless-elasticsearch` and `kibana` products (and for specs without a product). Other products, such as Elastic Cloud, get curl only, and the language carousel opens on its first card.

When the request body has an example, the samples carry its body (Console under the request line, curl as `-d` with a `Content-Type` header), so the body is not repeated in a block of its own. Author-provided `x-codeSamples` always win over these synthetic samples. When the rail has samples (or request examples) but the operation declares response status codes without example bodies, the rail still shows status-code tabs: responses with no content render **No body**, and responses without a usable explicit example or a JSON schema render **No example**. JSON schemas supply a [generated response example](#generated-response-examples).

When there is only a single example, the rail skips the example chips. **Console is the default language**, because you can paste it straight into Kibana Dev Tools. Once a reader picks another language, with a dot, an arrow, the keyboard, or by swiping the strip, that choice is remembered in the browser and applies on every operation page, and the link in the address bar follows it. Links can open a specific example and language with `#example=<example>&lang=<language>`, for example `#example=search-slicing&lang=curl`. The link is always written in lowercase and read without regard to case, so `lang=Curl` works too.

### Generated response examples

When a response content type has no explicit example, {{dbuild}} builds a representative JSON body from its schema. Named and media-type examples take precedence over this fallback. Each response status keeps its own examples, including content types other than the first.

Generated bodies use schema examples, defaults, constants, and enum values before type placeholders such as `"string"`, `0`, and `false`. They include optional and read-only properties and omit write-only properties. References, arrays, and `allOf` compositions are expanded. An `allOf` containing a union combines the base fields with the selected variant's fields. JSON and `+json` content types support generation; other content types retain their explicit examples.

The **Generated from the API spec** note identifies generated bodies in the page and Markdown export. A union uses its first non-null variant. Schema expansion stops after six levels or 200 values and may omit fields. Generated bodies illustrate the response shape; they do not describe every possible response. Explicit schema examples are copied intact.


### Paths and methods [operation-paths]

An operation page lists every path of the operation, one row per path, longest path first. The API overview and the tag landing pages show the main path only. The exception is a Kibana path that is another of the paths with `/s/{space_id}` in front: that is the same call made in a space, so it lists after the path it prefixes rather than leading, and the space prefix is not marked optional. Each row shows the dominant HTTP method (POST over PUT over PATCH over GET over DELETE over HEAD). Other methods on the same path appear as an **also GET** hint when they accept the same parameters, request body, and responses. Path segments that a shorter path leaves out are shown dashed, and their path parameters are marked **optional** with a hint naming the path to use without them. For example, the search operation shows `POST` (also `GET`) `/{index}/_search` and `POST` (also `GET`) `/_search`, with `index` optional.

The paths come from either of two sources, and both produce the same rows:

- The `**All methods and paths for this operation:**` (or `**Spaces method and path for this operation:**`) HTML block that bump.sh-flavoured specs put in the operation description. The block is removed from the rendered description. When it lists a `/s/{space_id}/…` route, the page also lists an optional `space_id` path parameter, described as the space identifier, defaulting to the default space, followed by the `Refer to [Spaces](…) for more information.` sentence that follows the block. That sentence is removed from the operation description.
- Separate spec operations that share `x-namespace` and `x-api-name` (see below).

### Required authorization [x-req-auth]

The **Required authorization** section lists the privileges an operation needs. It follows the description and also appears in the on-page table of contents. Like the other sections it is folded by default. Its header names the privileges: `a + b` when they are needed together and `a | b` when they are alternatives.

Add the operation-level `x-req-auth` extension to list those privileges. It is a JSON array of strings. Each non-empty string becomes one row, with leading and trailing whitespace trimmed.

```json
{
  "get": {
    "operationId": "get-snapshot",
    "responses": { "200": { "description": "ok" } },
    "x-req-auth": [
      "Cluster privileges: `cluster:admin/snapshot`"
    ]
  }
}
```

A line in the form ``Label: `value` `` shows the label as muted text and each value as a code chip. A line with several values, such as ``Index privileges: `read`, `view_index_metadata` ``, shows one chip per value. Any other line renders as Markdown.

Specs that do not use `x-req-auth` still work. The API Explorer reads two patterns from the operation description and moves them into the same section, so the text does not show twice:

- An Elasticsearch `## Required authorization` heading followed by bullets such as ``* Cluster privileges: `monitor` ``.
- A Kibana line such as `[Required authorization] Route required privileges: fleet-agent-policies-read OR fleet-settings-read.` The privileges show as chips joined by **or**, or by **and** when the line uses `AND`.

A line that mixes `OR` and `AND`, or a bullet with extra words around the values, stays in the description as written. When `x-req-auth` is present, it wins over the description. When neither source has privileges, the section is omitted. Malformed `x-req-auth` values are skipped and the build may log a warning.

`x-req-auth` is independent of OpenAPI `security` and `securitySchemes`. Privileges go in **Required authorization**. The credentials to send go in **Authentication**.

### Authentication

The **Authentication** section shows how to send a credential. It is collapsed by default, because a reader who already has auth set up does not need it. The header shows the scheme names, for example `Api key | Basic | Bearer`.

The API Explorer reads the document-level `security`, or one alternative per scheme in `components.securitySchemes` when the document has none. The full section lives on the product's `/authentication` page. An endpoint page shows its own section, below **Required authorization** and in the on-page table of contents, only when the operation sets a `security` that names other alternatives than the document's. An empty operation `security: []` is an explicit override to none and shows no section.

Every endpoint with authentication has an `Auth` badge in the row under its path, before the availability badge. The badges in that row, `Auth` and `Deprecated`, share the outlined look of the availability badge. `Deprecated` keeps a red outline and text, and a badge that links is underlined:

| Badge | When | Links to |
|---|---|---|
| `Auth` | The operation inherits the document `security` | The Authentication page |
| `Auth` | The operation sets its own, different `security` | Its own **Authentication** section |
| `Auth optional` | One alternative is empty (`{}`), which OpenAPI reads as "no authentication is fine too" | The Authentication page |
| `No auth` | The operation sets `security: []` | Nothing |

A spec that declares no `security` shows no badge.

Each listed scheme is resolved against `components.securitySchemes`, in the order the spec lists them:

| OpenAPI scheme | Label |
|---|---|
| `type: apiKey` | `Api key` |
| `type: http`, `scheme: basic` | `Basic` |
| `type: http`, `scheme: bearer` | `Bearer` |
| `type: http`, another scheme | The scheme name |
| `type: oauth2` | `OAuth 2.0` |
| `type: openIdConnect` | `OpenID Connect` |

Other scheme types, such as `mutualTLS`, are omitted. OpenAPI treats items in the `security` array as OR and keys inside one object as AND. Each item is one tab, and a requirement with several schemes is one tab labelled `Basic + Bearer` that shows every scheme together. Identical requirements show once.

Open the section to see one tab per scheme. A tab shows the header or query parameter to send, for example `Authorization: Basic <credentials>`, and the scheme `description`. One scheme shows no tabs. A search with the browser's find-in-page opens the section and selects the tab that contains the match.

The `/authentication` page is generated for every spec that declares security schemes, and the sidebar lists it under **Topics**. It shows the same tabs as an endpoint's section, so the Markdown below also applies there. The tabs are always open, under the page's own title, without the collapsible **Authentication** heading. Any prose comes first: an `x-topics` entry titled `Authentication` renders above the tabs, and so does a [`children:`](#api-children) page whose first heading is `Authentication`. When a child page stands for Authentication, it takes the place of the generated page in the sidebar and the badge links to it. The generated `/authentication` page stays for old links. Refer to [Topic pages](#x-topics).

The same accordion applies to **Path Parameters**, **Query Parameters**, and **Request** when those sections have more than one item. A single-item section stays expanded as a plain heading.

### Topic pages [x-topics]

`x-topics` is deprecated: write these pages as [`children:`](#api-children) pages instead. A child page that covers a topic replaces it.

The document-level `x-topics` extension lists pages of prose that explain the API as a whole, such as rate limits or how to use spaces. Each entry has a `title` and a Markdown `content`:

```yaml
x-topics:
  - title: Rate limiting
    content: |
      API calls are rate limited in a timing window.
  - title: Authentication
    content: |
      Elastic Cloud APIs support key-based authentication.
```

Each topic renders at `/topic/topic-<slug>`, the same URL as on bump.sh. The slug is the title in lowercase with spaces replaced by hyphens, so `Rate limiting` becomes `/topic/topic-rate-limiting`. Entries without a `title` are skipped.

The sidebar lists the topics under a **Topics** heading, above the endpoint groups. The Authentication page always comes first. Any `children:` pages follow in the order the docset declares them, then the spec's topics: Rate limiting first when the spec has it, and the others in alphabetical order of their titles. The heading starts expanded on every page, and it only appears when the product has at least one topic page.

A topic titled `Authentication` does not get its own topic page. Its content opens the `/authentication` page, above the authentication tabs. This also works for a spec without security schemes. The redirect from the bump.sh URL `/topic/topic-authentication` lives in `elastic/docs-infra`.

A `children:` page whose first heading or text matches a topic replaces it. Refer to [`children:`](#api-children).

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
- When `x-displayName` is absent, the canonical tag `name` is used as a fallback. If the name starts with a lowercase word (such as `elasticsearch projects`), its first letter is capitalized (`Elasticsearch projects`). Names that start with a code-like token, such as `ml_anomaly` or `v2`, or that already start with a capital letter are shown as written
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

The default sidebar matches bump.sh. The product overview and the **Topics** heading sit in one group, and the Topics heading starts expanded on every page. A divider separates that group from tag folders. Each OpenAPI operation is a visible child of its tag. A second divider sets the **Types** heading apart after the tag folders. Like Topics, Types is a heading with no page of its own.

Operations that share `x-namespace` and `x-api-name` render as **one page** when they are the same call (same parameters, request body and responses), built around the dominant method on the longest path. A sibling with a different contract keeps its own page. Specs without those extensions group by operation summary instead. The page URL uses the operation id without its numeric suffix: `search`, `search-1`, `search-2` and `search-3` become `operation-search`. The former per-operation URLs, such as `operation-search-2`, become redirect pages to the merged page, and they keep any query string and `#fragment`. Code samples carried by any of the merged operations show the dominant method of the path they call.

Sidebar grouping:

- When `x-tagGroups` is present and valid, the API Explorer uses it as an additional level of grouping in the sidebar.
- A group's section title links to the main API overview for that product. It is not a separate page and does not point at the first tag in the group. Tag landings stay under `/group/`.
- When `x-tagGroups` is absent, tags are listed directly under the API root.
- Any operation tag that is not listed under any group is still included. It appears under a fallback section named `unknown`, and the build logs a warning so you can fix the spec.
