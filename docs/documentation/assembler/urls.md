---
navigation_title: URLs
---

# URL structure in assembler builds

In an assembler build, the final URL of each page is composed from two parts:

1. The **path prefix** assigned in `navigation.yml`
2. The **page path** from the isolated build (determined by the file's position relative to `docset.yml`)

```
https://example.com/{path_prefix}/{page_path}
```

## How path prefixes work

Each `toc:` entry in `navigation.yml` gets a `path_prefix` that determines where its content appears in the assembled site:

```yaml
# navigation.yml
toc:
  - toc: elasticsearch://setup
    path_prefix: elasticsearch/setup
  - toc: kibana://getting-started
    path_prefix: kibana/getting-started
```

A page at `setup/install.md` in the Elasticsearch repo:
- **Isolated build URL**: `/setup/install`
- **Assembled URL**: `/elasticsearch/setup/install`

## Splitting a repository across the site

The same repository's TOC sections can be placed at different URL prefixes:

```yaml
toc:
  - toc: elastic-project://api
    path_prefix: reference/api
  - toc: elastic-project://guides
    path_prefix: learn/guides
```

The isolated build has `/api/overview` and `/guides/quickstart`. After assembly, they become `/reference/api/overview` and `/learn/guides/quickstart`.

## Relationship to isolated URLs

The [URL structure in isolated builds](../../isolated/urls.md) is the foundation — file position relative to `docset.yml` determines the base path. The assembler prepends `path_prefix` without changing any of the internal structure.

This means content authored for isolated preview renders at the same relative paths when assembled — only the prefix changes.
