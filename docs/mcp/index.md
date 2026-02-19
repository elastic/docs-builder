# MCP server

{{dbuild}} includes an [MCP (Model Context Protocol)](https://modelcontextprotocol.io/introduction) server that allows AI assistants and coding agents to interact with Elastic documentation directly.

The MCP server is deployed as a stateless HTTP service and exposes all tools through a single Streamable HTTP endpoint at `https://www.elastic.co/docs/_mcp/`.

## Installation

Add the {{dbuild}} MCP server to your editor or AI assistant using one of the options below.

:::::{tab-set}

::::{tab-item} Cursor

To install automatically, copy and paste the following link into your browser's address bar:

```text
cursor://anysphere.cursor-deeplink/mcp/install?name=elastic-docs&config=eyJ1cmwiOiJodHRwczovL3d3dy5lbGFzdGljLmNvL2RvY3MvX21jcC8ifQ==
```

Cursor will prompt you to install the `elastic-docs` MCP server.

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

To install automatically (requires VS Code 1.99+), copy and paste the following link into your browser's address bar:

```text
vscode:mcp/install?%7B%22name%22%3A%22elastic-docs%22%2C%22type%22%3A%22http%22%2C%22url%22%3A%22https%3A%2F%2Fwww.elastic.co%2Fdocs%2F_mcp%2F%22%7D
```

VS Code will prompt you to install the `elastic-docs` MCP server.

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

### Search tools

| Tool | Description |
|------|-------------|
| `SemanticSearch` | Searches all published Elastic documentation by meaning. Returns relevant documents with AI summaries, relevance scores, and navigation context. Supports filtering by product and navigation section. |
| `FindRelatedDocs` | Finds Elastic documentation pages related to a given topic. Useful for exploring what documentation exists around a subject and discovering related content. |

### Document tools

| Tool | Description |
|------|-------------|
| `GetDocumentByUrl` | Retrieves a specific Elastic documentation page by its URL. Accepts a full URL or a path. Returns title, AI summaries, headings, navigation context, and optionally the full body. |
| `AnalyzeDocumentStructure` | Analyzes the structure of an Elastic documentation page. Accepts a full URL or a path. Returns heading count, link count, parent pages, and AI enrichment status. |

### Coherence tools

| Tool | Description |
|------|-------------|
| `CheckCoherence` | Checks how coherently a topic is covered across all Elastic documentation by finding related documents and analyzing their coverage across products and sections. |
| `FindInconsistencies` | Finds potential inconsistencies across Elastic documentation pages covering the same topic. Identifies overlapping content within a product area. |

### Cross-link tools

| Tool | Description |
|------|-------------|
| `ResolveCrossLink` | Resolves an Elastic docs cross-link URI (e.g., `docs-content://get-started/intro.md`) to its published URL and returns available anchors. |
| `ListRepositories` | Lists all Elastic documentation source repositories in the cross-link index with their metadata. |
| `GetRepositoryLinks` | Gets all pages and anchors published by a specific Elastic documentation repository. |
| `FindCrossLinks` | Finds cross-links between Elastic documentation repositories. Can filter by source or target repository. |
| `ValidateCrossLinks` | Validates cross-links targeting an Elastic documentation repository and reports any broken links. |

### Content type tools

These tools help authors create and evaluate documentation using the [Elastic Docs content types](https://www.elastic.co/docs/contribute-docs/content-types).

| Tool | Description |
|------|-------------|
| `ListContentTypes` | Lists all Elastic documentation content types (overview, how-to, tutorial, troubleshooting, changelog) with descriptions and guidance on when to use each. |
| `GenerateTemplate` | Generates a ready-to-use Elastic documentation template for a specific content type. Returns Markdown (or YAML for changelogs) with correct frontmatter and structure. Optionally pre-fills title, description, and product. |
| `GetContentTypeGuidelines` | Returns detailed authoring and evaluation guidelines for a specific Elastic documentation content type, including required elements, recommended sections, best practices, and anti-patterns. |

## Testing with MCP Inspector

You can test the MCP server using the [MCP Inspector](https://github.com/modelcontextprotocol/inspector):

```bash
npx @modelcontextprotocol/inspector --url https://www.elastic.co/docs/_mcp/
```

This opens a web UI where you can browse all available tools and invoke them manually.
