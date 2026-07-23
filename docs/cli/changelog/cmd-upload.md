## Description

Upload changelog entries or bundle artifacts to S3 or Elasticsearch. The command discovers `.yaml` and `.yml` files in a local directory and uploads only files whose content hash changed since the last run. Changelog entries are uploaded once under `changelog/{org}/{repo}/{branch}/{file}`, keyed by the authoring owner, repository, and branch; bundles are uploaded under `bundle/{product}/{file}`, product-scoped from the bundle YAML.

For historical backfill runs, pass `--backfill` to switch to [backfill mode](#backfill-mode): explicit file selection instead of directory discovery, create-only writes instead of overwrites, and strict registry failure semantics.

To create bundles first, use [](/cli/changelog/bundle.md).
For the end-to-end workflow, see [](/contribute/bundle-changelogs.md).

## Requirements

### S3 uploads

When `--target s3`, you must pass `--s3-bucket-name`. The bucket must already exist in the AWS region your credentials target, and your principal must be authorized to write the object keys described in [S3 bucket structure](#s3-bucket-structure).

The command uses the **AWS SDK for .NET** (`AmazonS3Client`), not the `aws` CLI. You do not need the AWS CLI installed. Running `aws configure` is optional — it can populate `~/.aws/credentials`, but the upload command never invokes the `aws` binary.

#### AWS credentials

The SDK resolves credentials through the standard credential chain. Any of these sources work:

- Environment variables — `AWS_ACCESS_KEY_ID`, `AWS_SECRET_ACCESS_KEY`, and optionally `AWS_SESSION_TOKEN`
- Shared config — `~/.aws/credentials` and `AWS_PROFILE`
- IAM instance or task role — when running on EC2, ECS, or Lambda
- OIDC-assumed role — typical in GitHub Actions CI

Missing or invalid credentials cause authentication errors when the command tries to read or write S3 objects.

#### AWS region

Set `AWS_REGION` or `AWS_DEFAULT_REGION` to the region where your bucket lives. If the region does not match the bucket, uploads fail with SDK errors.

#### IAM permissions

Your IAM policy must allow these S3 actions on the target bucket:

| Permission | Purpose |
| ---------- | ------- |
| `s3:PutObject` | Upload changelog and bundle YAML files and `registry.json` manifests |
| `s3:GetObject` | Read existing `registry.json` for merge and compare remote content |
| `s3:GetObject` (metadata) | Compare remote ETags to skip unchanged files |

`s3:ListBucket` is not required. The command uploads to known keys derived from local file names and product IDs — it does not enumerate the bucket.

You can scope object-level permissions to the key prefixes the command writes:

- `bundle/*` (bundle YAML and `bundle/{product}/registry.json`)
- `changelog/*` (entry YAML and `changelog/{org}/{repo}/{branch}/registry.json`)

#### Local development

Export credentials and region before running the command:

```sh
export AWS_ACCESS_KEY_ID=...
export AWS_SECRET_ACCESS_KEY=...
export AWS_REGION=us-east-1

docs-builder changelog upload \
  --artifact-type bundle \
  --target s3 \
  --s3-bucket-name my-changelog-bundles
```

#### CI

In Elastic's documentation pipeline, CI assumes an IAM role via GitHub Actions OIDC and uploads to a private S3 bucket. A scrubber Lambda then copies sanitized artifacts to the public CDN bucket. See [Changelog bundle registry and CDN delivery](/development/changelog-bundle-registry.md) for that architecture.

### Elasticsearch uploads

`--target elasticsearch` has no additional authentication setup today. The target is not yet implemented — the command logs a warning and exits successfully without uploading.

## Artifact types

Use `--artifact-type` to choose what to upload:

| Value | Uploads | Default directory |
| ----- | ------- | ----------------- |
| `bundle` | Consolidated bundle YAML files | `bundle.output_directory` from `changelog.yml`, or `docs/releases` |
| `changelog` | Individual changelog entry YAML files | `bundle.directory` from `changelog.yml`, or `docs/changelog` |

Keying differs by artifact type:

- **Changelog entries** are uploaded **once** under the authoring owner/repo/branch, regardless of how many products they list (or none). The owner is resolved from `--owner`, then `bundle.owner` in `changelog.yml`, then the git remote origin; the repo from `--repo`, then `bundle.repo`, then the git remote origin; the branch from `--branch`, then the current checkout's branch. The branch is stored verbatim, so a branch name containing `/` (for example `feature/foo`) becomes additional key segments.
- **Bundles** are uploaded once per product listed in the bundle's `products[].product` field (a bundle that declares multiple products is written under each product prefix).

## Upload targets

Use `--target` to choose the destination:

| Value | Status |
| ----- | ------ |
| `s3` | Supported. Requires `--s3-bucket-name`. |
| `elasticsearch` | Not yet implemented. The command logs a warning and exits successfully without uploading. |

## S3 bucket structure

For each discovered file, the command writes to:

```text
s3://{bucket}/changelog/{org}/{repo}/{branch}/{filename}   # --artifact-type changelog
s3://{bucket}/bundle/{product}/{filename}                  # --artifact-type bundle
```

Changelog entries are written once under the authoring org/repo/branch. A bundle that applies to multiple products is uploaded to multiple keys — one per product.

After a successful upload, the command refreshes the relevant `registry.json` manifest:

```text
s3://{bucket}/changelog/{org}/{repo}/{branch}/registry.json   # changelog uploads
s3://{bucket}/bundle/{product}/registry.json                  # bundle uploads
```

When several repositories publish bundles for the same shared product (for example `cloud-serverless`), use a `{repo}-{dateOrVersion}.yaml` bundle filename convention so they don't overwrite each other under `bundle/{product}/`.

In live mode the registry refresh is best-effort: upload failures block the run, but a stale manifest does not fail an otherwise successful upload. In [backfill mode](#backfill-mode) a registry refresh failure fails the operation.

:::{note}
Upload uses content-hash–based incremental transfer. Unchanged files are skipped. Re-running the same command is safe and idempotent.
If it's necessary to re-trigger downstream scrubbers without changing file content, pass `--skip-etag-check` to upload every discovered file even when its content hash matches the remote object.
:::

## Backfill mode

`--backfill` switches the command to backfill publishing semantics for historical bundles. Three things change relative to a live upload:

1. **Explicit selection, no directory discovery.** Only the files named via `--files` are uploaded. `--files` accepts comma-separated bundle YAML paths or a path to a newline-delimited path list file, and can be repeated; relative paths resolve against the bundle directory. A selected file that cannot be mapped to a destination (missing file, no `products`, invalid product name) is an error that aborts the run before any write — never a silent skip.
2. **Create-only writes.** An object is only ever created, never replaced. The write is a conditional PUT (`If-None-Match: *`), so even a concurrent writer that creates the key between inspection and write surfaces as a conflict instead of an overwrite. Per key:
   - Key absent → created.
   - Key exists with identical content → skipped (reported as such; re-runs stay idempotent).
   - Key exists with different content → conflict; the run fails and the object is untouched. Correcting a published bundle requires the explicit corrections workflow — there is intentionally no force flag.
3. **Strict registry semantics.** A registry manifest that cannot be reconciled (after the bounded optimistic-concurrency retries, or due to an S3 error) fails the operation with a non-zero exit code, because uploaded objects that are not enumerable by consumers leave the backfill incomplete. Conflicted objects are excluded from the registry refresh so the manifest never misrepresents what is actually published.

Backfill mode only supports `--artifact-type bundle` (the backfill publishes bundles only) and cannot be combined with `--skip-etag-check`, whose forced re-upload semantics are the opposite of create-only.

Live (non-backfill) uploads are unaffected: without `--backfill`, discovery, overwrite, and best-effort registry behavior are unchanged.

## Options

| Option | Purpose |
| ------ | ------- |
| `--skip-etag-check` | Upload every discovered file even when its content hash matches the remote object. Each upload emits `s3:ObjectCreated`, which re-triggers the scrubber Lambda on the private bucket. Default behavior (without this flag) skips unchanged files. Mutually exclusive with `--backfill`. |
| `--files` | Exact bundle YAML paths to upload (comma-separated), or a path to a newline-delimited path list file. Can be specified multiple times. Requires `--backfill`; nothing outside this selection is uploaded. |
| `--backfill` | Backfill publishing mode: explicit `--files` selection, create-only conditional writes, and registry refresh failures fail the operation. See [Backfill mode](#backfill-mode). |

## Configuration

Directory resolution order:

1. `--directory` — explicit override for this run
2. `changelog.yml` — `bundle.output_directory` (bundles) or `bundle.directory` (changelog entries)
3. Built-in default — `docs/releases` (bundles) or `docs/changelog` (changelog entries)

Use `--config` to point at a `changelog.yml` file other than `docs/changelog.yml`.

## Examples

### Upload bundle artifacts to S3

Upload every bundle YAML in the default output directory (`docs/releases`):

```sh
docs-builder changelog upload \
  --artifact-type bundle \
  --target s3 \
  --s3-bucket-name my-changelog-bundles
```

### Upload changelog entries to S3

Upload individual changelog YAML files from the default changelog directory (`docs/changelog`). Entries are written to `changelog/{org}/{repo}/{branch}/...`; pass `--owner`, `--repo`, and `--branch` when the authoring owner/repo can't be inferred from `bundle.owner`/`bundle.repo` or the git remote, or to override the current checkout's branch:

```sh
docs-builder changelog upload \
  --artifact-type changelog \
  --target s3 \
  --s3-bucket-name my-changelog-bundles \
  --owner elastic \
  --repo my-repo \
  --branch main
```

### Override the source directory

Upload bundles from a custom folder instead of reading the path from `changelog.yml`:

```sh
docs-builder changelog upload \
  --artifact-type bundle \
  --target s3 \
  --s3-bucket-name my-changelog-bundles \
  --directory ./docs/changelog/bundles
```

### Use a custom changelog configuration

Read `bundle.directory` and `bundle.output_directory` from a non-default config file:

```sh
docs-builder changelog upload \
  --artifact-type bundle \
  --target s3 \
  --s3-bucket-name my-changelog-bundles \
  --config ./config/changelog.yml
```

### Backfill exactly two historical bundles

Upload only the two named bundles, refusing to touch any existing object and failing if the registry cannot be reconciled:

```sh
docs-builder changelog upload \
  --artifact-type bundle \
  --target s3 \
  --s3-bucket-name my-changelog-bundles \
  --backfill \
  --files "elasticsearch-8.9.0.yaml,elasticsearch-8.9.1.yaml"
```

A path list file works too — one bundle path per line:

```sh
docs-builder changelog upload \
  --artifact-type bundle \
  --target s3 \
  --s3-bucket-name my-changelog-bundles \
  --backfill \
  --files ./backfill-plan.txt
```
