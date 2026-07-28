## Description

Read-only drift diagnosis: for every planned group, compares the public `registry.json` against what a reconcile of the current public listing would write — the exact same listing spec and entry rules the scrubber Lambda uses — and reports each divergence:

| Kind | Meaning |
|---|---|
| `Missing` | A public object (or the manifest itself) the registry should describe but doesn't. |
| `Stale` | A manifest entry (or whole manifest) describing something no longer in the bucket, or manifest metadata a reconcile would rewrite. |
| `Corrupt` | The manifest exists but cannot be parsed. |
| `ObjectDivergent` | File present on both sides, but the recorded ETag or target disagrees with the object. |
| `UnsupportedSchema` | The manifest declares a newer `schema_version` than this tool understands. Reported distinctly and never rewritten. |

The command exits non-zero when any group diverges. Zero divergence across the plan is the completion gate after a [](/cli/changelog/registry/reconcile.md) run — and the standing way to answer "is the registry trustworthy right now?".

## Examples

```bash
docs-builder changelog registry verify \
  --s3-bucket-name elastic-docs-v3-changelog-bundles-private \
  --public-s3-bucket-name elastic-docs-v3-changelog-bundles
```
