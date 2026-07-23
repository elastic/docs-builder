## Description

Verifies that the scrubber-owned **public** bucket has converged to the state expected from the **private** bucket for one scope:

- the public `registry.json` must equal the private one (the scrubber passes manifests through verbatim);
- every private YAML object must have a public counterpart at the same key;
- no public object may outlive its private source.

Registry and YAML scrub events propagate independently, so transient divergence is normal. The command therefore re-checks under a **bounded retry policy** — up to `--max-attempts` comparisons, `--poll-interval-seconds` apart (defaults: 12 × 10 s, a two-minute budget) — and succeeds as soon as the state converges.

## Read-only by construction

The public bucket is a hard write boundary: this command never writes to it. Internally the comparison runs against a reader interface that exposes no write operations, so no code path can mutate either bucket.

If divergence persists after the retry budget — typically a lost or dead-lettered scrub event, or a bundle the scrubber refused to publish (unallowlisted private references) — the command reports each finding and exits non-zero. Recover with the explicit [`changelog registry republish`](/cli/changelog/registry/republish.md) operation on the private side.

## Divergence classes

| Finding | Meaning |
| ------- | ------- |
| `MissingPublicRegistry` | The private registry exists but its public pass-through copy does not. |
| `CorruptPublicRegistry` | The public registry cannot be parsed. |
| `RegistryMismatch` | Public registry entries differ from the private registry. |
| `MissingPublicObject` | A private object has no public counterpart. |
| `StalePublicObject` | A public object has no private counterpart. |

## Examples

```sh
docs-builder changelog registry verify-public \
  --s3-bucket-name elastic-docs-v3-changelog-bundles-private \
  --public-s3-bucket-name elastic-docs-v3-changelog-bundles \
  --product elasticsearch \
  --max-attempts 12 \
  --poll-interval-seconds 10
```
