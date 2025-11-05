# diff validate

Gathers the local changes by inspecting the git log, stashed and unstashed changes. 

It currently validates the following:

* Ensures that renames and deletions are reflected in [redirects.yml](../../how-to/redirects.md).

## Usage

```
docs-builder diff validate [options...] [-h|--help] [--version]
```

## Options

`-p|--path <string>`
:   Defaults to the`{pwd}/docs` folder (optional)