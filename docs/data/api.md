---
navigation_title: REST API
---

# REST API

{{dbuild}} includes a REST API server (`Elastic.Documentation.Api`) that powers search, AI assistance, and content change feeds for the documentation site.

## Endpoints

| Endpoint | Description |
|----------|-------------|
| Search | Full-text and semantic search across all published documentation |
| Navigation search | Search scoped to navigation structure and page titles |
| Ask AI | Streaming endpoint for AI-powered question answering over documentation |
| Changes feed | Content change feed for tracking documentation updates |
| API reference | Search, read, and list the REST API operations that API Explorer publishes |

## API reference lookups

The API reference endpoints answer the same lookups as the `search_api_operations`, `get_api_operation`, and `list_api_operations` tools of the [MCP server](mcp/index.md). They serve HTTP clients such as the Elastic CLI's `elastic docs api` commands. All three are `GET` requests under `/docs/_api/v1/api-reference`. Their responses are camelCase JSON, the same shapes the MCP tools return.

`api` is an API key such as `elasticsearch` or `kibana`. `version` is `main` or a major such as `9`, and needs `api`. Without `version`, a lookup reads the API's current version.

| Request | Query parameters | Returns |
|---------|------------------|---------|
| `/search` | `q` (required): words, a route such as `GET /_cluster/health`, or a field path. A trailing dot, as in `retriever.`, lists child fields.<br>`api`, `version`<br>`include_deprecated` (default `false`)<br>`page` (default `1`)<br>`size` (default `10`, at most `50`) | `query`, `totalHits`, and `results`. Each result has its `operation`, a `score`, and the `matchedFields`. |
| `/operation` | Exactly one of `operation_id` or `endpoint`, such as `GET /my-index/_search`.<br>`api`, `version`<br>`include_body` (default `false`) | The `operation`, its `versions`, every documented field in `fields`, `bodyLength`, `otherMatches`, and `body` only when `include_body=true` |
| `/` | `api`, `version`<br>`group`: one group of operations | Without `api`: `apis`, with each API's key, landing page, and versions.<br>With `api`: its `groups` of operations. An API with more than 150 operations lists only group names and counts, with a `note`, until you pass `group`. |

A request the lookups can't answer returns `{ "errorType": "...", "message": "..." }`. The status is `404` when `errorType` ends in `_not_found`: `operation_not_found`, `api_not_found`, or `group_not_found`. Invalid arguments return `400` with `invalid_arguments`.

## Ask AI provider

Each deployment of the API server picks its Ask AI provider with the `DOCUMENTATION_ASK_AI_PROVIDER` setting. A request can't change it.

| Value | Provider |
|-------|----------|
| Unset or empty | LLM Gateway (the default) |
| `LlmGateway` | LLM Gateway |
| `AgentBuilder` | Agent Builder |

The value is case-insensitive. Any other value stops the Ask AI service with a configuration error. LLM Gateway needs `LLM_GATEWAY_FUNCTION_URL` and `LLM_GATEWAY_SERVICE_ACCOUNT`. Agent Builder needs `DOCUMENTATION_KIBANA_URL` and `DOCUMENTATION_KIBANA_APIKEY`. A deployment that uses Agent Builder, such as Codex, must set `DOCUMENTATION_ASK_AI_PROVIDER=AgentBuilder`.

:::{note}
A formal OpenAPI specification should be generated from the API server as a follow-up. This page will be updated with full endpoint details, request/response schemas, and authentication requirements once the spec is available.
:::
