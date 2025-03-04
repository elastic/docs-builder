# Index Registry Update Lambda Function

From a linux `x86_64` machine you can use the followint to build a AOT binary that will run

on a vanilla `Amazon Linux 2023` without any dependencies.

```bash
docker build . -t publish-links-index:latest -f src/docs-lambda-index-publisher/lambda.DockerFile
```

Then you can copy the published artifacts from the image using:

```bash
docker cp (docker create --name tc publish-links-index:latest):/app/.artifacts/publish ./.artifacts && docker rm tc
```

The `bootstrap` binary should now be available under:

```
.artifacts/publish/docs-lambda-index-publisher/release_linux-x64/bootstrap
```

