## Description

Build the backfill census: an inventory document covering every release-notes product.

`config/products.yml` cannot say which products have release-note surfaces on its own, because the `release-notes` feature defaults to enabled. This command enumerates every product that participates in release notes, merges in a hand-maintained census seed mapping products to their sources, and writes the versioned inventory document that backfill planning consumes.

Products the seed does not cover stay visible as `source-unresolved` entries and produce a warning — "we looked and decided no" must always be distinguishable from "we never looked", and an unresolved scope can never silently produce empty bundles. Products can be deliberately deferred in the seed's `unmapped` list, each with a reason, which records the deferral without a warning.

The command is read-only apart from the local output file: it reads configuration, no S3 access, no remote writes.

### Defaults applied by the census

- Sources whose products are all stack-versioned and whose scheme is `semver` get the epic's default cutoff of `9.0.0` when the seed does not specify one.
- Unresolved products get a target scheme derived from their versioning system (`serverless`/project versioning → `date`, `ech` → `monthly`, everything else → `semver`), always paired with an unresolved note so a guess never reads as a confirmed fact.
- Attributed repositories are checked against the link allowlist in the local `assembler.yml`. Planning re-validates against the **deployed** scrubber allowlist identity before any upload; the census status is advisory.

## Seed format

```yaml
sources:
  - repository: elastic/docs-content    # where the release-note content lives
    git_ref: main
    docset: docs-content                # optional
    paths:
      - release-notes/elasticsearch
    products: [elasticsearch]           # products.yml ids
    target_scheme: semver               # semver | date | monthly
    cutoff:                             # optional; stack semver defaults to 9.0.0
      kind: version                     # version | date
      value: 9.0.0
      notes: optional free text
    substitutions: {}                   # docset variable expansions
    link_mappings: {}                   # source link -> canonical destination
    attributed_repositories:            # repos entries attribute changes to
      - elastic/elasticsearch
    default_repository: elastic/elasticsearch
    bundle_filename_convention: "{repo}-{target}.yaml"
    adoption: not-adopted               # not-adopted | partially-adopted | fully-adopted
    classification: published-history-found
    unresolved: []                      # open questions for a human
unmapped:
  - product: kibana
    reason: Deferred to the stack family pass.
```

Valid classifications: `published-history-found`, `native-artifacts-found`, `hybrid-page`, `declared-no-history`, `outside-cutoff`, `already-live`. `source-unresolved` is deliberately not seedable — it is the census's own conclusion for products nobody mapped, never something an operator writes by hand.

## Examples

```sh
# Census with a seed; writes the inventory document
docs-builder changelog backfill inventory \
  --sources config/backfill/inventory-sources.yml \
  --output .artifacts/backfill-inventory.json

# Without a seed: every release-notes product is reported source-unresolved,
# useful to see the full census surface before mapping begins
docs-builder changelog backfill inventory --output .artifacts/backfill-inventory.json
```
