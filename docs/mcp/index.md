# MCP server

{{dbuild}} includes an [MCP (Model Context Protocol)](https://modelcontextprotocol.io/introduction) server that allows AI assistants to interact with the documentation tooling directly.

The MCP server is deployed as an HTTP service and exposes all tools through a single endpoint.

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
| `GenerateTemplate` | Generates a ready-to-use template for a specific content type (overview, how-to, tutorial, troubleshooting, or changelog). Optionally pre-fills title, description, and product. Templates are fetched from the [docs-content repository](https://github.com/elastic/docs-content) when online, with embedded fallbacks for offline use. |
| `GetContentTypeGuidelines` | Returns detailed authoring and evaluation guidelines for a content type, including required elements, best practices, and anti-patterns. |

### Diagnostic tools

| Tool | Description |
|------|-------------|
| `GetDiagnostics` | Returns {{dbuild}} version, runtime environment, and workspace diagnostics. |

### Search tools

| Tool | Description |
|------|-------------|
| `SemanticSearch` | Performs semantic search across all Elastic documentation. Returns relevant documents with summaries, scores, and navigation context. |
| `FindRelatedDocs` | Finds documents related to a given topic or document. Useful for discovering related content and building context. |

### Document tools

| Tool | Description |
|------|-------------|
| `GetDocumentByUrl` | Gets a specific documentation page by its URL. Returns full document content including AI summaries and metadata. |
| `AnalyzeDocumentStructure` | Analyzes the structure of a documentation page. Returns heading count, links, parents, and AI enrichment status. |

### Coherence tools

| Tool | Description |
|------|-------------|
| `CheckCoherence` | Checks documentation coherence for a given topic by finding all related documents and analyzing their coverage. |
| `FindInconsistencies` | Finds potential inconsistencies in documentation by comparing documents about the same topic. |

## Configuration

To use the {{dbuild}} MCP server, add it to your IDE's MCP configuration.

::::{tab-set}

:::{tab-item} Cursor
Create or edit `.cursor/mcp.json` in your workspace:

```json
{
  "mcpServers": {
    "elastic-docs": {
      "url": "https://docs-builder.elastic.dev/docs/_mcp"
    }
  }
}
```

Then restart Cursor or reload the window. The tools will be available in **Agent mode**.
:::

:::{tab-item} Visual Studio Code
Create or edit `.vscode/mcp.json` in your workspace:

```json
{
  "servers": {
    "elastic-docs": {
      "type": "http",
      "url": "https://docs-builder.elastic.dev/docs/_mcp"
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
npx @modelcontextprotocol/inspector --url https://docs-builder.elastic.dev/docs/_mcp
```

This opens a web UI where you can browse all available tools and invoke them manually.
