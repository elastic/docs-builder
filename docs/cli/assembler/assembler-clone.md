---
navigation_title: "clone"
---

# assembler clone

Clones all repositories

## Usage

```
docs-builder assembler clone [options...] [-h|--help] [--version]
```

## Options

`--strict` `<bool?>`
:   Treat warnings as errors and fail the build on warnings (optional)

`--environment` `<string>`
:   The environment to build (optional)

`--fetch-latest` `<bool?>`
:   If true, fetch the latest commit of the branch instead of the link registry entry ref (optional)

`--assume-cloned` `<bool?>`
:   If true, assume the repository folder already exists on disk assume it's cloned already, primarily used for testing (optional)