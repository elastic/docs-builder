# serve

Continuously serve a documentation folder at http://localhost:3000.
File systems changes will be reflected without having to restart the server.

## Usage

```
serve [options...] [-h|--help] [--version]
```

## Options

`-p|--path <string?>`
:   Path to serve the documentation. Defaults to the`{pwd}/docs` folder (Default:   null)

`--port` `<int>`
:   Port to serve the documentation. (Default:   3000)