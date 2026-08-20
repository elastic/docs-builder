---
navigation_title: related-learning.yml
---

# Related learning

The [`related-learning.yml`](https://github.com/elastic/docs-builder/blob/main/config/related-learning.yml) file is a global catalog of learning destinations (training modules, labs, and similar). When a documentation page matches an entry's `pages` list, {{dbuild}} appends a **Related learning** heading and list to that page automatically. The heading is a normal H2, so it appears in **On this page**.

This catalog ships with docs-builder and is available in both isolated and assembler builds. Content repositories pick up catalog changes on the next docs-builder version.

## Example

```yml
links:
  apm-with-elastic:
    title: APM with Elastic
    url: https://www.elastic.co/training/apm-with-elastic
    pages:
      - docs-content://solutions/observability/apm/index.md
  index-basics:
    title: Index Basics
    url: https://www.elastic.co/training/index-basics
    pages:
      - docs-content://manage-data/data-store/index-basics.md
```

## Structure

`links`
:   A YAML mapping where each key is a stable link ID (typically the training URL slug). Each value is a mapping with:
* `title` (required): Link text shown under the **Related learning** heading.
* `url` (required): Absolute `https://` destination.
* `pages` (optional): List of documentation pages that should show this link. Each entry **must** be a qualified cross-link of the form `{repository}://path.md` (same scheme as TOC and cross-links). Unqualified paths are rejected when the catalog loads.

## How matching works

For each page, {{dbuild}} builds `{current-repository}://{path-relative-to-docset}` and looks for catalog entries whose `pages` list contains that exact cross-link.

- Matching is case-sensitive and uses forward slashes.
- A page can match more than one link. Matching links appear in **catalog file order**.
- If a listed file is missing from the named repository, the build does not fail; that link simply does not appear for any rendered page.

## Add a learning module

1. Open [`config/related-learning.yml`](https://github.com/elastic/docs-builder/blob/main/config/related-learning.yml) in docs-builder.
2. Add a new key under `links` with `title`, `url`, and one or more `pages` cross-links.
3. Open a pull request. After the next docs-builder release, assembler and isolated builds that use that version show the section on the mapped pages.

To stop showing a link on a page, remove that page from the entry's `pages` list.
