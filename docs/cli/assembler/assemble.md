# assemble

Do a full assembler clone, build and optional serving of the full documentation in one swoop

## Usage

```
docs-builder assemble [options...] [-h|--help] [--version]
```



## Usage examples

The following will clone the repository, build the documentation and serve it on port 4000 using the embedded configuration inside the `docs-builder` binary.

```bash
docs-builder assemble --serve
```

This single command is equivalent to the following commands:

```bash
docs-builder assembler clone
docs-builder assembler build
docs-builder assembler serve
```

### Using a local workspace for assembler builds

Where this command really shines is when you want to create a temporary workspace folder to validate:

* changes to [site wide configuration](../../configure/site/index.md)
* changes to one or more repositories and their effect on the assembler build.

To do that inside an empty folder, call:

```bash
docs-builder assembler config init --local
docs-builder assemble --serve
```

This will source the latest configuration from [The `config` folder on the `main` branch of `docs-builder`](https://github.com/elastic/docs-builder/tree/main/config)
and place them inside the `$(pwd)/config` folder.

Now when you call `docs-builder assemble` rather than using the embedded configuration, it will use local one that one you just created.
You can be explicit about the configuration source to use:

```bash
docs-builder assembler config init --local
docs-builder assemble --serve -c local
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