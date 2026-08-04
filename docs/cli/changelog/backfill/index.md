## Description

Backfill historical release-note bundles ([docs-eng-team#656](https://github.com/elastic/docs-eng-team/issues/656)).

The changelog/bundle pipeline only contains data produced since each repository adopted the live workflows. The backfill commands make the public data look as if the current system had been in use throughout the docs-builder era: they census the products with published release notes, plan exactly which resolved bundles to create, and publish them with create-only writes.

The pipeline is staged, and every stage exchanges versioned, content-addressed JSON documents (see `src/services/Elastic.Changelog/Backfill/README.md`):

1. `inventory` — the census: which products and release-note sources exist and what was decided about each.
2. Planning, materialization, apply, and verification stages follow as they are implemented.

No stage writes to S3 except the guarded apply stage; everything before it is read-only and reviewable.
