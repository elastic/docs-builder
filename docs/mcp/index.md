# MCP server

{{dbuild}} includes an [MCP (Model Context Protocol)](https://modelcontextprotocol.io/introduction) server that allows AI assistants to interact with the documentation tooling directly.

Two deployment modes are available: a **local stdio server** for IDE integration, and a **remote HTTP server** with the full tool surface.

## Available tools

### Local and remote

These tools are available in both local and remote modes.

| Tool | Description |
|------|-------------|
| `ResolveCrossLink` | Resolves a cross-link (like `docs-content://get-started/intro.md`) to its target URL and returns available anchors. |
| `ListRepositories` | Lists all repositories available in the cross-link index with their metadata. |
| `GetRepositoryLinks` | Gets all pages and their anchors published by a specific repository. |
| `FindCrossLinks` | Finds all cross-links between repositories. Can filter by source or target repository. |
| `ValidateCrossLinks` | Validates cross-links to a repository and reports any broken links. |
| `ListContentTypes` | Lists all Elastic Docs content types with descriptions and guidance on when to use each. |
| `GenerateTemplate` | Generates a ready-to-use template for a specific content type (overview, how-to, tutorial, troubleshooting, or changelog). Optionally pre-fills title, description, and product. Templates are fetched from the [docs-content repository](https://github.com/elastic/docs-content) when online, with embedded fallbacks for offline use. |
| `GetContentTypeGuidelines` | Returns detailed authoring and evaluation guidelines for a content type, including required elements, best practices, and anti-patterns. |
| `GetDiagnostics` | Returns docs-builder version, runtime environment, and workspace diagnostics. |

### Remote only

These tools require the Elasticsearch-backed search index and are only available on the remote HTTP server.

| Tool | Description |
|------|-------------|
| `SemanticSearch` | Performs semantic search across all Elastic documentation. Returns relevant documents with summaries, scores, and navigation context. |
| `FindRelatedDocs` | Finds documents related to a given topic or document. Useful for discovering related content and building context. |
| `GetDocumentByUrl` | Gets a specific documentation page by its URL. Returns full document content including AI summaries and metadata. |
| `AnalyzeDocumentStructure` | Analyzes the structure of a documentation page. Returns heading count, links, parents, and AI enrichment status. |
| `CheckCoherence` | Checks documentation coherence for a given topic by finding all related documents and analyzing their coverage. |
| `FindInconsistencies` | Finds potential inconsistencies in documentation by comparing documents about the same topic. |

## Configuration

::::{tab-set}

:::{tab-item} Local (stdio)
The local MCP server is started using the [`{{dbuild}} mcp`](../cli/mcp.md) command over stdio. It provides cross-link, content type, and diagnostic tools without requiring a remote connection.

Create or edit your IDE's MCP configuration:

**Cursor** (`.cursor/mcp.json`):

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

**VS Code** (`.vscode/mcp.json`):

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

Test using [MCP Inspector](https://github.com/modelcontextprotocol/inspector):

```bash
npx @modelcontextprotocol/inspector docs-builder mcp
```
:::

:::{tab-item} Remote (HTTP)
The remote MCP server is deployed as an HTTP service and includes the full tool surface: cross-links, content types, diagnostics, search, document analysis, and coherence checks.

Create or edit your IDE's MCP configuration:

**Cursor** (`.cursor/mcp.json`):

```json
{
  "mcpServers": {
    "docs-builder": {
      "url": "https://docs-builder.elastic.dev/docs/_mcp"
    }
  }
}
```

**VS Code** (`.vscode/mcp.json`):

```json
{
  "servers": {
    "docs-builder": {
      "type": "http",
      "url": "https://docs-builder.elastic.dev/docs/_mcp"
    }
  }
}
```

Test using [MCP Inspector](https://github.com/modelcontextprotocol/inspector):

```bash
npx @modelcontextprotocol/inspector --url https://docs-builder.elastic.dev/docs/_mcp
```
:::

::::
