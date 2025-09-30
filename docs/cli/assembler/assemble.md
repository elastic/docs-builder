# assemble

Do a full assembler clone and assembler build in one swoop

## Usage

```
docs-builder assemble [options...] [-h|--help] [--version]
```

## Options

`--strict` `<bool?>`
:   Treat warnings as errors and fail the build on warnings (optional)

`--environment` `<string>`
:   The environment to build (optional) defaults to 'dev'

`--fetch-latest` `<bool?>`
:   If true, fetch the latest commit of the branch instead of the link registry entry ref (optional)

`--assume-cloned` `<bool?>`
:   If true, assume the repository folder already exists on disk assume it's cloned already, primarily used for testing (optional)

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

`--serve`
:   Serve the documentation on port 4000 after successful build (Optional)