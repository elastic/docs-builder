## Description

:::{warning}
This command is **temporary**. It exists solely to migrate release notes that were published before the changelog pipeline existed into the S3 bundle store, and it will be deleted once the migration rollout ([docs-eng-team#683](https://github.com/elastic/docs-eng-team/issues/683)) completes. Do not build workflows on top of it.
:::

One-off migration of already-published release notes into the S3 bundle store. For each product in scope, the command:

1. Fetches the release-notes Markdown that backs the published pages — from `raw.githubusercontent.com` at the pinned commit recorded in the scope table, not by scraping live site HTML.
2. Parses each `## {version}` section (typed `### …` subsections become entries; prose is preserved as the bundle description) and maps it to the **existing** bundle YAML shape that [](/cli/changelog/upload.md) publishes. No new schema is introduced.
3. Uploads each release to `bundle/{product}/{version}.yaml` with **create-only** semantics (`If-None-Match: *`): keys that already exist are skipped and never overwritten, so the migration can never clobber bundles produced by the live pipeline.
4. Prints a per-key run report (created / skipped / failed, with the reason and object ETag) suitable for pasting into the tracking issue.

By default the command migrates **every product in the checked-in scope table**; use `--products` to narrow a run for tests and pilots.

## Migration scope

The scope table is checked into the command itself (`MigrateFromWebScope.All` in the docs-builder repository) rather than into a config file — it is temporary tooling state, added per rollout wave and deleted with the command. Each entry maps a product id (the `bundle/{product}/` S3 prefix, see `config/products.yml`) to the source of its published release notes and a version cutoff:

| Field | Meaning |
| ----- | ------- |
| `Owner` / `Repo` | GitHub repository whose docs back the published release notes. |
| `Path` | Repo-relative path of the release-notes Markdown page. |
| `Ref` | Pinned commit SHA at which the Markdown is fetched (reproducible runs). |
| `Cutoff` | Inclusive upper version bound; releases above it belong to the live pipeline. |

The page→product mapping is deliberately explicit: bundle product ids appear in no published metadata (page frontmatter carries the site taxonomy, not bundle ids), so deriving it automatically is not possible. Adding a product to the migration is a small PR against the table.

Releases above a product's cutoff are always skipped — they are owned by the live changelog pipeline. Use `--versions` to narrow a run to specific versions below the cutoff.

## Requirements

Uploads use the same AWS SDK credential chain, region, and IAM permissions as [](/cli/changelog/upload.md). No credentials are needed for `--dry-run` without `--s3-bucket-name`.

## Run report

The report lists one line per key with its outcome:

| Outcome | Meaning |
| ------- | ------- |
| `created` | The key did not exist and was written (conditional PUT succeeded). |
| `would-create` | Dry run only: the key would be written. |
| `skipped` | The key already exists (identical or different content — never overwritten), was created concurrently by another writer, is beyond the cutoff, or is not in the `--versions` selection. |
| `failed` | The write failed; the reason is included and the command exits non-zero. |

The command writes YAML bundle objects only — never a `registry.json`. The scrubber Lambda owns the public `bundle/{product}/registry.json` manifests and the shallow per-tree maps, reconciling them from the S3 events these creates emit ([#3738](https://github.com/elastic/docs-builder/pull/3738), [#3760](https://github.com/elastic/docs-builder/pull/3760)).

## Examples

### Dry run without credentials

Parse, map, and report what would be created — no S3 access at all:

```sh
docs-builder changelog migrate-from-web --dry-run
```

### Dry run against the real bucket

Also checks which keys already exist, so the report distinguishes `would-create` from `skipped`:

```sh
docs-builder changelog migrate-from-web \
  --dry-run \
  --s3-bucket-name my-changelog-bundles
```

### Perform the migration

```sh
docs-builder changelog migrate-from-web \
  --s3-bucket-name my-changelog-bundles
```

Re-running the same command is safe: every existing key is reported as `skipped` and the run is a no-op.

### Migrate a single product (pilots and tests)

```sh
docs-builder changelog migrate-from-web \
  --products edot-java \
  --s3-bucket-name my-changelog-bundles
```

### Migrate specific versions only

```sh
docs-builder changelog migrate-from-web \
  --products edot-java \
  --s3-bucket-name my-changelog-bundles \
  --versions 1.9.0,1.10.0
```
