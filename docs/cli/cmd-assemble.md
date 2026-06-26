## Description

Do a full assembler clone, build, and optional serving of the full documentation in one step.

This command is shorthand for running the following in sequence:

```bash
docs-builder assembler clone
docs-builder assembler build
docs-builder assembler serve
```

## Using a local workspace

Where this command really shines is when you want to create a temporary workspace folder to validate:

* changes to [site wide configuration](../configure/site/index.md).
* changes to one or more repositories and their effect on the assembler build.

To do that inside an empty folder, call:

```bash
docs-builder assembler config init --local
docs-builder assemble --serve
```

This will source the latest configuration from [the `config` folder on the `main` branch of `docs-builder`](https://github.com/elastic/docs-builder/tree/main/config)
and place them inside the `$(pwd)/config` folder.

Now when you call `docs-builder assemble` rather than using the embedded configuration, it will use the local one you just created.
You can be explicit about the configuration source to use:

```bash
docs-builder assembler config init --local
docs-builder assemble --serve -c local
```
