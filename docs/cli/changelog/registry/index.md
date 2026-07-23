The `changelog registry` commands inspect, repair, and verify the per-scope `registry.json` manifests that index published changelog artifacts in S3 — `bundle/{product}/registry.json` for bundle scopes and `changelog/{org}/{repo}/{branch}/registry.json` for authoring pools.

Registries merge additively under optimistic concurrency and are not authoritative for removals or discovery, so they can drift from the objects actually in the bucket. These commands detect that drift, reconcile the **private** registry from the actual private objects, and verify (without ever writing) that the scrubber-owned **public** bucket has converged.

## Scope selection

Every command addresses exactly one scope:

- `--product <id>` — a bundle scope (`bundle/{product}/`)
- `--owner <org> --repo <repo> --branch <branch>` — a changelog authoring pool (`changelog/{org}/{repo}/{branch}/`)

## Typical workflow

1. **Inspect** — `changelog registry inspect` reports every divergence between a scope's private registry and its actual objects, and can emit a machine-readable state snapshot.
2. **Repair** — `changelog registry repair` reconciles the private registry from the actual objects (explicit, never implicit; idempotent).
3. **Verify** — `changelog registry verify-public` waits with a bounded retry policy for the scrubber to propagate state to the public bucket and diagnoses divergence, strictly read-only.
4. **Republish** — `changelog registry republish` re-emits the private-bucket `ObjectCreated` event for selected objects when a scrub event was lost or dead-lettered.

See [Changelog bundle registry and CDN delivery](/development/changelog-bundle-registry.md) for the underlying architecture and reconciliation semantics.
