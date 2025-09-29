# assembler deploy plan

Creates a sync plan

## Usage

```
assembler deploy plan [options...] [-h|--help] [--version]
```

## Options

`--environment` `<string>`
:   The environment to build (Required)

`--s3-bucket-name` `<string>`
:   The S3 bucket name to deploy to (Required)

`--out` `<string>`
:   The file to write the plan to (Default: "")

`--delete-threshold` `<float?>`
:   The percentage of deletions allowed in the plan as float (Default:   null)