---
navigation_title: Changelog bundle registry
---

# Changelog bundle registry and CDN delivery

This page describes how changelog **bundles** are published to a public, CDN-fronted
S3 bucket, how the per-product `registry-index.json` manifest is produced, and the
planned `cdn:` mode for the [`{changelog}` directive](/syntax/changelog.md) that will
consume bundles directly from the CDN instead of from a local folder.

:::{note}
The **producer** side (manifest generation + scrubber pass-through) is implemented.
The **consumer** side (`{changelog}` directive `cdn:` mode) is **planned** and is
documented here as a design, not as shipped behavior.
:::

## Motivation

Today the `{changelog}` directive only renders bundles that live in a folder inside the
docset (default `changelog/bundles/`). That requires every consuming repository to vendor
a copy of the bundle YAML it wants to render.

The link service ([building block](/building-blocks/link-service.md)) already demonstrates
the pattern we want: an S3 bucket fronted by CloudFront, publicly readable, with a small
JSON index at a well-known key. We apply the same approach to changelog bundles so a docset
can render another product's release notes by pointing the directive at the CDN — no vendored
copies, no cross-repo file syncing.

## Architecture

```
┌──────────────┐   changelog upload    ┌────────────────────┐   s3:ObjectCreated   ┌───────────────────┐
│  Client CI   │  --artifact-type      │  Private bundles   │ ───────────────────▶ │ Changelog scrubber │
│ (docs-actions)│  bundle  ───────────▶ │  S3 bucket         │                       │ Lambda             │
└──────────────┘                       │                    │                       └─────────┬─────────┘
       │                               │  {product}/bundles/*.yaml                            │ scrub + copy
       │ also refreshes                │  {product}/registry-index.json                       │ (pass-through for
       └──────────────────────────────▶                    │                                  │  registry-index.json)
                                       └────────────────────┘                                 ▼
                                                                                   ┌───────────────────┐
                                                  {changelog} directive (planned)  │  Public bundles    │
                                                  reads via CDN  ◀───────────────  │  S3 bucket + CDN   │
                                                                                   └───────────────────┘
```

1. **Producer** — `changelog upload --artifact-type bundle --target s3` (invoked by the
   docs-actions changelog upload workflow) uploads each bundle to
   `{product}/bundles/{file}` in the **private** bucket, then refreshes
   `{product}/registry-index.json` for every product the run touched.
2. **Scrubber Lambda** — triggered by `s3:ObjectCreated` on the private bucket, it scrubs
   private repository references out of bundle YAML and writes the sanitized copy to the
   **public** bucket. The `registry-index.json` object is copied through **verbatim**.
3. **Consumer (planned)** — the `{changelog}` directive in `cdn:` mode reads
   `{product}/registry-index.json` from the CDN, then fetches each listed bundle.

### Why a registry instead of an S3 listing

The public surface is a CDN (CloudFront) in front of S3. CloudFront does not expose bucket
listing, so the consumer cannot enumerate `{product}/bundles/`. The registry is a stable,
cacheable manifest at a predictable key that lists exactly which bundles exist for a product.

## `registry-index.json` format

Stored at `{product}/registry-index.json`. Serialized with `snake_case` keys.

