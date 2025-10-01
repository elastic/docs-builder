---
navigation_title: "deploy apply"
---

# assembler deploy apply

Applies an incremental synchronization plan created by [`docs-builder assembler deploy plan`](./assembler-deploy-plan.md).

## Usage

```
docs-builder assembler deploy apply [options...] [-h|--help] [--version]
```

## Options

`--environment` `<string>`
:   The environment to build (Required)

`--s3-bucket-name` `<string>`
:   The S3 bucket name to deploy to (Required)

`--plan-file` `<string>`
:   The file path to the plan file to apply (Required)