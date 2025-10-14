---
navigation_title: "build"
---

# assembler build

:::note
This command requires that you've previously ran `docs-builder assembler clone` to clone the documentation sets.
If you clone using a certain `--environment` you must also use that same `--environment` when building.
:::

Builds all the documentation sets and assembles them into an assembled complete documentation site that's ready to be deployed.

It uses [the site configuration files](../../configure/site/index.md) to direct how the documentation sets should be assembled.

## Usage

```
docs-builder assembler build [options...] [-h|--help] [--version]
```

## Options

`--strict` `<bool?>`
:   Treat warnings as errors and fail the build on warnings (optional)

`--environment` `<string>`
:   The environment to build (optional)

`--metadata-only` `<bool?>`
:   Only emit documentation metadata to output, ignored if 'exporters' is also set (optional)

`--show-hints` `<bool?>`
:   Show hints from all documentation sets during assembler build (optional)

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

