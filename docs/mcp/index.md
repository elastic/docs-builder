# MCP server

{{dbuild}} includes an [MCP (Model Context Protocol)](https://modelcontextprotocol.io/introduction) server that allows AI assistants and coding agents to interact with Elastic documentation directly.

The MCP server is deployed as a stateless HTTP service and exposes all tools through a single Streamable HTTP endpoint at `https://www.elastic.co/docs/_mcp/`.

## Installation

Add the {{dbuild}} MCP server to your editor or AI assistant using one of the options below.

:::::{tab-set}

::::{tab-item} Cursor

One-click installation:

:::{button}
[Install in Cursor](https://ela.st/elastic-mcp-cursor)
:::

Or configure manually. Create or edit `.cursor/mcp.json` in your workspace:

```json
{
  "mcpServers": {
    "elastic-docs": {
      "url": "https://www.elastic.co/docs/_mcp/"
    }
  }
}
```

Then restart Cursor or reload the window. The tools will be available in **Agent mode**.
::::

::::{tab-item} Claude Code

Run the following command to add the server to your current project:

```bash
claude mcp add --transport http elastic-docs https://www.elastic.co/docs/_mcp/
```

To make it available across all your projects, add the `--scope user` flag:

```bash
claude mcp add --scope user --transport http elastic-docs https://www.elastic.co/docs/_mcp/
```

Or add it manually to your project's `.mcp.json` file:

```json
{
  "mcpServers": {
    "elastic-docs": {
      "type": "http",
      "url": "https://www.elastic.co/docs/_mcp/"
    }
  }
}
```
::::

::::{tab-item} Visual Studio Code

One-click installation (requires VS Code 1.99+):

:::{button}
[Install in VS Code](https://ela.st/elastic-mcp-vscode)
:::

Or configure manually. Create or edit `.vscode/mcp.json` in your workspace:

```json
{
  "servers": {
    "elastic-docs": {
      "type": "http",
      "url": "https://www.elastic.co/docs/_mcp/"
    }
  }
}
```

Requires GitHub Copilot with agent mode enabled.
::::

::::{tab-item} IntelliJ IDEA

Configure the server through the IDE settings:

1. Open **Settings** → **Tools** → **AI Assistant** → **Model Context Protocol**.
2. Click **Add** and select **HTTP** as the connection type.
3. Enter `https://www.elastic.co/docs/_mcp/` as the server URL, and `elastic-docs` as the name.
4. Click **OK** and apply the settings.

Or configure it directly via JSON by adding the following to your MCP settings file:

```json
{
  "mcpServers": {
    "elastic-docs": {
      "url": "https://www.elastic.co/docs/_mcp/"
    }
  }
}
```

Requires JetBrains AI Assistant plugin version 2025.2 or later.
::::

:::::

## Available tools

### Cross-link tools

| Tool | Description |
|------|-------------|
| `ResolveCrossLink` | Resolves a cross-link (like `docs-content://get-started/intro.md`) to its target URL and returns available anchors. |
| `ListRepositories` | Lists all repositories available in the cross-link index with their metadata. |
| `GetRepositoryLinks` | Gets all pages and their anchors published by a specific repository. |
| `FindCrossLinks` | Finds all cross-links between repositories. Can filter by source or target repository. |
| `ValidateCrossLinks` | Validates cross-links to a repository and reports any broken links. |

### Content type tools

These tools help authors create and evaluate documentation using the [Elastic Docs content types](https://www.elastic.co/docs/contribute-docs/content-types).

| Tool | Description |
|------|-------------|
| `ListContentTypes` | Lists all Elastic Docs content types with descriptions and guidance on when to use each. |
| `GenerateTemplate` | Generates a ready-to-use template for a specific content type (overview, how-to, tutorial, troubleshooting, or changelog). Optionally pre-fills title, description, and product. |
| `GetContentTypeGuidelines` | Returns detailed authoring and evaluation guidelines for a content type, including required elements, best practices, and anti-patterns. |

### Search tools

| Tool | Description |
|------|-------------|
| `SemanticSearch` | Performs semantic search across all Elastic documentation. Returns relevant documents with summaries, scores, and navigation context. Supports filtering by product and navigation section. |
| `FindRelatedDocs` | Finds documents related to a given topic or document. Useful for discovering related content and building context. |

### Document tools

| Tool | Description |
|------|-------------|
| `GetDocumentByUrl` | Gets a specific documentation page by its URL. Accepts a full URL or a path. Returns full document content including AI summaries and metadata. |
| `AnalyzeDocumentStructure` | Analyzes the structure of a documentation page. Accepts a full URL or a path. Returns heading count, links, parents, and AI enrichment status. |

### Coherence tools

| Tool | Description |
|------|-------------|
| `CheckCoherence` | Checks documentation coherence for a given topic by finding all related documents and analyzing their coverage. |
| `FindInconsistencies` | Finds potential inconsistencies in documentation by comparing documents about the same topic. |

## Testing with MCP Inspector

You can test the MCP server using the [MCP Inspector](https://github.com/modelcontextprotocol/inspector):

```bash
npx @modelcontextprotocol/inspector --url https://www.elastic.co/docs/_mcp/
```

This opens a web UI where you can browse all available tools and invoke them manually.
