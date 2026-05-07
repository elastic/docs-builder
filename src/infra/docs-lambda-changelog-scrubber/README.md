# Changelog Scrubber Lambda Function

SQS-triggered Lambda that reads changelog/bundle YAML from the private S3 bucket,
scrubs private repository references using `LinkAllowlistSanitizer`, and writes
sanitized copies to the public S3 bucket.

The public repo allowlist is derived from `config/assembler.yml` (baked into the
Lambda image as an embedded resource at build time). Changes to `assembler.yml`
trigger a Lambda redeploy via CI.

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

- **`s3:ObjectCreated:*`** on `.yaml`/`.yml` files: read from private bucket, scrub private references, write to public bucket
- **`s3:ObjectCreated:*`** on `.json` files: copy as-is (pass-through for `registry-index.json`)
- **`s3:ObjectRemoved:*`**: delete the same key from the public bucket
- Other keys are silently skipped

## Scrubbing logic

- **Bundle files** (`{product}/bundles/*.yaml`): `LinkAllowlistSanitizer.TryApplyBundle` scrubs `prs`/`issues` lists
- **Changelog entries** (`{product}/changelogs/*.yaml`): `LinkAllowlistSanitizer.TryApplyChangelogEntry` scrubs `prs`, `issues`, `description`, `impact`, `action`
- The allowlist is built once at cold start from the embedded `assembler.yml` via `BuildAllowReposFromAssembler`
