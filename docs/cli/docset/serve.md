# serve

Continuously serve a documentation folder at http://localhost:3000.

When running `docs-builder serve`, the documentation is not built in full. 
Each page will be build on the fly continuously when requested in the browser. 

The `serve` command is also `live reload` enabled so that file systems changes will be reflected without having to restart the server.
This includes changes to the documentation files, the navigation, or the configuration files.

## Usage

```
docs-builder serve [options...] [-h|--help] [--version]
```

## Options

`-p|--path <string>`
:   Path to serve the documentation. Defaults to the`{pwd}/docs` folder (optional)

`--port` `<int>`
:   Port to serve the documentation. (Default:   3000)