---
navigation_title: Supplemental content
---

# Writing supplemental content

Supplemental files change generated operation pages and tag landing pages. Put the files in `api/<key>/`. Do not put them next to the spec file. Do not list them in `toc.yml`.

{{dbuild}} reads only top-level `*.md` files in that folder. Nested folders are ignored.

If an `op-*.md` or `tag-*.md` name does not match the spec, the build fails. If an operation file lists a parameter or request-body field that the spec does not have, the build also fails.

Schema type pages do not use supplemental files.

For a walkthrough that uses this repository, see [API Explorer](./api-explorer.md).

## File naming

| File pattern | Matches |
|---|---|
| `op-<operationId>.md` | Operation whose spec `operationId` equals `<operationId>` |
| `tag-<tagSlug>.md` | Tag whose URL slug equals `<tagSlug>` |
| `op-<operationId>.vN.md` | Same operation, merged for major `N` only |
| `tag-<tagSlug>.vN.md` | Same tag, merged for major `N` only |

The `op-` stem is the spec `operationId` with no change. Do not slugify it.

The `tag-` stem is the tag URL slug. {{dbuild}} replaces spaces with hyphens. It lowercases the name. Underscores stay. `search` matches `tag-search.md`. `ML Anomaly` matches `tag-ml-anomaly.md`. `ml_anomaly` matches `tag-ml_anomaly.md`.

```text
api/docs-builder-elasticsearch/
  op-async-search-get.md
```

A top-level `.md` file that is not `op-*.md` or `tag-*.md` is not a supplemental file. If you want that file as its own page, list it under `children:`.

If the file lives under the documentation source directory, give it a `#` title so the docset scanner has a page title. That heading is not the operation description. Use `## Description` for the generated API page.

## Heading rules

Headings control how {{dbuild}} merges the file into the generated page.

### Frontmatter

{{dbuild}} strips YAML frontmatter so it is not the page body.

For `op-*.md` and `tag-*.md`, frontmatter is not applied to the generated page. `applies_to` and `navigation_title` in those files have no effect. Operation titles come from the spec summary. Availability badges come from the spec, not from supplemental YAML.

For `children:` pages, `navigation_title` in frontmatter sets the navigation label. Those pages run through the normal Markdown pipeline.

### No headings

A file with no `##` headings replaces the spec description:

```markdown
Retrieve the results of a previously submitted asynchronous search request.
```

### Description

A `## Description` section replaces the spec description. Other sections in the same file still apply.

### Parameters and request body

These headings work on **operation** files only:

- `## Parameters`
- `## Query parameters`
- `## Path parameters`
- `## Request body`

Each listed field starts with `: field_name` or `: \`field_name\``. Unlisted fields keep the spec text. An unknown key fails the build.

On a **tag** file, those headings are not shown. Tag files use the description and extra `##` sections only.

```markdown
## Parameters

: `keep_alive`
  How long Elasticsearch keeps this search and its saved results.

: id
  The async search id returned by the submit request.
```

### Additional sections

Any other `##` heading is appended after the generated reference content. Headings stay in document order.

## Version-specific files

If one major needs different text, add a `.vN.md` file next to the base file:

```text
api/elasticsearch/
  op-search.md
  op-search.v8.md
```

The version file uses the same heading rules as the base file. The two files merge as follows:

- `## Description` or bare text replaces the base description for that version.
- Listed parameter and request-body keys replace or add to the base overrides. Unlisted keys stay.
- A new extra `##` heading is added. If both files use the same extra heading, the version file replaces that section.
- Omitted sections keep the base file.

If a version has no `.vN.md` file, that version uses the base file only.

The unversioned `/api/doc/{key}/` tree uses the overlay of the highest numeric major that this product renders. Versionless products render only `main`. They have no numeric major, so they get no overlay.

{{dbuild}} checks a version-suffixed file against that major's spec. `op-search.v8.md` must match an `operationId` in the v8 spec.

## `children:` pages

`children:` pages are separate Markdown pages under the product root. You declare them in `docset.yml`. {{dbuild}} does not pick them up by file name.

```yaml
api:
  kibana:
    - spec: kibana-openapi.json
      product: kibana
      children:
        - file: kibana-api-overview.md
```

Paths are relative to `api/<key>/`. These pages can use MyST directives, substitutions, and cross-links.

A `*.vN.md` suffix limits that child to major `N`. `getting-started.md` appears in every version. `knn-guide.v9.md` appears in 9.x. If 9 is the highest numeric major, it also appears on the unversioned `main` tree.

### Child slugs

{{dbuild}} builds the URL slug from the filename:

- It lowercases the name.
- It replaces spaces and underscores with hyphens.
- It removes the `.md` extension.
- It drops a `.vN` suffix from the slug.

`Getting-Started.md` becomes `getting-started`. `knn-guide.v9.md` becomes `knn-guide`.

These slugs are reserved:

| Reserved slug | Reason |
|---|---|
| `types` | Schema type pages use `/types/` |
| `group` | Tag landing pages use `/group/` |
| `operation` | Operation pages use `/operation/` |

Do not list `op-*.md` or `tag-*.md` under `children:`.

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

Unmatched base files are reported against `the latest spec`. Version-suffixed files are reported against `version {N}`.

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
