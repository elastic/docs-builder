# assembler build

Builds all repositories

## Usage

```
assembler build [options...] [-h|--help] [--version]
```

## Options

`--strict` `<bool?>`
:   Treat warnings as errors and fail the build on warnings (Default:   null)

`--environment` `<string?>`
:   The environment to build (Default:   null)

`--metadata-only` `<bool?>`
:   Only emit documentation metadata to output, ignored if 'exporters' is also set (Default:   null)

`--show-hints` `<bool?>`
:   Show hints from all documentation sets during assembler build (Default:   null)

`--exporters` `<IReadOnlySet<Exporter>?>`
:   Set available exporters:   html, es, config, links, state, llm, redirect, metadata, none. Defaults to (html, config, links, state, redirect) or 'default'. (Default:   null)