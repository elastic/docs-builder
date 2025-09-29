# assembler clone

Clones all repositories

## Usage

```
assembler clone [options...] [-h|--help] [--version]
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