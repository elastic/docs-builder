Sources the configuration from [the `config` folder on the `main` branch of `docs-builder`](https://github.com/elastic/docs-builder/tree/main/config).

By default, the configuration is placed in a platform-specific application support folder, which is the primary location for CI environments:

* **macOS:** `~/Library/Application Support/docs-builder`
* **Linux:** `~/.config/docs-builder`
* **Windows:** `%APPDATA%\docs-builder`

Use the `--local` flag to save the configuration in the current working directory instead. This is the recommended approach when building a local workspace for testing.

See [using a local workspace for assembler builds](../cmd-assemble.md#using-a-local-workspace) for more information.
