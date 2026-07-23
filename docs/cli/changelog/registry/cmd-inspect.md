## Description

Compares a scope's private `registry.json` manifest against the objects actually stored under the scope's key prefix in the private bucket, and classifies every divergence:

| Class | Meaning |
| ----- | ------- |
| `missing` | An object exists in the scope but the registry has no entry for it. |
| `stale` | The registry lists a file whose object no longer exists. |
| `corrupt` | The manifest itself is unparseable or contains invalid (unsafe or duplicate) entries. |
| `object-divergent` | A registry entry's recorded metadata (ETag or target) disagrees with the actual object. |

The command is strictly **read-only** — it never writes to any bucket. It exits non-zero when the scope diverged; use [`changelog registry repair`](/cli/changelog/registry/repair.md) to reconcile.

A manifest that declares a `schema_version` newer than this docs-builder understands is reported as `UnsupportedSchema`: its entries cannot be judged and repair refuses to touch it.

For bundle scopes, the expected `target` of each entry is derived by reading the bundle YAML from S3 (for legacy amend sidecars without `products`, the parent bundle's target is used, matching the upload-time registry builder). Changelog scopes enumerate files only and never record a target.

## State snapshot

`--out <path>` writes a machine-readable JSON snapshot of the scope: registry health, the actual objects, the registry's current entries, the entries the registry *should* contain, and every divergence. The snapshot is the trustworthy current-state input for backfill planning, which cannot rely on the additive registry for discovery or removals.

## Examples

Inspect a product bundle scope:

```sh
docs-builder changelog registry inspect \
  --s3-bucket-name elastic-docs-v3-changelog-bundles-private \
  --product elasticsearch
```

Inspect an authoring pool and write the snapshot:

```sh
docs-builder changelog registry inspect \
  --s3-bucket-name elastic-docs-v3-changelog-bundles-private \
  --owner elastic --repo elasticsearch --branch main \
  --out ./state-snapshot.json
```
