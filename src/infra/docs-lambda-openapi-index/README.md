# OpenAPI Version Index Lambda Function

From a linux `x86_64` machine you can use the following to build a AOT binary that will run
on a vanilla `Amazon Linux 2023` without any dependencies.

```bash
docker build . -t openapi-version-index:latest -f src/infra/docs-lambda-openapi-index/lambda.DockerFile
```

Then you can copy the published artifacts from the image using:

```bash
docker cp (docker create --name tc openapi-version-index:latest):/app/.artifacts/publish ./.artifacts && docker rm tc
```

The `bootstrap` binary should now be available under:

```
.artifacts/publish/docs-lambda-openapi-index/release_linux-x64/bootstrap
```
