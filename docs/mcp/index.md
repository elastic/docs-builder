# MCP server

{{dbuild}} includes an [MCP (Model Context Protocol)](https://modelcontextprotocol.io/introduction) server that allows AI assistants to interact with the documentation tooling directly.

## Available tools

The MCP server currently exposes the following tools:

### Cross-link tools

| Tool | Description |
|------|-------------|
| `ResolveCrossLink` | Resolves a cross-link (like `docs-content://get-started/intro.md`) to its target URL and returns available anchors. |
| `ListRepositories` | Lists all repositories available in the cross-link index with their metadata. |
| `GetRepositoryLinks` | Gets all pages and their anchors published by a specific repository. |
| `FindCrossLinks` | Finds all cross-links between repositories. Can filter by source or target repository. |
| `ValidateCrossLinks` | Validates cross-links to a repository and reports any broken links. |

## Configuration

To use the {{dbuild}} MCP server, add it to your IDE's MCP configuration.

::::{tab-set}

:::{tab-item} Cursor
Create or edit `.cursor/mcp.json` in your workspace:

```json
{
  "mcpServers": {
    "docs-builder": {
      "command": "docs-builder",
      "args": ["mcp"]
    }
  }
}
```

Then restart Cursor or reload the window. The tools will be available in **Agent mode**.
:::

:::{tab-item} VS Code
Create or edit `.vscode/mcp.json` in your workspace:

```json
{
  "servers": {
    "docs-builder": {
      "type": "stdio",
      "command": "docs-builder",
      "args": ["mcp"]
    }
  }
}
```

Requires GitHub Copilot with MCP support enabled.
:::

::::

## Testing with MCP Inspector

You can test the MCP server using the [MCP Inspector](https://github.com/modelcontextprotocol/inspector):

```bash
npx @modelcontextprotocol/inspector docs-builder mcp
```

This opens a web UI where you can browse all available tools and invoke them manually.

