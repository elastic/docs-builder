## Description

Recreate individual changelog YAML files from a bundle.

Each entry is written through the same path as [`changelog add`](/cli/changelog/add.md) (PR-anchored entries) or [`changelog note`](/cli/changelog/note.md) (entries with no PR, or products that include versions). The output is a new changelog YAML file, not a byte-for-byte copy of the original files. Bundle `file.checksum` values are provenance of the sourced YAML at bundle time and will not match the unpacked files.

When you pass a full bundle file, its `.amend-*` files are merged first, the same way [`changelog render`](/cli/changelog/render.md) does. When you pass an amendment bundle file, only that file's `entries` are unpacked. `exclude-entries` are skipped; they are name and checksum stubs, not changelog files.

:::{important}
The bundle argument must be a local `.yaml` or `.yml` file that exists on disk.

If you download a bundle, get it from the private CDN instead of the public CDN.
Bundles in the public CDN have the private pull request and issue links removed.
:::

## Filenames

Filenames follow `changelog add` and `changelog note` rules, for example:

- Add: `{pr}.yaml` (or `{pr}-{pr}.yaml` when one entry cites multiple PRs)
- Note: `note-{slug}.yml`

If that name differs from the bundle provenance `file.name`, the command emits a warning and still writes the `add` or `note` name.

## Examples

```sh
docs-builder changelog unpack ./docs/releases/elasticsearch-serverless-2026-09-08.yaml \
  --output ./docs/changelog
```

```sh
docs-builder changelog unpack ./docs/releases/9.3.0.amend-1.yaml \
  --output ./docs/changelog \
  --concise
```
