# build

Builds a local documentation set folder.

Repeated invocations will do incremental builds of only the changed files unless:

* The base branch has changed 
* The state file in the output folder has been removed
* The `--force` option is specified.

## Usage

```
docs-builder [command] [options...] [-h|--help] [--version]
```

## Global Options

- `--log-level` `level`

## Options

`-p|--path <string>`
:   Defaults to the`{pwd}/docs` folder (optional)

`-o|--output <string>`
:   Defaults to `.artifacts/html` (optional)

`--path-prefix` `<string>`
:   Specifies the path prefix for urls (optional)

`--force` `<bool?>`
:   Force a full rebuild of the destination folder (optional)

`--strict` `<bool?>`
:   Treat warnings as errors and fail the build on warnings (optional)

`--allow-indexing` `<bool?>`
:   Allow indexing and following of HTML files (optional)

`--metadata-only` `<bool?>`
:   Only emit documentation metadata to output, ignored if 'exporters' is also set (optional)

`--exporters` `<exporters>`
:   Set available exporters:   

    * html
    * es, 
    * config, 
    * links, 
    * state, 
    * llm, 
    * redirect, 
    * metadata,
    * default
    * none. 

    Defaults to (html, llm, config, links, state, redirect) or 'default'. (optional)


`--canonical-base-url` `<string>`
:   The base URL for the canonical url tag (optional)