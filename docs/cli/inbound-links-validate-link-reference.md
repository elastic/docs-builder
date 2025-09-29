# inbound-links validate-link-reference

Validate a locally published links.json file against all published links.json files in the registry

## Usage

```
inbound-links validate-link-reference [options...] [-h|--help] [--version]
```

## Options

`--file` `<string?>`
:   Path to `links.json` defaults to '.artifacts/docs/html/links.json' (Default:   null)

`-p|--path <string?>`
:   Defaults to the `{pwd}` folder (Default:   null)