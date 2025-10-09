---
navigation_title: CLI (docs-builder)
---

# Command line interface

`docs-builder` is the binary used to invoke various commands. 
These commands can be roughly grouped into three main categories

- [Documentation Set commands](#documentation-set-commands)
- [Link commands](#link-commands)
- [Assembler commands](#assembler-commands)

### Global options 

The following options are available for all commands:

`--log-level <level>`
:   Change the log level one of ( `trace`, `debug`, `info`, `warn`, `error`, `critical`). Defaults to `info`

`--config-source` or `-c`
:   Explicitly set the configuration source one of `local`, `remote` or `embedded`. Defaults to `local` if available 
    other wise `embedded`

## Documentation Set Commands

Commands that operate over a single documentation set.

A `Documentation Set` is defined as a folder containing a [docset.yml](../configure/content-set/index.md) file.

These commands are typically what you interface with when you are working on the documentation of a single repository locally.

[See available CLI commands for documentation sets](docset/index.md) 

## Link Commands

Outbound links, those going from the documentation set to other sources, are validated as part of the build process.

Inbound links, those going from other sources to the documentation set, are validated using specialized commands.

[See available CLI commands for inbound links](links/index.md) 

## Assembler Commands

Assembler builds bring together all isolated documentation set builds and turn them into the overall documentation that gets published.

[See available CLI commands for assembler](assembler/index.md)
