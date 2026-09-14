## Description

Resolve the link allowlist identity of the deployed changelog scrubber.

The changelog scrubber Lambda embeds its link allowlist from `config/assembler.yml` at build time, so the allowlist the deployed scrubber actually runs with can differ from any local checkout. Links attributed to repositories that are not on the deployed allowlist are silently stripped on publication, which makes the deployed identity a required input for backfill planning and public verification.

The release pipeline attaches a `changelog-scrubber-allowlist.json` asset to the GitHub release **after** the scrubber deploy succeeded, so the presence of the asset attests that the release's allowlist was deployed. This command resolves the identity from that asset:

- Without `--tag`, the newest (non-draft) release carrying the asset wins — that is the most recent deploy that passed the gated pipeline.
- With `--tag`, the identity is read from that specific release and the command fails when the release does not carry the asset (it predates identity publication, or its scrubber deploy never completed).

When a local `assembler.yml` is available (`--assembler`, or `config/assembler.yml` in the current directory), its hash is compared against the deployed identity. A mismatch is reported as a **warning**, not an error: it means link decisions must be validated against the deployed allowlist, not the local checkout.

The command exits non-zero when no identity can be resolved. Backfill plans pin this identity, and a plan cannot be approved without it.

## Identity document

The resolved asset is a small JSON document:

```json
{
  "schema_version": 1,
  "artifact": "scrubber-allowlist-identity",
  "allowlist_sha256": "sha256:<64 hex characters>",
  "deployment_commit": "<full 40-character commit SHA>",
  "git_ref": "v5.7.0",
  "built_at": "2026-08-01T12:00:00Z"
}
```

`allowlist_sha256` is the SHA-256 of the raw `config/assembler.yml` bytes at the release tag — the same value `sha256sum config/assembler.yml` reports at that ref, and the same bytes the Lambda embeds as its allowlist source.

## Examples

```sh
# Resolve the identity of the most recent gated deploy
docs-builder changelog scrubber-allowlist

# Resolve the identity a specific release deployed
docs-builder changelog scrubber-allowlist --tag v5.7.0

# Compare an explicit local assembler.yml against the deployed allowlist
docs-builder changelog scrubber-allowlist --assembler ./config/assembler.yml
```
