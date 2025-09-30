---
navigation_title: "config init"
---

# assembler config init

Sources the configuration from [The `config` folder on the `main` branch of `docs-builder`](https://github.com/elastic/docs-builder/tree/main/config)

By default, the configuration is placed in a special application folder as its main usages is to be used by CI environments. 

* OSX: `~/Library/Application Support/docs-builder` [NSApplicationSupportDirectory](https://developer.apple.com/documentation/foundation/filemanager/searchpathdirectory/applicationsupportdirectory)
* Linux: `~/.config/docs-builder`
* {icon}`logo_windows` Windows: `%APPDATA%\docs-builder`

You can also use the `--local` option to save the configuration locally in the current working directory. This exposes a great way to assemble the full documentation locally in an empty directory.

See [using assemble to create local workspaces](assemble.md#using-a-local-workspace-for-assembler-builds) for more information.

## Usage

```
docs-builder assembler config init [options...] [-h|--help] [--version]
```

## Options

`--git-ref` `<string>`
:   The git reference of the config, defaults to 'main' (optional)

`--local`
:   Save the remote configuration locally in the pwd so later commands can pick it up as local (Optional)