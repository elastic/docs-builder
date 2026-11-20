---
navigation_title: API crawl check
---

# API crawl check

`docs-builder assembler crawl-check` crawls every API Explorer URL on a deployed docs site and compares each answer with the API page-type table in `src/Elastic.ApiExplorer/Infrastructure/ApiPageType.cs`. It prints each difference as an error, prints a summary per page type, and exits 1 when it finds any difference. Run it before a cutover or a major release, and again after a fix.

## Run it

```bash
# Edge: the same flag-on build as staging, without the Google sign-in
dotnet run --project src/tooling/docs-builder -- assembler crawl-check --environment edge

# Prod: robots must allow indexing, because prod sets allow_indexing
dotnet run --project src/tooling/docs-builder -- assembler crawl-check --environment prod

# A local or preview host, with the edge expectations
dotnet run --project src/tooling/docs-builder -- assembler crawl-check --environment edge --base-url http://localhost:4000
```

Staging answers every request with a Google sign-in redirect, so the check cannot crawl it. Edge serves the same build.

| Option | Default | What it does |
|---|---|---|
| `--environment` | `edge` | Reads `uri`, `path_prefix` and `allow_indexing` from `config/assembler.yml`. |
| `--base-url` | the environment `uri` | Crawls another scheme and host with the same expectations. |
| `--links-json` | docs-content `main` in the link index | A local `links.json` that lists the API pages. The default is read anonymously from the link index bucket. |
| `--redirect-status` | `302` | The status an old `/docs/api/doc/` URL must redirect with. |
| `--redirect-sample` | `50` | How many API pages to check through their old URL, on top of the redirect table and every tree root. |
| `--concurrency` | `16` | How many requests run at once. |

## What it checks

The pages come from the floating `api/{key}` sections of `links.json`: each page under its version root and its mirror root, plus the `/docs/api` hub. A URL in `sitemap-api.xml` that `links.json` does not list is crawled too. Each version root gets its page type from `ApiPageTypes.Of`, the function the renderer uses, and a path takes the type of its longest version root:

| Root | Page type |
|---|---|
| the current version, and the hub | `Versionless` |
| the `mirror_root` of the current version, `/vN` | `NewestMajorCopy` |
| `main` when it is not current, `/next` | `Unreleased` |
| any other version | `OlderMajor` |

The expected canonical URL, robots value and sitemap membership come from `ApiPageTypes`. The canonical URL is always on `https://www.elastic.co`, whatever host is crawled.

## What each difference means

| Field | Meaning |
|---|---|
| `Status` | The page did not answer 200 directly. It redirected, was missing, or failed. The other fields of that page are not checked. |
| `Canonical` | The `<link rel="canonical">` names another URL. A `NewestMajorCopy` page must name its versionless page; every other page names itself. |
| `Robots` | The `<meta name="robots">` value differs. A build without `allow_indexing` must say `noindex, nofollow` everywhere. On prod, `Unreleased` pages say `noindex` and the others `index, follow`. |
| `Sitemap` | The page is in `sitemap-api.xml` and its type does not belong there, or the other way round. Only `Versionless` and `OlderMajor` pages belong there. |
| `SitemapIndex` | `sitemap.xml` does not list `sitemap-api.xml` at its own URL beside the index (same host and path), or a sitemap did not answer 200. |
| `UnknownSitemapUrl` | A URL in `sitemap-api.xml` belongs to no API tree in `links.json`. |
| `Hops` | An old `/docs/api/doc/` URL did not redirect exactly once. |
| `RedirectStatus` | The redirect used another status than `--redirect-status`. |
| `RedirectTarget` | The redirect went somewhere other than `ApiLegacyRedirects.Resolve` says: the renamed root, the exact page, or the same path without `/doc`. |

The redirect sample takes the redirect table in `ApiLegacyRedirects`, then the root and a sample of pages of each `Versionless` and `OlderMajor` tree under `/docs/api/doc/`. `Unreleased` and `NewestMajorCopy` trees are left out, because their old `/next` and `/vN` URLs never existed. The table only describes a site under `/docs`, so on any other `path_prefix` the step is skipped with an info line.

The `Robots` value must match exactly, spacing included.

The summary counts the pages checked and the differences found per page type, plus a row for the redirect sample and one for the sitemap.
