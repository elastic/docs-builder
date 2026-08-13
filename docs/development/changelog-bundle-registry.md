---
navigation_title: Changelog bundle registry
---

# Changelog bundle registry and CDN delivery

This page describes how changelog **bundles** are published to a public, CDN-fronted
S3 bucket, how the per-product `registry.json` manifest and the shallow per-tree change
maps are produced by the **scrubber Lambda** (their sole writer — see
[elastic/docs-eng-team#688](https://github.com/elastic/docs-eng-team/issues/688)), and the
`cdn:` mode for the [`{changelog}` directive](/syntax/changelog.md) that consumes
bundles directly from the CDN instead of from a local folder.

## Motivation

Today the `{changelog}` directive only renders bundles that live in a folder inside the
docset (default `changelog/bundles/`). That requires every consuming repository to vendor
a copy of the bundle YAML it wants to render.

The link service ([link infrastructure](/documentation/distributed-builds.md)) already demonstrates
the pattern we want: an S3 bucket fronted by CloudFront, publicly readable, with a small
JSON index at a well-known key. We apply the same approach to changelog bundles so a docset
can render another product's release notes by pointing the directive at the CDN — no vendored
copies, no cross-repo file syncing.

## Architecture

```mermaid
flowchart LR
    CI["Client CI<br/>(docs-actions)"] -->|"changelog upload<br/>(YAML objects only)"| Private["Private S3 bucket<br/>bundle/{product}/*.yaml<br/>changelog/{org}/{repo}/{branch}/*.yaml"]
    Private -->|"s3:ObjectCreated / ObjectRemoved<br/>→ SQS"| Scrubber["Changelog scrubber<br/>Lambda"]
    Scrubber -->|"scrub + copy/delete,<br/>then reconcile bundle registry.json<br/>+ shallow maps from public listing"| Public["Public S3 bucket<br/>+ CloudFront CDN<br/>(incl. registry.json)"]
    Public -->|"reads via CDN"| Directive["{changelog} directive<br/>(cdn: mode)"]
```

1. **Uploader** — `changelog upload --target s3` (invoked by the docs-actions changelog
   upload workflow) writes bundle YAML to `bundle/{product}/{file}` and changelog-entry YAML
   to `changelog/{org}/{repo}/{branch}/{file}` in the **private** bucket. That is all it does:
   it never writes a `registry.json` (see [Ownership per tree](#ownership-per-tree)).
2. **Scrubber Lambda (the registry's sole producer)** — S3 events from the private bucket are
   *triggers, not instructions*: for each affected key the Lambda reconciles the public object
   against current private-bucket state (present → scrub and copy; absent → delete the public
   copy), then rebuilds the affected group's `registry.json` from the **public bucket's actual
   listing** (`registry = f(state)`, never `f(event)`). Any successful reconcile repairs *all*
   accumulated drift in the group, not just the change that triggered it. The touched tree's
   [shallow change map](#shallow-maps) is patched from the same listing pass.
3. **Consumer** — for each product declared under `release_notes` in `docset.yml`, docs-builder
   reads `bundle/{product}/registry.json` from the CDN at build startup and fetches each listed
   bundle; the `{changelog}` directive in `cdn:` mode then renders from the prefetched result.

### Why a registry instead of an S3 listing

The public surface is a CDN (CloudFront) in front of S3. CloudFront does not expose bucket
listing, so the consumer cannot enumerate `bundle/{product}/`. The registry is a stable,
cacheable manifest at a predictable key that lists exactly which bundles exist for a product.

## `registry.json` format

Both indexes share this schema, serialized with `snake_case` keys.

### Ownership per tree [ownership-per-tree]

The two trees part ways on who writes the manifest
(the [2026-08-10 update on elastic/docs-eng-team#688](https://github.com/elastic/docs-eng-team/issues/688)
narrowed reconciliation to the bundle tree):

- **Bundle index** — `bundle/{product}/registry.json`, **public bucket only**, produced
  exclusively by the scrubber Lambda's `BundleRegistryReconciler`. This is the manifest the
  `{changelog}` directive and external CDN consumers enumerate, and the subject of the rest of
  this page.
- **Changelog-entry index** — `changelog/{org}/{repo}/{branch}/registry.json`, a **legacy
  client-authored pass-through**: the current `changelog upload` never writes one, but manifests
  written by older CLI versions are still mirrored verbatim from the private bucket, because
  [`changelog bundle` entry sourcing](#entry-sourcing) still enumerates a pool through its
  manifest. It is *not* reconciled — its `producer` is null and its recorded `etag` is the old
  pre-scrub private-object hash (consumers ignore it). It goes away entirely once release-note
  discovery starts from PR lists (RFC [elastic/docs-eng-team#698](https://github.com/elastic/docs-eng-team/issues/698)).

```json
{
  "schema_version": 1,
  "producer": "changelog-scrubber-reconcile/1",
  "product": "elasticsearch",
  "generated_at": "2026-05-06T12:00:00+00:00",
  "bundles": [
    { "file": "9.4.0.yaml", "target": "9.4.0", "etag": "…" },
    { "file": "9.3.0.yaml", "target": "9.3.0", "etag": "…" }
  ]
}
```

| Field | Meaning |
|---|---|
| `schema_version` | Bumped when consumers must change their parser. A manifest declaring a *newer* schema than the reconciler understands is reported and left untouched, never downgraded. |
| `producer` | The reconcile algorithm version that wrote the manifest (`BundleRegistryReconciler.Producer`, currently `changelog-scrubber-reconcile/1`). A mismatch forces a full metadata recompute and a rewrite even when entries come out identical — this is how metadata-logic fixes roll out to every group. Null on legacy client-written manifests. Consumers should ignore it. |
| `product` | Grouping identifier — the product for a bundle index (`bundle/{product}/…`) or the `{org}/{repo}/{branch}` prefix for a changelog-entry index (`changelog/{org}/{repo}/{branch}/…`). |
| `generated_at` | UTC timestamp of the last reconcile that wrote the manifest. Never the only thing that changes — a reconcile whose entries are identical skips the write. |
| `bundles[].file` | Bundle file name, resolved at `bundle/{product}/{file}` (or entry file at `changelog/{org}/{repo}/{branch}/{file}` for the entry index). |
| `bundles[].target` | Target version/date from the bundle's declaration of **this** product (may be null). For an amend sidecar (`{name}.amend-{N}.yaml`), recomputed on every reconcile against its parent in the same public prefix; a missing parent records `null` and self-corrects once the parent lands. Entry indexes record no target. |
| `bundles[].etag` | The **public (CDN) object's ETag**, recorded verbatim from the public listing. Usable for HTTP cache revalidation against the CDN and as the reconciler's cheap change detector. |

Bundles are sorted by `target` descending (newest first) with a deterministic tiebreak on
`file`, so the JSON is stable across reruns.

### Absent ≠ empty

A group whose last public object is deleted gets its manifest **deleted**, not emptied: a
registry 404 means "unpublished" and is a fail-fast error for declared consumers (exactly as
for a product that was declared under `release_notes` but never published — the signal to
remove the declaration), while a manifest with an empty `bundles` list would read as a valid
zero-bundle state. The reconciler deliberately restores the former.

## Shallow per-tree change maps [shallow-maps]

Alongside the per-group manifests, the scrubber maintains one **shallow map per tree**, at the
tree roots: `bundle/registry.json` and `changelog/registry.json` — not to be confused with the
per-group `bundle/{product}/registry.json` one level down. Each is a flat JSON object mapping
every folder (a product, or an `{org}/{repo}/{branch}` pool) to an opaque change token:

```json
{
  "elastic/elasticsearch/main": "9c01f2…",
  "elastic/kibana/main": "3f2ab8…"
}
```

The token is a digest over the folder's full sorted file/ETag listing — deliberately *not* the
last-touched object's ETag, which goes stale when an *older* object is deleted (the newest
object, and therefore the value, would not change). Consumers must treat it as opaque: compare,
never parse.

The maps exist for **cache opt-out only**: a consumer that caches a folder's content can GET one
small object and skip the folder entirely when its token is unchanged. They are not a discovery
mechanism — they list folders, not files. Consumer-side adoption is tracked in
[elastic/docs-builder#3801](https://github.com/elastic/docs-builder/pull/3801).

Like the group reconcile, the maps are `f(state)`: touched folders are re-listed and the map is
patched with optimistic concurrency (`ShallowRegistryReconciler`). An absent or unparseable map
is rebuilt from a full tree listing — which is also how the maps were seeded on first deploy.

## Producer details: the scrubber Lambda reconciler

The bundle manifest is produced exclusively by the scrubber Lambda's `BundleRegistryReconciler`
(`Elastic.Changelog/Reconciliation/`; the handler pipeline is `ScrubberProcessor`). S3 event
notifications are at-least-once and can arrive out of order, so the handler never acts on an
event's *type* — an event only means "this key may have changed":

1. **Object-level reconcile** — GET the key from the private bucket: present → scrub the
   *current* content and PUT to public; 404 → conditionally delete the public copy. After the
   write, a HEAD re-validates that the private object still matches the snapshot the write was
   derived from, redoing the reconcile if a concurrent invocation raced it.
2. **Group reconcile** (`bundle/{product}/` keys only — the pool tree has none) — list the
   group's public prefix (paginated), reuse entries whose recorded ETag still matches the
   listing, GET and recompute the rest (amends always recomputed), and write the manifest back.
3. **Shallow-map reconcile** — patch the touched tree's
   [folder→token map](#shallow-maps) from the same public listings.

Within an SQS batch this work is coalesced: one object reconcile per distinct key, one group
reconcile per distinct group, one shallow-map reconcile per touched tree.

Registry-key events split by tree. A **bundle** manifest is never copied or deleted — the event
only schedules the group reconcile, so client-authored JSON never reaches the tree consumers
enumerate. A **pool** manifest is mirrored verbatim (the
[legacy pass-through](#ownership-per-tree)). Any other `.json` key is skipped with a warning.

### Concurrency: optimistic, conditional writes

Concurrent reconciles of one group (parallel uploads, redeliveries) are serialized through
**S3 conditional writes**:

- On **update**: `If-Match: <etag-from-read>` — only succeeds if the manifest hasn't changed.
- On **create**: `If-None-Match: *` — only succeeds if the manifest still doesn't exist.
- On **group emptied**: conditional DELETE with `If-Match`, so a stale empty observation can't
  destroy a manifest a concurrent reconciler just rebuilt.

A `412 Precondition Failed` (or `409` conditional-write conflict) means another writer won the
race; the reconciler re-lists, rebuilds, and retries with jittered backoff (bounded). On
exhaustion the SQS message fails and is redelivered. If the rebuilt manifest equals what's
published (same `schema_version`, `producer`, `product`, and entry list), the write is skipped —
`generated_at` alone never causes churn.

### Consistency: convergence, not atomicity

S3 has no cross-key atomicity — the public YAML write and the registry write are separate
operations, and a reconcile can fail between them. The guarantee is therefore:

> The public registry **converges** to the exact public bucket state once the scrubber queue
> drains successfully. Any successfully processed event for a group repairs *all* accumulated
> drift in that group, not just the event's own key.

Consumers must tolerate the convergence window: the manifest may briefly reference a bundle
that is not yet (or no longer) on the public bucket — treat a listed-but-missing bundle as
non-fatal (skip + warn), not an error. A bundle that fails scrubbing (private references that
cannot be allowlisted) is never written to the public bucket; its message lands in the DLQ
(alerting and the triage/redrive runbook are tracked in
[elastic/docs-eng-team#692](https://github.com/elastic/docs-eng-team/pull/692)), and the
manifest — describing actual public state — never lists it.

### Out-of-band drift repair

Failures no longer surface in any CI log, and drift can also be introduced out-of-band (manual
S3 operations, lost events). There is deliberately **no operator CLI**: the planned
`changelog registry reconcile`/`verify` commands were dropped together with the pool-reconcile
model ([elastic/docs-builder#3741](https://github.com/elastic/docs-builder/pull/3741), closed
unmerged — see the 2026-08-10 update on elastic/docs-eng-team#688). Repair rides the normal
event path instead: any successfully processed event for a group rebuilds its manifest from the
live listing, so re-running the group's upload with `--skip-etag-check` (which re-PUTs every
discovered file even when its content hash matches) forces a full re-scrub and reconcile of
that group.

### Buckets and infrastructure

The uploader (GitHub Actions OIDC role) writes YAML to the **private** bucket
(`elastic-docs-v3-changelog-bundles-private`) only. The scrubber Lambda is the sole writer to
the **public** bucket (`elastic-docs-v3-changelog-bundles`, served via CloudFront + OAC), which
preserves the invariant that everything on the public surface has been vetted.

Infrastructure lives in `docs-infra` (`aws/elastic-web/us-east-1/elastic-docs-v3-changelog-bundles/`):

- Private-bucket S3 → SQS notifications on `s3:ObjectCreated:*` / `s3:ObjectRemoved:*` trigger
  the Lambda (batch size 10, 5 s batching window — multi-file uploads to one group tend to
  coalesce into a single reconcile).
- The scrubber role has `s3:GetObject` on private, `s3:GetObject`/`s3:PutObject`/`s3:DeleteObject`
  on public, and `s3:ListBucket` on the **public** bucket only (group and shallow-map reconciles
  list public state; the Lambda never lists the private bucket).
- Queue metrics (main and DLQ) are streamed to the docs-o11y Elastic project
  (`observability.tf`); the alert rules and the triage/redrive runbook are tracked in
  [elastic/docs-eng-team#692](https://github.com/elastic/docs-eng-team/pull/692).
- CloudFront caching is **disabled** (`Managed-CachingDisabled`), so a written manifest is
  visible on the CDN immediately.

## `changelog bundle` entry sourcing (org/repo/branch gate) [entry-sourcing]

The `changelog bundle` command aggregates individual changelog **entries**. It can read those
entries from the local folder or fetch the **authoring pool's** published entries from the CDN
(`changelog/{org}/{repo}/{branch}/registry.json` → `changelog/{org}/{repo}/{branch}/{file}`, via
`CdnChangelogEntryFetcher`). The pool manifest it enumerates is the
[legacy client-authored index](#ownership-per-tree); this enumeration is what keeps the
pass-through alive until PR-list-driven discovery (RFC elastic/docs-eng-team#698) replaces it.

Under the artifact-root layout, entries are org/repo/branch-scoped — not product-scoped — so CDN
entry sourcing keys off the resolvable authoring pool (repo with the same precedence as upload:
`--repo` > `bundle.repo` > git remote; owner from `--owner` > `bundle.owner`, default `elastic`;
branch from `--branch` > `bundle.branch`, default `main`), **not** off the bundle's target products.
This is what lets one repo (for example `kibana`) produce a bundle for a shared product (for example
`cloud-serverless`) while sourcing its own entries from `changelog/elastic/kibana/main/`, without that
product appearing in the repo's own `docset.yml`. The decision is made per run by
`ChangelogBundlingService`:

- **Local folder** when `bundle.use_local_changelogs: true`, when `--directory` is passed, or when
  the authoring repo cannot be resolved.
- **CDN** when the authoring repo resolves, local sourcing is not forced, and a CDN base is
  configured (`DOCS_BUILDER_CHANGELOG_CDN`, defaulting to the public distribution).

The same gate drives the `--plan` `needs_network` output, so a planning step and the actual bundle
run agree on whether the Docker bundle needs network access. The registry-fetch is fail-fast and an
entry still missing after its retry budget fails the bundle (an incomplete release would otherwise
ship silently). `CdnChangelogEntryFetcher` reuses a shared `HttpClient` in production and disposes an
owned client only when a test injects a handler, mirroring `CdnChangelogFetcher`.

## Consumer: `{changelog}` directive `cdn:` mode (implemented)

### Syntax

```markdown
:::{changelog}
:cdn: elasticsearch
:::
```

The directive accepts a `:cdn:` option naming the **product** to render (validated against
`[a-zA-Z0-9_-]+`). It is a *selector* over bundles that were prefetched at build startup, so the
product must be declared under `release_notes` in `docset.yml` (see
[Declaration and prefetch](#declaration-and-prefetch)). The product is optional: a valueless
`:cdn:` infers the product from the current repository (`BuildContext.Git.RepositoryName`),
mapped to its canonical product id via `products.yml` (for example the `elastic-otel-java` repo →
`edot-java` product). Multi-product repositories (for example `cloud`, which publishes
`cloud-hosted`, `cloud-serverless`, and `cloud-enterprise`) must name the product explicitly. When
the product cannot be inferred (git information unavailable) or is not declared under
`release_notes`, the directive emits an error.

The CDN base URL is environment configuration, not authored per page: it
defaults to the public changelog bundles distribution and is overridable via the
`DOCS_BUILDER_CHANGELOG_CDN` environment variable (absolute `http`/`https` URL) for
staging, local development, and testing.

When `:cdn:` is set, the local-folder argument is ignored (a warning is emitted if one is
also given) and the directive renders the prefetched CDN bundles instead.

### Declaration and prefetch [declaration-and-prefetch]

CDN-sourced products are declared once per docset under `release_notes` in `docset.yml`, mirroring
the `cross_links` mechanism:

```yaml
# docset.yml
release_notes:
  - product: elasticsearch
  - product: edot-java
```

Each entry is validated against `products.yml` (the id must exist and carry the `release-notes`
feature). At build startup — before any markdown is parsed — `ReleaseNotesFetcher` fetches the
registry and bundles for every declared product **concurrently**, stores the result in an immutable
`FetchedReleaseNotes`, and exposes it through `IReleaseNotesResolver`. The resolver is threaded into
the parser via `DocumentationSet`/`ParserContext`, so the `{changelog}` directive's `:cdn:` mode is
a pure in-memory lookup with no network I/O at parse time. Build paths that do not source release
notes use `NoopReleaseNotesResolver`.

### Fetch flow

1. `GET {cdnBase}/bundle/{product}/registry.json`.
2. Parse it; for each `bundles[].file`, `GET {cdnBase}/bundle/{product}/{file}`.
3. Feed the downloaded YAML into the existing `BundleLoader` → `MergeBundlesByTarget` →
   render pipeline. **Rendering is unchanged**; only the source of the bundle bytes differs.

Implemented by `CdnChangelogFetcher` (a stateless async fetch engine in
`Elastic.Documentation.Configuration`) and `BundleLoader.LoadBundlesFromContent`. Because public
bundles are already scrubbed and **resolved** (entries are inline/self-contained), the fetcher never
needs to download separate entry files; the existing private-repo link and description visibility
logic still applies via `assembler.yml`, exactly as for local bundles.

### Behavior and decisions

- **Async prefetch at startup.** Bundles are fetched once per declared product before parsing, via
  `HttpClient.GetAsync`, rather than synchronously inside the Markdig block parser. The directive
  then selects from the prefetched, immutable `FetchedReleaseNotes`.
- **Fail-fast registry, tolerant bundles.** A declared product whose registry cannot be fetched or
  parsed fails the build; an individual bundle that 404s or fails to parse is a warning and is
  skipped, per [Consistency: convergence, not atomicity](#consistency-convergence-not-atomicity).
- **Undeclared product.** A `:cdn:` directive naming a product not declared under `release_notes`
  is an error — its bundles were never prefetched — which keeps network sources auditable in one
  place.
- **Schema evolution.** A `schema_version` newer than the consumer understands produces a
  clear error rather than a silent mis-parse.
- **Filtering.** `:type:`, `:link-visibility:`, `:description-visibility:`, `:dropdowns:` and
  `hide-features` apply identically to CDN-sourced bundles.
- **Version selection.** `:version:` renders a single target and works in both modes (shared match
  on registry `target` or bundle file name, see `ChangelogVersionMatch`). In CDN mode it filters the
  prefetched bundles at render time. When the fetcher itself is asked for a single version, an amend
  sidecar is additionally downloaded when its parent bundle matches, so amends published without
  `products` (null registry `target`) still reach version-filtered consumers.
- **Security.** The base URL is trusted configuration; the product and registry-supplied bundle
  file names are validated to single path segments so neither can traverse outside
  `bundle/{product}/`.

### Follow-ups (not yet implemented) [implementation-notes]

- **Persistent / offline cache.** Each build prefetches declared products once into memory but does
  not persist a disk cache, so a cold build always reaches the CDN and an unreachable declared
  product fails the build. A follow-up should add an ETag-keyed on-disk cache under the
  docs-builder app-data directory (mirroring `CrossLinkFetcher`) with offline fallback.
- **`serve` mode staleness.** The prefetch runs per reload, but within a single `serve` process a
  product's CDN content is pinned until the next reload. Acceptable for now (serve targets local
  markdown authoring, not changelog bundles); revisit alongside the disk cache.
- **CDN latency.** CloudFront caching is disabled (`Managed-CachingDisabled`), so a written
  manifest is visible immediately; the only delay between an upload and its appearance in
  `registry.json` is the scrubber pipeline itself (SQS batching window + reconcile).
- **Caching key.** When the disk cache lands, use the CDN response ETag (not the registry
  `etag` field) for revalidation.

### Out of scope

- Cross-product aggregation in a single directive block (one product per block).
- Authenticated/private CDN access (the public bucket is anonymous-read by design).

## Related

- [Changelog directive](/syntax/changelog.md) — current (local-folder) behavior.
- [Publish changelogs](/data/release-notes/publish.md) — the upload workflow.
- [Link infrastructure](/documentation/distributed-builds.md) — the S3 + CloudFront pattern this reuses.
