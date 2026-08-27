## Description

Create a changelog note file for an item that applies to one or more specific release versions and has no associated pull request.
Notes are used for known issues, security advisories, and other items that are not tied to a single PR.
For details and examples, go to [](/data/release-notes/create.md).

Note files are named `note-{slug}.yml` and are uploaded to the changelog pool like any other entry.
Each note declares `products[].versions` — the release versions it applies to — instead of deriving its release line from a branch.

## Options

: `--products`
  Products and versions in the format `"product versions lifecycle, ..."` where `versions` is a `|`-separated list of release versions (for example, `"elasticsearch 9.3.0|9.4.0 ga"`).
  Unlike `changelog add`, the middle slot is interpreted as a `|`-separated version list, not a single target.
  The valid product identifiers are listed in [products.yml](https://github.com/elastic/docs-builder/blob/main/config/products.yml).

: `--title`
  A short, user-facing headline for the note (max 80 characters). Required.

: `--type`
  The type of change. For valid values, see [ChangelogEntryType.cs](https://github.com/elastic/docs-builder/blob/main/src/Elastic.Documentation/ChangelogEntryType.cs). Required.

: `--description`
  Additional information about the note (max 600 characters). Optional.

: `--issues`
  URLs of related issues. Optional citation field; does not determine note addressability.

## Product and version format

The `--products` option uses the same positional format as `changelog add`, but the middle slot is a version list:

- `"elasticsearch 9.3.0 ga"` — one version
- `"elasticsearch 9.3.0|9.4.0|9.5.0 ga"` — multiple versions
- `"cloud-serverless 2025-08-05"` — date-based release, one version

A note that spans products can declare each product separately:

```sh
docs-builder changelog note \
  --title "Known issue with aggregations" \
  --type known-issue \
  --products "elasticsearch 9.3.0|9.4.0 ga" \
  --products "kibana 9.3.0|9.4.0 ga"
```

## Output

The command writes a `note-{slug}.yml` file to the configured output directory.
The file contains `products[].versions` instead of `products[].target`:

```yaml
title: Known issue with aggregations
type: known-issue
products:
  - product: elasticsearch
    versions: [9.3.0, 9.4.0]
    lifecycle: ga
```

## Lifecycle after creation

Notes are uploaded to `changelog/{org}/{repo}/{branch}/note-*.yml` in the private S3 bucket and go through the scrubber exactly like entries.
A Lambda-maintained index at `changelog/{org}/{repo}/notes-{version}.json` lists every note that applies to a given version.

If the release bundle for that version has already shipped when a note is uploaded, the scrubber Lambda automatically generates an amend sidecar (`{bundle}.amend-notes.yaml`) so the note reaches CDN consumers without a manual rerun.

## Configuration checks

The same configuration-file checks that apply to `changelog add` apply here:
valid `products`, `lifecycles`, and `type` values are validated against `docs/changelog.yml` when it exists.

Specifying a version target in `--products` for `changelog add` is an error; use `changelog note` instead.
Conversely, `--versions` has no meaning for `changelog add` — it is note-specific.
