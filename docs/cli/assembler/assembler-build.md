---
navigation_title: "build"
---

# assembler build

Builds all repositories

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

`--exporters` `<IReadOnlySet<Exporter>?>`
:   Set available exporters:   html, es, config, links, state, llm, redirect, metadata, none. Defaults to (html, config, links, state, redirect) or 'default'. (optional)