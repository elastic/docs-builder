---
navigation_title: Supplemental content
---

# Writing supplemental content

Supplemental files add context to generated API Explorer pages. They also add usage examples and richer parameter descriptions. You do not edit the OpenAPI spec.

**Files are discovered automatically.** Drop a file into `api/<key>/` that follows the naming convention. The next build finds the file. Do not add it to `toc.yml` or `children:`.

**Validation is strict.** If a file name does not match an `operationId` or tag in the spec, the build fails. If a parameter key or request-body key is unknown, the build also fails. If you rename or remove an operation, an old file fails the build.

**Frontmatter is metadata only.** YAML frontmatter can set `description`, `applies_to`, and `navigation_title`. {{dbuild}} copies that metadata to the generated page. It does not render frontmatter as the page body.

Schema type pages use descriptions from the OpenAPI spec only. There is no `schema-*.md` convention.

For a step-by-step example, see [API Explorer](./api-explorer.md).

## File naming

{{dbuild}} discovers only top-level `*.md` files in `api/<key>/`. It ignores nested folders.

| File pattern | Matches |
|---|---|
| `op-<operationId>.md` | Operation whose spec `operationId` equals `<operationId>` with no rewriting |
| `tag-<tagSlug>.md` | Tag whose URL slug equals `<tagSlug>` |
| `op-<operationId>.vN.md` | Same operation, merged on major `N` only |
| `tag-<tagSlug>.vN.md` | Same tag, merged on major `N` only |

The `op-` stem is the spec `operationId` with no change. Do not slugify it.

The `tag-` stem is the tag URL slug. {{dbuild}} replaces spaces with hyphens. It lowercases the name. Underscores stay. `search` matches `tag-search.md`. `ML Anomaly` matches `tag-ml-anomaly.md`. `ml_anomaly` matches `tag-ml_anomaly.md`.

```text
api/elasticsearch/
  op-async-search-get.md
  op-search.md
  op-search.v8.md
  tag-search.md
  getting-started.md          # not auto-discovered; list it under children:
```

{{dbuild}} ignores any other top-level `.md` file during supplemental discovery. If you want that file as a page, list it under `children:`.

## Heading rules

The heading structure of a supplemental file controls what the file adds to the generated page.

### Frontmatter

```markdown
---
description: Retrieve results of a previously submitted async search.
applies_to:
  stack: ga
---

## Description

Poll `GET /_async_search/{id}` until `is_running` is `false`.
```

{{dbuild}} keeps frontmatter as metadata. It uses the `## Description` section as the page description. If the file has only frontmatter, it uses the spec description.

### No headings

A file with no `##` headings replaces the spec description:

```markdown
Retrieve the results of a previously submitted asynchronous search request.
```

### Description

A `## Description` section replaces the spec description. Other sections in the same file still apply:

```markdown
## Description

Retrieve results of a previously submitted async search.
Elasticsearch restricts access to the user or API key that submitted the request.

## When to poll

Retry until `is_running` is `false`.
```

### Parameters and request body

These headings replace descriptions of the fields you list:

- `## Parameters`
- `## Query parameters`
- `## Path parameters`
- `## Request body`

Unlisted fields keep the spec text.

Each entry starts with `: field_name` or `: \`field_name\``:

```markdown
## Parameters

: `keep_alive`
  How long Elasticsearch keeps this search. Extending it also extends the
  validity of the saved results.

: id
  The async search id returned by the submit request.
```

If a key is unknown, the build fails.

### Additional sections

Any other `##` heading is appended after the generated reference content. Headings stay in document order. Use these sections for examples or background that belong after the parameter tables.

## Version-specific files

If a description or parameter must differ for one major, add a `.vN.md` file next to the base file:

```text
api/elasticsearch/
  op-search.md        # every version that has this operation
  op-search.v8.md     # merged on top of the base file for 8.x only
  tag-ml-anomaly.md
  tag-ml-anomaly.v9.md
```

The version file uses the same heading rules as the base file. Two files merge, so one extra rule applies:

- `## Description` or bare text replaces the base description for that version.
- Listed parameter and request-body keys replace or add to the base overrides. Unlisted keys stay.
- Extra `##` sections with a new heading are added alongside the base file. If both files use the same extra heading, the version file replaces the base section.
- Sections you omit keep the base file.

If a version has no matching `.vN.md`, that version uses the base file with no change. Tag files use this same merge.

The unversioned `main` tree uses the overlay of the current major. Then `/api/doc/{key}/` matches `/vN/` for that major. Versionless products (serverless and similar) get no overlay.

{{dbuild}} validates a version-suffixed file against that major's spec. `op-search.v8.md` must match an `operationId` in the v8 spec. A match in `main` only is not enough.

## `children:` pages

`children:` pages are full Markdown pages under the product root. You declare them in `docset.yml`. {{dbuild}} does not discover them by name:

```yaml
api:
  kibana:
    - spec: kibana-openapi.json
      product: kibana
      children:
        - file: kibana-api-overview.md
```

Paths are relative to `api/<key>/`. Child pages can use all MyST directives, substitutions, and cross-links.

A `*.vN.md` suffix limits that child to major `N` only. `getting-started.md` appears in every version. `knn-guide.v9.md` appears only in 9.x. If 9 is the current major, it also appears in the unversioned `main` tree.

### Child slugs

{{dbuild}} builds the URL slug from the filename:

- It lowercases the name.
- It replaces spaces and underscores with hyphens.
- It removes the `.md` extension.
- It does not include a `.vN` suffix in the slug.

`Getting-Started.md` becomes `getting-started`. `knn-guide.v9.md` becomes `knn-guide`.

These slugs are reserved. Do not use them as child file names:

| Reserved slug | Reason |
|---|---|
| `types` | Schema type pages live under `/types/` |
| `group` | Tag landing pages live under `/group/` |
| `operation` | Operation pages live under `/operation/` |

Do not list `op-*.md` or `tag-*.md` under `children:`. {{dbuild}} discovers those files automatically.

## Validation errors

Authors see these messages at build time.

**Supplemental files**

```text
API supplemental file 'op-nope.md' does not match any operationId in the latest spec
API supplemental file 'tag-nope.md' does not match any tag in the latest spec
API supplemental file 'op-search.v8.md' does not match any operationId in version 8
API supplemental: Parameter 'typo' not found in operation 'async-search-get' in the latest spec
API supplemental: Request body field 'typo' not found in operation 'search' in the latest spec
```

Unmatched base files (`op-*.md` without `.vN`) are reported against `the latest spec`. Version-suffixed files are reported against `version {N}`.

**`children:` and slugs**

```text
Child page 'op-search.md' for API 'elasticsearch' uses a supplemental file name (op-*.md / tag-*.md). Those files are auto-discovered and cannot be listed under children:.
Child page 'missing.md' for API 'elasticsearch' does not exist under 'api/elasticsearch/'.
Markdown file slug 'types' (from 'types.md') conflicts with reserved API Explorer segment in product 'elasticsearch'. Reserved segments: types, group, operation
Duplicate markdown slug 'getting-started' found in API product 'elasticsearch'.
```

**`api:` config**

```text
API configuration for 'elasticsearch' must have exactly one entry, found 2.
API 'elasticsearch' is missing required 'product:'. It must match a product id defined in products.yml.
Unknown 'product: widgets' for API 'elasticsearch'. It must be a product id defined in products.yml.
API 'elasticsearch' is missing required 'spec:'. Its basename is required to resolve the remote version index, even when the file is not present locally.
'repository: elasticsearch-specification' for API 'elasticsearch' must be in 'org/repo' form, e.g. 'elastic/elasticsearch-specification'.
```
