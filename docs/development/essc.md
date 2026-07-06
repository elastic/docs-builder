---
navigation_title: essc — sourcing CLI
---

# essc — sourcing CLI

`essc` is an AOT-compiled .NET CLI for indexing elastic.co content into
Elasticsearch. It lives in `src/tooling/essc/` and shares the search document
contract (`Elastic.Documentation.Search.Contract`) with the docs indexing
pipeline, keeping every content source schema-consistent. Commands are
namespaced by content source so new sources can be added without breaking the
existing interface.

Current sources:

| Source       | Namespace           | Description                                          |
| ------------ | ------------------- | ---------------------------------------------------- |
| Contentstack | `contentstack`      | elastic.co marketing, blog, product, and event pages |
| Labs         | `labs`              | Search, security, and observability labs properties  |
| Legacy docs  | `guide` _(planned)_ | `/guide` legacy documentation                        |

## Installation

Pull the distroless container image:

```bash
docker pull ghcr.io/elastic/website-search-essc:latest
```

Available tags: `latest`, `edge` (latest main), and per-version tags.

Or run from source:

```bash
dotnet run --project src/tooling/essc -- --help
```

## Configuration

`essc` resolves credentials in this order of precedence:

1. CLI flags (`--es-url`, `--es-api-key`)
2. Environment variables (see table below)
3. Dotnet user-secrets store `docs-builder` (local development) — the same store the
   rest of this repository uses, so aspire and essc share `Parameters:ElasticsearchUrl`
   and `Parameters:ElasticsearchApiKey`

### Environment variables

| Environment variable          | Description                           |
| ----------------------------- | ------------------------------------- |
| `ELASTICSEARCH_URL`           | Full Elasticsearch URL including port |
| `ELASTICSEARCH_API_KEY`       | Elasticsearch API key                 |
| `CONTENTSTACK_API_KEY`        | Contentstack Content Delivery API key |
| `CONTENTSTACK_DELIVERY_TOKEN` | Contentstack delivery token           |

The dotnet configuration section keys (`Parameters:ElasticsearchUrl`, `ContentStack:ApiKey`,
etc.) are also accepted. For CI, use the flat environment variable names above.

For local development, populate the secrets store directly:

```bash
dotnet user-secrets set --id docs-builder Parameters:ElasticsearchUrl <url>
dotnet user-secrets set --id docs-builder Parameters:ElasticsearchApiKey <key>
dotnet user-secrets set --id docs-builder ContentStack:ApiKey <key>
dotnet user-secrets set --id docs-builder ContentStack:DeliveryToken <token>
```

If you previously used essc from the website-search-data repository, copy the
`Parameters:Elasticsearch*` and `ContentStack:*` values from your old
`elastic-website-ai-search` store into the `docs-builder` store
(`~/.microsoft/usersecrets/`, on Windows `%APPDATA%\Microsoft\UserSecrets\`).

## Contentstack commands

### `contentstack sync`

Fetches all published content from Contentstack and indexes it into Elasticsearch.
Uses 5 parallel lanes. Cursors are saved after every page so interrupted runs
resume automatically.

```bash
essc contentstack sync
```

| Flag             | Default            | Description                                                             |
| ---------------- | ------------------ | ----------------------------------------------------------------------- |
| `--force`        | false              | Delete stored cursors and reindex from scratch                          |
| `--no-index`     | false              | Fetch and map only — skip Elasticsearch indexing                        |
| `--no-ai`        | false              | Skip generative AI (no post-sync enrich batch)                          |
| `--max-ai-docs`  | 100 (when omitted) | Positive cap on documents enriched after finalize; omit for default 100 |
| `--max-ai-time`  | none               | Wall-clock cap for post-sync AI (minimum `1m` when set)                 |
| `--es-url`       | from secrets       | Override Elasticsearch endpoint                                         |
| `--es-api-key`   | from secrets       | Override Elasticsearch API key                                          |
| `--page-per`     | 0 (unlimited)      | Max pages per content type (useful for testing)                         |
| `--cache-folder` | OS app data        | Override cursor state directory                                         |

`--max-ai-docs` uses DataAnnotations: it must be at least **1** when passed; **`0` is invalid**. For a large or unbounded batch without re-syncing, use **`contentstack ai`** instead.

The sync runs in two phases:

1. **Fetch + index** — pages through Contentstack in parallel, maps each item to a
   `SiteDocument`, and bulk-indexes into both the lexical and semantic indices.
2. **Finalize** — flushes the remaining buffer, reindexes the lexical index into
   the semantic index, publishes synonyms and query rules, then runs a **bounded**
   generative AI enrichment pass on the semantic write index (unless `--no-ai`).

### `contentstack ai`

Runs generative AI enrichment on existing **`site-*`** semantic indices without calling Contentstack. Same Elasticsearch overrides as `contentstack sync`.

```bash
essc contentstack ai
```

| Flag             | Default       | Description                               |
| ---------------- | ------------- | ----------------------------------------- |
| `--max-run-time` | unlimited     | Stop enrichment after N minutes           |
| `--max-run-docs` | 0 (unlimited) | Enrich at most N documents (`0` = no cap) |
| `--es-url`       | from secrets  | Override Elasticsearch endpoint           |
| `--es-api-key`   | from secrets  | Override Elasticsearch API key            |

### `contentstack types`

Lists all Contentstack content types and their document counts.

```bash
essc contentstack types
```

### `contentstack samples`

Dumps raw Contentstack entries for a given content type, useful for inspecting
the source data structure.

```bash
essc contentstack samples <content-type>
```

## Labs commands

### `labs sync`

Discovers labs URLs from sitemaps, crawls HTML, and bulk-ingests into **`labs-*`** indices. See `essc labs sync --help` for crawl flags (`--dry-run`, `--force`, `--no-ai`, `--max-ai-docs`, `--max-ai-time`, etc.).

### `labs ai`

Runs generative AI enrichment on existing **`labs-*`** semantic indices without re-crawling. Flags match **`contentstack ai`** (`--max-run-time`, `--max-run-docs`, `--es-url`, `--es-api-key`).

```bash
essc labs ai
```

## CI behaviour

When `CI=true` or stdout is not a TTY, `essc` switches from interactive progress
widgets to plain log lines:

```
[Lane 1]    100%   blog — 1,240 fetched, 1,198 indexed
[Lane 2]     14%   customer_tile — 42 fetched, 40 indexed (2 skipped)
…      14%   secondary reindex — 1,240/8,500
✓     100%   secondary reindex — 8,500/8,500
```

## Release pipeline

`essc` ships as a **distroless container** only, published to
`ghcr.io/elastic/website-search-essc` using the .NET SDK's native container
support as part of the docs-builder release pipeline (`./build.sh publishcontainers`).
Base image: `mcr.microsoft.com/dotnet/nightly/runtime-deps:10.0-noble-chiseled`.
Tagged `edge;latest;<version>` on release, `edge` on every push to main.

AOT compilation is validated on every CI run by building the container image and
running `essc --help` against it.
