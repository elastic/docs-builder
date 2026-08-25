## Description

Backfills changelog entries, notes registries, and bundle YAML from published release-notes pages — either the live elastic.co site page or a pinned raw.githubusercontent.com ref for products whose published page is an empty `<changelog>` stub. All output is written to disk; nothing is published to S3. Use [`changelog upload`](/cli/changelog/upload.md) to publish the output.

For each product in scope the command:

1. Fetches the release-notes Markdown — from `elastic.co/docs/release-notes/{path}.md` (site source) or `raw.githubusercontent.com/{owner}/{repo}/{ref}/{path}` (repo source).
2. Parses each `## {version}` section into typed entries. Unrecognized `### …` subsections whose body is bullets become `Other`-typed entries; prose subsections are preserved in the bundle description.
3. Writes bundle YAML to `{output}/{product}/changelog/bundles/{version}.yaml`.
4. Prints a per-product report including entry counts and no-PR rates.

The primary use case is measuring how much published release-notes content cannot be traced back to a PR — determining how much historical content needs the PR-less "note" format from [docs-eng-team#789](https://github.com/elastic/docs-eng-team/issues/789).

## Scope table

The scope table is checked into the command itself (`BackfillScope.All` in the docs-builder repository). It covers 40 products — 39 via site source and one (`edot-java`) via a pinned GitHub ref, because the published elastic.co page for edot-java is an empty `<changelog>` stub.

| Field | Meaning |
| ----- | ------- |
| `ProductId` | Bundle product id (the `bundle/{product}/` S3 prefix, see `config/products.yml`). |
| `Path` | Site-relative path used to build the `.md` export URL. |
| `Owner` / `Repo` / `Ref` | Repo-source only: pinned commit for reproducible fetches. |
| `Cutoff` | Inclusive upper version bound; releases above it are skipped (live pipeline owns them). |

Runs cover the whole table unless narrowed with `--products`.

## Output layout

```
{output}/{product}/changelog/bundles/{version}.yaml
{output}/{product}/changelog/{pr}.yaml           # (planned: entry files)
{output}/{product}/changelog/note-{slug}.yaml    # (planned: PR-less entries)
{output}/{product}/changelog/notes-{target}.json # (planned: notes registry)
```

The `bundles/` leaf is a flat directory shaped for `changelog upload --artifact-type bundle --directory {product}/changelog/bundles`.

## Outcomes per product

| Outcome | Meaning |
| ------- | ------- |
| `ok` | Parsed and wrote bundles successfully. |
| `empty` | Fetch succeeded but found no `## ` version headings (e.g. an empty `<changelog>` stub). |
| `unavailable` | The page returned HTTP 404 — the URL may have moved. |
| `failed` | Non-404 HTTP error after retries. The run exits non-zero but other products still complete. |
| `skipped` | Version filtered by `--versions` or above the product's cutoff. |

## Examples

### Dry run (fetch and parse, write nothing)

```sh
docs-builder changelog backfill --dry-run
```

### Backfill specific products

```sh
docs-builder changelog backfill --products elastic-security,kibana
```

### Backfill to a custom output directory

```sh
docs-builder changelog backfill --output /tmp/release-notes-backfill
```

### Backfill specific versions

```sh
docs-builder changelog backfill \
  --products edot-java \
  --versions 1.9.0,1.10.0
```

### Upload bundles after backfill

```sh
docs-builder changelog backfill --products edot-java
docs-builder changelog upload \
  --artifact-type bundle \
  --directory .artifacts/release-notes-backfill/edot-java/changelog/bundles \
  --target s3 \
  --s3-bucket-name my-changelog-bundles
```
