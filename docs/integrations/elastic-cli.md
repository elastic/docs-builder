---
navigation_title: Elastic CLI
---

# Elastic CLI

The [Elastic CLI](https://github.com/elastic/cli) includes a `docs` sub-command for interacting with Elastic documentation from the terminal.

## Commands

### Search documentation

```bash
elastic docs search "<query>" --accept-experimental
```

Search across all published Elastic documentation by keyword or phrase.

### Ask questions

```bash
elastic docs ask "<question>" --accept-experimental
```

Ask a natural language question about Elastic documentation and receive an AI-generated answer. This is an experimental feature.

### Read a page

```bash
elastic docs read <path>
```

Read the content of a specific documentation page by its path.

:::{note}
The `search` and `ask` commands are experimental and require the `--accept-experimental` flag.
:::

## Installation

See the [Elastic CLI repository](https://github.com/elastic/cli) for installation instructions and additional documentation.
