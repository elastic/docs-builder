# mcp

Starts an [MCP (Model Context Protocol)](../mcp/index.md) server over stdio, allowing AI assistants to interact with documentation tooling directly.

The MCP server communicates using JSON-RPC over stdin/stdout. All logging is directed to stderr to keep the protocol channel clean.

## Usage

```
{{dbuild}} mcp [-h|--help] [--version]
```

## Available tools

For a full list of available tools and IDE configuration instructions, see the [MCP server documentation](../mcp/index.md).

## Example

Test the MCP server using the [MCP Inspector](https://github.com/modelcontextprotocol/inspector):

```bash
{{dbuild}} mcp
```
