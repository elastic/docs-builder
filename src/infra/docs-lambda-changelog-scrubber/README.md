# Changelog Scrubber Lambda Function

SQS-triggered Lambda that reads changelog/bundle YAML from the private S3 bucket,
scrubs private repository references using `LinkAllowlistSanitizer`, writes
sanitized copies to the public S3 bucket, and is the **sole producer** of the public
`bundle/{product}/registry.json` and `changelog/{org}/{repo}/{branch}/registry.json`
manifests and the shallow per-tree change maps, reconciled from actual public bucket state
([elastic/docs-eng-team#688](https://github.com/elastic/docs-eng-team/issues/688)).
The handler logic lives in `Elastic.Changelog` (`Scrubbing/ScrubberProcessor`,
`Reconciliation/BundleRegistryReconciler`, `Reconciliation/ShallowRegistryReconciler`);
`Program.cs` is a thin AOT adapter.

The public repo allowlist is derived from `config/assembler.yml` (baked into the
Lambda image as an embedded resource at build time). Changes to `assembler.yml`
trigger a Lambda redeploy via CI.

The deployed allowlist's identity (SHA-256 of the embedded `assembler.yml`, plus the
build commit) is published as a `changelog-scrubber-allowlist.json` asset on the GitHub
release, attached only after a successful deploy. Resolve it with
`docs-builder changelog scrubber-allowlist` — backfill planning and verification pin
this identity so link decisions are checked against the deployed allowlist, not a
local checkout (docs-eng-team#671).

## Build

From a linux `x86_64` machine (or Docker):

```bash
docker build . -t changelog-scrubber:latest -f src/infra/docs-lambda-changelog-scrubber/lambda.DockerFile
```

Copy the published artifacts from the image:

```bash
docker cp $(docker create --name tc changelog-scrubber:latest):/app/.artifacts/publish ./.artifacts && docker rm tc
```

The `bootstrap` binary should be available under:

```
.artifacts/publish/docs-lambda-changelog-scrubber/release_linux-x64/bootstrap
```

## Event handling

S3 events are *triggers, not instructions* — the event type is ignored and current bucket
state decides (events are at-least-once and can arrive out of order). Work is coalesced per
SQS batch: one object reconcile per distinct key, one registry reconcile per distinct
group (`bundle/{product}/` or `changelog/{org}/{repo}/{branch}/`), one shallow-map reconcile
per touched tree.

- **`.yaml`/`.yml` keys**: object-level reconcile — GET the key from the private bucket;
  present → scrub the current content and PUT to public, absent → conditionally delete the
  public copy. A post-write HEAD re-validates the source and redoes the work if a concurrent
  invocation raced it. The key's group then gets its `registry.json` reconciled from the
  public listing, and the touched tree's shallow folder→token map is patched.
- **Registry keys** (`ChangelogKeys.IsRegistry`): never copied or deleted — the event only
  schedules the group reconcile, so client-authored JSON never reaches the tree consumers
  enumerate. Pool listings are listing-only (`target` is null); bundle listings record
  each file's `target`.
- **Other `.json` keys**: skipped with a warning; other extensions are skipped silently.

Registry and shallow-map writes use conditional PUT/DELETE (`If-Match` / `If-None-Match: *`)
with bounded retries; exhaustion fails the batch item for SQS redelivery. Per-invocation
metrics are emitted as CloudWatch EMF. See
[Changelog bundle registry](../../../docs/development/changelog-bundle-registry.md) for the
full reconcile rules and consistency model.

## Scrubbing logic

- **Bundle files** (`bundle/{product}/*.yaml`, detected by the `bundle/` key prefix): `LinkAllowlistSanitizer.ScrubBundleForPublic` scrubs `prs`/`issues` lists and text fields, dropping disallowed references (no sentinels in public output)
- **Changelog entries** (`changelog/{org}/{repo}/{branch}/*.yaml`): `LinkAllowlistSanitizer.TryApplyChangelogEntry` scrubs `prs`, `issues`, `description`, `impact`, `action`
- The allowlist is built once at cold start from the embedded `assembler.yml` via `BuildAllowReposFromAssembler`: every reference repository **not** marked `private: true` is allowed. `skip: true` is ignored here — it only means the repo publishes no docs, so public link-only repos (e.g. `elastic/roadmap`) stay linkable
