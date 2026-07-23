## Description

Reconciles a scope's **private** `registry.json` from the objects actually stored in the private bucket: missing entries are added, stale entries removed, and object-divergent metadata (ETag, target) corrected. A corrupt manifest is rebuilt from scratch.

Repair is a separate, explicit operation — nothing runs it implicitly — and it is **idempotent**: a clean scope writes nothing, and running repair twice yields no further changes.

## Concurrency safety

The write uses the same optimistic-concurrency conditional PUT as the live upload path:

- **update**: `If-Match: <etag-from-read>` — only succeeds if the manifest hasn't changed since the repair read it;
- **create**: `If-None-Match: *` — only succeeds if the manifest still doesn't exist.

A `412 Precondition Failed` means a concurrent live upload refreshed the manifest; the repair then re-inspects — a fresh registry read **and** a fresh object listing — and retries (bounded attempts). The registry is always read before the objects are listed, so an object uploaded concurrently either appears in the re-listing or its registry refresh invalidates the precondition; either way it survives the repair.

## Safety rails

- A repair that would produce an **empty** manifest aborts unless `--allow-empty` is passed.
- A manifest with a `schema_version` newer than this docs-builder is never rewritten (that would silently downgrade it).
- `--dry-run` reports the full audit (what would be added, removed, and corrected) without writing.
- Every applied change is logged entry by entry with before/after values for audit.

Only the private registry is written. The public copy is scrubber-owned: the repaired manifest reaches the public bucket through the scrubber's verbatim pass-through, triggered by this write's own `ObjectCreated` event.

## Examples

Preview a repair:

```sh
docs-builder changelog registry repair \
  --s3-bucket-name elastic-docs-v3-changelog-bundles-private \
  --product elasticsearch \
  --dry-run
```

Repair an authoring pool:

```sh
docs-builder changelog registry repair \
  --s3-bucket-name elastic-docs-v3-changelog-bundles-private \
  --owner elastic --repo elasticsearch --branch main
```
