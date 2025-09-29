# assemble

Do a full assembler clone and assembler build in one swoop

## Usage

```
assemble [options...] [-h|--help] [--version]
```

## Options

`--strict` `<bool?>`
:   Treat warnings as errors and fail the build on warnings (Default:   null)

`--environment` `<string?>`
:   The environment to build (Default:   null)

`--fetch-latest` `<bool?>`
:   If true, fetch the latest commit of the branch instead of the link registry entry ref (Default:   null)

`--assume-cloned` `<bool?>`
:   If true, assume the repository folder already exists on disk assume it's cloned already, primarily used for testing (Default:   null)

`--metadata-only` `<bool?>`
:   Only emit documentation metadata to output, ignored if 'exporters' is also set (Default:   null)

`--show-hints` `<bool?>`
:   Show hints from all documentation sets during assembler build (Default:   null)

`--exporters` `<IReadOnlySet<Exporter>?>`
:   Set available exporters:   html, es, config, links, state, llm, redirect, metadata, none. Defaults to (html, config, links, state, redirect) or 'default'. (Default:   null)

`--serve`
:   Serve the documentation on port 4000 after successful build (Optional)