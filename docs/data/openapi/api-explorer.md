---
navigation_title: API Explorer
---

# API Explorer

The API Explorer turns an OpenAPI spec into HTML pages. If you add an `api:` entry in `docset.yml`, {{dbuild}} generates:

- a product landing page
- one tag landing page per tag
- one operation page per operation
- schema type pages for shared types

:::{warning}
This feature is still under development and the functionality described on this page might change.
:::

## Get started

This repository includes a working example. Follow these steps against that example.

:::::{stepper}

::::{step} Read the `api:` entry in `_docset.yml`

The `api` key is valid in `docset.yml` only. Do not put it in `toc.yml`.

This repository uses `_docset.yml`. The live entry is:

```yaml
api:
  docs-builder-elasticsearch:
    - spec: elasticsearch.json
      product: elasticsearch
      repository: elastic/elasticsearch-specification
```

The map key is the URL suffix. This key produces `/api/doc/docs-builder-elasticsearch/`.

Each key takes a sequence with exactly one entry. That entry requires `spec:` and `product:`. `repository:` and `children:` are optional. See [Reference](#reference).

::::

::::{step} Preview the generated pages

If you pass `--watch`, {{dbuild}} does not generate API pages. Run serve without `--watch`:

```bash
docs-builder serve
```

Open [http://localhost:3000/api/doc/docs-builder-elasticsearch/](http://localhost:3000/api/doc/docs-builder-elasticsearch/). {{dbuild}} generates API pages on the first `/api/` request. After that, it rebuilds them when the spec file or files under `api/<key>/` change.

If you only edit Markdown outside the API tree, pass `--skip-api` to `docs-builder build`.

::::

::::{step} Open the supplemental fixture

Put operation files in `api/<key>/`. The file name is `op-` plus the spec `operationId`. Do not add a toc entry.

This repository includes `docs/api/docs-builder-elasticsearch/op-async-search-get.md`. After serve, open:

[http://localhost:3000/api/doc/docs-builder-elasticsearch/operation/operation-async-search-get/](http://localhost:3000/api/doc/docs-builder-elasticsearch/operation/operation-async-search-get/)

That file:

- replaces the spec description
- overrides the `keep_alive` and `id` parameter text
- appends a **When to poll** section after the generated reference

Heading rules, tag files, and `children:` pages are in [Writing supplemental content](./supplemental.md).

::::

::::{step} Override one major version

If one major needs different text, add a `.vN.md` file next to the base file. This repository does not ship a `.vN.md` file. The pattern is:

```text
api/elasticsearch/
  op-search.md
  op-search.v8.md
```

The unversioned `/api/doc/<key>/` tree uses the overlay of the highest numeric major that this product renders. Merge rules are in [Writing supplemental content](./supplemental.md#version-specific-files).

::::

::::{step} Read the build error, then fix the file

If the file name does not match an `operationId`, the build fails. If a parameter key is not in the spec, the build also fails.

```text
API supplemental file 'op-nope.md' does not match any operationId in the latest spec
API supplemental: Parameter 'typo' not found in operation 'async-search-get' in the latest spec
```

Fix the file. Then rebuild. More messages are in [Writing supplemental content](./supplemental.md#validation-errors).

::::

:::::

## Reference

| `docset.yml` key | Required | Description |
|---|---|---|
| `spec:` | yes | Path to an OpenAPI file, relative to `docset.yml`. If the file exists, {{dbuild}} renders it for `main`. The basename is always used to look up the remote version index. |
| `product:` | yes | A product id from `products.yml`. This binds the API to that product's versioning system. |
| `repository:` | no | `org/repo` used to look up the version index. Set this when the spec is published from a different GitHub repository than the docset. |
| `children:` | no | Extra Markdown pages under `api/<key>/`, in declared order. See [children:](./supplemental.md#children-pages). |

Each product key must have exactly one sequence entry. That entry must have exactly one `spec:`. An empty sequence fails the build. A sequence with more than one entry also fails the build.

### `spec:`

If the file exists on disk, {{dbuild}} uses it for the `main` moniker. Older majors still come from the version index.

The basename always looks up the version index. That is true when the file exists. It is also true when the file is missing. See [Remote spec resolution](#remote-spec-resolution).

### `product:`

If `product:` is not a known product id, the build fails. The error includes a suggestion.

### `repository:`

This repository sets `repository: elastic/elasticsearch-specification` because the spec is published from that repository, not from `elastic/docs-builder`.

If you omit `repository:`, {{dbuild}} uses the GitHub remote of the current checkout.

### `children:`

`children:` adds full Markdown pages under the product root. Supplemental `op-*.md` and `tag-*.md` files are not `children:` pages. They merge into generated operation and tag pages.

{{dbuild}} does not emit child files as normal docset HTML. Do not add them to `exclude:`.

## Page URLs

`{key}` is the `api:` map key. It is not the `product:` id.

| Page | Path |
|---|---|
| Product root (`main`) | `/api/doc/{key}/` |
| Released major | `/api/doc/{key}/v9/`, `/api/doc/{key}/v8/` |
| Operation | `/api/doc/{key}/operation/operation-{operationId}/` |
| Tag landing | `/api/doc/{key}/group/endpoint-{tagSlug}/` |
| Schema type | `/api/doc/{key}/types/{schemaMoniker}/` |
| Child Markdown | `/api/doc/{key}/{slug}/` |

{{dbuild}} lowercases the `operationId` in the URL. For tag slugs, it replaces spaces with hyphens and lowercases the name. Underscores stay.

The `api:` key creates this URL tree. Do not list generated operation pages in `toc.yml`. From Markdown inside an API page, you can link with paths such as `../group/endpoint-search` and `../operation/operation-search`. {{dbuild}} rewrites those links against the current product base.

## Multi-version behavior

For a versioned product, {{dbuild}} renders every resolved version:

| Index moniker | URL path | Role |
|---|---|---|
| `main` | `/api/doc/{key}/` | Current-major tree |
| `9`, `8` | `/api/doc/{key}/v9/`, `/v8/` | Frozen major snapshots |

The numeric `9` entry is a frozen snapshot. It is not the same as `main`. The unversioned tree uses the overlay of the highest numeric major that this product renders.

If a local spec file exists, it overrides `main` only.

A versionless product (`versioning: serverless` and similar) renders only `/api/doc/{key}/`. If more than one version is rendered, the left navigation shows a version dropdown.

## Remote spec resolution

If `spec:` does not resolve to a file on disk, {{dbuild}} fetches `main` from a CloudFront version index. Repositories that publish OpenAPI specs share this index.

Object keys in the bucket look like this:

```
<org>/<repo>/<branch>/<spec-name>.<ext>
```

Example: `elastic/elasticsearch-specification/main/elasticsearch.json`.

The root manifest is `https://d29hkgsdo66d1n.cloudfront.net/index.json`. It is keyed by `org/repo`, then spec basename, then moniker (`main`, `9`, `8`). {{dbuild}} fetches that manifest once per build. It looks up `repository:` first. If `repository:` is missing, it uses the GitHub remote of the current checkout. Spec objects are fetched at `{base}/{org}/{repo}/{version}/{spec-basename}`.

If there is no local spec and the index has no matching entry, the build fails. If a local spec exists, that miss is a warning. Then {{dbuild}} renders the local file.

## When the API Explorer runs

- **`docs-builder build`.** {{dbuild}} generates API pages unless you pass `--skip-api`.
- **`docs-builder serve`.** {{dbuild}} generates API pages on the first `/api/` request. It rebuilds them when the spec or `api/<key>/` Markdown files change. `--watch` skips API generation.
- **Assembler builds.** {{dbuild}} generates API pages when the `assembler-api-explorer` feature flag is on. `staging` and `preview` set `ASSEMBLER_API_EXPLORER`. Production does not.

## OpenAPI extensions

These spec extensions change how pages render. They live in the OpenAPI file, not in supplemental Markdown.

### `x-codeSamples`

If an operation has `x-codeSamples`, the operation page shows a **Code Examples** section. Each array item needs `lang` and `source`. Console is sorted first when present. The tab list is the `lang` values in the spec. Tabs use the `api-language` sync group, so the selected language stays across pages.

```json
"x-codeSamples": [
  { "lang": "Console", "source": "GET /_search" },
  { "lang": "curl", "source": "curl -X GET ..." }
]
```

### `x-req-auth`

If an operation has `x-req-auth` as a JSON array of strings, the operation page shows a **Prerequisites** section after **Paths**. Empty strings are dropped. If the value is not an array, the section is omitted and the build may log a warning.

```json
"x-req-auth": [
  "Cluster privilege: `cluster:admin/snapshot`"
]
```

### `x-displayName`

If a tag object has `x-displayName`, navigation and tag headings use that string. URLs still use the canonical tag `name`. If two tag names slug to the same URL segment, the build fails.

### `x-tagGroups`

If the document has `x-tagGroups`, the sidebar groups tags by those lists. Group order follows the array. A group title links to the product landing page. It is not its own URL. Tags that are missing from every group appear under `unknown`. The build logs a warning.
