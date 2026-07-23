## Description

Re-emits the private-bucket `s3:ObjectCreated` event for selected objects in a scope so the changelog scrubber Lambda re-processes them. This is the explicit recovery path when [`changelog registry verify-public`](/cli/changelog/registry/verify-public.md) shows a persistently missing public object — typically because a scrub event was lost or ended up in the dead-letter queue.

The re-emission is a **metadata-preserving S3 self-copy**: each selected object is copied onto its own key with `MetadataDirective: REPLACE`, re-supplying its original content type and user metadata. Content and ETag are unchanged; the copy produces the `ObjectCreated` notification the scrubber listens for, and the scrubber then re-scrubs and re-publishes the object to the public bucket itself.

Republishing never writes to the public bucket — the scrubber remains the sole public-side writer — and it only ever happens through this explicit command.

## Selection

Exactly one selection is required:

- `--files <name>[,<name>…]` — specific file names within the scope (for example `9.3.0.yaml`, or `registry.json` to re-trigger the manifest pass-through);
- `--all` — every object in the scope, including its `registry.json`.

## Examples

Re-emit one lost bundle scrub event:

```sh
docs-builder changelog registry republish \
  --s3-bucket-name elastic-docs-v3-changelog-bundles-private \
  --product elasticsearch \
  --files 9.3.0.yaml
```

Re-emit everything in an authoring pool:

```sh
docs-builder changelog registry republish \
  --s3-bucket-name elastic-docs-v3-changelog-bundles-private \
  --owner elastic --repo elasticsearch --branch main \
  --all
```

:::{note}
If the objects also exist locally, `changelog upload --skip-etag-check` achieves a similar re-trigger by re-uploading unchanged files. `republish` works purely from bucket state and needs no local checkout.
:::