```json
{
  "schema_version": 1,
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
| `schema_version` | Bumped when consumers must change their parser. |
| `product` | Product identifier; matches the first S3 key segment. |
| `generated_at` | UTC timestamp of the last regeneration. |
| `bundles[].file` | Bundle file name, resolved at `{product}/bundles/{file}`. |
| `bundles[].target` | Target version/date from the bundle's declaration of **this** product (may be null). |
| `bundles[].etag` | See the ETag caveat below. |

Bundles are sorted by `target` descending (newest first) with a deterministic tiebreak on
`file`, so the JSON is stable across reruns.

### ETag caveat

`bundles[].etag` is the ETag of the bundle object **as uploaded to the private bucket**
(pre-scrub). The scrubber rewrites any bundle that contains private references, so for
scrubbed bundles this value **will not match** the public (CDN) object's ETag.

Consumers **must not** use it for integrity checks or HTTP cache validation against the
public bucket — use the CDN response's own `ETag`/`Last-Modified` for caching. The field is
only a best-effort change hint (e.g. detecting whether a bundle changed between two manifest
reads of the same bucket).

## Producer details (implemented)

The refresh runs inside `ChangelogUploadService` after a successful **bundle** upload (it is
skipped for `--artifact-type changelog`). `RegistryIndexBuilder`:

- Groups the run's upload targets by product (from the `{product}/bundles/{file}` key).
- For each product, derives one `registry-index` entry per bundle (file name, that product's
  target, locally-computed S3 ETag).
- Reads the existing manifest from S3, merges by file name (re-uploads replace their entry;
  others are preserved), and writes the merged manifest back.

### Concurrency: optimistic, conditional writes

Two uploads that touch the same product (for example two repositories that both map to one
product, or parallel CI) could otherwise clobber each other's index via a naive
read-modify-write. The writer instead uses **S3 conditional PUT**:

- On **update**: `If-Match: <etag-from-read>` — only succeeds if the object hasn't changed.
- On **create**: `If-None-Match: *` — only succeeds if the object still doesn't exist.

A `412 Precondition Failed` means another writer won the race; the builder re-reads,
re-merges, and retries (bounded retries). This mirrors the link-index writer
(`AwsS3LinkIndexReaderWriter.SaveRegistry`). If the merge result already equals what's
published, the write is skipped so re-uploads stay idempotent.

The refresh is **best-effort**: any failure is logged and surfaced as a warning but never
fails the upload, because the bundle objects themselves are already in S3.

### Buckets and infrastructure

The registry is written to the **private** bucket
(`elastic-docs-v3-changelog-bundles-private`) — the same bucket and key space as the bundles
themselves — and reaches the **public** bucket (`elastic-docs-v3-changelog-bundles`, served
only via CloudFront + OAC) through the scrubber's verbatim pass-through. The uploader never
writes to the public bucket; the scrubber Lambda is the sole writer there, which preserves the
invariant that everything on the public surface has been vetted.

The required infrastructure already exists in `docs-infra`
(`aws/elastic-web/us-east-1/elastic-docs-v3-changelog-bundles/`) — **no infra change is needed
for the producer**:

- The private bucket's S3 → SQS notification fires on `s3:ObjectCreated:*` / `s3:ObjectRemoved:*`
  with **no suffix filter**, so registry `.json` events already reach the scrubber.
- The uploader (GitHub Actions OIDC) role already has `s3:GetObject`/`s3:PutObject`/`s3:ListBucket`
  on the private bucket, so the producer's conditional GET + PUT work. Conditional
  (`If-Match`/`If-None-Match`) writes need no extra permission.
- The scrubber role has `s3:GetObject` on private and `s3:PutObject`/`s3:DeleteObject` on public,
  covering the registry `CopyObject` pass-through and the `ObjectRemoved` delete.
- A CloudFront cache policy tuned for the manifest already exists (default TTL 1h, min 60s).

The scrubber only passes through keys accepted by `RegistryIndexKey.IsRegistryIndex` (a single
`{product}/registry-index.json` segment), so arbitrary JSON cannot reach the public surface.

**No new docs-actions workflow logic is required** for the producer either: the refresh is a
side-effect of the existing `changelog upload` step; docs-actions only needs a docs-builder
build that includes this feature.

:::{note}
The CDN cache policy comment refers to `registry.json` while the implementation uses
`registry-index.json`. This is only a comment (there is no path-based cache behavior), so it is
harmless, but the two should be aligned to avoid confusion.
:::

### Consistency notes the consumer must tolerate

- The manifest pass-through and the per-bundle scrub are independent S3 events, so the index
  may briefly reference a bundle that is not yet on the public bucket.
- A bundle that fails scrubbing (private references that cannot be allowlisted) is never
  written to the public bucket, even though the index may list it.

Consumers must therefore treat a missing bundle as non-fatal (skip + warn), not an error.

## Consumer: `{changelog}` directive `cdn:` mode (planned)

### Proposed syntax

```markdown
:::{changelog}
:cdn: elasticsearch
:::
```

The directive would accept a `:cdn:` option naming the **product** to fetch. The CDN base
URL is environment configuration (not authored per page), defaulting to the public changelog
bundles distribution and overridable for staging/local.

When `:cdn:` is set, the local-folder argument is ignored and the directive sources bundles
from the CDN instead.

### Fetch flow

1. `GET {cdnBase}/{product}/registry-index.json`.
2. Parse it; for each `bundles[].file`, `GET {cdnBase}/{product}/bundles/{file}`.
3. Feed the downloaded YAML into the existing `BundleLoader` → `MergeBundlesByTarget` →
   render pipeline. **Rendering is unchanged**; only the source of the bundle bytes differs.

Because public bundles are already scrubbed and resolved, the existing private-repo link and
description visibility logic still applies via `assembler.yml`, exactly as for local bundles.

### Open design decisions

- **Build-time network access.** Fetching at build time makes builds depend on the CDN.
  Options: (a) fetch during the build with an on-disk cache under the docs-builder app-data
  directory (mirrors `CrossLinkFetcher`/link-index); (b) a separate fetch step that
  materializes bundles into the working tree before the build. Caching + ETag revalidation
  against the CDN is the likely answer.
- **Local/offline development.** The directive must degrade gracefully when the CDN is
  unreachable (use cache; otherwise emit a clear, actionable diagnostic) so local builds and
  PR previews don't hard-fail on transient network issues.
- **Missing/partial bundles.** Skip-and-warn per the consistency notes above; never fail the
  whole page on a single missing bundle.
- **Schema evolution.** Honor `schema_version`; a newer major than the consumer understands
  should produce a clear error rather than a silent mis-parse.
- **Filtering.** `:type:`, `:link-visibility:`, `:description-visibility:`, `:dropdowns:` and
  `hide-features` apply identically to CDN-sourced bundles.
- **Caching key.** Use the CDN response ETag (not the registry `etag` field) for revalidation.
- **CDN staleness.** The distribution caches the manifest with a 1h default TTL (60s min), so a
  freshly uploaded bundle may not appear in the CDN-served `registry-index.json` for up to an
  hour. Acceptable for release notes, but if faster propagation is needed the producer (or a
  docs-actions step) would have to issue a CloudFront invalidation on registry write. Out of
  scope for the first iteration.

### Out of scope for the first iteration

- Cross-product aggregation in a single directive block (start with one product per block).
- Authenticated/private CDN access (the public bucket is anonymous-read by design).

## Related

- [Changelog directive](/syntax/changelog.md) — current (local-folder) behavior.
- [Publish changelogs](/contribute/publish-changelogs.md) — the upload workflow.
- [Link service](/building-blocks/link-service.md) — the S3 + CloudFront pattern this reuses.
